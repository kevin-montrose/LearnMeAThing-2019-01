
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Components
{
    enum SwordKnightFacing
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum SwordKnightWalking
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum SwordKnightSearching
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum SwordKnightChasing
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    sealed class SwordKnightStateComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.SwordKnightState;

        public Point InitialPosition { get; private set; }

        public SwordKnightFacing? FacingDirection { get; private set; }

        public SwordKnightWalking? WalkingDirection { get; private set; }

        public SwordKnightSearching? SearchingDirection { get; private set; }

        public bool IsChasing { get; private set; }
        public SwordKnightChasing? ChasingDirection { get; private set; }

        public bool IsDieing { get; private set; }

        public int Steps { get; set; }
        
        public void Initialize(SwordKnightFacing initialDir, int initialX, int initialY)
        {
            FacingDirection = initialDir;
            InitialPosition = new Point(initialX, initialY);

            WalkingDirection = null;
            SearchingDirection = null;
            SearchingDirection = null;
            IsChasing = false;
            ChasingDirection = null;
            IsDieing = false;
        }
        
        public void SetFacing(SwordKnightFacing facing)
        {
            FacingDirection = facing;
            WalkingDirection = null;
            SearchingDirection = null;
            IsChasing = false;
            ChasingDirection = null;
            IsDieing = false;
        }

        public void SetWalking(SwordKnightWalking walking)
        {
            FacingDirection = null;
            WalkingDirection = walking;
            SearchingDirection = null;
            IsChasing = false;
            ChasingDirection = null;
            IsDieing = false;
        }

        public void SetSearching(SwordKnightFacing facing, SwordKnightSearching searching)
        {
            FacingDirection = facing;
            WalkingDirection = null;
            SearchingDirection = searching;
            IsChasing = false;
            ChasingDirection = null;
            IsDieing = false;
        }

        public void SetChasing()
        {
            FacingDirection = null;
            WalkingDirection = null;
            SearchingDirection = null;
            IsChasing = true;
            ChasingDirection = null;    // will be set later
            IsDieing = false;
        }

        public void SetChasingDirection(SwordKnightChasing chasing)
        {
            ChasingDirection = chasing;
        }

        public void Die()
        {
            Steps = 0;
            FacingDirection = null;
            WalkingDirection = null;
            SearchingDirection = null;
            IsChasing = false;
            ChasingDirection = null;
            IsDieing = true;
        }

        public override string ToString() => $"{nameof(Type)}: {Type}";
    }
}
