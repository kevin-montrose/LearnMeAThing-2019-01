using LearnMeAThing.Managers;

namespace LearnMeAThing.Systems
{
    /// <summary>
    /// System responsible for book keeping and cleanup tasks.
    /// 
    /// Modeled as a system so we get profiling and whatnot.
    /// 
    /// Right now the only cleanup task is compacting entities.
    /// </summary>
    sealed class CleanupSystem : ASystem<object>
    {
        public override SystemType Type => SystemType.Cleanup;

        private readonly int CollectEvery;
        private readonly int FragmentationRatio;

        private int Iteration;

        public CleanupSystem(int collectEvery, int fragmentationRatio)
        {
            CollectEvery = collectEvery;
            FragmentationRatio = fragmentationRatio;
            Iteration = 0;
        }

        public override object DesiredEntities(EntityManager manager)
        => null;

        public override void Update(GameState state, object ignored)
        {
            Iteration++;
            if(Iteration == CollectEvery || state.EntityManager.FragmentationRatio >= FragmentationRatio || state.EntityManager.IsFull)
            {
                state.CompactEntities();
                Iteration = 0;
            }
        }
    }
}
