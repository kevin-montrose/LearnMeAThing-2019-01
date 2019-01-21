using Microsoft.Xna.Framework.Input;
using System;

namespace LearnMeAThing.Utilities
{
    [Flags]
    public enum PressedKeys
    {
        NONE = 0,

        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,

        SwingSword = 16
    }

    /// <summary>
    /// An interface to let us mock inputs for testing
    /// </summary>
    interface IHardwareInput
    {
        PressedKeys GetPressed(int playerId);
    }

    /// <summary>
    /// An adapter for taking MonoGame gamepads and keyboard inputs, and determing what keys are _currently_ pressed
    /// </summary>
    sealed class MonoGameHardwareInputAdapter : IHardwareInput
    {
        // it only makes sense for there ever to be one of these
        public static readonly IHardwareInput Instance = new MonoGameHardwareInputAdapter();

        private MonoGameHardwareInputAdapter() { }

        public PressedKeys GetPressed(int playerId)
        {
            var ret = PressedKeys.NONE;

            var pad = GamePad.GetState(playerId - 1); // first entity Id is 1

            // using a game pad?
            if (pad.IsConnected)
            {
                if (pad.DPad.Up == ButtonState.Pressed) ret |= PressedKeys.Up;
                if (pad.DPad.Down == ButtonState.Pressed) ret |= PressedKeys.Down;
                if (pad.DPad.Left == ButtonState.Pressed) ret |= PressedKeys.Left;
                if (pad.DPad.Right == ButtonState.Pressed) ret |= PressedKeys.Right;
                if (pad.Buttons.B == ButtonState.Pressed) ret |= PressedKeys.SwingSword;

                return ret;
            }

            // if not, the first player gets the keyboard
            if (playerId == 1)
            {
                var keyboard = Keyboard.GetState();

                if (keyboard.IsKeyDown(Keys.Up)) ret |= PressedKeys.Up;
                if (keyboard.IsKeyDown(Keys.Down)) ret |= PressedKeys.Down;
                if (keyboard.IsKeyDown(Keys.Left)) ret |= PressedKeys.Left;
                if (keyboard.IsKeyDown(Keys.Right)) ret |= PressedKeys.Right;
                if (keyboard.IsKeyDown(Keys.Space)) ret |= PressedKeys.SwingSword;
            }

            // everybody else gets NOOOOOOOTTTTTHING
            return ret;
        }
    }
}
