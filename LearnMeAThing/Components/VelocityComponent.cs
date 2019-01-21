namespace LearnMeAThing.Components
{
    sealed class VelocityComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Velocity;

        public int X_SubPixels { get; set; }
        public int Y_SubPixels { get; set; }
        
        public void Initialize(int xSubPixels, int ySubPixels)
        {
            X_SubPixels = xSubPixels;
            Y_SubPixels = ySubPixels;
        }

        public override string ToString() => $"{Type}: {nameof(X_SubPixels)}={X_SubPixels}, {nameof(Y_SubPixels)}={Y_SubPixels}";
    }
}
