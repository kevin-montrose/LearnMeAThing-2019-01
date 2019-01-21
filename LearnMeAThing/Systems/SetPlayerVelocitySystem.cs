using LearnMeAThing.Components;
using LearnMeAThing.Managers;
using System;

namespace LearnMeAThing.Systems
{
    /// <summary>
    /// Takes player inputs and sets their desired velocity, given... whatever state
    /// 
    /// In the future we can do things like look for boots here.
    /// </summary>
    sealed class SetPlayerVelocitySystem : ASystem<EntityManager.EntitiesWithFlagComponentEnumerable>
    {
        public const int DIAGONAL_SPEED = 48;
        public const int MAX_SPEED = 64;

        public override SystemType Type => SystemType.SetPlayerVelocity;

        public override EntityManager.EntitiesWithFlagComponentEnumerable DesiredEntities(EntityManager manager)
        => manager.EntitiesWith(FlagComponent.Player);

        public override void Update(GameState state, EntityManager.EntitiesWithFlagComponentEnumerable requestedEntities)
        {
            var manager = state.EntityManager;
            foreach(var player in requestedEntities)
            {
                var inputs = manager.GetInputsFor(player);
                if (inputs == null) continue;
                var velocity = manager.GetVelocityFor(player);
                if (velocity == null) continue;

                DetermineMotion(inputs, out var deltaX, out var deltaY);

                var accel = manager.GetAccelerationFor(player);
                if(accel != null)
                {
                    deltaX += accel.DeltaX;
                    deltaY += accel.DeltaY;

                    accel.RemainingTicks--;
                    if(accel.RemainingTicks <= 0)
                    {
                        manager.RemoveComponent(player, accel);
                    }
                }

                velocity.X_SubPixels = deltaX;
                velocity.Y_SubPixels = deltaY;
            }
        }

        private static void DetermineMotion(InputsComponent toRead, out int xChange, out int yChange)
        {
            var movingUp = toRead.Up && !toRead.Down;
            var movingDown = toRead.Down && !toRead.Up;
            var movingLeft = toRead.Left && !toRead.Right;
            var movingRight = toRead.Right && !toRead.Left;

            if (!movingUp && !movingDown && !movingLeft && !movingRight)
            {
                xChange = yChange = 0;
                return;
            }

            if (movingUp)
            {
                if (movingLeft)
                {
                    xChange = -DIAGONAL_SPEED;
                    yChange = -DIAGONAL_SPEED;
                    return;
                }

                if (movingRight)
                {
                    xChange = DIAGONAL_SPEED;
                    yChange = -DIAGONAL_SPEED;
                    return;
                }

                xChange = 0;
                yChange = -MAX_SPEED;
                return;
            }

            if (movingDown)
            {
                if (movingLeft)
                {
                    xChange = -DIAGONAL_SPEED;
                    yChange = DIAGONAL_SPEED;
                    return;
                }

                if (movingRight)
                {
                    xChange = DIAGONAL_SPEED;
                    yChange = DIAGONAL_SPEED;
                    return;
                }

                xChange = 0;
                yChange = MAX_SPEED;
                return;
            }

            if (movingLeft)
            {
                // have already handled up left and down left
                yChange = 0;
                xChange = -MAX_SPEED;
                return;
            }

            if (movingRight)
            {
                // have already handle up right and down right
                yChange = 0;
                xChange = MAX_SPEED;
                return;
            }

            throw new InvalidOperationException("This shouldn't be possible");
        }
    }
}
