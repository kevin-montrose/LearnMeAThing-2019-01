using LearnMeAThing.Assets;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Components
{
    enum PlayerWalking
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum PlayerStanding
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum PlayerPushing
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum PlayerFacing
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    enum PlayerSwinging
    {
        NONE = 0,

        North,
        South,
        East,
        West
    }

    sealed class PlayerStateComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.PlayerState;

        /// <summary>
        /// Direction the player is currently walking, if they are walking.
        /// </summary>
        public PlayerWalking? WalkingDirection { get; set; }
        /// <summary>
        /// Direction the player is currently facing, if they are standing.
        /// </summary>
        public PlayerStanding? StandingDirection { get; set; }
        /// <summary>
        /// Direction the player is currently pushing, if they are pushing.
        /// </summary>
        public PlayerPushing? PushingDirection { get; set; }
        /// <summary>
        /// The direction the player is currently swinging their sword, if they are
        ///    swinging their sword.
        /// </summary>
        public PlayerSwinging? SwingingDirection { get; set; }
        /// <summary>
        /// The last entity the player collided with.
        /// </summary>
        public Entity? CollidedWith { get; set; }
        /// <summary>
        /// The last entity the player was pushed out of contact with.
        /// </summary>
        public Entity? PushedFrom { get; set; }
        
        /// <summary>
        /// The player hit (or was hit) and needs to recoil in this direction.
        /// </summary>
        public Vector? RecoilAlong { get; set; }

        public void Initialize(PlayerStanding initialStandingDir)
        {
            WalkingDirection = null;
            StandingDirection = initialStandingDir;
            PushingDirection = null;
            SwingingDirection = null;
            CollidedWith = null;
            PushedFrom = null;
        }

        public void SetStandingFacing(PlayerStanding dir)
        {
            CollidedWith = null;
            PushedFrom = null;
            WalkingDirection = null;
            PushingDirection = null;
            SwingingDirection = null;

            StandingDirection = dir;
        }

        public void SetCollidedWith(Entity? with)
        {
            CollidedWith = with;
        }

        public void SetPushedFrom(Entity? with)
        {
            PushedFrom = with;
        }

        public void Swing(PlayerSwinging dir)
        {
            StandingDirection = null;
            WalkingDirection = null;
            PushingDirection = null;

            SwingingDirection = dir;
        }

        public void StopSwing()
        {
            var dir = GetFacingDirection();
            if (dir == null) return;

            switch (dir.Value)
            {
                case PlayerFacing.East: StandingDirection = PlayerStanding.East; break;
                case PlayerFacing.West: StandingDirection = PlayerStanding.West; break;
                case PlayerFacing.South: StandingDirection = PlayerStanding.South; break;
                case PlayerFacing.North: StandingDirection = PlayerStanding.North; break;
                default: return;
            }


            WalkingDirection = null;
            PushingDirection = null;
            SwingingDirection = null;
        }

        public PlayerFacing? GetFacingDirection()
        {
            if (WalkingDirection.HasValue)
            {
                switch (WalkingDirection.Value)
                {
                    case PlayerWalking.North: return PlayerFacing.North;
                    case PlayerWalking.South: return PlayerFacing.South;
                    case PlayerWalking.East: return PlayerFacing.East;
                    case PlayerWalking.West: return PlayerFacing.West;

                    default: return null;
                }
            }

            if (StandingDirection.HasValue)
            {
                switch (StandingDirection.Value)
                {
                    case PlayerStanding.North: return PlayerFacing.North;
                    case PlayerStanding.South: return PlayerFacing.South;
                    case PlayerStanding.East: return PlayerFacing.East;
                    case PlayerStanding.West: return PlayerFacing.West;

                    default: return null;
                }
            }

            if (PushingDirection.HasValue)
            {
                switch (PushingDirection.Value)
                {
                    case PlayerPushing.North: return PlayerFacing.North;
                    case PlayerPushing.South: return PlayerFacing.South;
                    case PlayerPushing.East: return PlayerFacing.East;
                    case PlayerPushing.West: return PlayerFacing.West;

                    default: return null;
                }
            }

            if (SwingingDirection.HasValue)
            {
                switch (SwingingDirection.Value)
                {
                    case PlayerSwinging.North: return PlayerFacing.North;
                    case PlayerSwinging.South: return PlayerFacing.South;
                    case PlayerSwinging.East: return PlayerFacing.East;
                    case PlayerSwinging.West: return PlayerFacing.West;

                    default: return null;
                }
            }

            return null;
        }

        public override string ToString() => $"{Type}; {nameof(WalkingDirection)}: {WalkingDirection}, {nameof(StandingDirection)}: {StandingDirection}";
    }
}
