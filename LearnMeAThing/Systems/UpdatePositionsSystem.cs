using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Systems
{
    sealed class UpdatePositionsSystem : ASystem<object>
    {
        sealed class State
        {
            public Buffer<(int Id1, int Id2)> AlreadyPushedScratch;
            public FlagComponent Level;

            private State(FlagComponent level, Buffer<(int Id1, int Id2)> buffer)
            {
                Level = level;
                AlreadyPushedScratch = buffer;
            }

            public static State Make(FlagComponent level, int maxEntities)
            {
                return
                    new State
                    (
                        level,
                        new Buffer<(int Id1, int Id2)>(maxEntities * (maxEntities - 1))
                    );
            }
        }

        public override SystemType Type => SystemType.UpdatePositions;
        
        private Job<State> FloorJob;
        private Job<State> MiddleJob;
        private Job<State> TopJob;
        private Job<State> CeilingJob;

        public UpdatePositionsSystem(int maxEntities, JobRunner jobRunner)
        {
            var floorState = State.Make(FlagComponent.Level_Floor, maxEntities);
            var middleState = State.Make(FlagComponent.Level_Middle, maxEntities);
            var topState = State.Make(FlagComponent.Level_Top, maxEntities);
            var ceilingState = State.Make(FlagComponent.Level_Ceiling, maxEntities);

            FloorJob = jobRunner.CreateJob(UpdateJobDelegate, floorState);
            MiddleJob = jobRunner.CreateJob(UpdateJobDelegate, middleState);
            TopJob = jobRunner.CreateJob(UpdateJobDelegate, topState);
            CeilingJob = jobRunner.CreateJob(UpdateJobDelegate, ceilingState);
        }

        public override object DesiredEntities(EntityManager manager)
        => null;

        public override void Update(GameState state, object _)
        {
            var tok = state.JobRunner.StartJobs(FloorJob, MiddleJob, TopJob, CeilingJob);
            tok.WaitForCompletion();
        }

        private static void UpdateJobDelegate(GameState game, State state)
        {
            var manager = game.EntityManager;
            var requested = manager.EntitiesWithVelocity();

            UpdateImpl(game, requested, state.Level, ref state.AlreadyPushedScratch);
        }

        private static void UpdateImpl(
            GameState state, 
            EntityManager.EntitiesWithStatefulComponentEnumerable<VelocityComponent> requestedEntities, 
            FlagComponent level,
            ref Buffer<(int Id1, int Id2)> AlreadyPushedScratch
        )
        {
            var roomSize = state.RoomManager.Measure(state.CurrentRoom.Name);

            var roomWidthSubPixels = roomSize.Width * PositionComponent.SUBPIXELS_PER_PIXEL;
            var roomHeightSubPixels = roomSize.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            // move everything to where it "wants" to be
            var manager = state.EntityManager;
            var hitMapManager = state.HitMapManager;
            foreach (var moving in requestedEntities)
            {
                var vel = moving.Component;
                var position = manager.GetPositionFor(moving.Entity);
                if (position == null) continue;
                var collision = manager.GetCollisionFor(moving.Entity);
                if (collision == null) continue;

                var flags = manager.GetFlagComponentsForEntity(moving.Entity);
                if (!flags.Success) continue;
                if (!flags.Value.HasFlag(level)) continue;


                // don't let it move outside the current room
                var newXSubPixels = position.X_SubPixel + vel.X_SubPixels;
                var newYSubPixels = position.Y_SubPixel + vel.Y_SubPixels;

                // don't let the player move out of bounds
                if (moving.Entity.Id == state.Player_Feet.Id)
                {
                    var dimensions = hitMapManager.Measure(collision.HitMap);

                    var entityWidthSubPixels = dimensions.Width * PositionComponent.SUBPIXELS_PER_PIXEL;
                    var entityHeightSubPixels = dimensions.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

                    bool tryExitLeft, tryExitRight, tryExitTop, tryExitBottom;
                    tryExitLeft = tryExitRight = tryExitTop = tryExitBottom = false;

                    if (newXSubPixels < 0)
                    {
                        newXSubPixels = 0;
                        tryExitLeft = true;
                    }

                    if (newXSubPixels + entityWidthSubPixels > roomWidthSubPixels)
                    {
                        newXSubPixels = roomWidthSubPixels - entityWidthSubPixels;
                        tryExitRight = true;
                    }

                    if (newYSubPixels < 0)
                    {
                        newYSubPixels = 0;
                        tryExitTop = true;
                    }

                    if (newYSubPixels + entityHeightSubPixels > roomHeightSubPixels)
                    {
                        newYSubPixels = roomHeightSubPixels - entityHeightSubPixels;
                        tryExitBottom = true;
                    }

                    HandleExits(state, tryExitLeft, tryExitRight, tryExitTop, tryExitBottom);
                }

                position.X_SubPixel = newXSubPixels;
                position.Y_SubPixel = newYSubPixels;
            }

            // now push things out of collision with each other
            
            AlreadyPushedScratch.Clear();

            PushOutOfContact(state, roomHeightSubPixels, manager, hitMapManager, requestedEntities, level, ref AlreadyPushedScratch);
        }

        private static void HandleExits(GameState state, bool left, bool right, bool top, bool bottom)
        {
            if (!left && !right && !top && !bottom) return;

            if (left)
            {
                state.ExitSystem.RequestExit(ExitDirection.West);
                return;
            }

            if (right)
            {
                state.ExitSystem.RequestExit(ExitDirection.East);
                return;
            }

            if (top)
            {
                state.ExitSystem.RequestExit(ExitDirection.North);
                return;
            }

            if (bottom)
            {
                state.ExitSystem.RequestExit(ExitDirection.South);
                return;
            }
        }

        private static void PushOutOfContact(
            GameState state,
            int roomHeightSubPixels,
            EntityManager manager,
            IHitMapManager hitMapManager,
            EntityManager.EntitiesWithStatefulComponentEnumerable<VelocityComponent> requestedEntities, 
            FlagComponent level,
            ref Buffer<(int Id1, int Id2)> AlreadyPushedScratch
        )
        {
            const int MIN_STEP = 1;

            startOver:
            foreach (var moving in requestedEntities)
            {
                var mE = moving.Entity;
                var velMoving = moving.Component;
                var collisionMoving = manager.GetCollisionFor(mE);
                if (collisionMoving == null) continue;
                var positionMoving = manager.GetPositionFor(mE);
                if (positionMoving == null) continue;
                if (velMoving.X_SubPixels == 0 && velMoving.Y_SubPixels == 0) continue;

                var meFlags = manager.GetFlagComponentsForEntity(mE);
                if (!meFlags.Success) continue;
                if (!meFlags.Value.HasFlag(level)) continue;

                foreach (var other in requestedEntities)
                {
                    var oE = other.Entity;
                    if (mE.Id == oE.Id)
                    {
                        continue;
                    }

                    // if it isn't on the current level, it can't collide
                    var oeFlags = manager.GetFlagComponentsForEntity(oE);
                    if (!oeFlags.Success) continue;
                    if (!oeFlags.Value.HasFlag(level)) continue;

                    // check to see if we've pushed them before, if so bail
                    var key = (Math.Min(mE.Id, oE.Id), Math.Max(mE.Id, oE.Id));
                    if (AlreadyPushedScratch.Contains(key)) continue;

                    var velOther = other.Component;
                    var collisionOther = manager.GetCollisionFor(oE);
                    if (collisionOther == null) continue;
                    var positionOther = manager.GetPositionFor(oE);
                    if (positionOther == null) continue;

                    // if the overall bounding boxes can't collide, don't bother checking the individual polygons
                    if (!CouldCollide(hitMapManager, collisionMoving.HitMap, positionMoving, velMoving, collisionOther.HitMap, positionOther, velOther))
                    {
                        continue;
                    }

                    // got check the actual polygons
                    var hitMapsMoving = hitMapManager.GetFor(collisionMoving.HitMap);
                    foreach (var hitMapMoving in hitMapsMoving)
                    {
                        var polyMoving = CollisionDetectionSystem.TranslateToCartesian(roomHeightSubPixels, hitMapMoving, positionMoving);

                        // take the _largest_ MTV that pushes these
                        //   two _entities_ out of collision with each other
                        var hitMapsOther = hitMapManager.GetFor(collisionOther.HitMap);
                        Vector? mtvCartesian = null;
                        foreach (var hitMapOther in hitMapsOther)
                        {
                            var polyOther = CollisionDetectionSystem.TranslateToCartesian(roomHeightSubPixels, hitMapOther, positionOther);

                            var mtvCartesianForThis = CollisionDetector.GetMinimumTranslationVector(polyOther, polyMoving);
                            if (mtvCartesianForThis == null) continue;

                            if (!mtvCartesianForThis.Value.TryMagnitude(out var mtvCartesianForThisMag)) continue;

                            if (mtvCartesian == null)
                            {
                                mtvCartesian = mtvCartesianForThis;
                            }
                            else
                            {
                                // has been checked in the past, so can't fail
                                mtvCartesian.Value.TryMagnitude(out var mtvCartesianMag);
                                if(mtvCartesianMag < mtvCartesianForThisMag)
                                {
                                    mtvCartesian = mtvCartesianForThis;
                                }
                            }
                        }

                        if (mtvCartesian != null)
                        {
                            AlreadyPushedScratch.Add(key);

                            var mtvScreen = CollisionDetectionSystem.TranslateToScreen(mtvCartesian.Value);

                            var deltaX = (int)mtvScreen.DeltaX;
                            var deltaY = (int)mtvScreen.DeltaY;

                            if (Math.Abs(deltaX) < MIN_STEP) deltaX = mtvScreen.DeltaX.Sign() * MIN_STEP;  // don't round down to zero
                            if (Math.Abs(deltaY) < MIN_STEP) deltaY = mtvScreen.DeltaY.Sign() * MIN_STEP;  // don't round down to zero

                            positionMoving.X_SubPixel += deltaX;
                            positionMoving.Y_SubPixel += deltaY;

                            var mtvScreenNorm = mtvScreen.Normalize();

                            // notify that we pushed these things out of contact with each other
                            collisionMoving.OnPush(state, mE, oE, mtvScreenNorm);
                            collisionOther.OnPush(state, oE, mE, mtvScreenNorm);

                            // we might have push this into contact with something else...
                            //    so check everything again
                            goto startOver;
                        }
                    }
                }
            }
        }

        private static bool CouldCollide(
            IHitMapManager hitMapManager,
            AssetNames m1,
            PositionComponent p1,
            VelocityComponent v1,
            AssetNames m2,
            PositionComponent p2,
            VelocityComponent v2
        )
        {
            var d1 = hitMapManager.Measure(m1);
            var d2 = hitMapManager.Measure(m2);

            var b1 = CollisionDetectionSystem.GetMovingBoundsScreen(p1, d1, v1);
            var b2 = CollisionDetectionSystem.GetMovingBoundsScreen(p2, d2, v2);

            return CollisionDetectionSystem.CouldCollide(b1, b2);
        }
    }
}
