namespace LearnMeAThing.Components
{
    sealed class InputsComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Inputs;

        // last update
        public bool PreviousLeft { get; private set; }
        public bool PreviousRight { get; private set; }
        public bool PreviousUp { get; private set; }
        public bool PreviousDown { get; private set; }
        public bool PreviousSwingSword { get; private set; }

        // current
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool SwingSword { get; set; }
        
        public void Initialize()
        {
            PreviousLeft = PreviousRight = PreviousUp = PreviousDown = PreviousSwingSword = Left = Right = Up = Down = SwingSword = false;
        }

        /// <summary>
        /// Indicate that we're about to update state, so move everything
        ///   old to the PreviousX properties and clear the current one
        /// </summary>
        public void MoveToPrevious()
        {
            PreviousLeft = Left;
            PreviousRight = Right;
            PreviousUp = Up;
            PreviousDown = Down;
            PreviousSwingSword = SwingSword;

            Left = Right = Up = Down = SwingSword = false;
        }

        public override string ToString() => $"{Type}: {nameof(Left)}={Left}, {nameof(Right)}={Right}, {nameof(Up)}={Up}, {nameof(Down)}={Down}";
    }
}
