using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Components
{
    sealed class AccelerationComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Acceleration;

        public int DeltaX { get; set; }
        public int DeltaY { get; set; }
        public int RemainingTicks { get; set; }
        
        public void Initialize(int initialX, int initialY, int ticks)
        {
            DeltaX = initialX;
            DeltaY = initialY;
            RemainingTicks = ticks;
        }

        public void Push(Vector step, int overTicks)
        {
            var baseX = DeltaX * RemainingTicks;
            var baseY = DeltaY * RemainingTicks;

            var addedX = (int)(step.DeltaX * overTicks);
            var addedY = (int)(step.DeltaY * overTicks);

            var totalX = baseX + addedX;
            var totalY = baseY + addedY;

            var finalTicks = Math.Max(RemainingTicks, overTicks);
            var finalX = totalX / finalTicks;
            var finalY = totalY / finalTicks;

            DeltaX = finalX;
            DeltaY = finalY;
            RemainingTicks = finalTicks;
        }

        public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(DeltaX)}: {DeltaX:N0}, {nameof(DeltaY)}: {DeltaY:N0}, {nameof(RemainingTicks)}: {RemainingTicks:N0}";
    }
}
