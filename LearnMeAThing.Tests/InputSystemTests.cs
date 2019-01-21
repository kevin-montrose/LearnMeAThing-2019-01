using LearnMeAThing.Components;
using LearnMeAThing.Utilities;
using System.Collections.Generic;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class InputSystemTests
    {
        [Theory]
        [InlineData(PressedKeys.Up, "1,0,0,0")]
        [InlineData(PressedKeys.Up | PressedKeys.Down, "1,1,0,0")]
        [InlineData(PressedKeys.Up | PressedKeys.Left, "1,0,1,0")]
        [InlineData(PressedKeys.Up | PressedKeys.Right, "1,0,0,1")]
        [InlineData(PressedKeys.Up | PressedKeys.Down | PressedKeys.Left, "1,1,1,0")]
        [InlineData(PressedKeys.Up | PressedKeys.Down | PressedKeys.Right, "1,1,0,1")]
        [InlineData(PressedKeys.Up | PressedKeys.Down | PressedKeys.Left | PressedKeys.Right, "1,1,1,1")]
        [InlineData(PressedKeys.Down, "0,1,0,0")]
        [InlineData(PressedKeys.Down | PressedKeys.Left, "0,1,1,0")]
        [InlineData(PressedKeys.Down | PressedKeys.Right, "0,1,0,1")]
        [InlineData(PressedKeys.Down | PressedKeys.Left | PressedKeys.Right, "0,1,1,1")]
        [InlineData(PressedKeys.Left, "0,0,1,0")]
        [InlineData(PressedKeys.Left | PressedKeys.Right, "0,0,1,1")]
        [InlineData(PressedKeys.Right, "0,0,0,1")]
        public void Updates(PressedKeys keys, string expected)
        {
            var game = new GameState();
            game.Initialize(new _RoomManager(),  new _Input(keys), new _AssetMeasurer(), new _AnimationManager(), new _IHitMapManager());
            game.RunSystem(game.InputSystem);

            var player = game.Player_Feet;
            var input = game.EntityManager.GetInputsFor(player);
            Assert.NotNull(input);
            
            var shouldBe = (input.Up ? 1 : 0) + "," + (input.Down ? 1 : 0) + "," + (input.Left ? 1 : 0) + "," + (input.Right ? 1 : 0);

            Assert.Equal(expected, shouldBe);
        }
    }
}
