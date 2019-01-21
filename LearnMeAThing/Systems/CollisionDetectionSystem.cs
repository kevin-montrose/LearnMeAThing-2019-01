using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Systems
{
    sealed class CollisionDetectionSystem: ASystem<object>
    {
        sealed class State
        {
            public readonly FlagComponent Level;
            
            public readonly CollisionDetector Detector;
            public readonly ConvexPolygon[] Polygons;
            public readonly Entity[] PolygonToEntityMap;
            public readonly Vector[] Velocity;
            
            public readonly FixedPoint TimeStep;

            public readonly (int PolyIndex1, int PolyIndex2, byte Count)[] AlreadyCollided;
            public int AlreadyCollidedSize;

            public int Return;

            private State(int maxEntities, int maxPolygonsPerEntity, int fractionalRounding, FlagComponent level)
            {
                Level = level;
                TimeStep = FixedPoint.One / fractionalRounding;
                Detector = new CollisionDetector(maxEntities * 4, 12);
                Polygons = new ConvexPolygon[maxEntities * maxPolygonsPerEntity];
                PolygonToEntityMap = new Entity[Polygons.Length];
                Velocity = new Vector[maxEntities];
                AlreadyCollided = new (int PolyIndex1, int PolyIndex2, byte Count)[maxEntities * (maxEntities - 1)];
                AlreadyCollidedSize = 0;
            }

            public static State Make(int maxEntities, int maxPolygonsPerEntity, int fractionalRounding, FlagComponent level) 
            => new State(maxEntities, maxPolygonsPerEntity, fractionalRounding, level);
        }

        public override SystemType Type => SystemType.CollisionDetection;
        
        
        public Buffer<ConvexPolygon> LastUsedPolygonsCartesian;

        private readonly Job<State> FloorJob;
        private readonly Job<State> MiddleJob;
        private readonly Job<State> TopJob;
        private readonly Job<State> CeilingJob;

        public CollisionDetectionSystem(int maxEntities, int maxPolygonsPerEntity, int fractionalRounding, JobRunner jobRunner)
        {
            LastUsedPolygonsCartesian = new Buffer<ConvexPolygon>(maxEntities * maxPolygonsPerEntity);

            var floorState = State.Make(maxEntities, maxPolygonsPerEntity, fractionalRounding, FlagComponent.Level_Floor);
            var midState = State.Make(maxEntities, maxPolygonsPerEntity, fractionalRounding, FlagComponent.Level_Middle);
            var topState = State.Make(maxEntities, maxPolygonsPerEntity, fractionalRounding, FlagComponent.Level_Top);
            var ceilingState = State.Make(maxEntities, maxPolygonsPerEntity, fractionalRounding, FlagComponent.Level_Ceiling);

            FloorJob = jobRunner.CreateJob(UpdateJobDelegate, floorState);
            MiddleJob = jobRunner.CreateJob(UpdateJobDelegate, midState);
            TopJob = jobRunner.CreateJob(UpdateJobDelegate, topState);
            CeilingJob = jobRunner.CreateJob(UpdateJobDelegate, ceilingState);
        }

        public override object DesiredEntities(EntityManager manager)
        => null;

        public override void Update(GameState state, object requestedEntities)
        {
            var tok = state.JobRunner.StartJobs(FloorJob, MiddleJob, TopJob, CeilingJob);
            tok.WaitForCompletion();

            // copy debug info
            LastUsedPolygonsCartesian.Clear();
            LastUsedPolygonsCartesian.CopyInto(FloorJob.State.Polygons, FloorJob.State.Return);
            LastUsedPolygonsCartesian.CopyInto(MiddleJob.State.Polygons, MiddleJob.State.Return);
            LastUsedPolygonsCartesian.CopyInto(TopJob.State.Polygons, TopJob.State.Return);
            LastUsedPolygonsCartesian.CopyInto(CeilingJob.State.Polygons, CeilingJob.State.Return);
        }

        private static void UpdateJobDelegate(GameState gameState, State state)
        {
            var manager = gameState.EntityManager;
            var requestedEntities = manager.EntitiesWithCollision();
            state.Return = UpdateImpl(gameState, state.Detector, state.TimeStep, requestedEntities, state.Level, state.AlreadyCollided, ref state.AlreadyCollidedSize, state.Polygons, state.Velocity, state.PolygonToEntityMap);
        }

        private static int UpdateImpl(
            GameState state, 
            CollisionDetector Detector,
            FixedPoint TimeStep,
            EntityManager.EntitiesWithStatefulComponentEnumerable<CollisionListener> requestedEntities, 
            FlagComponent level,
            (int PolyIndex1, int PolyIndex2, byte Count)[] AlreadyCollided,
            ref int AlreadyCollidedSize,
            ConvexPolygon[] Polygons, 
            Vector[] Velocity, 
            Entity[] PolygonToEntityMap
        )
        {
            var roomHeightSubPixels = state.RoomManager.Measure(state.CurrentRoom.Name).Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var manager = state.EntityManager;
            var hitMapManager = state.HitMapManager;

            ClearAlreadyCollided(ref AlreadyCollidedSize);
            
            while (true)
            {
                var toCheck = UpdateBuffers(level, requestedEntities, manager, hitMapManager, roomHeightSubPixels, ref Polygons, ref Velocity, ref PolygonToEntityMap);
                var needsResolution = FindFirstCollisionToHandle(Detector, toCheck, Polygons, Velocity, TimeStep, PolygonToEntityMap, AlreadyCollidedSize, AlreadyCollided);
                if (needsResolution == null) return toCheck;

                GetDetails(
                    roomHeightSubPixels,
                    needsResolution.Value,
                    manager,
                    requestedEntities,
                    Polygons,
                    PolygonToEntityMap,
                    out var i1,
                    out var poly1,
                    out var p1,
                    out var v1,
                    out var i2,
                    out var poly2,
                    out var p2,
                    out var v2,
                    out var pt
                );

                var collisionPtCartesian = needsResolution.Value.CollisionAt;

                var e1 = PolygonToEntityMap[needsResolution.Value.FirstPolygonIndex];
                var e2 = PolygonToEntityMap[needsResolution.Value.SecondPolygonIndex];

                var c1 = manager.GetCollisionFor(e1);
                var c2 = manager.GetCollisionFor(e2);

                c1.OnCollision(state, e1, e2, collisionPtCartesian, poly1, poly2);
                c2.OnCollision(state, e2, e1, collisionPtCartesian, poly2, poly1);
                
                MarkAlreadyCollided(needsResolution.Value.FirstPolygonIndex, needsResolution.Value.SecondPolygonIndex, AlreadyCollided, ref AlreadyCollidedSize);
            }
        }
        /// <summary>
        /// Go over all the entities we found for this frame and
        ///   shove their details into the appropriate buffers
        ///
        /// Returns the number of entities we iterated over
        /// </summary>
        private static int UpdateBuffers(
            FlagComponent level,
            EntityManager.EntitiesWithStatefulComponentEnumerable<CollisionListener> requestedEntities, 
            EntityManager manager, 
            IHitMapManager hitMapManager,
            int roomHeightSubPixels,
            ref ConvexPolygon[] Polygons,
            ref Vector[] Velocity,
            ref Entity[] PolygonToEntityMap

        )
        {
            var nextPolygonIx = 0;
            foreach (var e in requestedEntities)
            {
                var eCollisionState = e.Component;
                var eLocation = manager.GetPositionFor(e.Entity);
                if (eLocation == null) continue;
                var eVelocity = manager.GetVelocityFor(e.Entity);
                if (eVelocity == null) continue;

                var eFlags = manager.GetFlagComponentsForEntity(e.Entity);
                if (!eFlags.Success) continue;
                if (!eFlags.Value.HasFlag(level)) continue;

                var eIsMoving = eVelocity.X_SubPixels != 0 || eVelocity.Y_SubPixels != 0;

                var eDims = hitMapManager.Measure(eCollisionState.HitMap);
                var eMovingHitBox = GetMovingBoundsScreen(eLocation, eDims, eVelocity);

                var couldCollide = false;
                foreach(var o in requestedEntities)
                {
                    if (e.Entity.Id == o.Entity.Id) continue;

                    var oCollisionState = o.Component;
                    var oLocation = manager.GetPositionFor(o.Entity);
                    if (oLocation == null) continue;
                    var oVelocity = manager.GetVelocityFor(o.Entity);
                    if (oVelocity == null) continue;

                    var oFlags = manager.GetFlagComponentsForEntity(o.Entity);
                    if (!oFlags.Success) continue;
                    if (!oFlags.Value.HasFlag(level)) continue;

                    var oIsMoving = oVelocity.X_SubPixels != 0 || oVelocity.Y_SubPixels != 0;

                    // things that aren't moving can't collide
                    if (!eIsMoving && !oIsMoving) continue;

                    var oDims = hitMapManager.Measure(oCollisionState.HitMap);
                    var oMovingHitBox = GetMovingBoundsScreen(oLocation, oDims, oVelocity);

                    if (CouldCollide(eMovingHitBox, oMovingHitBox))
                    {
                        couldCollide = true;
                    }
                }

                // this entire entity isn't in a location that can collide,
                //   so don't bother decomposing it and asking for individual 
                //   collision checks
                if (!couldCollide) continue;

                var hitMaps = hitMapManager.GetFor(eCollisionState.HitMap);

                foreach (var hitMap in hitMaps)
                {
                    Polygons[nextPolygonIx] = TranslateToCartesian(roomHeightSubPixels, hitMap, eLocation);
                    Velocity[nextPolygonIx] = TranslateToCartesian(eVelocity);
                    PolygonToEntityMap[nextPolygonIx] = e.Entity;
                    nextPolygonIx++;
                }
            }

            return nextPolygonIx;
        }

        private static void ClearAlreadyCollided(ref int AlreadyCollidedSize)
        {
            AlreadyCollidedSize = 0;
        }

        private static void MarkAlreadyCollided(int pIx1, int pIx2, (int PolyIndex1, int PolyIndex2, byte Count)[] AlreadyCollided, ref int AlreadyCollidedSize)
        {
            for(var i = 0; i < AlreadyCollidedSize; i++)
            {
                var c = AlreadyCollided[i];
                if(c.PolyIndex1 == pIx1 && c.PolyIndex2 == pIx2)
                {
                    c.Count++;
                    AlreadyCollided[i] = c;

                    return;
                }
            }

            var nextIx = AlreadyCollidedSize;
            if(nextIx == AlreadyCollided.Length)
            {
                throw new InvalidOperationException("More simultaneous collisions have occurred than were accounted for");
            }

            AlreadyCollided[nextIx] = (pIx1, pIx2, 1);
            AlreadyCollidedSize++;
        }

        private static bool HaveAlreadyCollidedTooManyTimes(
            int pIx1, 
            int pIx2, 
            int AlreadyCollidedSize,
            (int PolyIndex1, int PolyIndex2, byte Count)[] AlreadyCollided
        )
        {
            for(var i = 0; i < AlreadyCollidedSize; i++)
            {
                var c = AlreadyCollided[i];
                if(c.PolyIndex1 == pIx1 && c.PolyIndex2 == pIx2)
                {
                    return c.Count >= 5;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Extracts relevant details from a single collision.
        /// 
        /// Everything set is in screen coordinates.
        /// </summary>
        private static void GetDetails(
            int roomHeightSubPixels,
            Collision collision,
            EntityManager manager,
            EntityManager.EntitiesWithStatefulComponentEnumerable<CollisionListener> relevantEntities,
            ConvexPolygon[] Polygons,
            Entity[] PolygonToEntityMap,
            out CollisionListener item1,
            out ConvexPolygon poly1,
            out PositionComponent screenPosition1,
            out VelocityComponent screenVelocity1,
            out CollisionListener item2,
            out ConvexPolygon poly2,
            out PositionComponent screenPosition2,
            out VelocityComponent screenVelocity2,
            out Point collisionPointScreen
        )
        {
            // a single entity can map to multiple polygons, so we need to reverse
            //    the mapping before trying to find the entities
            var firstPolygonEntity = PolygonToEntityMap[collision.FirstPolygonIndex];
            var secondPolygonEntity = PolygonToEntityMap[collision.SecondPolygonIndex];

            poly1 = Polygons[collision.FirstPolygonIndex];
            poly2 = Polygons[collision.SecondPolygonIndex];

            item1 = null;
            screenPosition1 = null;
            screenVelocity1 = null;

            item2 = null;
            screenPosition2 = null;
            screenVelocity2 = null;
            
            foreach (var e in relevantEntities)
            {
                var thisE = e.Entity;
                if (thisE.Id == firstPolygonEntity.Id)
                {
                    item1 = manager.GetCollisionFor(e.Entity);
                    screenPosition1 = manager.GetPositionFor(e.Entity);
                    screenVelocity1 = manager.GetVelocityFor(e.Entity);
                    continue;
                }

                if (thisE.Id == secondPolygonEntity.Id)
                {
                    item2 = manager.GetCollisionFor(e.Entity);
                    screenPosition2 = manager.GetPositionFor(e.Entity);
                    screenVelocity2 = manager.GetVelocityFor(e.Entity);
                    // we guarantee that SecondPolygonIndex > FirstPolygonIndex
                    //   so getting here means we've already gone through the first if
                    break;
                }
            }

            collisionPointScreen = TranslateToScreen(roomHeightSubPixels, collision.CollisionAt);
        }

        private static Collision? FindFirstCollisionToHandle(
            CollisionDetector detector, 
            int count, 
            ConvexPolygon[] polys,
            Vector[] vels, 
            FixedPoint step,
            Entity[] PolygonToEntityMap,
            int AlreadyCollidedSize,
            (int PolyIndex1, int PolyIndex2, byte Count)[] AlreadyCollided
        )
        {
            Collision? earliest = null;
            
            using (var collisions = detector.FindCollisions(count, polys, vels, step, true))
            {
                for (var i = 0; i < collisions.Count; i++)
                {
                    var c = collisions[i];
                    if (c.AtTime > 1)
                    {
                        // won't happen this frame
                        continue;
                    }

                    var entityOwningFirstPoly = PolygonToEntityMap[c.FirstPolygonIndex];
                    var entityOwningSecondPoly = PolygonToEntityMap[c.SecondPolygonIndex];
                    if(entityOwningFirstPoly.Id == entityOwningSecondPoly.Id)
                    {
                        // can't collide with yourself
                        continue;
                    }

                    // this already happened this frame, continuous bouncing doesn't count
                    if (HaveAlreadyCollidedTooManyTimes(c.FirstPolygonIndex, c.SecondPolygonIndex, AlreadyCollidedSize, AlreadyCollided))
                    {
                        continue;
                    }

                    if (earliest == null || c.AtTime < earliest.Value.AtTime)
                    {
                        earliest = c;
                    }
                }
            }

            return earliest;
        }

        /// <summary>
        /// We need to lookup the _hit box_ 
        ///   and convert things into cartesian coordinates.
        ///   
        /// The game works with (0,0) in the top left
        ///   but CollisionDetector works with (0,0) in the bottom left
        /// </summary>
        public static ConvexPolygon TranslateToCartesian(int roomHeightSubPixels, ConvexPolygonPattern cartesianHitMap, PositionComponent screenLocation)
        {
            // we need to translate all the points
            var cartesianPos = TranslateToCartesian(roomHeightSubPixels, screenLocation);
            
            var newY = cartesianPos.Y - cartesianHitMap.OriginalHeight;

            var cartesianPoly = new ConvexPolygon(cartesianHitMap).Translate(cartesianPos.X, newY);
            
            return cartesianPoly;
        }

        /// <summary>
        /// Collision detector works in cartesian coordinates (0,0) in 
        ///   the bottom left, but the game runs with (0,0) in the top
        ///   left.
        ///   
        /// For velocity/vectors, this means we just need to flip the y
        ///    axis.
        /// </summary>
        private static Vector TranslateToCartesian(VelocityComponent loc)
        => new Vector(loc.X_SubPixels,-loc.Y_SubPixels);

        /// <summary>
        /// Collision detector works in cartesian coordinates (0,0) in 
        ///   the bottom left, but the game runs with (0,0) in the top
        ///   left.
        ///   
        /// For velocity/vectors, this means we just need to flip the y
        ///    axis.
        /// </summary>
        public static Vector TranslateToScreen(Vector screenVector)
        => new Vector(screenVector.DeltaX, -screenVector.DeltaY);

        /// <summary>
        /// Collision detector works in cartesian coordinates (0,0) in 
        ///   the bottom left, but the game runs with (0,0) in the top
        ///   left.
        ///   
        /// For points, this means we need to add the y coord
        ///   to the -height of the screen.
        /// </summary>
        private static Point TranslateToScreen(int roomHeightSubPixels, Point pt)
        => new Point(pt.X, roomHeightSubPixels - pt.Y);

        private static Point TranslateToCartesian(int roomHeightSubPixels, PositionComponent pos)
        {
            var x = pos.X_SubPixel;
            var y = pos.Y_SubPixel;

            var translatedY = roomHeightSubPixels - y;
            return new Point(x, translatedY);
        }

        internal static (FixedPoint Left, FixedPoint Right, FixedPoint Top, FixedPoint Bottom) GetMovingBoundsScreen(
            PositionComponent loc, 
            (int Width, int Height) dim, 
            VelocityComponent vel
        )
        {
            var x = loc.X_SubPixel;
            var y = loc.Y_SubPixel;

            var diffX = Math.Abs(vel.X_SubPixels);
            var diffY = Math.Abs(vel.Y_SubPixels);

            var widthSubPixels = dim.Width * PositionComponent.SUBPIXELS_PER_PIXEL;
            var heightSubPixels = dim.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var leftX = x - diffX - FixedPoint.One;
            var rightX = x + widthSubPixels + diffX + FixedPoint.One;
            var topY = y - diffY - FixedPoint.One;
            var bottomY = y + heightSubPixels + diffY + FixedPoint.One;

            return (leftX, rightX, topY, bottomY);
        }

        internal static bool CouldCollide(
            (FixedPoint Left, FixedPoint Right, FixedPoint Top, FixedPoint Bottom) b1,
            (FixedPoint Left, FixedPoint Right, FixedPoint Top, FixedPoint Bottom) b2
        )
        {
            // doing all of this in screen coords

            var b1IsAboveB2 = b1.Bottom < b2.Top;
            var b1IsBelowB2 = b1.Top > b2.Bottom;
            var b1IsLeftOfB2 = b1.Right < b2.Left;
            var b1IsRightOfB2 = b1.Left > b2.Right;

            var touching = !(b1IsAboveB2 || b1IsBelowB2 || b1IsLeftOfB2 || b1IsRightOfB2);

            return touching;
        }
    }
}
