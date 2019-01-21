using LearnMeAThing.Managers;

namespace LearnMeAThing.Systems
{
    public enum SystemType
    {
        NONE = 0,

        Input,
        Camera,
        SetPlayerVelocity,
        UpdatePositions,
        Animation,
        CollisionDetection,
        Triangle,
        PlayerState,
        Sword,
        Bush,
        Cleanup,
        Exit,
        SwordKnight
    }

    abstract class ASystem<T>
    {
        public abstract SystemType Type { get; }

        public abstract T DesiredEntities(EntityManager manager);

        public abstract void Update(GameState state, T requestedEntities);
    }
}
