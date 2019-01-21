using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using System;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class CameraSystemTests
    {
        [Fact]
        public void CentersOnPlayer()
        {
            const int ROOM_WIDTH = 10000;
            const int ROOM_HEIGHT = 20000;
            const int PLAYER_WIDTH = 16;
            const int PLAYER_FEET_HEIGHT = 8;
            const int PLAYER_BODY_HEIGHT = 8;
            const int PLAYER_HEAD_HEIGHT = 8;
            const int PLAYER_HEIGHT = PLAYER_FEET_HEIGHT + PLAYER_BODY_HEIGHT + PLAYER_HEAD_HEIGHT;

            var frameRenderer = new FrameStateManager(1_000, 20);

            var game = new GameState();
            game.Initialize(
                new _RoomManager(
                    ((RoomNames)999_999, default(RoomTemplate), ROOM_WIDTH, ROOM_HEIGHT)
                ),
                new _Input(0),
                new _AssetMeasurer(
                    ((AssetNames)999_999, (ROOM_WIDTH, ROOM_HEIGHT)),
                    (AssetNames.Player_Feet, (PLAYER_WIDTH, PLAYER_FEET_HEIGHT)),
                    (AssetNames.Player_Body, (PLAYER_WIDTH, PLAYER_BODY_HEIGHT)),
                    (AssetNames.Player_Head, (PLAYER_WIDTH, PLAYER_HEAD_HEIGHT))
                ),
                new _AnimationManager(
                    (AnimationNames.Player_Feet, new AnimationTemplate(AnimationNames.Player_Feet, new[] { AssetNames.Player_Feet }, 0)),
                    (AnimationNames.Player_Body, new AnimationTemplate(AnimationNames.Player_Body, new[] { AssetNames.Player_Body }, 0)),
                    (AnimationNames.Player_Head, new AnimationTemplate(AnimationNames.Player_Head, new[] { AssetNames.Player_Head }, 0))
                ),
                new _IHitMapManager()
            );
            game.CurrentRoom = new Room(new RoomTemplate((RoomNames)999_999, null, null, null, null, null, null, ROOM_WIDTH, ROOM_HEIGHT, default, null, null));

            var playerPos = game.EntityManager.GetPositionFor(game.Player_Feet);
            Assert.NotNull(playerPos);

            // top left
            {
                playerPos.X_SubPixel = 0;
                playerPos.Y_SubPixel = (0 + PLAYER_BODY_HEIGHT +PLAYER_HEAD_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // top right
            {
                playerPos.X_SubPixel = (ROOM_WIDTH - PLAYER_WIDTH) * PositionComponent.SUBPIXELS_PER_PIXEL;
                playerPos.Y_SubPixel = (0 + PLAYER_BODY_HEIGHT + PLAYER_HEAD_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL; // account for head and body;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(ROOM_WIDTH - GameState.WIDTH_HACK, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // bottom left
            {
                playerPos.X_SubPixel = 0;
                playerPos.Y_SubPixel = (ROOM_HEIGHT - PLAYER_HEIGHT + PLAYER_BODY_HEIGHT + PLAYER_HEAD_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(ROOM_HEIGHT - GameState.HEIGHT_HACK, frame.BackgroundOffsetY);
                }
            }

            // bottom right
            {
                playerPos.X_SubPixel = (ROOM_WIDTH - PLAYER_WIDTH) * PositionComponent.SUBPIXELS_PER_PIXEL;
                playerPos.Y_SubPixel = (ROOM_HEIGHT - PLAYER_HEIGHT + PLAYER_BODY_HEIGHT + PLAYER_HEAD_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(ROOM_WIDTH - GameState.WIDTH_HACK, frame.BackgroundOffsetX);
                    Assert.Equal(ROOM_HEIGHT - GameState.HEIGHT_HACK, frame.BackgroundOffsetY);
                }
            }

            // in the interior
            {
                var rand = new Random();
                for (var i = 0; i < 1000; i++)
                {
                    var x = ROOM_WIDTH / 2 + rand.Next(ROOM_WIDTH / 4);
                    var y = ROOM_HEIGHT / 2 + rand.Next(ROOM_HEIGHT / 4);

                    playerPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
                    playerPos.Y_SubPixel = (y + PLAYER_BODY_HEIGHT + PLAYER_HEAD_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                    game.RunSystem(game.CameraSystem);

                    using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                    {
                        var expectedX = playerPos.X + PLAYER_WIDTH / 2 - GameState.WIDTH_HACK / 2;
                        var expectedY = (playerPos.Y - PLAYER_BODY_HEIGHT - PLAYER_HEAD_HEIGHT) + PLAYER_HEIGHT / 2 - GameState.HEIGHT_HACK / 2;

                        Assert.Equal(expectedX, frame.BackgroundOffsetX);
                        Assert.Equal(expectedY, frame.BackgroundOffsetY);
                    }
                }
            }
        }

        [Fact]
        public void ExactSizeRoom()
        {
            const int ROOM_WIDTH = GameState.WIDTH_HACK;
            const int ROOM_HEIGHT = GameState.HEIGHT_HACK;
            const int PLAYER_WIDTH = 16;
            const int PLAYER_HEIGHT = 24;

            var frameRenderer = new FrameStateManager(1_000, 20);

            var game = new GameState();
            game.Initialize(
                new _RoomManager(
                    ((RoomNames)999_999, default(RoomTemplate), ROOM_WIDTH, ROOM_HEIGHT)
                ),
                new _Input(0),
                new _AssetMeasurer(
                    ((AssetNames)999_999, (ROOM_WIDTH, ROOM_HEIGHT)),
                    (AssetNames.Player_Feet, (PLAYER_WIDTH, PLAYER_HEIGHT / 3)),
                    (AssetNames.Player_Body, (PLAYER_WIDTH, PLAYER_HEIGHT / 3)),
                    (AssetNames.Player_Head, (PLAYER_WIDTH, PLAYER_HEIGHT / 3))
                ),
                new _AnimationManager(
                    (AnimationNames.Player_Feet, new AnimationTemplate(AnimationNames.Player_Feet, new[] { AssetNames.Player_Feet }, 0)),
                    (AnimationNames.Player_Body, new AnimationTemplate(AnimationNames.Player_Body, new[] { AssetNames.Player_Body }, 0)),
                    (AnimationNames.Player_Head, new AnimationTemplate(AnimationNames.Player_Head, new[] { AssetNames.Player_Head }, 0))
                ),
                new _IHitMapManager()
            );
            game.CurrentRoom = new Room(new RoomTemplate((RoomNames)999_999, null, null, null, null, null, null, ROOM_WIDTH, ROOM_HEIGHT, default, null, null));

            var playerPos = game.EntityManager.GetPositionFor(game.Player_Feet);
            Assert.NotNull(playerPos);

            // top left
            {
                playerPos.X_SubPixel = 0;
                playerPos.Y_SubPixel = 0;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // top right
            {
                playerPos.X_SubPixel = (ROOM_WIDTH - PLAYER_WIDTH) * PositionComponent.SUBPIXELS_PER_PIXEL;
                playerPos.Y_SubPixel = 0;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // bottom left
            {
                playerPos.X_SubPixel = 0;
                playerPos.Y_SubPixel = (ROOM_HEIGHT - PLAYER_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // bottom right
            {
                playerPos.X_SubPixel = (ROOM_WIDTH - PLAYER_WIDTH) * PositionComponent.SUBPIXELS_PER_PIXEL;
                playerPos.Y_SubPixel = (ROOM_HEIGHT - PLAYER_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // in the interior
            {
                var rand = new Random();
                for (var i = 0; i < 1000; i++)
                {
                    var x = ROOM_WIDTH / 2 + rand.Next(ROOM_WIDTH / 4);
                    var y = ROOM_HEIGHT / 2 + rand.Next(ROOM_HEIGHT / 4);

                    playerPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
                    playerPos.Y_SubPixel = y * PositionComponent.SUBPIXELS_PER_PIXEL;
                    game.RunSystem(game.CameraSystem);

                    using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                    {
                        Assert.Equal(0, frame.BackgroundOffsetX);
                        Assert.Equal(0, frame.BackgroundOffsetY);
                    }
                }
            }
        }

        [Fact]
        public void SmallRoom()
        {
            const int ROOM_WIDTH = 100;
            const int ROOM_HEIGHT = 100;
            const int PLAYER_WIDTH = 16;
            const int PLAYER_HEIGHT = 24;

            var frameRenderer = new FrameStateManager(1_000, 20);

            var game = new GameState();
            game.Initialize(
                new _RoomManager(
                    ((RoomNames)999_999, default(RoomTemplate), ROOM_WIDTH, ROOM_HEIGHT)
                ),
                new _Input(0),
                new _AssetMeasurer(
                    ((AssetNames)999_999, (ROOM_WIDTH, ROOM_HEIGHT)),
                    (AssetNames.Player_Feet, (PLAYER_WIDTH, PLAYER_HEIGHT / 3)),
                    (AssetNames.Player_Body, (PLAYER_WIDTH, PLAYER_HEIGHT / 3)),
                    (AssetNames.Player_Head, (PLAYER_WIDTH, PLAYER_HEIGHT / 3))
                ),
                new _AnimationManager(
                    (AnimationNames.Player_Feet, new AnimationTemplate(AnimationNames.Player_Feet, new[] { AssetNames.Player_Feet }, 0)),
                    (AnimationNames.Player_Body, new AnimationTemplate(AnimationNames.Player_Body, new[] { AssetNames.Player_Body }, 0)),
                    (AnimationNames.Player_Head, new AnimationTemplate(AnimationNames.Player_Head, new[] { AssetNames.Player_Head }, 0))
                ),
                new _IHitMapManager()
            );
            game.CurrentRoom = new Room(new RoomTemplate((RoomNames)999_999, null, null, null, null, null, null, ROOM_WIDTH, ROOM_HEIGHT, default, null, null));

            var playerPos = game.EntityManager.GetPositionFor(game.Player_Feet);
            Assert.NotNull(playerPos);

            // top left
            {
                playerPos.X_SubPixel = 0;
                playerPos.Y_SubPixel = 0;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // top right
            {
                playerPos.X_SubPixel = (ROOM_WIDTH - PLAYER_WIDTH) * PositionComponent.SUBPIXELS_PER_PIXEL;
                playerPos.Y_SubPixel = 0;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // bottom left
            {
                playerPos.X_SubPixel = 0;
                playerPos.Y_SubPixel = (ROOM_HEIGHT - PLAYER_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // bottom right
            {
                playerPos.X_SubPixel = (ROOM_WIDTH - PLAYER_WIDTH) * PositionComponent.SUBPIXELS_PER_PIXEL;
                playerPos.Y_SubPixel = (ROOM_HEIGHT - PLAYER_HEIGHT) * PositionComponent.SUBPIXELS_PER_PIXEL;
                game.RunSystem(game.CameraSystem);

                using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                {
                    Assert.Equal(0, frame.BackgroundOffsetX);
                    Assert.Equal(0, frame.BackgroundOffsetY);
                }
            }

            // in the interior
            {
                var rand = new Random();
                for (var i = 0; i < 1000; i++)
                {
                    var x = ROOM_WIDTH / 2 + rand.Next(ROOM_WIDTH / 4);
                    var y = ROOM_HEIGHT / 2 + rand.Next(ROOM_HEIGHT / 4);

                    playerPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
                    playerPos.Y_SubPixel = y * PositionComponent.SUBPIXELS_PER_PIXEL;
                    game.RunSystem(game.CameraSystem);

                    using (var frame = frameRenderer.CaptureFrom(game, int.MaxValue, int.MaxValue, 0))
                    {
                        Assert.Equal(0, frame.BackgroundOffsetX);
                        Assert.Equal(0, frame.BackgroundOffsetY);
                    }
                }
            }
        }
    }
}