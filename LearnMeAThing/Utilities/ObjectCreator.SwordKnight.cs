using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Handlers;
using LearnMeAThing.Managers;
using System;

namespace LearnMeAThing.Utilities
{
    static partial class ObjectCreator
    {
        private static readonly Action<EntityManager, AnimationComponent> FreeAnimation = (e, a) => e.ReleaseComponent(a);
        private static readonly Action<EntityManager, PositionComponent> FreePosition = (e, a) => e.ReleaseComponent(a);
        private static readonly Action<EntityManager, VelocityComponent> FreeVelocity = (e, a) => e.ReleaseComponent(a);
        private static readonly Action<EntityManager, CollisionListener> FreeCollision = (e, a) => e.ReleaseComponent(a);

        private static readonly Func<EntityManager, Entity, AnimationComponent, Result> AddAnimation = (m, e, a) => m.AddComponent(e, a);
        private static readonly Func<EntityManager, Entity, PositionComponent, Result> AddPosition = (m, e, a) => m.AddComponent(e, a);
        private static readonly Func<EntityManager, Entity, VelocityComponent, Result> AddVelocity = (m, e, a) => m.AddComponent(e, a);
        private static readonly Func<EntityManager, Entity, CollisionListener, Result> AddCollision = (m, e, a) => m.AddComponent(e, a);
        
        internal static Result<int> MakeSwordKnight(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var initialFacing = def.GetProperty("InitialFacingDirection");
            if (!Enum.TryParse<SwordKnightFacing>(initialFacing, ignoreCase: true, out var initialFacingParsed)) throw new InvalidOperationException($"Unexpected {nameof(SwordKnightFacing)}: {initialFacing}");

            var manager = state.EntityManager;

            var entitiesRes = MakeSwordKnight_MakeEntities(state);
            if (!entitiesRes.Success)
            {
                return Result.FailFor<int>();
            }

            var animationRes = MakeSwordKnight_MakeAnimations(state);
            if (!animationRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }

            var positionRes = MakeSwordKnight_MakePositions(state, def.X, def.Y);
            if(!positionRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeAnimation, animationRes.Value);
                return Result.FailFor<int>();
            }

            var velocityRes = MakeSwordKnight_MakeVelocities(state);
            if (!velocityRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeAnimation, animationRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreePosition, positionRes.Value);
                return Result.FailFor<int>();
            }

            var collisionRes = MakeSwordKnight_MakeCollisions(state);
            if (!collisionRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeAnimation, animationRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreePosition, positionRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeVelocity, velocityRes.Value);
                return Result.FailFor<int>();
            }

            if(!MakeSwordKnight_AddComponents(state, entitiesRes.Value, animationRes.Value, AddAnimation, FreeAnimation))
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreePosition, positionRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeVelocity, velocityRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeCollision, collisionRes.Value);
                return Result.FailFor<int>();
            }

            if (!MakeSwordKnight_AddComponents(state, entitiesRes.Value, positionRes.Value, AddPosition, FreePosition))
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeVelocity, velocityRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeCollision, collisionRes.Value);
                return Result.FailFor<int>();
            }

            if (!MakeSwordKnight_AddComponents(state, entitiesRes.Value, velocityRes.Value, AddVelocity, FreeVelocity))
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                MakeSwordKnight_ReleaseComponents(manager, FreeCollision, collisionRes.Value);
                return Result.FailFor<int>();
            }

            if (!MakeSwordKnight_AddComponents(state, entitiesRes.Value, collisionRes.Value, AddCollision, FreeCollision))
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }
            
            var feet = entitiesRes.Value.Feet;
            var body = entitiesRes.Value.Body;
            var head = entitiesRes.Value.Head;
            var shield = entitiesRes.Value.Shield;
            var sword = entitiesRes.Value.Sword;

            // setup the vision cone, which is kind of special
            var coneRes = MakeSwordKnight_MakeVisionCode(state, head, feet);
            if (!coneRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }
            
            var cone = coneRes.Value;

            // connect everything
            var associatedRes = manager.CreateAssociatedEntity(body, head, shield, sword, cone);
            if (!associatedRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }
            var associated = associatedRes.Value;
            if(!manager.AddComponent(feet, associated).Success)
            {
                manager.ReleaseComponent(associated);
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            var associatedBodyRes = manager.CreateAssociatedEntity(feet);
            if (!associatedBodyRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }
            var associatedBody = associatedBodyRes.Value;
            if(!manager.AddComponent(body, associatedBody).Success)
            {
                manager.ReleaseComponent(associatedBody);
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            // set everything's level
            if (!manager.AddComponent(feet, FlagComponent.Level_Floor).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(body, FlagComponent.Level_Middle).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(head, FlagComponent.Level_Top).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(shield, FlagComponent.Level_Middle).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(sword, FlagComponent.Level_Middle).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(cone, FlagComponent.Level_Top).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(cone, FlagComponent.Level_Middle).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                manager.ReleaseEntity(cone);
                return Result.FailFor<int>();
            }

            // make the sword deal damage
            if (!manager.AddComponent(sword, FlagComponent.DealsDamage).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }

            // make the feet, body, and head take damage
            if(!manager.AddComponent(feet, FlagComponent.TakesDamage).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(body, FlagComponent.TakesDamage).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }

            if (!manager.AddComponent(head, FlagComponent.TakesDamage).Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }

            // create and attach sword knight state
            var stateRes = manager.CreateSwordKnightState(initialFacingParsed, positionRes.Value.Feet.X, positionRes.Value.Feet.Y);
            if (!stateRes.Success)
            {
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }
            var swordKnightState = stateRes.Value;
            if(!manager.AddComponent(feet, swordKnightState).Success)
            {
                manager.ReleaseComponent(swordKnightState);
                MakeSwordKnight_ReleaseEntities(manager, entitiesRes.Value);
                return Result.FailFor<int>();
            }
            
            newEntityBuffer[0] = feet;
            newEntityBuffer[1] = body;
            newEntityBuffer[2] = head;
            newEntityBuffer[3] = shield;
            newEntityBuffer[4] = sword;

            return Result.From(5);
        }

        private static Result<Entity> MakeSwordKnight_MakeVisionCode(GameState state, Entity head, Entity feet)
        {
            var manager = state.EntityManager;

            var coneRes = manager.NewEntity();
            if (!coneRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var cone = coneRes.Value;

            var headPos = manager.GetPositionFor(head);
            if (headPos == null)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }

            var headAnim = manager.GetAnimationFor(head);
            if (headAnim == null)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }

            var coneAnimRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.SwordKnight_Vision_Left, 0);
            if (!coneAnimRes.Success)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(cone, coneAnimRes.Value).Success)
            {
                manager.ReleaseEntity(cone);
                manager.ReleaseComponent(coneAnimRes.Value);
                return Result.FailFor<Entity>();
            }


            var headDims = state.AssetMeasurer.Measure(headAnim.GetCurrentFrame(state.AnimationManager));
            var coneDims = state.AssetMeasurer.Measure(coneAnimRes.Value.GetCurrentFrame(state.AnimationManager));

            var shiftX = headDims.Width / 2 - coneDims.Width / 2;
            var shiftY = headDims.Height / 2 - coneDims.Height / 2;

            var conePosX = headPos.X + shiftX;
            var conePosY = headPos.Y - shiftY;

            var conePosRes = manager.CreatePosition(conePosX, conePosY);
            if (!conePosRes.Success)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(cone, conePosRes.Value).Success)
            {
                manager.ReleaseEntity(cone);
                manager.ReleaseComponent(conePosRes.Value);
                return Result.FailFor<Entity>();
            }

            var coneCollisionRes = manager.CreateCollision(AssetNames.SwordKnight_Vision_Left, 0, SwordKnightCollisionHandler.VisionConeCollision, SwordKnightCollisionHandler.VisionConePush);
            if (!coneCollisionRes.Success)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(cone, coneCollisionRes.Value).Success)
            {
                manager.ReleaseEntity(cone);
                manager.ReleaseComponent(coneCollisionRes.Value);
                return Result.FailFor<Entity>();
            }

            var coneVelRes = manager.CreateVelocity(1, 1); // moving, so collision detect will kick in
            if (!coneVelRes.Success)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(cone, coneVelRes.Value).Success)
            {
                manager.ReleaseEntity(cone);
                manager.ReleaseComponent(coneVelRes.Value);
                return Result.FailFor<Entity>();
            }

            var coneAssocRes = manager.CreateAssociatedEntity(feet);    // need to get back to the knight state, which is attached to the feet
            if (!coneAnimRes.Success)
            {
                manager.ReleaseEntity(cone);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(cone, coneAssocRes.Value).Success)
            {
                manager.ReleaseEntity(cone);
                manager.ReleaseComponent(coneAssocRes.Value);
                return Result.FailFor<Entity>();
            }

            return Result.From(cone);
        }

        /// <summary>
        ///  Helper for adding sets of components to sets of entities
        ///  
        /// If something goes wrong, the _components_ are freed but the 
        ///   entities are not.
        ///   
        /// Returns true if nothing went wrong, false otherwise.
        /// </summary>
        private static bool MakeSwordKnight_AddComponents<T>(
            GameState state, 
            (Entity a, Entity b, Entity c, Entity d, Entity e) entities, 
            (T a, T b, T c, T d, T e) components, 
            Func<EntityManager, Entity, T,Result> adder,
            Action<EntityManager, T> releaser
        )
        {
            var manager = state.EntityManager;

            if(!adder(manager, entities.a, components.a).Success)
            {
                // free everything
                releaser(manager, components.a);
                releaser(manager, components.b);
                releaser(manager, components.c);
                releaser(manager, components.d);
                releaser(manager, components.e);
                return false;
            }

            if (!adder(manager, entities.b, components.b).Success)
            {
                // free only what hasn't been attached
                releaser(manager, components.b);
                releaser(manager, components.c);
                releaser(manager, components.d);
                releaser(manager, components.e);
                return false;
            }

            if (!adder(manager, entities.c, components.c).Success)
            {
                // free only what hasn't been attached
                releaser(manager, components.c);
                releaser(manager, components.d);
                releaser(manager, components.e);
                return false;
            }

            if (!adder(manager, entities.d, components.d).Success)
            {
                // free only what hasn't been attached
                releaser(manager, components.d);
                releaser(manager, components.e);
                return false;
            }

            if (!adder(manager, entities.e, components.e).Success)
            {
                // free only what hasn't been attached
                releaser(manager, components.e);
                return false;
            }

            return true;
        }

        // Helper to just release a bunch of entities
        private static void MakeSwordKnight_ReleaseEntities(EntityManager m, (Entity a, Entity b, Entity c, Entity d, Entity e) tuple)
        {
            m.ReleaseEntity(tuple.a);
            m.ReleaseEntity(tuple.b);
            m.ReleaseEntity(tuple.c);
            m.ReleaseEntity(tuple.d);
            m.ReleaseEntity(tuple.e);
        }

        // Helper to just release a bunch of components
        private static void MakeSwordKnight_ReleaseComponents<T>(EntityManager m, Action<EntityManager, T> freeIt, (T a, T b, T c, T d, T e) tuple)
        {
            freeIt(m, tuple.a);
            freeIt(m, tuple.b);
            freeIt(m, tuple.c);
            freeIt(m, tuple.d);
            freeIt(m, tuple.e);
        }

        private static Result<(CollisionListener Feet, CollisionListener Body, CollisionListener Head, CollisionListener Shield, CollisionListener Sword)> MakeSwordKnight_MakeCollisions(GameState state)
        {
            var fail = Result.FailFor<(CollisionListener Feet, CollisionListener Body, CollisionListener Head, CollisionListener Shield, CollisionListener Sword)>();

            var manager = state.EntityManager;

            var feetColRes = manager.CreateCollision(AssetNames.SwordKnight_Feet_Right1, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
            if (!feetColRes.Success)
            {
                return fail;
            }

            var feetCol = feetColRes.Value;

            var bodyColRes = manager.CreateCollision(AssetNames.SwordKnight_Body_Right, 0, DoNothingCollisionHandler.Collision, SwordKnightCollisionHandler.BodyPushed);
            if (!bodyColRes.Success)
            {
                manager.ReleaseComponent(feetCol);
                return fail;
            }

            var bodyCol = bodyColRes.Value;

            var headColRes = manager.CreateCollision(AssetNames.SwordKnight_Body_Right, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
            if (!headColRes.Success)
            {
                manager.ReleaseComponent(feetCol);
                manager.ReleaseComponent(bodyCol);
                return fail;
            }

            var headCol = headColRes.Value;

            var shieldColRes = manager.CreateCollision(AssetNames.SwordKnight_Shield_Right, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
            if (!shieldColRes.Success)
            {
                manager.ReleaseComponent(feetCol);
                manager.ReleaseComponent(bodyCol);
                manager.ReleaseComponent(headCol);
                return fail;
            }

            var shieldCol = shieldColRes.Value;

            var swordColRes = manager.CreateCollision(AssetNames.SwordKnight_Sword_Right, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
            if (!swordColRes.Success)
            {
                manager.ReleaseComponent(feetCol);
                manager.ReleaseComponent(bodyCol);
                manager.ReleaseComponent(headCol);
                manager.ReleaseComponent(shieldCol);
                return fail;
            }

            var swordCol = swordColRes.Value;

            var ret = (feetCol, bodyCol, headCol, shieldCol, swordCol);

            return Result.From(ret);
        }

        private static Result<(VelocityComponent Feet, VelocityComponent Body, VelocityComponent Head, VelocityComponent Shield, VelocityComponent Sword)> MakeSwordKnight_MakeVelocities(GameState state)
        {
            var fail = Result.FailFor<(VelocityComponent Feet, VelocityComponent Body, VelocityComponent Head, VelocityComponent Shield, VelocityComponent Sword)>();

            var manager = state.EntityManager;

            var feetVelRes = manager.CreateVelocity(0, 0);
            if (!feetVelRes.Success)
            {
                return fail;
            }

            var feetVel = feetVelRes.Value;

            var bodyVelRes = manager.CreateVelocity(0, 0);
            if (!bodyVelRes.Success)
            {
                manager.ReleaseComponent(feetVel);
                return fail;
            }

            var bodyVel = bodyVelRes.Value;

            var headVelRes = manager.CreateVelocity(0, 0);
            if (!headVelRes.Success)
            {
                manager.ReleaseComponent(feetVel);
                manager.ReleaseComponent(bodyVel);
                return fail;
            }

            var headVel = headVelRes.Value;

            var shieldVelRes = manager.CreateVelocity(0, 0);
            if (!shieldVelRes.Success)
            {
                manager.ReleaseComponent(feetVel);
                manager.ReleaseComponent(bodyVel);
                manager.ReleaseComponent(headVel);
                return fail;
            }

            var shieldVel = shieldVelRes.Value;

            var swordVelRes = manager.CreateVelocity(0, 0);
            if (!swordVelRes.Success)
            {
                manager.ReleaseComponent(feetVel);
                manager.ReleaseComponent(bodyVel);
                manager.ReleaseComponent(headVel);
                manager.ReleaseComponent(shieldVel);
                return fail;
            }

            var swordVel = swordVelRes.Value;

            var ret = (feetVel, bodyVel, headVel, shieldVel, swordVel);

            return Result.From(ret);
        }

        private static Result<(PositionComponent Feet, PositionComponent Body, PositionComponent Head, PositionComponent Shield, PositionComponent Sword)> MakeSwordKnight_MakePositions(GameState state, int x, int y)
        {
            var fail = Result.FailFor<(PositionComponent Feet, PositionComponent Body, PositionComponent Head, PositionComponent Shield, PositionComponent Sword)>();

            var manager = state.EntityManager;
            var assetMeasurer = state.AssetMeasurer;

            var feetX = x;
            var feetY = y + RoomTemplate.TILE_HEIGHT_PIXELS - assetMeasurer.Measure(AssetNames.SwordKnight_Feet_Right1).Height;

            var feetPosRes = manager.CreatePosition(feetX, feetY);
            if (!feetPosRes.Success)
            {
                return fail;
            }

            var feetPos = feetPosRes.Value;

            var bodyDims = assetMeasurer.Measure(AssetNames.SwordKnight_Body_Right);
            var bodyX = x;
            var bodyY = feetY - bodyDims.Height;

            var bodyPosRes = manager.CreatePosition(bodyX, bodyY);
            if (!bodyPosRes.Success)
            {
                manager.ReleaseComponent(feetPos);
                return fail;
            }

            var bodyPos = bodyPosRes.Value;

            var headX = x;
            var headY = bodyY - assetMeasurer.Measure(AssetNames.SwordKnight_Head_Right).Height;

            var headPosRes = manager.CreatePosition(headX, headY);
            if (!headPosRes.Success)
            {
                manager.ReleaseComponent(feetPos);
                manager.ReleaseComponent(bodyPos);
                return fail;
            }

            var headPos = headPosRes.Value;

            var shieldDims = assetMeasurer.Measure(AssetNames.SwordKnight_Sword_Right);
            var shieldX = x + bodyDims.Width;
            var shieldY = y + RoomTemplate.TILE_HEIGHT_PIXELS / 3;

            var shieldPosRes = manager.CreatePosition(shieldX, shieldY);
            if (!shieldPosRes.Success)
            {
                manager.ReleaseComponent(feetPos);
                manager.ReleaseComponent(bodyPos);
                manager.ReleaseComponent(headPos);
                return fail;
            }

            var shieldPos = shieldPosRes.Value;

            var swordX = x + bodyDims.Width + shieldDims.Width;
            var swordY = y + RoomTemplate.TILE_HEIGHT_PIXELS / 2;

            var swordPosRes = manager.CreatePosition(swordX, swordY);
            if (!swordPosRes.Success)
            {
                manager.ReleaseComponent(feetPos);
                manager.ReleaseComponent(bodyPos);
                manager.ReleaseComponent(headPos);
                manager.ReleaseComponent(shieldPos);
                return fail;
            }

            var swordPos = swordPosRes.Value;

            var ret = (feetPos, bodyPos, headPos, shieldPos, swordPos);

            return Result.From(ret);
        }

        private static Result<(AnimationComponent Feet, AnimationComponent Body, AnimationComponent Head, AnimationComponent Shield, AnimationComponent Sword)> MakeSwordKnight_MakeAnimations(GameState state)
        {
            var fail = Result.FailFor<(AnimationComponent Feet, AnimationComponent Body, AnimationComponent Head, AnimationComponent Shield, AnimationComponent Sword)>();

            var manager = state.EntityManager;
            var animManager = state.AnimationManager;
            
            var feetAnimRes = manager.CreateAnimation(animManager, AnimationNames.SwordKnight_Feet_StandingRight, 0);
            if (!feetAnimRes.Success)
            {
                return fail;
            }

            var feetAnim = feetAnimRes.Value;

            var bodyAnimRes = manager.CreateAnimation(animManager, AnimationNames.SwordKnight_Body_Right, 0);
            if (!bodyAnimRes.Success)
            {
                manager.ReleaseComponent(feetAnim);
                return fail;
            }

            var bodyAnim = bodyAnimRes.Value;

            var headAnimRes = manager.CreateAnimation(animManager, AnimationNames.SwordKnight_Head_Right, 0);
            if (!headAnimRes.Success)
            {
                manager.ReleaseComponent(feetAnim);
                manager.ReleaseComponent(bodyAnim);
                return fail;
            }

            var headAnim = headAnimRes.Value;

            var shieldAnimRes = manager.CreateAnimation(animManager, AnimationNames.SwordKnight_Shield_Right, 0);
            if (!shieldAnimRes.Success)
            {
                manager.ReleaseComponent(feetAnim);
                manager.ReleaseComponent(bodyAnim);
                manager.ReleaseComponent(headAnim);
                return fail;
            }

            var shieldAnim = shieldAnimRes.Value;

            var swordAnimRes = manager.CreateAnimation(animManager, AnimationNames.SwordKnight_Sword_Right, 0);
            if (!swordAnimRes.Success)
            {
                manager.ReleaseComponent(feetAnim);
                manager.ReleaseComponent(bodyAnim);
                manager.ReleaseComponent(headAnim);
                manager.ReleaseComponent(shieldAnim);
                return fail;
            }

            var swordAnim = swordAnimRes.Value;

            var ret = (feetAnim, bodyAnim, headAnim, shieldAnim, swordAnim);

            return Result.From(ret);
        }

        private static Result<(Entity Feet, Entity Body, Entity Head, Entity Shield, Entity Sword)> MakeSwordKnight_MakeEntities(GameState state)
        {
            var fail = Result.FailFor<(Entity Feet, Entity Body, Entity Head, Entity Shield, Entity Sword)>();

            var manager = state.EntityManager;
            
            var headRes = manager.NewEntity();
            if (!headRes.Success)
            {
                return fail;
            }

            var head = headRes.Value;

            var bodyRes = manager.NewEntity();
            if (!bodyRes.Success)
            {
                manager.ReleaseEntity(head);
                return fail;
            }

            var body = bodyRes.Value;

            var feetRes = manager.NewEntity();
            if (!feetRes.Success)
            {
                manager.ReleaseEntity(head);
                manager.ReleaseEntity(body);
                return fail;
            }

            var feet = feetRes.Value;

            var shieldRes = manager.NewEntity();
            if (!shieldRes.Success)
            {
                manager.ReleaseEntity(head);
                manager.ReleaseEntity(body);
                manager.ReleaseEntity(feet);
                return fail;
            }

            var shield = shieldRes.Value;

            var swordRes = manager.NewEntity();
            if (!swordRes.Success)
            {
                manager.ReleaseEntity(head);
                manager.ReleaseEntity(body);
                manager.ReleaseEntity(feet);
                manager.ReleaseEntity(shield);
                return fail;
            }

            var sword = swordRes.Value;

            var ret = (feet, body, head, shield, sword);

            return Result.From(ret);
        }
    }
}
