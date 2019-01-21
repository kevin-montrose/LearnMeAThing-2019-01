using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class SwordCollisionHandler
    {
        public static readonly CollisionHandler Collision = NOP;
        public static readonly PushedAwayHandler Push = OnPush;

        private static void NOP(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon) { }
        private static void OnPush(GameState state, Entity self, Entity collidedWith, Vector pushDir)
        {
            // we trigger on _this_ because we're spawning the sword as part of a swing
            //    so it might start as colliding (which won't trigger the collision handler)
            state.SwordSystem.CollidedWith(state, self, collidedWith, pushDir);
        }
    }
}
