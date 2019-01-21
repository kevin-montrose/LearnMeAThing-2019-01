namespace LearnMeAThing.Components
{
    sealed class PositionComponent : AStatefulComponent
    {
        public const int SUBPIXELS_PER_PIXEL = 16;

        public override ComponentType Type => ComponentType.Position;
        
        public int X => X_SubPixel / SUBPIXELS_PER_PIXEL;
        public int Y => Y_SubPixel / SUBPIXELS_PER_PIXEL;

        public int X_SubPixel { get; set; }
        public int Y_SubPixel { get; set; }
        
        public void Initialize(int initialX, int initialY)
        {
            X_SubPixel = initialX * SUBPIXELS_PER_PIXEL;
            Y_SubPixel = initialY * SUBPIXELS_PER_PIXEL;
        }
        
        public override string ToString() => $"{Type}: {nameof(X)}={X}, {nameof(Y)}={Y}, {nameof(X_SubPixel)}={X_SubPixel}, {nameof(Y_SubPixel)}={Y_SubPixel}";
    }
}
