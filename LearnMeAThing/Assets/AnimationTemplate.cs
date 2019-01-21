namespace LearnMeAThing.Assets
{
    readonly struct AnimationTemplate
    {
        private readonly AnimationNames _Name;
        public AnimationNames Name => _Name;

        private readonly AssetNames[] _Frames;
        public AssetNames[] Frames => _Frames;
        
        private readonly int _StepAfter;
        public int StepAfter => _StepAfter;

        public AnimationTemplate(AnimationNames name, AssetNames[] frames, int stepAfter)
        {
            _Name = name;
            _Frames = frames;
            _StepAfter = stepAfter;
        }
    }
}
