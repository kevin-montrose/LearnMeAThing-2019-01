using LearnMeAThing.Assets;

namespace LearnMeAThing.Components
{
    enum StairDirections
    {
        NONE  = 0,

        Up,
        Down
    }

    /// <summary>
    /// Data backing stairs.
    /// </summary>
    sealed class StairsComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Stairs;

        /// <summary>
        /// What direction the stairs are taking the player.
        /// </summary>
        public StairDirections Direction { get; private set; }
        /// <summary>
        /// Which room to come out of
        /// </summary>
        public RoomNames TargetRoom { get; private set; }
        /// <summary>
        /// The X _TILE_ to come out on
        /// </summary>
        public int TargetX { get; private set; }
        /// <summary>
        /// The Y _TILE_ to come out on
        /// </summary>
        public int TargetY { get; private set; }
        
        public void Initialize(StairDirections dir, RoomNames targetRoom, int targetX, int targetY)
        {
            Direction = dir;
            TargetRoom = targetRoom;
            TargetX = targetX;
            TargetY = targetY;
        }

        public override string ToString()=> $"{nameof(Type)}: {Type}, {nameof(Direction)}: {Direction}, {nameof(TargetRoom)}: {TargetRoom}, {nameof(TargetX)}: {TargetX:N0}, {nameof(TargetY)}: {TargetY:N0}";
    }
}
