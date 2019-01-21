using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class SwordKnightCollisionHandler
    {
        public static readonly CollisionHandler VisionConeCollision = NOP;
        public static readonly PushedAwayHandler VisionConePush = OnVisionConePush;
        public static readonly PushedAwayHandler BodyPushed = OnBodyPush;

        private static void NOP(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon) { }

        private static void OnVisionConePush(GameState state, Entity visionCone, Entity collidedWith, Vector pushDir)
        {
            if (collidedWith.Id != state.Player_Body.Id && collidedWith.Id != state.Player_Head.Id) return;

            var manager = state.EntityManager;

            var assoc = manager.GetAssociatedEntityFor(visionCone);
            if (assoc == null)
            {
                // glitch: ???
                return;
            }

            var feet = assoc.FirstEntity;

            var knight = manager.GetSwordKnightStateFor(feet);
            if (knight == null)
            {
                // glitch: ???
                return;
            }

            // can get multiple hits at once, so act lock here
            lock (knight)
            {
                if (!knight.IsChasing)
                {
                    knight.SetChasing();
                }
            }
        }

        private static void OnBodyPush(GameState state, Entity body, Entity collidedWith, Vector pushDir)
        {
            var manager = state.EntityManager;
            var assoc = manager.GetAssociatedEntityFor(body);
            if (assoc == null)
            {
                // glitch: ???
                return;
            }

            var feet = assoc.FirstEntity;

            var knight = manager.GetSwordKnightStateFor(feet);
            if (knight == null)
            {
                // glitch: ???
                return;
            }

            var flagsRes = manager.GetFlagComponentsForEntity(collidedWith);
            if (!flagsRes.Success) return;

            if (!flagsRes.Value.HasFlag(FlagComponent.DealsDamage)) return;

            knight.Die();
        }
    }
}
