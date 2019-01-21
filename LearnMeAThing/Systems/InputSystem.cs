using LearnMeAThing.Components;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Systems
{
    /// <summary>
    /// Handles taking input from player(s) and updating the state of their attached InputsComponent.
    /// </summary>
    sealed class InputSystem: ASystem<EntityManager.EntitiesWithFlagComponentEnumerable>
    {
        public override SystemType Type => SystemType.Input;

        IHardwareInput Adapter;

        public InputSystem(IHardwareInput adapter)
        {
            Adapter = adapter;
        }

        public override EntityManager.EntitiesWithFlagComponentEnumerable DesiredEntities(EntityManager manager) => manager.EntitiesWith(FlagComponent.Player);

        public override void Update(GameState state, EntityManager.EntitiesWithFlagComponentEnumerable players)
        {
            var manager = state.EntityManager;

            // even though we only have 1 player, this is written to handle as many as we can fetch
            foreach (var p in players)
            {
                var pressed = Adapter.GetPressed(p.Id);

                var left = (pressed & PressedKeys.Left) != 0;
                var right = (pressed & PressedKeys.Right) != 0;
                var up = (pressed & PressedKeys.Up) != 0;
                var down = (pressed & PressedKeys.Down) != 0;
                var swing = (pressed & PressedKeys.SwingSword) != 0;

                var inputs = manager.GetInputsFor(p);
                if (inputs != null)
                {
                    inputs.MoveToPrevious();
                    inputs.Left = left;
                    inputs.Right = right;
                    inputs.Up = up;
                    inputs.Down = down;
                    inputs.SwingSword = swing;
                }
                else
                {
                    // GLITCH: opportunity here
                }
            }
        }
    }
}
