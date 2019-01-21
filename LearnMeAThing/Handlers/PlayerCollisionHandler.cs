using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class PlayerCollisionHandler
    {
        public static readonly CollisionHandler Collision = Collided;
        public static readonly PushedAwayHandler Push = Pushed;

        public static readonly PushedAwayHandler PlayerBodyPush = PlayerBodyPushed;

        private static void Collided(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon)
        {
            var manager = state.EntityManager;
            var playerState = manager.GetPlayerStateFor(state.Player_Feet);
            if(playerState == null)
            {
                // glitch: ???
                return;
            }

            playerState.SetCollidedWith(collidedWith);
        }

        private static void Pushed(GameState state, Entity self, Entity other, Vector pushDir)
        {
            var manager = state.EntityManager;
            var playerState = manager.GetPlayerStateFor(state.Player_Feet);
            if (playerState == null)
            {
                // glitch: ???
                return;
            }

            playerState.SetPushedFrom(other);
        }

        private static void PlayerBodyPushed(GameState state, Entity self, Entity other, Vector pushDir)
        {
            var manager = state.EntityManager;
            var flags = manager.GetFlagComponentsForEntity(other);
            if (!flags.Success) return;

            var player = manager.GetPlayerStateFor(state.Player_Feet);
            if(player == null)
            {
                // glitch: ???
                return;
            }

            if(flags.Value.HasFlag(FlagComponent.DealsDamage))
            {
                // player needs to recoil
                player.RecoilAlong = pushDir;
            }
        }
    }
}
