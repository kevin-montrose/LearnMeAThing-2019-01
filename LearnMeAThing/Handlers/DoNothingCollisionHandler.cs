using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class DoNothingCollisionHandler
    {
        public static readonly CollisionHandler Collision = NOP;
        public static readonly PushedAwayHandler Push = NOP;

        private static void NOP(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon) { }
        private static void NOP(GameState state, Entity self, Entity collidedWith, Vector pushDir) { }
    }
}
