namespace LearnMeAThing.Components
{
    sealed class SwordComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Sword;
        
        public void Initialize()
        {
            // just here for symmetry with the other components
        }

        public override string ToString() => $"{nameof(Type)}: {Type}";
    }
}
