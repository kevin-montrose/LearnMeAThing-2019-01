using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Systems;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class ExitSystemTests
    {
        [Theory]
        [InlineData(10_000, 10_000, 0, 500, ExitDirection.West)]
        [InlineData(10_000, 10_000, 9_991, 500, ExitDirection.East)]
        [InlineData(10_000, 10_000, 500, 0, ExitDirection.North)]
        [InlineData(10_000, 10_000, 500, 9_991, ExitDirection.South)]
        public void Scrolling(int roomWidth, int roomHeight, int playerX, int playerY, ExitDirection requested)
        {
            // set everything up
            const int PLAYER_WIDTH = 9;
            const int FEET_HEIGHT = 3;
            const int BODY_HEIGHT = 3;
            const int HEAD_HEIGHT = 3;

            var roomTemp =
                new RoomTemplate(
                    (RoomNames)999_999,
                    null,
                    null,
                    (RoomNames)999_999,
                    (RoomNames)999_999,
                    (RoomNames)999_999,
                    (RoomNames)999_999,
                    roomWidth,
                    roomHeight,
                    default(TileMap),
                    null,
                    null
                );

            var game = new GameState();
            game.Initialize(
                new _RoomManager(
                    (
                        (RoomNames)999_999,
                        roomTemp,
                        roomWidth,
                        roomHeight
                    )
                ),
                new _Input(0),
                new _AssetMeasurer(
                    ((AssetNames)999_999, (roomWidth, roomHeight)),
                    (AssetNames.Player_Feet, (PLAYER_WIDTH, FEET_HEIGHT)),
                    (AssetNames.Player_Body, (PLAYER_WIDTH, BODY_HEIGHT)),
                    (AssetNames.Player_Head, (PLAYER_WIDTH, HEAD_HEIGHT))
                ),
                new _AnimationManager(
                    (AnimationNames.Player_Feet, new AnimationTemplate(AnimationNames.Player_Feet, new[] { AssetNames.Player_Feet }, 0)),
                    (AnimationNames.Player_Body, new AnimationTemplate(AnimationNames.Player_Body, new[] { AssetNames.Player_Body }, 0)),
                    (AnimationNames.Player_Head, new AnimationTemplate(AnimationNames.Player_Head, new[] { AssetNames.Player_Head }, 0))
                ),
                new _IHitMapManager()
            );

            game.CurrentRoom = new Room(roomTemp);

            var pPos = game.EntityManager.GetPositionFor(game.Player_Feet);
            pPos.X_SubPixel = playerX * PositionComponent.SUBPIXELS_PER_PIXEL;
            pPos.Y_SubPixel = playerY * PositionComponent.SUBPIXELS_PER_PIXEL;

            // focus the camera
            var camera = game.CameraSystem;
            camera.Update(game, null);

            // trigger exit system once
            var exit = game.ExitSystem;
            exit.RequestExit(requested);
            exit.Update(game, null);

            // make a note of where we expect to be
            var expectedPlayerEnd = exit.FinalPlayerPos;
            var expectedCameraEnd = exit.FinalCameraPos;

            while (exit.IsTransitioning)
            {
                exit.Update(game, null);
                camera.Update(game, null);
            }

            var finalPlayerPos = game.EntityManager.GetPositionFor(game.Player_Feet);
            var finalCameraPos = game.EntityManager.GetPositionFor(game.Camera);

            Assert.Equal((int)expectedPlayerEnd.X, finalPlayerPos.X);
            Assert.Equal((int)expectedPlayerEnd.Y, finalPlayerPos.Y);

            Assert.Equal((int)expectedCameraEnd.X, finalCameraPos.X);
            Assert.Equal((int)expectedCameraEnd.Y, finalCameraPos.Y);

            Assert.Null(camera.ExplicitCameraTarget);
        }

        [Theory]
        [InlineData(10_000, 10_000, 0, 500, ExitDirection.West, "FCP: X=8,700, Y=98, FPP: X=9,990, Y=500, IPP: X=10,000, Y=500, IT: True, IT: X=10,000, Y=98, PCP: X=0, Y=98, PR: 999999, S1: West, S2: 1")]
        [InlineData(10_000, 10_000, 9_991, 500, ExitDirection.East, "FCP: X=0, Y=98, FPP: X=1, Y=500, IPP: X=-(9), Y=500, IT: True, IT: X=-(1,300), Y=98, PCP: X=8,700, Y=98, PR: 999999, S1: East, S2: 1")]
        [InlineData(10_000, 10_000, 500, 0, ExitDirection.North, "FCP: X=0, Y=9,200, FPP: X=500, Y=9,990, IPP: X=500, Y=10,000, IT: True, IT: X=0, Y=10,000, PCP: X=0, Y=0, PR: 999999, S1: North, S2: 1")]
        [InlineData(10_000, 10_000, 500, 9_991, ExitDirection.South, "FCP: X=0, Y=0, FPP: X=500, Y=1, IPP: X=500, Y=-(9), IT: True, IT: X=0, Y=-(800), PCP: X=0, Y=9,200, PR: 999999, S1: South, S2: 1")]
        public void Data(int roomWidth, int roomHeight, int playerX, int playerY, ExitDirection requested, string expected)
        {
            // set everything up
            const int PLAYER_WIDTH = 9;
            const int FEET_HEIGHT = 3;
            const int BODY_HEIGHT = 3;
            const int HEAD_HEIGHT = 3;

            var roomTemp = 
                new RoomTemplate(
                    (RoomNames)999_999,
                    null,
                    null,
                    (RoomNames)999_999,
                    (RoomNames)999_999,
                    (RoomNames)999_999,
                    (RoomNames)999_999,
                    roomWidth,
                    roomHeight,
                    default(TileMap),
                    null,
                    null
                );

            var game = new GameState();
            game.Initialize(
                new _RoomManager(
                    (
                        (RoomNames)999_999,
                        roomTemp,
                        roomWidth, 
                        roomHeight
                    )
                ),
                new _Input(0),
                new _AssetMeasurer(
                    ((AssetNames)999_999, (roomWidth, roomHeight)),
                    (AssetNames.Player_Feet, (PLAYER_WIDTH, FEET_HEIGHT)),
                    (AssetNames.Player_Body, (PLAYER_WIDTH, BODY_HEIGHT)),
                    (AssetNames.Player_Head, (PLAYER_WIDTH, HEAD_HEIGHT))
                ),
                new _AnimationManager(
                    (AnimationNames.Player_Feet, new AnimationTemplate(AnimationNames.Player_Feet, new[] { AssetNames.Player_Feet }, 0)),
                    (AnimationNames.Player_Body, new AnimationTemplate(AnimationNames.Player_Body, new[] { AssetNames.Player_Body }, 0)),
                    (AnimationNames.Player_Head, new AnimationTemplate(AnimationNames.Player_Head, new[] { AssetNames.Player_Head }, 0))
                ),
                new _IHitMapManager()
            );

            game.CurrentRoom = new Room(roomTemp);

            var pPos = game.EntityManager.GetPositionFor(game.Player_Feet);
            pPos.X_SubPixel = playerX * PositionComponent.SUBPIXELS_PER_PIXEL;
            pPos.Y_SubPixel = playerY * PositionComponent.SUBPIXELS_PER_PIXEL;

            // focus the camera
            game.CameraSystem.Update(game, null);

            // trigger exit system
            var exit = game.ExitSystem;

            exit.RequestExit(requested);
            exit.Update(game, null);
            
            var val =
                string.Join(
                    ", ",
                    $"FCP: {exit.FinalCameraPos}",
                    $"FPP: {exit.FinalPlayerPos}",
                    $"IPP: {exit.InitialPlayerPos}",
                    $"IT: {exit.IsTransitioning}",
                    $"IT: {exit.NewCameraPos}",
                    $"PCP: {exit.PreviousCameraPos}",
                    $"PR: {exit.PreviousRoom}",
                    $"S1: {exit.Scrolling}",
                    $"S2: {exit.Step}"
                );

            Assert.Equal(expected, val);
        }
    }
}
