using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using System;

namespace LearnMeAThing.Systems
{
    sealed class CameraSystem : ASystem<object>
    {
        public override SystemType Type => SystemType.Camera;

        /// <summary>
        /// Where, if anywhere, the camera has been explicitly pointed.
        /// </summary>
        internal (int X, int Y)? ExplicitCameraTarget { get; private set; }

        public override object DesiredEntities(EntityManager manager) =>
        null;

        /// <summary>
        /// Point the camera at a specific point, overrides
        ///   normal camera behavior until ResetCamera()
        ///   is called.
        /// </summary>
        public void PointCameraAt(int x, int y)
        {
            ExplicitCameraTarget = (x, y);
        }

        /// <summary>
        /// Resets camera behavior, causing it to point
        ///   at the player again.
        ///   
        /// Expected to be called sometime after PointCameraAt(...)
        ///   is called.
        /// </summary>
        public void ResetCamera()
        {
            ExplicitCameraTarget = null;
        }

        public override void Update(GameState state, object _)
        {
            var manager = state.EntityManager;

            var playerFeet = state.Player_Feet;
            var playerBody = state.Player_Body;
            var playerHead = state.Player_Head;
            var camera = state.Camera;

            var playerFeetPos = manager.GetPositionFor(playerFeet);

            var cameraPos = manager.GetPositionFor(camera);

            // todo: uggggggh, maybe make this like.... work?
            var cameraDims = (Width: GameState.WIDTH_HACK, Height: GameState.HEIGHT_HACK);

            if (playerFeetPos == null || cameraPos == null)
            {
                // glitch: probably the same as above...
                return;
            }

            var playerFeetDims = state.AssetMeasurer.Measure(AssetNames.Player_Feet);
            var playerBodyDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
            var playerHeadDims = state.AssetMeasurer.Measure(AssetNames.Player_Head);

            var playerWidth = Math.Max(playerFeetDims.Width, Math.Max(playerBodyDims.Width, playerHeadDims.Width));
            var playerHeight = playerFeetDims.Height + playerBodyDims.Height + playerHeadDims.Height;
            var playerX = playerFeetPos.X;
            var playerY = playerFeetPos.Y - playerBodyDims.Height - playerHeadDims.Height;

            var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);

            (int X, int Y) newCameraPos;

            // if something has requested the camera point at a specific point,
            //    go ahead and honor it.
            if (ExplicitCameraTarget.HasValue)
            {
                newCameraPos = ExplicitCameraTarget.Value;
            }
            else
            {
                newCameraPos =
                    CalculateNewPosition(
                        roomDims.Width,
                        roomDims.Height,
                        playerX,
                        playerY,
                        playerWidth,
                        playerHeight,
                        cameraDims.Width,
                        cameraDims.Height
                    );
            }

            cameraPos.X_SubPixel = newCameraPos.X * PositionComponent.SUBPIXELS_PER_PIXEL;
            cameraPos.Y_SubPixel = newCameraPos.Y * PositionComponent.SUBPIXELS_PER_PIXEL;
        }

        internal static (int X, int Y) CalculateNewPosition(
            int roomWidth,
            int roomHeight,
            int playerX,
            int playerY,
            int playerWidth,
            int playerHeight,
            int cameraWidth,
            int cameraHeight
        )
        {
            var playerCenterX = playerX + playerWidth / 2;
            var playerCenterY = playerY + playerHeight / 2;

            // if the camera is _bigger_ than the room, we should act like it's
            //    the same size as the room so we don't position the camera
            //    outside of the room

            cameraWidth = Math.Min(cameraWidth, roomWidth);
            cameraHeight = Math.Min(cameraHeight, roomHeight);

            // diagram of camera in room
            //
            //      /-----roomWidth------\
            //      |                    |
            //   /- ----------------------
            //   |  |                    |
            // r |  |      -------       |
            // H |  |    c |     |       |
            // e |  |    H |  P  |       | < playerCenterY
            // i |  |    e |     |       |
            // h |  |      -------       |
            // t |  |       cWidth       |
            //   \- ----------------------
            //                ^ playerCenterX

            var newCameraX = playerCenterX - cameraWidth / 2;
            var newCameraY = playerCenterY - cameraHeight / 2;
            
            // if we go too far towards the upper left, the camera needs to stop
            newCameraX = Math.Max(0, newCameraX);
            newCameraY = Math.Max(0, newCameraY);
            
            // if we go too far towards the lower right, the camera needs to stop
            var furthestRight = roomWidth - cameraWidth;
            var furthestDown = roomHeight - cameraHeight;
            newCameraX = Math.Min(furthestRight, newCameraX);
            newCameraY = Math.Min(furthestDown, newCameraY);

            return (newCameraX, newCameraY);
        }
    }
}