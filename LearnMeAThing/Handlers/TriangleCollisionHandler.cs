using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class TriangleCollisionHandler
    {
        public static readonly CollisionHandler Collision = Bounce;
        public static readonly PushedAwayHandler Push = NOP;

        private static void NOP(GameState state, Entity self, Entity collidedWith, Vector pushDir) { }

        private static void Bounce(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon)
        {
            var manager = state.EntityManager;
            var hitMapManager = state.HitMapManager;
            var roomHeightSubPixels = state.RoomManager.Measure(state.CurrentRoom.Name).Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var clSelf = manager.GetCollisionFor(self);
            var clOther = manager.GetCollisionFor(collidedWith);

            var locSelf = manager.GetPositionFor(self);
            var velSelf = manager.GetVelocityFor(self);

            DoBounce(hitMapManager, roomHeightSubPixels, atPoint, clSelf, ourPoly, locSelf, velSelf, clOther, theirPolygon);
        }

        private static void DoBounce(
            IHitMapManager hitMapManager,
            int roomHeightSubPixels,
            Point collisionPtCartesian,

            CollisionListener self,
            ConvexPolygon selfPoly,
            PositionComponent selfLoc,
            VelocityComponent selfVelocity,

            CollisionListener other,
            ConvexPolygon otherPoly
        )
        {
            // nothing to do
            if (selfVelocity.X_SubPixels == 0 && selfVelocity.Y_SubPixels == 0) return;

            var edgeCartesian = ClosestTo(collisionPtCartesian, otherPoly);
            Vector normOfWallScreen;
            if (edgeCartesian == null)
            {
                // vertex on vertex collision
                var otherVertex = ClosestVertexTo(collisionPtCartesian, otherPoly);
                var betweenVerticesCartesian = new LineSegment2D(collisionPtCartesian, otherVertex);

                var betweenVerticesScreen = TranslateToScreen(roomHeightSubPixels, betweenVerticesCartesian);
                var wallSlopeScreen = betweenVerticesScreen.Normal();
                normOfWallScreen = wallSlopeScreen.Normal();
            }
            else
            {
                var edgeScreen = TranslateToScreen(roomHeightSubPixels, edgeCartesian.Value);
                normOfWallScreen = edgeScreen.Normal();
            }

            // based on: https://gamedev.stackexchange.com/a/23676/155
            var curSpeed = new Vector(selfVelocity.X_SubPixels, selfVelocity.Y_SubPixels);
            var newSpeed = curSpeed - 2 * curSpeed.Dot(normOfWallScreen) * normOfWallScreen;

            newSpeed = newSpeed.Normalize() * FixedPoint.FromInt(self.DesiredSpeed_HACK);

            selfVelocity.X_SubPixels = (int)newSpeed.DeltaX;
            selfVelocity.Y_SubPixels = (int)newSpeed.DeltaY;
        }

        /// <summary>
        /// Searches the given poly for it's vertex that is closest to the given point.
        /// </summary>
        private static Point ClosestVertexTo(Point pt, ConvexPolygon poly)
        {
            FixedPoint? minDist = null;
            Point ret = default;

            for (var i = 0; i < poly.NumVertices; i++)
            {
                var v = poly.GetVertex(i);
                var diff = new Vector(v.X - pt.X, v.Y - pt.Y);
                if (diff.DeltaX * diff.DeltaX + diff.DeltaY * diff.DeltaY < 0)
                {
                    continue;
                }

                if (!diff.TryMagnitude(out var dist)) continue;

                if (minDist == null || dist < minDist)
                {
                    minDist = dist;
                    ret = v;
                }
            }

            return ret;
        }

        private static LineSegment2D? ClosestTo(Point pt, ConvexPolygon poly)
        {
            FixedPoint? closest = null;
            LineSegment2D edge = default;

            for(var i = 0; i < poly.NumLineSegments; i++)
            {
                var seg = poly.GetLineSegment(i);
                var dist = CollisionDetector.DetermineClosestPoint(seg, pt);
                if (dist == null) continue;

                if (closest == null || dist.Value.Distance < closest)
                {
                    closest = dist.Value.Distance;
                    edge = seg;
                }
            }

            if (closest == null) return null;

            return edge;
        }

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

        private static LineSegment2D TranslateToScreen(int roomHeightSubPixels, LineSegment2D seg)
        => new LineSegment2D(TranslateToScreen(roomHeightSubPixels, seg.P1), TranslateToScreen(roomHeightSubPixels, seg.P2));
    }
}
