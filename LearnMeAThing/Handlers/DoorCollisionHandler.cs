using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Handlers
{
    static class DoorCollisionHandler
    {
        public static readonly CollisionHandler Collision = NOP;
        public static readonly PushedAwayHandler Push = OnPush;

        private static void NOP(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon) { }

        private static void OnPush(GameState state, Entity self, Entity collidedWith, Vector pushDir)
        {
            var manager = state.EntityManager;
            var door = manager.GetDoorFor(self);
            if(door == null)
            {
                // glitch: ???
                return;
            }

            var player = manager.GetPlayerStateFor(collidedWith);
            if(player == null)
            {
                // we only take action when the _player_ walks into a door
                return;
            }

            // let's get to it
            var exit = state.ExitSystem;
            exit.RequestExit(door);
        }
    }
}
