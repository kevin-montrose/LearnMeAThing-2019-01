using LearnMeAThing.Utilities;

namespace LearnMeAThing.Components
{
    enum ComponentType
    {
        NONE = 0,

        Position,
        Inputs,
        CollisionListener,
        Velocity,
        Animation,
        PlayerState,
        Sword,
        Acceleration,
        Bush,
        Door,
        Stairs,
        Pit,
        SwordKnightState,
        AssociatedEntity
    }

    abstract class AStatefulComponent: IIntrusiveLinkedListElement
    {
        public abstract ComponentType Type { get; }
        public int? PreviousIndex { get; set; }
        public int? NextIndex { get; set; }

        private int? _Id;
        public int Id => _Id.Value;

        public void AssignId(int id) => _Id = id;
        public void ClearId() => _Id = null;

        public abstract override string ToString();
    }
}
