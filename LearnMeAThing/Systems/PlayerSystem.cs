using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Systems
{
    /// <summary>
    /// Actually updates the state of a player, things like:
    ///   - active animations
    ///   - running
    ///   - in bounds
    ///   - head attached to body
    ///   - etc.
    /// </summary>
    sealed class PlayerSystem : ASystem<object>
    {
        private const int RECOIL_SUBPIXELS = 128;
        private const int RECOIL_TICKS = 15;

        public override SystemType Type => SystemType.PlayerState;

        public override object DesiredEntities(EntityManager manager)
        => null;


        private AnimationNames? FeetOveride;
        private AnimationNames? BodyOveride;
        private AnimationNames? HeadOveride;

        private Buffer<FixedPoint> TimeBuffer;

        public PlayerSystem(int maxSimultaneousCollisions)
        {
            TimeBuffer = new Buffer<FixedPoint>(maxSimultaneousCollisions);
        }

        public override void Update(GameState state, object ignored)
        {
            var player = state.Player_Feet;
            var manager = state.EntityManager;
            var playerState = manager.GetPlayerStateFor(player);

            if (state.ExitSystem.IsTransitioning)
            {
                SetAnimations(manager, state.Player_Head, state.Player_Body, state.Player_Feet, playerState);
                KeepBodyAndHeadOnPlayer(state);
            }
            else
            {
                HandleStandingWalkingPushing(state, player, playerState);

                SetAnimations(manager, state.Player_Head, state.Player_Body, state.Player_Feet, playerState);

                KeepBodyAndHeadOnPlayer(state);

                // need to see if we're still pushing next check, so clear this
                playerState.SetPushedFrom(null);
            }
        }


        public void RequestAnimation(AnimationNames? feet, AnimationNames? body, AnimationNames? head)
        {
            FeetOveride = feet;
            BodyOveride = body;
            HeadOveride = head;
        }

        public void ClearAnimationRequest()
        {
            FeetOveride = null;
            BodyOveride = null;
            HeadOveride = null;
        }

        private void SetAnimations(EntityManager manager, Entity playerHead, Entity playerBody, Entity playerFeet, PlayerStateComponent playerState)
        {
            var animHead = manager.GetAnimationFor(playerHead);
            var animBody = manager.GetAnimationFor(playerBody);
            var animFeet = manager.GetAnimationFor(playerFeet);

            var desiredHeadAnim = GetDesiredHeadAnimation(playerState);
            if (desiredHeadAnim.HasValue && animHead.Name != desiredHeadAnim.Value)
            {
                animHead.SwitchTo(desiredHeadAnim.Value);
            }

            var desiredFeetAnim = GetDesiredFeetAnimation(playerState);
            if(desiredFeetAnim.HasValue && animFeet.Name != desiredFeetAnim.Value)
            {
                animFeet.SwitchTo(desiredFeetAnim.Value);
            }

            var desiredBodyAnim = GetDesiredBodyAnimation(playerState);
            if (desiredBodyAnim.HasValue && animBody.Name != desiredBodyAnim.Value)
            {
                animBody.SwitchTo(desiredBodyAnim.Value);
            }
        }

        private AnimationNames? GetDesiredBodyAnimation(PlayerStateComponent playerState)
        {
            if (BodyOveride != null) return BodyOveride.Value;

            if (playerState.StandingDirection.HasValue)
            {
                switch (playerState.StandingDirection.Value)
                {
                    case PlayerStanding.East: return AnimationNames.Player_Body_FacingRight;
                    case PlayerStanding.North: return AnimationNames.Player_Body_FacingTop;
                    case PlayerStanding.South: return AnimationNames.Player_Body_FacingBottom;
                    case PlayerStanding.West: return AnimationNames.Player_Body_FacingLeft;
                }
            }

            if (playerState.WalkingDirection.HasValue)
            {
                switch (playerState.WalkingDirection.Value)
                {
                    case PlayerWalking.East: return AnimationNames.Player_Body_WalkingRight;
                    case PlayerWalking.North: return AnimationNames.Player_Body_WalkingTop;
                    case PlayerWalking.South: return AnimationNames.Player_Body_WalkingBottom;
                    case PlayerWalking.West: return AnimationNames.Player_Body_WalkingLeft;
                }
            }

            if (playerState.PushingDirection.HasValue)
            {
                switch (playerState.PushingDirection.Value)
                {
                    case PlayerPushing.East: return AnimationNames.Player_Body_PushingRight;
                    case PlayerPushing.North: return AnimationNames.Player_Body_PushingTop;
                    case PlayerPushing.South: return AnimationNames.Player_Body_PushingBottom;
                    case PlayerPushing.West: return AnimationNames.Player_Body_PushingLeft;
                }
            }

            if (playerState.SwingingDirection.HasValue)
            {
                switch (playerState.SwingingDirection.Value)
                {
                    case PlayerSwinging.East: return AnimationNames.Player_Body_SwingingRight;
                    case PlayerSwinging.North: return AnimationNames.Player_Body_SwingingTop;
                    case PlayerSwinging.South: return AnimationNames.Player_Body_SwingingBottom;
                    case PlayerSwinging.West: return AnimationNames.Player_Body_SwingingLeft;
                }
            }

            return null;
        }

        private AnimationNames? GetDesiredFeetAnimation(PlayerStateComponent playerState)
        {
            if (FeetOveride != null) return FeetOveride.Value;

            if (playerState.StandingDirection.HasValue)
            {
                switch (playerState.StandingDirection.Value)
                {
                    case PlayerStanding.East: return AnimationNames.Player_Feet_FacingRight;
                    case PlayerStanding.North: return AnimationNames.Player_Feet_FacingTop;
                    case PlayerStanding.South: return AnimationNames.Player_Feet_FacingBottom;
                    case PlayerStanding.West: return AnimationNames.Player_Feet_FacingLeft;
                }
            }

            if (playerState.WalkingDirection.HasValue)
            {
                switch (playerState.WalkingDirection.Value)
                {
                    case PlayerWalking.East: return AnimationNames.Player_Feet_WalkingRight;
                    case PlayerWalking.North: return AnimationNames.Player_Feet_WalkingTop;
                    case PlayerWalking.South: return AnimationNames.Player_Feet_WalkingBottom;
                    case PlayerWalking.West: return AnimationNames.Player_Feet_WalkingLeft;
                }
            }

            if (playerState.PushingDirection.HasValue)
            {
                switch (playerState.PushingDirection.Value)
                {
                    case PlayerPushing.East: return AnimationNames.Player_Feet_PushingRight;
                    case PlayerPushing.North: return AnimationNames.Player_Feet_PushingTop;
                    case PlayerPushing.South: return AnimationNames.Player_Feet_PushingBottom;
                    case PlayerPushing.West: return AnimationNames.Player_Feet_PushingLeft;
                }
            }

            if (playerState.SwingingDirection.HasValue)
            {
                switch (playerState.SwingingDirection.Value)
                {
                    case PlayerSwinging.East: return AnimationNames.Player_Feet_SwingingRight;
                    case PlayerSwinging.North: return AnimationNames.Player_Feet_SwingingTop;
                    case PlayerSwinging.South: return AnimationNames.Player_Feet_SwingingBottom;
                    case PlayerSwinging.West: return AnimationNames.Player_Feet_SwingingLeft;
                }
            }

            return null;
        }

        private AnimationNames? GetDesiredHeadAnimation(PlayerStateComponent playerState)
        {
            if (HeadOveride != null) return HeadOveride.Value;

            if (playerState.StandingDirection.HasValue)
            {
                switch (playerState.StandingDirection.Value)
                {
                    case PlayerStanding.East: return AnimationNames.Player_Head_FacingRight;
                    case PlayerStanding.North: return AnimationNames.Player_Head_FacingTop;
                    case PlayerStanding.South: return AnimationNames.Player_Head_FacingBottom;
                    case PlayerStanding.West: return AnimationNames.Player_Head_FacingLeft;
                }
            }

            if (playerState.WalkingDirection.HasValue)
            {
                switch (playerState.WalkingDirection.Value)
                {
                    case PlayerWalking.East: return AnimationNames.Player_Head_WalkingRight;
                    case PlayerWalking.North: return AnimationNames.Player_Head_WalkingTop;
                    case PlayerWalking.South: return AnimationNames.Player_Head_WalkingBottom;
                    case PlayerWalking.West: return AnimationNames.Player_Head_WalkingLeft;
                }
            }

            if (playerState.PushingDirection.HasValue)
            {
                switch (playerState.PushingDirection.Value)
                {
                    case PlayerPushing.East: return AnimationNames.Player_Head_FacingRight;
                    case PlayerPushing.North: return AnimationNames.Player_Head_FacingTop;
                    case PlayerPushing.South: return AnimationNames.Player_Head_FacingBottom;
                    case PlayerPushing.West: return AnimationNames.Player_Head_FacingLeft;
                }
            }

            return null;
        }

        /// <summary>
        /// Look at input and figure out how the player is trying to move,
        ///   and update playerState accordingly.
        /// </summary>
        private void HandleStandingWalkingPushing(GameState state, Entity player, PlayerStateComponent playerState)
        {
            // hack
            if (playerState.SwingingDirection.HasValue)
            {
                return;
            }
            // end hack

            var manager = state.EntityManager;
            var inputs = manager.GetInputsFor(player);
            if(inputs == null)
            {
                // glitch: ??
                return;
            }

            var tryingToMove = inputs.Left || inputs.Right || inputs.Up || inputs.Down;
            if (tryingToMove)
            {
                // not standing anymore
                playerState.StandingDirection = null;

                if (inputs.Left) playerState.WalkingDirection = PlayerWalking.West;
                if (inputs.Right) playerState.WalkingDirection = PlayerWalking.East;
                if (inputs.Up) playerState.WalkingDirection = PlayerWalking.North;
                if (inputs.Down) playerState.WalkingDirection = PlayerWalking.South;

                if (playerState.CollidedWith != null && playerState.WalkingDirection != null && playerState.PushedFrom != null)
                {
                    // colliding!
                    if (playerState.CollidedWith.Value.Id == playerState.PushedFrom.Value.Id)
                    {
                        switch (playerState.WalkingDirection.Value)
                        {
                            case PlayerWalking.North: playerState.PushingDirection = PlayerPushing.North; break;
                            case PlayerWalking.South: playerState.PushingDirection = PlayerPushing.South; break;
                            case PlayerWalking.East: playerState.PushingDirection = PlayerPushing.East; break;
                            case PlayerWalking.West: playerState.PushingDirection = PlayerPushing.West; break;
                            default:
                                // glitch: ??
                                break;
                        }

                        if (playerState.PushingDirection != null)
                        {
                            // no longer walking
                            playerState.WalkingDirection = null;
                        }
                    }
                }
            }
            else
            {
                // we're standing still, so... stand
                if (playerState.WalkingDirection != null)
                {
                    playerState.StandingDirection = GetStandingDirection(playerState.WalkingDirection.Value);
                    playerState.WalkingDirection = null;
                }

                if(playerState.PushingDirection != null)
                {
                    playerState.StandingDirection = GetStandingDirection(playerState.PushingDirection.Value);
                    playerState.PushingDirection = null;
                }
            }

            // we got hit by something... recoil
            if(playerState.RecoilAlong != null)
            {
                var accel = manager.GetAccelerationFor(state.Player_Feet);
                if (accel == null)
                {
                    var accelRes = manager.CreateAcceleration(0, 0, 0);
                    if (!accelRes.Success)
                    {
                        // glitch: ???
                        return;
                    }
                    accel = accelRes.Value;
                    manager.AddComponent(state.Player_Feet, accel);
                }

                if (accel.DeltaX == 0 && accel.DeltaY == 0)
                {
                    var transition = new Vector(playerState.RecoilAlong.Value.DeltaX * RECOIL_SUBPIXELS, playerState.RecoilAlong.Value.DeltaY * RECOIL_SUBPIXELS);
                    accel.Push(transition, RECOIL_TICKS);
                }

                playerState.RecoilAlong = null;
            }
        }

        private static PlayerStanding GetStandingDirection(PlayerWalking walking)
        {
            switch (walking)
            {
                case PlayerWalking.North: return PlayerStanding.North;
                case PlayerWalking.South: return PlayerStanding.South;
                case PlayerWalking.East: return PlayerStanding.East;
                case PlayerWalking.West: return PlayerStanding.West;

                default:
                    // glitch: ???
                    return PlayerStanding.South;
            }
        }

        private static PlayerStanding GetStandingDirection(PlayerPushing pushing)
        {
            switch (pushing)
            {
                case PlayerPushing.North: return PlayerStanding.North;
                case PlayerPushing.South: return PlayerStanding.South;
                case PlayerPushing.East: return PlayerStanding.East;
                case PlayerPushing.West: return PlayerStanding.West;

                default:
                    // glitch: ???
                    return PlayerStanding.South;
            }
        }

        private void KeepBodyAndHeadOnPlayer(GameState state)
        {
            const int FEET_INSET_DEFAULT_SUBPIXELS = 22 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int HEAD_INSET_DEFAULT_SUBPIXELS = 26 * PositionComponent.SUBPIXELS_PER_PIXEL;

            const int FEET_INSET_LEFT_RIGHT_2_SUBPIXELS = 36 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_LEFT_RIGHT_3_SUBPIXELS = 30 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_LEFT_RIGHT_4_SUBPIXELS = 28 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_LEFT_RIGHT_5_SUBPIXELS = 28 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_LEFT_RIGHT_6_SUBPIXELS = 28 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_PUSHING_LEFT_RIGHT_SUBPIXELS = 32 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_PUSHING_TOP_SUBPIXELS = 8 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_PUSHING_BOTTOM_SUBPIXELS = 40 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int FEET_INSET_SWINGING_RIGHT_SUBPIXELS = 30 * PositionComponent.SUBPIXELS_PER_PIXEL;

            const int HEAD_INSET_PUSH_TOP_SUBPIXELS = 48 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int HEAD_INSET_PUSH_BOTTOM_SUBPIXELS = 30 * PositionComponent.SUBPIXELS_PER_PIXEL;

            var manager = state.EntityManager;

            var feet = state.Player_Feet;
            var body = state.Player_Body;
            var head = state.Player_Head;

            var feetAnim = manager.GetAnimationFor(feet);
            if (feetAnim == null) return;
            var feetPos = manager.GetPositionFor(feet);
            if (feetPos == null) return;

            var bodyAnim = manager.GetAnimationFor(body);
            if (bodyAnim == null) return;
            var bodyPos = manager.GetPositionFor(body);
            if (bodyPos == null) return;

            var headAnim = manager.GetAnimationFor(head);
            if (headAnim == null) return;
            var headPos = manager.GetPositionFor(head);
            if (headPos == null) return;

            var curHeadFrame = headAnim.GetCurrentFrame(state.AnimationManager);
            var curBodyFrame = bodyAnim.GetCurrentFrame(state.AnimationManager);
            var curFeetFrame = feetAnim.GetCurrentFrame(state.AnimationManager);

            int activeFeetInset;
            switch (curFeetFrame)
            {
                case AssetNames.Player_Feet_WalkingLeft2:
                case AssetNames.Player_Feet_WalkingRight2: activeFeetInset = FEET_INSET_LEFT_RIGHT_2_SUBPIXELS; break;

                case AssetNames.Player_Feet_WalkingLeft3:
                case AssetNames.Player_Feet_WalkingRight3: activeFeetInset = FEET_INSET_LEFT_RIGHT_3_SUBPIXELS; break;

                case AssetNames.Player_Feet_WalkingLeft4:
                case AssetNames.Player_Feet_WalkingRight4: activeFeetInset = FEET_INSET_LEFT_RIGHT_4_SUBPIXELS; break;

                case AssetNames.Player_Feet_WalkingLeft5:
                case AssetNames.Player_Feet_WalkingRight5: activeFeetInset = FEET_INSET_LEFT_RIGHT_5_SUBPIXELS; break;

                case AssetNames.Player_Feet_WalkingLeft6:
                case AssetNames.Player_Feet_WalkingRight6: activeFeetInset = FEET_INSET_LEFT_RIGHT_6_SUBPIXELS; break;

                case AssetNames.Player_Feet_PushingLeft:
                case AssetNames.Player_Feet_PushingRight: activeFeetInset = FEET_INSET_PUSHING_LEFT_RIGHT_SUBPIXELS; break;

                case AssetNames.Player_Feet_PushingTop: activeFeetInset = FEET_INSET_PUSHING_TOP_SUBPIXELS; break;

                case AssetNames.Player_Feet_PushingBottom: activeFeetInset = FEET_INSET_PUSHING_BOTTOM_SUBPIXELS; break;

                case AssetNames.Player_Feet_SwingingLeft:
                case AssetNames.Player_Feet_SwingingRight: activeFeetInset = FEET_INSET_SWINGING_RIGHT_SUBPIXELS; break;

                default: activeFeetInset = FEET_INSET_DEFAULT_SUBPIXELS; break;
            }

            int activeHeadInset;
            switch (curBodyFrame)
            {
                case AssetNames.Player_Body_PushingTop: activeHeadInset = HEAD_INSET_PUSH_TOP_SUBPIXELS; break;
                case AssetNames.Player_Body_PushingBottom: activeHeadInset = HEAD_INSET_PUSH_BOTTOM_SUBPIXELS; break;
                default: activeHeadInset = HEAD_INSET_DEFAULT_SUBPIXELS; break;
            }

            var bodyHeightSubPixels = state.AssetMeasurer.Measure(curBodyFrame).Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var bodyX = feetPos.X_SubPixel;
            var bodyY = feetPos.Y_SubPixel - bodyHeightSubPixels + activeFeetInset;
            bodyPos.X_SubPixel = bodyX;
            bodyPos.Y_SubPixel = bodyY;

            var headHeightSubPixels = state.AssetMeasurer.Measure(curHeadFrame).Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var headX = bodyX;
            var headY = bodyY - headHeightSubPixels + activeHeadInset;
            headPos.X_SubPixel = headX;
            headPos.Y_SubPixel = headY;
        }

        /// <summary>
        /// Make sure the player stays in the bounds of the room.
        /// </summary>
        private void KeepPlayerInBounds(GameState state)
        {
            var playerDims = DeterminePlayerDimensions(state);
            if (playerDims == null)
            {
                // glitch: what to do?
                return;
            }

            var playerPosition = DeterminePlayerPosition(state);
            if (playerPosition == null)
            {
                // glitch: probably the same as above
                return;
            }

            var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);
            var roomWidthSubPixels = roomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL;
            var roomHeightSubPixels = roomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var roomXBounds = roomWidthSubPixels - playerDims.Value.Width_SubPixels;
            var roomYBounds = roomHeightSubPixels - playerDims.Value.Height_SubPixels;

            var newXSubPixels = playerPosition.Value.X_SubPixels;
            var newYSubPixels = playerPosition.Value.Y_SubPixels;

            var needsUpdate = false;

            if (newXSubPixels < 0)
            {
                newXSubPixels = 0;
                needsUpdate = true;
            }

            if (newYSubPixels < 0)
            {
                newYSubPixels = 0;
                needsUpdate = true;
            }

            if (newXSubPixels > roomXBounds)
            {
                newXSubPixels = roomXBounds;
                needsUpdate = true;
            }

            if (newYSubPixels > roomYBounds)
            {
                newYSubPixels = roomYBounds;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                SetPlayerPosition(state, newXSubPixels, newYSubPixels);
            }
        }

        /// <summary>
        /// Sets the players current position, overall.
        /// 
        /// This takes in the top left point a player should be position at.
        /// 
        /// Because a player is composed of multiple entities, this isn't as trivial as you'd think.
        /// 
        /// This method only guarantees that the feet of a player (it's "brain") is position correctly.
        /// 
        /// Coordinates should be screen coordinates, in sub pixels.
        /// </summary>
        private void SetPlayerPosition(GameState state, int xSubPixels, int ySubPixels)
        {
            var manager = state.EntityManager;

            var feet = state.Player_Feet;
            var body = state.Player_Body;
            var head = state.Player_Head;

            var feetPos = manager.GetPositionFor(feet);
            if (feetPos == null) return;
            var bodyAnim = manager.GetAnimationFor(body);
            if (bodyAnim == null) return;
            var headAnim = manager.GetAnimationFor(head);
            if (headAnim == null) return;

            var bodyDims = state.AssetMeasurer.Measure(bodyAnim.GetCurrentFrame(state.AnimationManager));
            var headDims = state.AssetMeasurer.Measure(headAnim.GetCurrentFrame(state.AnimationManager));

            var footX = xSubPixels;
            var footY = ySubPixels + headDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL + bodyDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            feetPos.X_SubPixel = footX;
            feetPos.Y_SubPixel = footY;
        }

        /// <summary>
        /// Determines the players current position, overall.
        /// 
        /// A player is made up of multiple entities, so this isn't as simple as it seems.
        /// 
        /// Returned coordinates are in screen coords and sub-pixels.
        /// </summary>
        private (int X_SubPixels, int Y_SubPixels)? DeterminePlayerPosition(GameState state)
        {
            var manager = state.EntityManager;

            var feet = state.Player_Feet;
            var body = state.Player_Body;
            var head = state.Player_Head;

            var feetPos = manager.GetPositionFor(feet);
            if (feetPos == null) return null;
            var bodyAnim = manager.GetAnimationFor(body);
            if (bodyAnim == null) return null;
            var headAnim = manager.GetAnimationFor(head);
            if (headAnim == null) return null;

            var bodyDims = state.AssetMeasurer.Measure(bodyAnim.GetCurrentFrame(state.AnimationManager));
            var headDims = state.AssetMeasurer.Measure(headAnim.GetCurrentFrame(state.AnimationManager));

            var x = feetPos.X_SubPixel;
            var y = feetPos.Y_SubPixel - bodyDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL - headDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            return (x, y);
        }

        /// <summary>
        /// Determines the player current height, considering their current frames.
        /// 
        /// Returns pixels.
        /// </summary>
        private (int Width_SubPixels, int Height_SubPixels)? DeterminePlayerDimensions(GameState state)
        {
            var manager = state.EntityManager;

            var feet = state.Player_Feet;
            var body = state.Player_Body;
            var head = state.Player_Head;

            var feetAnim = manager.GetAnimationFor(feet);
            if (feetAnim == null) return null;
            var bodyAnim = manager.GetAnimationFor(body);
            if (bodyAnim == null) return null;
            var headAnim = manager.GetAnimationFor(head);
            if (headAnim == null) return null;

            var feetDims = state.AssetMeasurer.Measure(feetAnim.GetCurrentFrame(state.AnimationManager));
            var bodyDims = state.AssetMeasurer.Measure(bodyAnim.GetCurrentFrame(state.AnimationManager));
            var headDims = state.AssetMeasurer.Measure(headAnim.GetCurrentFrame(state.AnimationManager));

            var width = Math.Max(feetDims.Width, Math.Max(bodyDims.Width, headDims.Width)) * PositionComponent.SUBPIXELS_PER_PIXEL;
            var height = (feetDims.Height + bodyDims.Height + headDims.Height) * PositionComponent.SUBPIXELS_PER_PIXEL;

            return (width, height);
        }
    }
}
