using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class BushCollisionHandler
    {
        public static readonly CollisionHandler Collision = NOP;
        public static readonly PushedAwayHandler Push = OnPush;

        private static void NOP(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon) { }

        private static void OnPush(GameState state, Entity self, Entity collidedWith, Vector pushDir)
        {
            var manager = state.EntityManager;

            var flagsRes = manager.GetFlagComponentsForEntity(collidedWith);
            if (!flagsRes.Success)
            {
                // glitch: ???
                return;
            }

            var flags = flagsRes.Value;

            // we only cut bushes if they're actually dealt damage
            if (!flags.HasFlag(FlagComponent.DealsDamage)) return;

            state.BushSystem.Cut(state, self);
        }
    }
}
