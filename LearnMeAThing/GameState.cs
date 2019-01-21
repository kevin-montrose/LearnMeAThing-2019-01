using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Handlers;
using LearnMeAThing.Managers;
using LearnMeAThing.Systems;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing
{
    // Just a convient place to keep all the state, without getting all mungled in the actual *Game class
    sealed class GameState:
        IHoldsEntity,
        IIdIssuer
    {
        public const int MAX_ENTITIES = 1000;
        const int MAX_STATEFUL_COMPONENTS_PER_ENTITY = 100;
        const long TICKS_PER_FRAME = 10_000_000 / 60;

        // dirty hack that need to go away
        public const int WIDTH_HACK = 2600 / 2;
        public const int HEIGHT_HACK = 1600 / 2;
        
        // end dirty hacks
        
        // room state
        internal Room CurrentRoom;
        private Entity[] RoomCreationBuffer;
        internal Room? NextRoom;

        // special entities
        public int EntitiesCount => 4;
        internal Entity Player_Feet;    // a player's _feet_ hold all the relevant components
        internal Entity Player_Body;
        internal Entity Player_Head;
        internal Entity Camera;
        public Entity GetEntity(int ix)
        {
            switch (ix)
            {
                case 0: return Player_Feet;
                case 1: return Player_Body;
                case 2: return Player_Head;
                case 3: return Camera;
                default: throw new InvalidOperationException();
            }
        }
        public void SetEntity(int ix, Entity e)
        {
            switch (ix)
            {
                case 0: Player_Feet = e; break;
                case 1: Player_Body = e; break;
                case 2: Player_Head = e; break;
                case 3: Camera = e; break;
                default: throw new InvalidOperationException();
            }
        }
        
        // systems
        internal InputSystem InputSystem;
        internal CameraSystem CameraSystem;
        internal SetPlayerVelocitySystem SetPlayerVelocitySystem;
        internal UpdatePositionsSystem UpdatePositionsSystem;
        internal CollisionDetectionSystem CollisionDetectionSystem;
        internal AnimationSystem AnimationSystem;
        internal PlayerSystem PlayerStateSystem;
        internal SwordSystem SwordSystem;
        internal BushSystem BushSystem;
        internal CleanupSystem CleanupSystem;
        internal ExitSystem ExitSystem;
        internal SwordKnightSystem SwordKnightSystem;

        internal IHoldsEntity[] HoldingEntities;

        // managers
        internal EntityManager EntityManager;
        internal IAssetMeasurer AssetMeasurer;
        internal IRoomManager RoomManager;
        internal IAnimationManager AnimationManager;
        internal IHitMapManager HitMapManager;

        // job runner
        internal JobRunner JobRunner;

        // timer
        internal Timings Timings;

        // we want to uniquely identify everything in the state, this is how we keep track
        private int NextId;

        // ticks that we "left over" last time we did an update
        private long ResidualTicks;
        
        public GameState()
        {
            ResidualTicks = 0;
            EntityManager = new EntityManager(this, MAX_ENTITIES);
            NextId = MAX_ENTITIES * MAX_STATEFUL_COMPONENTS_PER_ENTITY + 100;   // some gap, why not
            RoomCreationBuffer = new Entity[16];
            Timings = new Timings(60);
        }

        public void Initialize(
            IRoomManager roomManager,
            IHardwareInput inputSource, 
            IAssetMeasurer assetMeasurer,
            IAnimationManager animationManager,
            IHitMapManager hitMapManager
        )
        {
            AssetMeasurer = assetMeasurer;
            RoomManager = roomManager;
            AnimationManager = animationManager;
            HitMapManager = hitMapManager;

            // setup player
            var defaultRoom = RoomNames.Kakariko;   // hack!
            var defaultRoomSize = RoomManager.Measure(defaultRoom);

            var playerSizeWidth = 
                Math.Max(
                    assetMeasurer.Measure(AssetNames.Player_Feet).Width,
                    Math.Max(
                        assetMeasurer.Measure(AssetNames.Player_Body).Width,
                        assetMeasurer.Measure(AssetNames.Player_Head).Width
                    )
                );
            var playerSizeHeight =
                    assetMeasurer.Measure(AssetNames.Player_Feet).Height +
                    assetMeasurer.Measure(AssetNames.Player_Body).Height +
                    assetMeasurer.Measure(AssetNames.Player_Head).Height;
            var playerSize = (Width: playerSizeWidth, Height: playerSizeHeight);
            
            var playerX = defaultRoomSize.Width / 2 - playerSize.Width / 2;
            var playerY = defaultRoomSize.Height / 2 - playerSize.Height / 2 + 100;

            // create player
            Player_Feet = ObjectCreator.CreatePlayerFeet(this, playerX, playerY).Value;
            Player_Body = ObjectCreator.CreatePlayerBody(this, playerX, playerY).Value;
            Player_Head = ObjectCreator.CreatePlayerHead(this, playerX, playerY).Value;
            if (Player_Feet.Id != 1) throw new InvalidOperationException($"Somehow player feet ended up with non-1 {nameof(Entity.Id)}={Player_Feet.Id}");
            if (Player_Body.Id != 2) throw new InvalidOperationException($"Somehow player body ended up with non-2 {nameof(Entity.Id)}={Player_Body.Id}");
            if (Player_Head.Id != 3) throw new InvalidOperationException($"Somehow player head ended up with non-3 {nameof(Entity.Id)}={Player_Head.Id}");

            // setup current room
            CurrentRoom = CreateRoom(defaultRoom);

            // setup the camera
            Camera = ObjectCreator.CreateCamera(this, playerX, playerY).Value;

            // prepare the job runner
            var numThreads = Environment.ProcessorCount - 2;
            if (numThreads < 1) numThreads = 1;
            JobRunner = new JobRunner(this, numThreads, 8);
            JobRunner.Initialize();

            // initialize systems
            InputSystem = new InputSystem(inputSource);
            CameraSystem = new CameraSystem();
            SetPlayerVelocitySystem = new SetPlayerVelocitySystem();
            UpdatePositionsSystem = new UpdatePositionsSystem(MAX_ENTITIES, JobRunner);
            CollisionDetectionSystem = new CollisionDetectionSystem(MAX_ENTITIES, 10, 10_000, JobRunner);
            AnimationSystem = new AnimationSystem();
            PlayerStateSystem = new PlayerSystem(12);
            SwordSystem = new SwordSystem(12);
            BushSystem = new BushSystem();
            CleanupSystem = new CleanupSystem(100, 2);  // if we ever have > 50% dead space, or every 100 cycles, do a compaction
            ExitSystem = new ExitSystem();
            SwordKnightSystem = new SwordKnightSystem();

            HoldingEntities = new IHoldsEntity[] { this };

            // force a GC
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        }

        public void MakeCollisionTest_HACK()
        {
            //var playerPos = EntityManager.GetPositionFor(Player_Feet);
            //if (playerPos == null) throw new Exception("wut");

            //var playerX = playerPos.X;
            //var playerY = playerPos.Y;

            //var triangleSize = AssetMeasurer.Measure(AssetNames.Triangle);
            //var triangleX = playerX + 200;
            //var triangleY = playerY - 10;
            //var triangle = EntityManager.NewEntity().Value;
            //EntityManager.AddComponent(triangle, new PositionComponent(GetNextId(), triangleX, triangleY));
            //EntityManager.AddComponent(triangle, new VelocityComponent(GetNextId(), -32, 0));
            //EntityManager.AddComponent(triangle, new CollisionListener(GetNextId(), AssetNames.Triangle, 32, TriangleCollisionHandler.Collision, TriangleCollisionHandler.Push));
            //EntityManager.AddComponent(triangle, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Triangle, 0));
            //EntityManager.AddComponent(triangle, FlagComponent.Level_Floor);

            //MakeSquareRange_HACK(triangleX, triangleY + 10);

            //var triangle2X = triangleX - triangleSize.Width - 36;
            //var triangle2Y = triangleY + 5;
            //var t2 = EntityManager.NewEntity().Value;
            //EntityManager.AddComponent(t2, new PositionComponent(GetNextId(), triangle2X, triangle2Y));
            //EntityManager.AddComponent(t2, new VelocityComponent(GetNextId(), -32, -32));
            //EntityManager.AddComponent(t2, new CollisionListener(GetNextId(), AssetNames.Triangle, 45, TriangleCollisionHandler.Collision, TriangleCollisionHandler.Push));
            //EntityManager.AddComponent(t2, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Triangle, 0));
            //EntityManager.AddComponent(t2, FlagComponent.Level_Floor);

            //var triangle3X = triangleX + triangleSize.Width + 36;
            //var triangle3Y = triangleY - triangleSize.Height - 16;
            //var t3 = EntityManager.NewEntity().Value;
            //EntityManager.AddComponent(t3, new PositionComponent(GetNextId(), triangle3X, triangle3Y));
            //EntityManager.AddComponent(t3, new VelocityComponent(GetNextId(), 32, -32));
            //EntityManager.AddComponent(t3, new CollisionListener(GetNextId(), AssetNames.Triangle, 45, TriangleCollisionHandler.Collision, TriangleCollisionHandler.Push));
            //EntityManager.AddComponent(t3, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Triangle, 0));
            //EntityManager.AddComponent(t3, FlagComponent.Level_Floor);

            //MakeTie_HACK(playerX, playerY);
        }

        private void MakeTie_HACK(int playerX, int playerY)
        {
            //var tie = EntityManager.NewEntity().Value;
            //var tieSize = AssetMeasurer.Measure(AssetNames.Tie);
            //var tieX = playerX - 180;
            //var tieY = playerY - 120;

            //EntityManager.AddComponent(tie, new PositionComponent(GetNextId(), tieX, tieY));
            //EntityManager.AddComponent(tie, new VelocityComponent(GetNextId(), 0, 0));
            //EntityManager.AddComponent(tie, new CollisionListener(GetNextId(), AssetNames.Tie, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push));
            //EntityManager.AddComponent(tie, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Tie, 0));
            //EntityManager.AddComponent(tie, FlagComponent.Level_Floor);
        }

        private void MakeSquareRange_HACK(int triangleX, int triangleY)
        {
            //var squareSize = AssetMeasurer.Measure(AssetNames.Square);

            //// intentionally boxes overlapping, so there's never a corner-corner case
            //var usedWidth = squareSize.Width - 1;
            //var usedHeight = squareSize.Height - 1;
            
            //// top line!
            //for (var i = 0; i < 7; i++)
            //{
            //    var s = EntityManager.NewEntity().Value;
            //    var sX = triangleX - 3 * usedWidth + usedWidth * i;
            //    var sY = triangleY - 3 * usedHeight;

            //    EntityManager.AddComponent(s, new PositionComponent(GetNextId(), sX, sY));
            //    EntityManager.AddComponent(s, new VelocityComponent(GetNextId(), 0, 0));
            //    EntityManager.AddComponent(s, new CollisionListener(GetNextId(), AssetNames.Square, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push));
            //    EntityManager.AddComponent(s, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Square, 0));
            //    EntityManager.AddComponent(s, FlagComponent.Level_Floor);
            //}

            //// bottom line!
            //for (var i = 0; i < 7; i++)
            //{
            //    var s = EntityManager.NewEntity().Value;
            //    var sX = triangleX - 3 * usedWidth + usedWidth * i;
            //    var sY = triangleY + 3 * usedHeight;

            //    EntityManager.AddComponent(s, new PositionComponent(GetNextId(), sX, sY));
            //    EntityManager.AddComponent(s, new VelocityComponent(GetNextId(), 0, 0));
            //    EntityManager.AddComponent(s, new CollisionListener(GetNextId(), AssetNames.Square, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push));
            //    EntityManager.AddComponent(s, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Square, 0));
            //    EntityManager.AddComponent(s, FlagComponent.Level_Floor);
            //}

            //// left line!
            //for(var i = 0; i < 5; i++)
            //{
            //    var s = EntityManager.NewEntity().Value;
            //    var sX = triangleX - 3 * usedWidth;
            //    var sY = triangleY - 2 * usedHeight + usedHeight * i;

            //    EntityManager.AddComponent(s, new PositionComponent(GetNextId(), sX, sY));
            //    EntityManager.AddComponent(s, new VelocityComponent(GetNextId(), 0, 0));
            //    EntityManager.AddComponent(s, new CollisionListener(GetNextId(), AssetNames.Square, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push));
            //    EntityManager.AddComponent(s, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Square, 0));
            //    EntityManager.AddComponent(s, FlagComponent.Level_Floor);
            //}

            //// right line!
            //for (var i = 0; i < 5; i++)
            //{
            //    var s = EntityManager.NewEntity().Value;
            //    var sX = triangleX + 3 * usedWidth;
            //    var sY = triangleY - 2 * usedHeight + usedHeight * i;

            //    EntityManager.AddComponent(s, new PositionComponent(GetNextId(), sX, sY));
            //    EntityManager.AddComponent(s, new VelocityComponent(GetNextId(), 0, 0));
            //    EntityManager.AddComponent(s, new CollisionListener(GetNextId(), AssetNames.Square, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push));
            //    EntityManager.AddComponent(s, new AnimationComponent(GetNextId(), AnimationManager, AnimationNames.Square, 0));
            //    EntityManager.AddComponent(s, FlagComponent.Level_Floor);
            //}
        }

        public int GetNextId() => NextId++;

        public void Update(TimeSpan sinceLastUpdate)
        {
            var totalTimeSinceLastUpdate = sinceLastUpdate.Ticks + ResidualTicks;

            var steps = totalTimeSinceLastUpdate / TICKS_PER_FRAME;
            var newResidual = totalTimeSinceLastUpdate % TICKS_PER_FRAME;

            for(var i = 0; i < steps; i++)
            {
                AdvanceStateByFrame();
            }

            ResidualTicks = newResidual;
        }

        internal void AdvanceStateByFrame()
        {
            if (!ExitSystem.IsTransitioning)
            {
                // rough sketch of systems to run
                // ------------------------------
                // read input
                RunSystem(InputSystem);
            }

            // advance running animations
            //   so the rest of the systems are working off of "current"
            //   frames
            RunSystem(AnimationSystem);

            if (!ExitSystem.IsTransitioning)
            {
                // the player wants to move... set it up
                RunSystem(SetPlayerVelocitySystem);

                // physics! detect any collisions and deal with them
                RunSystem(CollisionDetectionSystem);

                // update everything's position, and force things to
                //   not be overlapping
                RunSystem(UpdatePositionsSystem);
            }

            // read off anything that mattered and make changes to the player
            RunSystem(PlayerStateSystem);

            // sword knights!
            RunSystem(SwordKnightSystem);

            if (!ExitSystem.IsTransitioning)
            {
                // swing the sword (and keep swinging swords working)
                RunSystem(SwordSystem);

                // cut some bushes
                RunSystem(BushSystem);
            }

            // handle player trying to move to a new room
            RunSystem(ExitSystem);

            // position the camera
            RunSystem(CameraSystem);

            // todo: the rest

            // everything should be "done" now, so do any cleanups we need
            RunSystem(CleanupSystem);
        }

        // actual run the given system, performing whatever needs doing
        internal void RunSystem<T>(ASystem<T> system)
        {
            using (Timings.Time(system.Type))
            {
                var entities = system.DesiredEntities(EntityManager);
                system.Update(this, entities);
            }
        }

        /// <summary>
        /// Triggers a compacting of entities.
        /// 
        /// This can only be safely called when there are no entities on the stack.
        /// </summary>
        internal void CompactEntities()
        {
            EntityManager.Compact(HoldingEntities);
        }
        
        /// <summary>
        /// Create a room using the template identified by the given name.
        /// 
        /// Takes in a reusable buffer, so we don't allocate any while
        ///    creating a room.  This buffer must be set, and it's size
        ///    should be such that any room can be accomidated.
        /// </summary>
        internal Room CreateRoom(RoomNames name)
        {
            var roomManager = this.RoomManager;
            var entityManager = this.EntityManager;

            var template = roomManager.Get(name);
            var ret = new Room(template);
            if (template.ObjectsOnFloor != null)
            {
                foreach (var obj in template.ObjectsOnFloor)
                {
                    var lRes = ObjectCreator.Create(this, obj, RoomCreationBuffer);
                    if (!lRes.Success)
                    {
                        // glitch: well, what should happen here?
                        continue;
                    }
                }
            }

            return ret;
        }
    }
}
