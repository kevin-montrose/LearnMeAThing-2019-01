using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Managers
{
    /// <summary>
    /// Interface for anything that might hold onto an entity outside
    ///   of the EntityManager.
    ///   
    /// Used to implement entity compacting.
    /// </summary>
    interface IHoldsEntity
    {
        int EntitiesCount { get; }
        Entity GetEntity(int ix);
        void SetEntity(int ix, Entity e);
    }

    interface IIdIssuer
    {
        int GetNextId();
    }
    
    /// <summary>
    /// Manager for entity:
    ///   - creation
    ///   - release
    ///   - enumeration
    ///   - attaching components
    ///   - removing components
    ///   
    /// Goes out of it's way to allocate basically nothing after it's created.
    /// </summary>
    sealed partial class EntityManager
    {
        public bool IsFull => NextAvailableEntity == Entities.Length;

        public int FragmentationRatio => NumLiveEntities == 0 ? 0 : (NextAvailableEntity - 1) / NumLiveEntities;

        internal int NextAvailableEntity;

        internal int NumLiveEntities;

        internal int NumLiveComponents =>
            AccelerationPool.Allocated +
            AnimationPool.Allocated +
            AssociatedEntityPool.Allocated +
            BushPool.Allocated +
            CollisionPool.Allocated +
            DoorPool.Allocated +
            InputPool.Allocated +
            PitPool.Allocated + 
            PlayerStatePool.Allocated +
            PositionPool.Allocated +
            StairsPool.Allocated +
            SwordKnightStatePool.Allocated + 
            SwordPool.Allocated +
            VelocityPool.Allocated;

        // thing providing ids for newly "allocated" entities
        private readonly IIdIssuer IdProvider;

        // entities actively in use
        private readonly Entity[] Entities;
        // flags for those entities
        private readonly FlagComponents[] FlagComponents;

        // scratch space for when we do an entity compaction
        private readonly int[] CompactionScratch;

        // components allocated to entities
        private readonly IntrusiveLinkedList<AccelerationComponent> AccelerationComponents;
        private readonly IntrusiveLinkedList<AnimationComponent> AnimationComponents;
        private readonly IntrusiveLinkedList<AssociatedEntityComponent> AssociatedEntityComponents;
        private readonly IntrusiveLinkedList<BushComponent> BushComponents;
        private readonly IntrusiveLinkedList<CollisionListener> CollisionComponents;
        private readonly IntrusiveLinkedList<DoorComponent> DoorComponents;
        private readonly IntrusiveLinkedList<InputsComponent> InputComponents;
        private readonly IntrusiveLinkedList<PitComponent> PitComponents;
        private readonly IntrusiveLinkedList<PlayerStateComponent> PlayerStateComponents;
        private readonly IntrusiveLinkedList<PositionComponent> PositionComponents;
        private readonly IntrusiveLinkedList<StairsComponent> StairsComponents;
        private readonly IntrusiveLinkedList<SwordKnightStateComponent> SwordKnightStateComponents;
        private readonly IntrusiveLinkedList<SwordComponent> SwordComponents;
        private readonly IntrusiveLinkedList<VelocityComponent> VelocityComponents;

        // object pools for entities
        private readonly ComponentObjectPool<AccelerationComponent> AccelerationPool;
        private readonly ComponentObjectPool<AnimationComponent> AnimationPool;
        private readonly ComponentObjectPool<AssociatedEntityComponent> AssociatedEntityPool;
        private readonly ComponentObjectPool<BushComponent> BushPool;
        private readonly ComponentObjectPool<CollisionListener> CollisionPool;
        private readonly ComponentObjectPool<DoorComponent> DoorPool;
        private readonly ComponentObjectPool<InputsComponent> InputPool;
        private readonly ComponentObjectPool<PitComponent> PitPool;
        private readonly ComponentObjectPool<PlayerStateComponent> PlayerStatePool;
        private readonly ComponentObjectPool<PositionComponent> PositionPool;
        private readonly ComponentObjectPool<StairsComponent> StairsPool;
        private readonly ComponentObjectPool<SwordKnightStateComponent> SwordKnightStatePool;
        private readonly ComponentObjectPool<SwordComponent> SwordPool;
        private readonly ComponentObjectPool<VelocityComponent> VelocityPool;

        /// <summary>
        /// Number of time a fallable method has been invoked.
        /// </summary>
        //[Obsolete("Only provided for testing, do not use in real code.")]
        internal int FallibleCallCount { get; private set; }
        
        /// <summary>
        /// After this many failible calls, start failing.
        /// </summary>
        //[Obsolete("Only provided for testing, do not use in real code.")]
        internal int? FailAfterCalls { get; set; }

        /// <summary>
        /// If set, every method that can fail will fail
        /// </summary>
        //[Obsolete("Only provided for testing, do not use in real code.")]
        internal bool ShouldAlwaysFail => FailAfterCalls.HasValue && FallibleCallCount > FailAfterCalls.Value;

        /// <summary>
        /// Create a manager for tracking entities.
        /// 
        /// Takes in the maximum number of entities that can be tracked at one time.
        /// Takes in the maximum number of components that can be assigned to any one entity.
        /// </summary>
        public EntityManager(IIdIssuer idProvider, int capacity)
        {
            IdProvider = idProvider;

            // we need one more slot, because 0 is a magic number for us
            var backingCapacity = capacity + 1;
            
            NextAvailableEntity = 1;
            NumLiveEntities = 0;
            Entities = new Entity[backingCapacity];
            CompactionScratch = new int[backingCapacity];

            FlagComponents = new FlagComponents[backingCapacity];

            AccelerationComponents = new IntrusiveLinkedList<AccelerationComponent>(backingCapacity);
            AnimationComponents = new IntrusiveLinkedList<AnimationComponent>(backingCapacity);
            AssociatedEntityComponents = new IntrusiveLinkedList<AssociatedEntityComponent>(backingCapacity);
            BushComponents = new IntrusiveLinkedList<BushComponent>(backingCapacity);
            DoorComponents = new IntrusiveLinkedList<DoorComponent>(backingCapacity);
            CollisionComponents = new IntrusiveLinkedList<CollisionListener>(backingCapacity);
            InputComponents = new IntrusiveLinkedList<InputsComponent>(backingCapacity);
            PitComponents = new IntrusiveLinkedList<PitComponent>(backingCapacity);
            PlayerStateComponents = new IntrusiveLinkedList<PlayerStateComponent>(backingCapacity);
            PositionComponents = new IntrusiveLinkedList<PositionComponent>(backingCapacity);
            StairsComponents = new IntrusiveLinkedList<StairsComponent>(backingCapacity);
            SwordKnightStateComponents = new IntrusiveLinkedList<SwordKnightStateComponent>(backingCapacity);
            SwordComponents = new IntrusiveLinkedList<SwordComponent>(backingCapacity);
            VelocityComponents = new IntrusiveLinkedList<VelocityComponent>(backingCapacity);

            AccelerationPool = new ComponentObjectPool<AccelerationComponent>(backingCapacity);
            AnimationPool = new ComponentObjectPool<AnimationComponent>(backingCapacity);
            AssociatedEntityPool = new ComponentObjectPool<AssociatedEntityComponent>(backingCapacity);
            BushPool = new ComponentObjectPool<BushComponent>(backingCapacity);
            DoorPool = new ComponentObjectPool<DoorComponent>(backingCapacity);
            CollisionPool = new ComponentObjectPool<CollisionListener>(backingCapacity);
            InputPool = new ComponentObjectPool<InputsComponent>(backingCapacity);
            PitPool = new ComponentObjectPool<PitComponent>(backingCapacity);
            PlayerStatePool = new ComponentObjectPool<PlayerStateComponent>(backingCapacity);
            PositionPool = new ComponentObjectPool<PositionComponent>(backingCapacity);
            StairsPool = new ComponentObjectPool<StairsComponent>(backingCapacity);
            SwordKnightStatePool = new ComponentObjectPool<SwordKnightStateComponent>(backingCapacity);
            SwordPool = new ComponentObjectPool<SwordComponent>(backingCapacity);
            VelocityPool = new ComponentObjectPool<VelocityComponent>(backingCapacity);
        }

        /// <summary>
        /// Remaps entities and components such that there's no gaps in the 
        ///   internal entity tables.
        ///   
        /// In theory this will make everything a little better from a caching perspective,
        ///   and frees up more space for creating entities.
        /// </summary>
        public void Compact(IHoldsEntity[] holders)
        {
            // figure out how we want to compact things
            var nextIx = 1;                                 // we should never map anything to index 0, it's special
            for(var i = 1; i < NextAvailableEntity; i++)    // skip Entities[0], because that's always unallocated
            {
                var e = Entities[i];
                if (e.Id == 0) continue;

                // e is in use, we should pull it _back_ to whatever the next unallocated 
                //   index is
                CompactionScratch[i] = nextIx;
                nextIx++;
            }
            
            // now CompactScratch[someCurrentEntityId] = postCompactionId
            
            // compact flag components
            for(var oldIndex = 1; oldIndex < NextAvailableEntity; oldIndex++)
            {
                var newIndex = CompactionScratch[oldIndex];
                if (newIndex == 0) continue;   // slot was dead, continue

                var flags = FlagComponents[oldIndex];
                FlagComponents[newIndex] = flags;
            }
            Array.Clear(FlagComponents, nextIx, NextAvailableEntity - nextIx);    // clear everything after the end
            
            // compact all the various compontent trackers
            AccelerationComponents.Compact(CompactionScratch);
            AnimationComponents.Compact(CompactionScratch);
            AssociatedEntityComponents.Compact(CompactionScratch);
            BushComponents.Compact(CompactionScratch);
            DoorComponents.Compact(CompactionScratch);
            CollisionComponents.Compact(CompactionScratch);
            InputComponents.Compact(CompactionScratch);
            PitComponents.Compact(CompactionScratch);
            PlayerStateComponents.Compact(CompactionScratch);
            PositionComponents.Compact(CompactionScratch);
            StairsComponents.Compact(CompactionScratch);
            SwordKnightStateComponents.Compact(CompactionScratch);
            SwordComponents.Compact(CompactionScratch);
            VelocityComponents.Compact(CompactionScratch);

            // now anything else that might hold an entity needs to be updated
            for(var i = 0; i < holders.Length; i++)
            {
                var holder = holders[i];
                var holding = holder.EntitiesCount;
                for (var j = 0; j < holding; j++)
                {
                    var oldE = holder.GetEntity(j);
                    var newE = new Entity(CompactionScratch[oldE.Id]);
                    holder.SetEntity(j, newE);
                }
            }

            // now update the associated entity components, since they
            //   hold entities
            foreach(var e in EntitiesWithAssociatedEntity())
            {
                var holder = e.Component;
                var holding = holder.EntitiesCount;
                for (var j = 0; j < holding; j++)
                {
                    var oldE = holder.GetEntity(j);
                    var newE = new Entity(CompactionScratch[oldE.Id]);
                    holder.SetEntity(j, newE);
                }
            }

            // now actually update the entity map
            for (var i = 1; i < nextIx; i++)
            {
                Entities[i] = new Entity(i);
            }
            Array.Clear(Entities, nextIx, NextAvailableEntity - nextIx);        // clear everything after the end

            // clear the scratch space, so it's ready for next time
            Array.Clear(CompactionScratch, 0, NextAvailableEntity);

            // pull the next available entry back to where it should be, now that
            //   we've squeezed a lot of space out
            NextAvailableEntity = nextIx;
        }

        /// <summary>
        /// Allocate a new entity, returning a Result that indicates whether it could do so.
        /// </summary>
        public Result<Entity> NewEntity()
        {
            if(TryTestRecordAndFail<Entity>(out var earlyRes)) return earlyRes;

            if (NextAvailableEntity == Entities.Length) return Result.FailFor<Entity>();

            var ent = Entities[NextAvailableEntity] = new Entity(NextAvailableEntity);

            NextAvailableEntity++;
            NumLiveEntities++;
            return Result.From(ent);
        }

        /// <summary>
        /// Free an entity, returning all of it's resources to be used future entities to be allocated.
        /// </summary>
        public void ReleaseEntity(Entity e)
        {
            var eId = e.Id;

            if (eId <= 0) throw new InvalidOperationException($"Cannot release {typeof(Entity)} with Id={e.Id}, must be positive");
            if (eId >= Entities.Length) throw new InvalidOperationException($"Cannot release {typeof(Entity)} with Id={e.Id}, out of range");
            if(Entities[eId].Id == 0) throw new InvalidOperationException($"Cannot release {typeof(Entity)} with Id={e.Id}, entity in slot already freed");
            if (Entities[eId].Id != eId) throw new InvalidOperationException($"Cannot release {typeof(Entity)} with Id={e.Id}, entity in slot appears corrupted");

            Entities[eId] = default;
            FlagComponents[eId] = default;

            var aC = AccelerationComponents.Remove(eId);
            if (aC != null) AccelerationPool.Return(aC);
            var anC = AnimationComponents.Remove(eId);
            if (anC != null) AnimationPool.Return(anC);
            var asC = AssociatedEntityComponents.Remove(eId);
            if (asC != null) AssociatedEntityPool.Return(asC);
            var bC = BushComponents.Remove(eId);
            if (bC != null) BushPool.Return(bC);
            var cC=  CollisionComponents.Remove(eId);
            if (cC != null) CollisionPool.Return(cC);
            var dC = DoorComponents.Remove(eId);
            if (dC != null) DoorPool.Return(dC);
            var iC = InputComponents.Remove(eId);
            if (iC != null) InputPool.Return(iC);
            var piC = PitComponents.Remove(eId);
            if (piC != null) PitPool.Return(piC);
            var psC = PlayerStateComponents.Remove(eId);
            if (psC != null) PlayerStatePool.Return(psC);
            var pC = PositionComponents.Remove(eId);
            if (pC != null) PositionPool.Return(pC);
            var sC = StairsComponents.Remove(eId);
            if (sC != null) StairsPool.Return(sC);
            var sksC = SwordKnightStateComponents.Remove(eId);
            if (sksC != null) SwordKnightStatePool.Return(sksC);
            var swC = SwordComponents.Remove(eId);
            if (swC != null) SwordPool.Return(swC);
            var vC = VelocityComponents.Remove(eId);
            if (vC != null) VelocityPool.Return(vC);

            NumLiveEntities--;
        }

        /// <summary>
        /// Returns the flag components for a given entity
        /// </summary>
        public Result<FlagComponents> GetFlagComponentsForEntity(Entity e)
        {
            if(TryTestRecordAndFail<FlagComponents>(out var earlyRes)) return earlyRes;

            var eId = e.Id;
            if (Entities[eId].Id == 0) return Result.FailFor<FlagComponents>();

            var flags = FlagComponents[eId];
            return Result.From(flags);
        }

        /// <summary>
        /// Add the given component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, FlagComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            var eId = toEntity.Id;
            if(Entities[eId].Id == 0) return Result.Fail;

            FlagComponents[eId].SetFlag(component);
            return Result.Succeed;
        }

        /// <summary>
        /// "Create" an acceleration component with the initial values.
        /// </summary>
        public Result<AccelerationComponent> CreateAcceleration(int initialX, int initialY, int ticks)
        {
            if(TryTestRecordAndFail<AccelerationComponent>(out var earlyRes)) return earlyRes;

            var res = AccelerationPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(initialX, initialY, ticks);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" an animation component with the initial values.
        /// </summary>
        public Result<AnimationComponent> CreateAnimation(IAnimationManager manager, AnimationNames name, int startFrame)
        {
            if(TryTestRecordAndFail<AnimationComponent>(out var earlyRes)) return earlyRes;

            var res = AnimationPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(manager, name, startFrame);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" an associated entity component with the initial values.
        /// </summary>
        public Result<AssociatedEntityComponent> CreateAssociatedEntity(Entity e1, Entity? e2 = null, Entity? e3 = null, Entity? e4 = null, Entity? e5 = null)
        {
            if (TryTestRecordAndFail<AssociatedEntityComponent>(out var earlyRes)) return earlyRes;

            var res = AssociatedEntityPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(e1, e2, e3, e4, e5);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a bush component with the initial values.
        /// </summary>
        public Result<BushComponent> CreateBush()
        {
            if(TryTestRecordAndFail<BushComponent>(out var earlyRes)) return earlyRes;

            var res = BushPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize();

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a collision component with the initial values.
        /// </summary>
        public Result<CollisionListener> CreateCollision(AssetNames hitMap, int desiredSpeed, CollisionHandler onCollide, PushedAwayHandler onPush)
        {
            if(TryTestRecordAndFail<CollisionListener>(out var earlyRes)) return earlyRes;

            var res = CollisionPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(hitMap, desiredSpeed, onCollide, onPush);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a door component with the initial values.
        /// </summary>
        public Result<DoorComponent> CreateDoor(RoomNames targetRoom, int targetX, int targetY)
        {
            if(TryTestRecordAndFail<DoorComponent>(out var earlyRes)) return earlyRes;

            var res = DoorPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(targetRoom, targetX, targetY);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" an input component with the initial values.
        /// </summary>
        public Result<InputsComponent> CreateInputs()
        {
            if(TryTestRecordAndFail<InputsComponent>(out var earlyRes)) return earlyRes;

            var res = InputPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize();

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a pit component with the initial values.
        /// </summary>
        public Result<PitComponent> CreatePit(RoomNames targetRoom, int targetX, int targetY)
        {
            if (TryTestRecordAndFail<PitComponent>(out var earlyRes)) return earlyRes;

            var res = PitPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(targetRoom, targetX, targetY);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a player state component with the initial values.
        /// </summary>
        public Result<PlayerStateComponent> CreatePlayerState(PlayerStanding initialStandingDir)
        {
            if(TryTestRecordAndFail<PlayerStateComponent>(out var earlyRes)) return earlyRes;

            var res = PlayerStatePool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(initialStandingDir);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a position component with the initial values.
        /// </summary>
        public Result<PositionComponent> CreatePosition(int initialX, int initialY)
        {
            if(TryTestRecordAndFail<PositionComponent>(out var earlyRes)) return earlyRes;

            var res = PositionPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(initialX, initialY);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a stairs component with the initial values.
        /// </summary>
        public Result<StairsComponent> CreateStairs(StairDirections dir, RoomNames targetRoom, int targetX, int targetY)
        {
            if(TryTestRecordAndFail<StairsComponent>(out var earlyRes)) return earlyRes;

            var res = StairsPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(dir, targetRoom, targetX, targetY);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a sword knight state component with the given id.
        /// </summary>
        public Result<SwordKnightStateComponent> CreateSwordKnightState(SwordKnightFacing initialDir, int initialX, int initialY)
        {
            if (TryTestRecordAndFail<SwordKnightStateComponent>(out var earlyRes)) return earlyRes;

            var res = SwordKnightStatePool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(initialDir, initialX, initialY);

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a sword component with the given id.
        /// </summary>
        public Result<SwordComponent> CreateSword()
        {
            if(TryTestRecordAndFail<SwordComponent>(out var earlyRes)) return earlyRes;

            var res = SwordPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize();

            return Result.From(component);
        }

        /// <summary>
        /// "Create" a velocity component with the initial values.
        /// </summary>
        public Result<VelocityComponent> CreateVelocity(int xSubPixels, int ySubPixels)
        {
            if(TryTestRecordAndFail<VelocityComponent>(out var earlyRes)) return earlyRes;

            var res = VelocityPool.Get(IdProvider.GetNextId());
            if (!res.Success) return res;

            var component = res.Value;
            component.Initialize(xSubPixels, ySubPixels);

            return Result.From(component);
        }

        /// <summary>
        /// Add an acceleration component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, AccelerationComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            AccelerationComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add an animation component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, AnimationComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            AnimationComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add an associated entity component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, AssociatedEntityComponent component)
        {
            if (TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            AssociatedEntityComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a bush component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, BushComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            BushComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a collision component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, CollisionListener component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            CollisionComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a door component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, DoorComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            DoorComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add an inputs component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, InputsComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            InputComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a pit component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, PitComponent component)
        {
            if (TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            PitComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a player state component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, PlayerStateComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            PlayerStateComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a position component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, PositionComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            PositionComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a stairs component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, StairsComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            StairsComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a sword knight state component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, SwordKnightStateComponent component)
        {
            if (TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            SwordKnightStateComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a sword component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, SwordComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            SwordComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Add a velocity component to the given entity
        /// </summary>
        public Result AddComponent(Entity toEntity, VelocityComponent component)
        {
            if(TryTestRecordAndFail(out var earlyRes)) return earlyRes;

            if (component == null) throw new InvalidOperationException($"Tried to add null component to {nameof(Entity)}.{nameof(Entity.Id)}={toEntity.Id}");

            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return Result.Fail;

            VelocityComponents.Add(component, eId);
            return Result.Succeed;
        }

        /// <summary>
        /// Returns the accelation component for an entity, if it exists.
        /// </summary>
        public AccelerationComponent GetAccelerationFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return AccelerationComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the associated entity component for an entity, if it exists.
        /// </summary>
        public AssociatedEntityComponent GetAssociatedEntityFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return AssociatedEntityComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the animation component for an entity, if it exists.
        /// </summary>
        public AnimationComponent GetAnimationFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return AnimationComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the bush component for an entity, if it exists.
        /// </summary>
        public BushComponent GetBushFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return BushComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the collision component for an entity, if it exists.
        /// </summary>
        public CollisionListener GetCollisionFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return CollisionComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the door component for an entity, if it exists.
        /// </summary>
        public DoorComponent GetDoorFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return DoorComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the inputs component for an entity, if it exists.
        /// </summary>
        public InputsComponent GetInputsFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return InputComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the pit component for an entity, if it exists.
        /// </summary>
        public PitComponent GetPitFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return PitComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the player state component for an entity, if it exists.
        /// </summary>
        public PlayerStateComponent GetPlayerStateFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return PlayerStateComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the position state component for an entity, if it exists.
        /// </summary>
        public PositionComponent GetPositionFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return PositionComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the stairs state component for an entity, if it exists.
        /// </summary>
        public StairsComponent GetStairsFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return StairsComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the sword knight state component for an entity, if it exists.
        /// </summary>
        public SwordKnightStateComponent GetSwordKnightStateFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return SwordKnightStateComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the sword state component for an entity, if it exists.
        /// </summary>
        public SwordComponent GetSwordFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return SwordComponents.Elements[eId];
        }

        /// <summary>
        /// Returns the sword state component for an entity, if it exists.
        /// </summary>
        public VelocityComponent GetVelocityFor(Entity e)
        {
            var eId = e.Id;
            if (Entities[eId].Id == 0) return null;

            return VelocityComponents.Elements[eId];
        }

        /// <summary>
        /// Remove the given component from the given entity
        /// </summary>
        public void RemoveComponent(Entity toEntity, FlagComponent component)
        {
            var eId = toEntity.Id;
            if (Entities[eId].Id == 0) return;

            FlagComponents[eId].ClearFlag(component);
        }

        /// <summary>
        /// Release an acceleration component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(AccelerationComponent component)
        {
            AccelerationPool.Return(component);
        }

        /// <summary>
        /// Release an animation component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(AnimationComponent component)
        {
            AnimationPool.Return(component);
        }

        /// <summary>
        /// Release an associated entity component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(AssociatedEntityComponent component)
        {
            AssociatedEntityPool.Return(component);
        }

        /// <summary>
        /// Release a bush component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(BushComponent component)
        {
            BushPool.Return(component);
        }

        /// <summary>
        /// Release a collision component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(CollisionListener component)
        {
            CollisionPool.Return(component);
        }

        /// <summary>
        /// Release a door component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(DoorComponent component)
        {
            DoorPool.Return(component);
        }

        /// <summary>
        /// Release an input component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(InputsComponent component)
        {
            InputPool.Return(component);
        }

        /// <summary>
        /// Release a pit component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(PitComponent component)
        {
            PitPool.Return(component);
        }

        /// <summary>
        /// Release a player state component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(PlayerStateComponent component)
        {
            PlayerStatePool.Return(component);
        }

        /// <summary>
        /// Release a position component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(PositionComponent component)
        {
            PositionPool.Return(component);
        }

        /// <summary>
        /// Release a stairs component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(StairsComponent component)
        {
            StairsPool.Return(component);
        }

        /// <summary>
        /// Release a sword knight state component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(SwordKnightStateComponent component)
        {
            SwordKnightStatePool.Return(component);
        }

        /// <summary>
        /// Release a sword component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(SwordComponent component)
        {
            SwordPool.Return(component);
        }

        /// <summary>
        /// Release a velocity component.  Only use this if the 
        ///   component _isn't_ attached to an entity.
        /// </summary>
        public void ReleaseComponent(VelocityComponent component)
        {
            VelocityPool.Return(component);
        }

        /// <summary>
        /// Remove an acceleration component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, AccelerationComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            AccelerationComponents.Remove(eId);
            AccelerationPool.Return(component);
        }

        /// <summary>
        /// Remove an animation component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, AnimationComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            AnimationComponents.Remove(eId);
            AnimationPool.Return(component);
        }

        /// <summary>
        /// Remove an associated entity component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, AssociatedEntityComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            AssociatedEntityComponents.Remove(eId);
            AssociatedEntityPool.Return(component);
        }

        /// <summary>
        /// Remove a bush component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, BushComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            BushComponents.Remove(eId);
            BushPool.Return(component);
        }

        /// <summary>
        /// Remove a collision component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, CollisionListener component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            CollisionComponents.Remove(eId);
            CollisionPool.Return(component);
        }

        /// <summary>
        /// Remove a collision component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, DoorComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            DoorComponents.Remove(eId);
            DoorPool.Return(component);
        }

        /// <summary>
        /// Remove an inputs component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, InputsComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            InputComponents.Remove(eId);
            InputPool.Return(component);
        }

        /// <summary>
        /// Remove a pit component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, PitComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            PitComponents.Remove(eId);
            PitPool.Return(component);
        }

        /// <summary>
        /// Remove a player state component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, PlayerStateComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            PlayerStateComponents.Remove(eId);
            PlayerStatePool.Return(component);
        }

        /// <summary>
        /// Remove a position component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, PositionComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            PositionComponents.Remove(eId);
            PositionPool.Return(component);
        }

        /// <summary>
        /// Remove a stairs component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, StairsComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            StairsComponents.Remove(eId);
            StairsPool.Return(component);
        }

        /// <summary>
        /// Remove a sword component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, SwordComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            SwordComponents.Remove(eId);
            SwordPool.Return(component);
        }

        /// <summary>
        /// Remove a velocity component from the given entity.
        /// </summary>
        public void RemoveComponent(Entity fromEntity, VelocityComponent component)
        {
            var eId = fromEntity.Id;
            if (Entities[eId].Id == 0) return;

            VelocityComponents.Remove(eId);
            VelocityPool.Return(component);
        }

        /// <summary>
        /// For testing purposes, this method may fail when the rest of the 
        ///   system wouldn't otherwise fail.
        /// </summary>
        private bool TryTestRecordAndFail<T>(out Result<T> quickFail)
        {
            FallibleCallCount++;

            if (ShouldAlwaysFail)
            {
                quickFail = Result.FailFor<T>();
                return true;
            }

            quickFail = default;
            return false;
        }

        /// <summary>
        /// For testing purposes, this method may fail when the rest of the 
        ///   system wouldn't otherwise fail.
        /// </summary>
        private bool TryTestRecordAndFail(out Result quickFail)
        {
            FallibleCallCount++;

            if (ShouldAlwaysFail)
            {
                quickFail = Result.Fail;
                return true;
            }

            quickFail = default;
            return false;
        }

        /// <summary>
        /// Returns all the entities with the given component
        /// </summary>
        public EntitiesWithFlagComponentEnumerable EntitiesWith(FlagComponent flag) => new EntitiesWithFlagComponentEnumerable(FlagComponents, flag, NextAvailableEntity);

        /// <summary>
        /// Returns all the entities with an acceleration component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<AccelerationComponent> EntitiesWithAcceleration()
        => new EntitiesWithStatefulComponentEnumerable<AccelerationComponent>(AccelerationComponents);

        /// <summary>
        /// Returns all the entities with an animation component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<AnimationComponent> EntitiesWithAnimation()
        => new EntitiesWithStatefulComponentEnumerable<AnimationComponent>(AnimationComponents);

        /// <summary>
        /// Returns all the entities with an associated entity component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<AssociatedEntityComponent> EntitiesWithAssociatedEntity()
        => new EntitiesWithStatefulComponentEnumerable<AssociatedEntityComponent>(AssociatedEntityComponents);

        /// <summary>
        /// Returns all the entities with a bush component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<BushComponent> EntitiesWithBush()
        => new EntitiesWithStatefulComponentEnumerable<BushComponent>(BushComponents);

        /// <summary>
        /// Returns all the entities with a collision component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<CollisionListener> EntitiesWithCollision()
        => new EntitiesWithStatefulComponentEnumerable<CollisionListener>(CollisionComponents);

        /// <summary>
        /// Returns all the entities with a door component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<DoorComponent> EntitiesWithDoor()
        => new EntitiesWithStatefulComponentEnumerable<DoorComponent>(DoorComponents);

        /// <summary>
        /// Returns all the entities with an inputs component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<InputsComponent> EntitiesWithInputs()
        => new EntitiesWithStatefulComponentEnumerable<InputsComponent>(InputComponents);

        /// <summary>
        /// Returns all the entities with a pit component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<PitComponent> EntitiesWithPits()
        => new EntitiesWithStatefulComponentEnumerable<PitComponent>(PitComponents);

        /// <summary>
        /// Returns all the entities with a player state component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<PlayerStateComponent> EntitiesWithPlayerState()
        => new EntitiesWithStatefulComponentEnumerable<PlayerStateComponent>(PlayerStateComponents);

        /// <summary>
        /// Returns all the entities with a position component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<PositionComponent> EntitiesWithPosition()
        => new EntitiesWithStatefulComponentEnumerable<PositionComponent>(PositionComponents);

        /// <summary>
        /// Returns all the entities with a stairs component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<StairsComponent> EntitiesWithStairs()
        => new EntitiesWithStatefulComponentEnumerable<StairsComponent>(StairsComponents);

        /// <summary>
        /// Returns all the entities with a sword knight state component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<SwordKnightStateComponent> EntitiesWithSwordKnightState()
        => new EntitiesWithStatefulComponentEnumerable<SwordKnightStateComponent>(SwordKnightStateComponents);

        /// <summary>
        /// Returns all the entities with a sword component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<SwordComponent> EntitiesWithSword()
        => new EntitiesWithStatefulComponentEnumerable<SwordComponent>(SwordComponents);

        /// <summary>
        /// Returns all the entities with a velocity component.
        /// </summary>
        public EntitiesWithStatefulComponentEnumerable<VelocityComponent> EntitiesWithVelocity()
        => new EntitiesWithStatefulComponentEnumerable<VelocityComponent>(VelocityComponents);

        public AllEntitiesEnumerable AllEntities()
        => new AllEntitiesEnumerable(Entities, NextAvailableEntity);
    }
}
