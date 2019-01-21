using LearnMeAThing.Assets;

namespace LearnMeAThing.Components
{
    /// <summary>
    /// Data backing a basic door.
    /// </summary>
    sealed class DoorComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Door;

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
        
        public void Initialize(RoomNames targetRoom, int targetX, int targetY)
        {
            TargetRoom = targetRoom;
            TargetX = targetX;
            TargetY = targetY;
        }

        public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(TargetRoom)}: {TargetRoom}, {nameof(TargetX)}: {TargetX:N0}, {nameof(TargetY)}: {TargetY:N0}";
    }
}
