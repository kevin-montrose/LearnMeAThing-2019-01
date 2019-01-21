namespace LearnMeAThing.Components
{
    sealed class BushComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Bush;

        public bool IsCut { get; set; }

        public bool NeedsCut { get; set; }
        
        public void Initialize()
        {
            IsCut = false;
            NeedsCut = false;
        }
        
        public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(IsCut)}: {IsCut}";
    }
}
