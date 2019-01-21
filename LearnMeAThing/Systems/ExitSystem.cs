using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Systems
{
    public enum ExitDirection
    {
        NONE = 0,

        North,
        South,
        East,
        West,

        Door,
        Stairs,
        Pit
    }

    public enum FadingType
    {
        NONE = 0,

        Out,
        In
    }

    public enum FallingType
    {
        NONE = 0,

        IntoFloor,
        FromCeiling
    }

    /// <summary>
    /// Handles the transition between two rooms, caused
    ///   by the player exiting.
    /// </summary>
    sealed class ExitSystem : ASystem<object>
    {
        public const int SCROLL_FOR_STEPS = 60;

        public override SystemType Type => SystemType.Exit;

        public bool IsTransitioning => Scrolling != null || Fading != null || Falling != null;

        /// <summary>
        /// If we're currently falling, which direction we're falling.
        /// </summary>
        private FallingType? Falling { get; set; }

        /// <summary>
        /// If we're currently fading, what sort of fading we're doing.
        /// </summary>
        private FadingType? Fading { get; set; }

        /// <summary>
        /// If we're taking a fading transition and taking stairs, 
        ///   the direction the stairs are going.
        /// </summary>
        private StairDirections? FadingStairsDirection { get; set; }

        /// <summary>
        /// If we're taking a fading transition, the room we're going to.
        /// </summary>
        private RoomNames FadingDestinationRoom { get; set; }

        /// <summary>
        /// If we're taking a fading transition, the square we're going to.
        /// </summary>
        private Point FadingDestinationSquare { get; set; }

        /// <summary>
        /// Which, if any, exit has been requested.
        /// </summary>
        private ExitDirection? ExitRequested { get; set; }

        /// <summary>
        /// Which, if any, direction are we _actively_ scrolling.
        /// </summary>
        internal ExitDirection? Scrolling { get; private set; }

        /// <summary>
        /// The room that we're transitioning out of.
        /// </summary>
        internal RoomNames PreviousRoom { get; private set; }

        /// <summary>
        /// Where the camera was at the time the transition started,
        ///   in the _previous_ rooms coordinates.
        /// </summary>
        internal Point PreviousCameraPos { get; private set; }

        /// <summary>
        /// Where the camera was at the time the transition started,
        ///   but in the _new_ rooms coordinates
        /// </summary>
        internal Point NewCameraPos { get; private set; }

        /// <summary>
        /// Where we want the camera to end, in the new room's coordinates.
        /// </summary>
        internal Point FinalCameraPos { get; private set; }

        /// <summary>
        /// Where we the player started, in the new room's coordinates.
        /// </summary>
        internal Point InitialPlayerPos { get; private set; }

        /// <summary>
        /// Where we want the player to end, in the new room's coordinates.
        /// </summary>
        internal Point FinalPlayerPos { get; private set; }

        /// <summary>
        /// Where the pit a player walked into (if any) is located, in the old
        ///    room's coordinates.
        /// </summary>
        internal Point PitPos { get; private set; }

        /// <summary>
        /// How many iterations deep we are into transitioning between
        ///   two rooms.
        /// </summary>
        internal int Step { get; private set; }

        public override object DesiredEntities(EntityManager manager)
        => null;

        public override void Update(GameState state, object ignored)
        {
            if (Scrolling.HasValue)
            {
                HandleScrollUpdate(state);
            }

            if (Falling.HasValue)
            {
                HandleFallingUpdate(state);
            }

            if (Fading.HasValue)
            {
                HandleFadingUpdate(state);
            }

            if (ExitRequested.HasValue)
            {
                HandleExitRequested(state);
            }
        }

        /// <summary>
        /// Setup all the state needing for scrolling.
        /// </summary>
        private void SetScrollingState(
            ExitDirection scrolling,
            RoomNames prevRoom,
            Point prevCameraPos,
            Point newCameraPos,
            Point finalCameraPos,
            Point initialPlayerPos,
            Point finalPlayerPos
        )
        {
            Scrolling = scrolling;
            PreviousRoom = prevRoom;
            PreviousCameraPos = prevCameraPos;
            NewCameraPos = newCameraPos;
            FinalCameraPos = finalCameraPos;
            InitialPlayerPos = initialPlayerPos;
            FinalPlayerPos = finalPlayerPos;
            Step = 1;
        }

        /// <summary>
        /// Trigger an exit from walking into the edge of a room.
        /// </summary>
        public void RequestExit(ExitDirection dir)
        {
            if (ExitRequested != null) throw new InvalidOperationException("Second exit requested while other still pending");

            ExitRequested = dir;
            FadingStairsDirection = null;
        }

        /// <summary>
        /// Trigger an exit from walking into a door.
        /// </summary>
        public void RequestExit(DoorComponent door)
        {
            ExitRequested = ExitDirection.Door;

            // nothing to do just yet, but we're going here so make a note
            FadingDestinationRoom = door.TargetRoom;
            FadingDestinationSquare = new Point(door.TargetX, door.TargetY);
            FadingStairsDirection = null;
        }

        /// <summary>
        /// Trigger an exit from walking into a door.
        /// </summary>
        public void RequestExit(PitComponent pit, PositionComponent pitPos)
        {
            ExitRequested = ExitDirection.Pit;

            FadingDestinationRoom = pit.TargetRoom;
            FadingDestinationSquare = new Point(pit.TargetX, pit.TargetY);
            FadingStairsDirection = null;

            PitPos = new Point(pitPos.X, pitPos.Y);
        }

        /// <summary>
        /// Trigger an exit from walking into some stairs.
        /// </summary>
        public void RequestExit(StairsComponent stairs)
        {
            ExitRequested = ExitDirection.Stairs;

            // nothing to do just yet, but we're going here so make a note
            FadingStairsDirection = stairs.Direction;
            FadingDestinationRoom = stairs.TargetRoom;
            FadingDestinationSquare = new Point(stairs.TargetX, stairs.TargetY);
        }

        /// <summary>
        /// We're in the middle of a transition,
        ///   update all the state so the fade is as appropriate
        ///   and the player is appropriate.
        ///   
        /// Also handles triggering the end of fading.
        /// </summary>
        private void HandleFallingUpdate(GameState state)
        {
            if (Step > SCROLL_FOR_STEPS)
            {
                EndFalling(state);
                return;
            }

            if (Step > SCROLL_FOR_STEPS / 2 && Falling == FallingType.IntoFloor)
            {
                Falling = FallingType.FromCeiling;

                // switch back to normal poses
                var player = state.PlayerStateSystem;
                player.ClearAnimationRequest();

                // make the player standing, now that they're back in a room
                var playerState = state.EntityManager.GetPlayerStateFor(state.Player_Feet);
                if (playerState != null)
                {
                    var facing = playerState.GetFacingDirection();
                    if (facing != null)
                    {
                        switch (facing.Value)
                        {
                            case PlayerFacing.East: playerState.SetStandingFacing(PlayerStanding.East); break;
                            case PlayerFacing.West: playerState.SetStandingFacing(PlayerStanding.West); break;
                            case PlayerFacing.North: playerState.SetStandingFacing(PlayerStanding.North); break;
                            case PlayerFacing.South: playerState.SetStandingFacing(PlayerStanding.South); break;
                            default:
                                // glitch: ??
                                return;
                        }
                    }
                }

                // free all the current entities loaded
                foreach (var e in state.EntityManager.AllEntities())
                {
                    // don't free the player
                    if (e.Id == state.Player_Body.Id) continue;
                    if (e.Id == state.Player_Feet.Id) continue;
                    if (e.Id == state.Player_Head.Id) continue;
                    // don't free the camera
                    if (e.Id == state.Camera.Id) continue;

                    state.EntityManager.ReleaseEntity(e);
                }

                // move to the new room
                var newRoom = state.CreateRoom(FadingDestinationRoom);
                state.CurrentRoom = newRoom;

                // point at the final destination
                var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);
                var playerFeetDims = state.AssetMeasurer.Measure(AssetNames.Player_Feet);
                var playerBodyDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
                var playerHeadDims = state.AssetMeasurer.Measure(AssetNames.Player_Head);

                var playerWidth = Math.Max(playerFeetDims.Width, Math.Max(playerBodyDims.Width, playerHeadDims.Width));
                var playerHeight = playerFeetDims.Height + playerBodyDims.Height + playerHeadDims.Height;

                var playerXForCamera = (int)FinalPlayerPos.X;
                var playerYForCamera = (int)FinalPlayerPos.Y - playerBodyDims.Height - playerHeadDims.Height;

                var (playerCenterX, playerCenterY) =
                    CameraSystem.CalculateNewPosition(
                        roomDims.Width,
                        roomDims.Height,
                        playerXForCamera,
                        playerYForCamera,
                        playerWidth,
                        playerHeight,
                        GameState.WIDTH_HACK,
                        GameState.HEIGHT_HACK
                    );

                state.CameraSystem.PointCameraAt(playerCenterX, playerCenterY);

                // place a shadow under the player's feet
                var dropShadowDims = state.AssetMeasurer.Measure(AssetNames.DropShadow1);
                
                var dropShadowX = (int)FinalPlayerPos.X;
                var dropShadowY = (int)FinalPlayerPos.Y;

                var dropShadowShiftX = dropShadowDims.Width / 2 - playerFeetDims.Width / 2;
                var dropShadowShiftY = dropShadowDims.Height / 2 - playerFeetDims.Height / 2;

                dropShadowX += dropShadowShiftX;
                dropShadowY += dropShadowShiftY;
                
                // we don't care if this succeeds, we'll clean it up later if it does
                ObjectCreator.CreateDropShadow(state, dropShadowX, dropShadowY);
            }

            NudgePlayerForFallingAnimation(state);

            Step++;
        }

        private void NudgePlayerForFallingAnimation(GameState state)
        {
            // nothing to do
            if (Step <= SCROLL_FOR_STEPS / 2) return;

            var startX = (int)FinalPlayerPos.X;
            var startY = (int)FinalPlayerPos.Y - GameState.HEIGHT_HACK / 2;  // fall from a whole screen above

            var stopX = (int)FinalPlayerPos.X;
            var stopY = (int)FinalPlayerPos.Y;

            var curStep = Step - (SCROLL_FOR_STEPS / 2);
            var stopStep = SCROLL_FOR_STEPS / 2;

            var pt = GetInterimPoint(startX, startY, stopX, stopY, curStep, stopStep);

            var playerPos = state.EntityManager.GetPositionFor(state.Player_Feet);
            if (playerPos == null)
            {
                // glitch: ???
                return;
            }

            playerPos.X_SubPixel = pt.X * PositionComponent.SUBPIXELS_PER_PIXEL;
            playerPos.Y_SubPixel = pt.Y * PositionComponent.SUBPIXELS_PER_PIXEL;
        }

        private void EndFalling(GameState state)
        {
            Falling = null;

            // free the camera to move however
            state.CameraSystem.ResetCamera();

            // cleanup the drop shadow(s)
            foreach (var e in state.EntityManager.EntitiesWith(FlagComponent.DropShadow))
            {
                state.EntityManager.ReleaseEntity(e);
            }
        }

        /// <summary>
        /// We're in the middle of a transition,
        ///   update all the state so the fade is as appropriate
        ///   and the player is appropriate.
        ///   
        /// Also handles triggering the end of fading.
        /// </summary>
        private void HandleFadingUpdate(GameState state)
        {
            if (Step > SCROLL_FOR_STEPS)
            {
                EndFading(state);
                return;
            }

            // we're transitioning between, so we need to move rooms and whatnot
            if (Step > SCROLL_FOR_STEPS / 2 && Fading == FadingType.Out)
            {
                // free all the current entities loaded
                foreach (var e in state.EntityManager.AllEntities())
                {
                    // don't free the player
                    if (e.Id == state.Player_Body.Id) continue;
                    if (e.Id == state.Player_Feet.Id) continue;
                    if (e.Id == state.Player_Head.Id) continue;
                    // don't free the camera
                    if (e.Id == state.Camera.Id) continue;

                    state.EntityManager.ReleaseEntity(e);
                }

                // move to the new room
                var newRoom = state.CreateRoom(FadingDestinationRoom);
                state.CurrentRoom = newRoom;

                // move the player
                var playerPos = state.EntityManager.GetPositionFor(state.Player_Feet);

                var playerFeetDims = state.AssetMeasurer.Measure(AssetNames.Player_Feet);
                var playerBodyDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
                var playerHeadDims = state.AssetMeasurer.Measure(AssetNames.Player_Head);

                var playerWidth = Math.Max(playerFeetDims.Width, Math.Max(playerBodyDims.Width, playerHeadDims.Width));
                var playerHeight = playerFeetDims.Height + playerBodyDims.Height + playerHeadDims.Height;

                var destSquareX_SubPixels = (int)FadingDestinationSquare.X * RoomTemplate.TILE_WIDTH_PIXELS * PositionComponent.SUBPIXELS_PER_PIXEL;
                var destSquareY_SubPixels = (int)FadingDestinationSquare.Y * RoomTemplate.TILE_WIDTH_PIXELS * PositionComponent.SUBPIXELS_PER_PIXEL;

                var centerInX = RoomTemplate.TILE_WIDTH_PIXELS;
                if (FadingStairsDirection != null)
                {
                    centerInX = state.AssetMeasurer.Measure(AssetNames.StairsDoorFrame).Width;
                }

                var pushLeft_SubPixels = ((centerInX - playerWidth) / 2) * PositionComponent.SUBPIXELS_PER_PIXEL;
                var pushDown_SubPixels = ((playerHeight - playerFeetDims.Height) + ((RoomTemplate.TILE_HEIGHT_PIXELS - playerHeight)) / 2) * PositionComponent.SUBPIXELS_PER_PIXEL;

                var newPlayerPosX_SubPixels = destSquareX_SubPixels + pushLeft_SubPixels;
                var newPlayerPosY_SubPixels = destSquareY_SubPixels + pushDown_SubPixels;

                playerPos.X_SubPixel = newPlayerPosX_SubPixels;
                playerPos.Y_SubPixel = newPlayerPosY_SubPixels;

                Fading = FadingType.In;

                // if we're taking the stairs, we need to change our animation at the half-way point
                if (FadingStairsDirection.HasValue)
                {
                    switch (FadingStairsDirection.Value)
                    {
                        case StairDirections.Down:
                            state.PlayerStateSystem.RequestAnimation(
                                AnimationNames.Player_Feet_WalkingDownStairs2,
                                AnimationNames.Player_Body_WalkingDownStairs2,
                                AnimationNames.Player_Head_WalkingDownStairs2
                            );
                            break;
                        case StairDirections.Up:
                            state.PlayerStateSystem.RequestAnimation(
                                AnimationNames.Player_Feet_WalkingUpStairs2,
                                AnimationNames.Player_Body_WalkingUpStairs2,
                                AnimationNames.Player_Head_WalkingUpStairs2
                            );
                            break;
                    }
                }
            }

            if (FadingStairsDirection.HasValue)
            {
                NudgePlayerForStairsAnimation(state);
            }

            Step++;
        }

        /// <summary>
        /// If we're taking the stairs, the player needs to be nudged along.
        /// </summary>
        private void NudgePlayerForStairsAnimation(GameState state)
        {
            const int FIRST_STEP_DISTANCE_PIXELS = 16;
            const int SECOND_STEP_DISTANCE_PIXELS = 12;
            const int THIRD_STEP_DISTANCE_PIXELS = 16;

            const int STEP1_CUTOFF_STEPS = SCROLL_FOR_STEPS / 2 / 3;
            const int STEP2_CUTOFF_STEPS = STEP1_CUTOFF_STEPS + STEP1_CUTOFF_STEPS;
            const int STEP3_CUTOFF_STEPS = STEP2_CUTOFF_STEPS + STEP1_CUTOFF_STEPS;
            const int STEP4_CUTOFF_STEPS = STEP3_CUTOFF_STEPS + STEP1_CUTOFF_STEPS;
            const int STEP5_CUTOFF_STEPS = STEP4_CUTOFF_STEPS + STEP1_CUTOFF_STEPS;
            const int STEP6_CUTOFF_STEPS = SCROLL_FOR_STEPS;

            var startX = (int)InitialPlayerPos.X;
            var startY = (int)InitialPlayerPos.Y;

            var endX = (int)FinalPlayerPos.X;
            var endY = (int)FinalPlayerPos.Y;

            int finalX, finalY;
            finalX = finalY = -1;

            switch (FadingStairsDirection.Value)
            {
                case StairDirections.Up:
                    {
                        if (Step < SCROLL_FOR_STEPS / 2)
                        {
                            // stages (going in, walking up)
                            //   walking forward
                            //   walking forward and right
                            //   walking right

                            if (Step <= STEP1_CUTOFF_STEPS)
                            {
                                // walking foward
                                var stopX = startX;
                                var stopY = startY - FIRST_STEP_DISTANCE_PIXELS;

                                (finalX, finalY) = GetInterimPoint(startX, startY, stopX, stopY, Step, STEP1_CUTOFF_STEPS);
                            }
                            else if (Step <= SCROLL_FOR_STEPS / 2 / 3 * 2)
                            {
                                // walking foward and right
                                var prevStepStartX = startX;
                                var prevStepStartY = startY - FIRST_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStartX + SECOND_STEP_DISTANCE_PIXELS;
                                var stopY = prevStepStartY - SECOND_STEP_DISTANCE_PIXELS;

                                var newStepStart = Step - STEP1_CUTOFF_STEPS;
                                var newStepStop = STEP2_CUTOFF_STEPS - STEP1_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStartX, prevStepStartY, stopX, stopY, newStepStart, newStepStop);
                            }
                            else
                            {
                                // walking right
                                var prevStepStartX = startX + SECOND_STEP_DISTANCE_PIXELS;
                                var prevStepStartY = startY - FIRST_STEP_DISTANCE_PIXELS - FIRST_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStartX + THIRD_STEP_DISTANCE_PIXELS;
                                var stopY = prevStepStartY;

                                var newStepStart = Step - STEP2_CUTOFF_STEPS;
                                var newStepStop = STEP3_CUTOFF_STEPS - STEP2_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStartX, prevStepStartY, stopX, stopY, newStepStart, newStepStop);
                            }
                        }
                        else
                        {
                            // stages (coming down, walking out)
                            //   walking right
                            //   walking down and right
                            //   walking down
                            if (Step <= STEP4_CUTOFF_STEPS)
                            {
                                // walking right
                                var implicitStartX = endX - SECOND_STEP_DISTANCE_PIXELS - THIRD_STEP_DISTANCE_PIXELS;
                                var implicitStartY = endY - FIRST_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS;

                                var stopX = implicitStartX + THIRD_STEP_DISTANCE_PIXELS;
                                var stopY = implicitStartY;

                                var newStepStart = Step - STEP3_CUTOFF_STEPS;
                                var newStepStop = STEP4_CUTOFF_STEPS - STEP3_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(implicitStartX, implicitStartY, stopX, stopY, newStepStart, newStepStop);
                            }
                            else if (Step <= STEP5_CUTOFF_STEPS)
                            {
                                // walking down and right
                                var prevStepStopX = endX - SECOND_STEP_DISTANCE_PIXELS - THIRD_STEP_DISTANCE_PIXELS + THIRD_STEP_DISTANCE_PIXELS;
                                var prevStepStopY = endY - FIRST_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStopX + SECOND_STEP_DISTANCE_PIXELS;
                                var stopY = prevStepStopY + SECOND_STEP_DISTANCE_PIXELS;

                                var newStepStart = Step - STEP4_CUTOFF_STEPS;
                                var newStepStop = STEP5_CUTOFF_STEPS - STEP4_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStopX, prevStepStopY, stopX, stopY, newStepStart, newStepStop);
                            }
                            else
                            {
                                // walking down
                                var prevStepStopX = endX - SECOND_STEP_DISTANCE_PIXELS - THIRD_STEP_DISTANCE_PIXELS + THIRD_STEP_DISTANCE_PIXELS + SECOND_STEP_DISTANCE_PIXELS;
                                var prevStepStopY = endY - FIRST_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS + SECOND_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStopX;
                                var stopY = prevStepStopY + FIRST_STEP_DISTANCE_PIXELS;

                                var newStepStart = Step - STEP5_CUTOFF_STEPS;
                                var newStepStop = STEP6_CUTOFF_STEPS - STEP5_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStopX, prevStepStopY, stopX, stopY, newStepStart, newStepStop);
                            }
                        }
                    }
                    break;
                case StairDirections.Down:
                    {
                        if (Step < SCROLL_FOR_STEPS / 2)
                        {
                            // stages (going in, walking down)
                            //   walking forward
                            //   walking forward and left
                            //   walking left

                            if (Step <= STEP1_CUTOFF_STEPS)
                            {
                                // walking foward
                                var stopX = startX;
                                var stopY = startY - FIRST_STEP_DISTANCE_PIXELS;

                                (finalX, finalY) = GetInterimPoint(startX, startY, stopX, stopY, Step, STEP1_CUTOFF_STEPS);
                            }
                            else if (Step <= SCROLL_FOR_STEPS / 2 / 3 * 2)
                            {
                                // walking foward and left
                                var prevStepStartX = startX;
                                var prevStepStartY = startY - FIRST_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStartX - SECOND_STEP_DISTANCE_PIXELS;
                                var stopY = prevStepStartY - SECOND_STEP_DISTANCE_PIXELS;

                                var newStepStart = Step - STEP1_CUTOFF_STEPS;
                                var newStepStop = STEP2_CUTOFF_STEPS - STEP1_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStartX, prevStepStartY, stopX, stopY, newStepStart, newStepStop);
                            }
                            else
                            {
                                // walking left
                                var prevStepStartX = startX - SECOND_STEP_DISTANCE_PIXELS;
                                var prevStepStartY = startY - FIRST_STEP_DISTANCE_PIXELS - FIRST_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStartX - THIRD_STEP_DISTANCE_PIXELS;
                                var stopY = prevStepStartY;

                                var newStepStart = Step - STEP2_CUTOFF_STEPS;
                                var newStepStop = STEP3_CUTOFF_STEPS - STEP2_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStartX, prevStepStartY, stopX, stopY, newStepStart, newStepStop);
                            }
                        }
                        else
                        {
                            // stages (coming down, walking out)
                            //   walking left
                            //   walking down and left
                            //   walking down
                            if (Step <= STEP4_CUTOFF_STEPS)
                            {
                                // walking left
                                var implicitStartX = endX + SECOND_STEP_DISTANCE_PIXELS + THIRD_STEP_DISTANCE_PIXELS;
                                var implicitStartY = endY - FIRST_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS;

                                var stopX = implicitStartX - THIRD_STEP_DISTANCE_PIXELS;
                                var stopY = implicitStartY;

                                var newStepStart = Step - STEP3_CUTOFF_STEPS;
                                var newStepStop = STEP4_CUTOFF_STEPS - STEP3_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(implicitStartX, implicitStartY, stopX, stopY, newStepStart, newStepStop);
                            }
                            else if (Step <= STEP5_CUTOFF_STEPS)
                            {
                                // walking down and left
                                var prevStepStopX = endX + SECOND_STEP_DISTANCE_PIXELS + THIRD_STEP_DISTANCE_PIXELS - THIRD_STEP_DISTANCE_PIXELS;
                                var prevStepStopY = endY - FIRST_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStopX - SECOND_STEP_DISTANCE_PIXELS;
                                var stopY = prevStepStopY + SECOND_STEP_DISTANCE_PIXELS;

                                var newStepStart = Step - STEP4_CUTOFF_STEPS;
                                var newStepStop = STEP5_CUTOFF_STEPS - STEP4_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStopX, prevStepStopY, stopX, stopY, newStepStart, newStepStop);
                            }
                            else
                            {
                                // walking down
                                var prevStepStopX = endX + SECOND_STEP_DISTANCE_PIXELS + THIRD_STEP_DISTANCE_PIXELS - THIRD_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS;
                                var prevStepStopY = endY - FIRST_STEP_DISTANCE_PIXELS - SECOND_STEP_DISTANCE_PIXELS + SECOND_STEP_DISTANCE_PIXELS;

                                var stopX = prevStepStopX;
                                var stopY = prevStepStopY + FIRST_STEP_DISTANCE_PIXELS;

                                var newStepStart = Step - STEP5_CUTOFF_STEPS;
                                var newStepStop = STEP6_CUTOFF_STEPS - STEP5_CUTOFF_STEPS;

                                (finalX, finalY) = GetInterimPoint(prevStepStopX, prevStepStopY, stopX, stopY, newStepStart, newStepStop);
                            }
                        }
                    }
                    break;
            }

            var playerPos = state.EntityManager.GetPositionFor(state.Player_Feet);
            playerPos.X_SubPixel = finalX * PositionComponent.SUBPIXELS_PER_PIXEL;
            playerPos.Y_SubPixel = finalY * PositionComponent.SUBPIXELS_PER_PIXEL;
        }

        /// <summary>
        /// We've finished fading back in after a room
        ///   transition.
        ///   
        /// Clean everything up.
        /// </summary>
        private void EndFading(GameState state)
        {
            Fading = null;

            // in case we were taking some stairs, remove our animation override
            state.PlayerStateSystem.ClearAnimationRequest();

            if (FadingStairsDirection != null)
            {
                var player = state.EntityManager.GetPlayerStateFor(state.Player_Feet);
                player.SetStandingFacing(PlayerStanding.South);
            }
        }

        /// <summary>
        /// Returns how close the screen should be to all black,
        ///    where 0 == not at all and
        ///    where 100 == completely black
        /// </summary>
        public int GetFadeToBlackProgress()
        {
            var fadingCase =
                Fading != null ?
                    Fading.Value :
                    (Falling != null ?
                        (Falling.Value == FallingType.IntoFloor ?
                            FadingType.Out :
                            FadingType.In
                        ) :
                        default(FadingType?)
                    );

            if (fadingCase == null) return 0;

            switch (fadingCase.Value)
            {
                case FadingType.In:
                    {
                        var diff = Step - (SCROLL_FOR_STEPS / 2);
                        var perc = 100 / (SCROLL_FOR_STEPS / 2) * diff;
                        return 100 - perc;
                    }
                case FadingType.Out:
                    {
                        // make sure there's at least one frame of pure black
                        if (Step == SCROLL_FOR_STEPS / 2) return 100;

                        var diff = Step;
                        var perc = 100 / (SCROLL_FOR_STEPS / 2) * diff;
                        return perc;
                    }
                default: throw new InvalidOperationException($"Tried to fade with unexpected {nameof(FadingType)}: {Fading.Value}");
            }
        }

        /// <summary>
        /// We're in the middle of a transition,
        ///   update all state so the player is the appropriate
        ///   spot and the camera is pointed appropriately.
        ///   
        /// Also handles triggering the end of scrolling.
        /// </summary>
        private void HandleScrollUpdate(GameState state)
        {
            if (Step > SCROLL_FOR_STEPS)
            {
                EndScrolling(state);
                return;
            }

            var manager = state.EntityManager;
            var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);

            var playerPos = manager.GetPositionFor(state.Player_Feet);
            var playerDest = GetInterimPlayerPos();
            playerPos.X_SubPixel = playerDest.X * PositionComponent.SUBPIXELS_PER_PIXEL;
            playerPos.Y_SubPixel = playerDest.Y * PositionComponent.SUBPIXELS_PER_PIXEL;

            var cameraDest = GetInterimCameraPos();
            state.CameraSystem.PointCameraAt(cameraDest.X, cameraDest.Y);

            Step++;
        }

        /// <summary>
        /// For the current Steps, returns where we want the player to be.
        /// </summary>
        private (int X, int Y) GetInterimPlayerPos()
        => GetInterimPoint((int)InitialPlayerPos.X, (int)InitialPlayerPos.Y, (int)FinalPlayerPos.X, (int)FinalPlayerPos.Y, Step, SCROLL_FOR_STEPS);

        /// <summary>
        /// For the current Steps, returns where we want the camera to be.
        /// </summary>
        private (int X, int Y) GetInterimCameraPos()
        => GetInterimPoint((int)NewCameraPos.X, (int)NewCameraPos.Y, (int)FinalCameraPos.X, (int)FinalCameraPos.Y, Step, SCROLL_FOR_STEPS);

        /// <summary>
        /// Produce a description of how far we've transitioned the camera (and from which room)
        /// </summary>
        public bool TryGetTransitionProgress(GameState state, out RoomNames previousRoom, out Point previousCameraPos, out Vector cameraCurrentlyShiftBy)
        {
            if (Scrolling == null)
            {
                previousRoom = default;
                previousCameraPos = default;
                cameraCurrentlyShiftBy = default;
                return false;
            }

            var cameraPos = state.EntityManager.GetPositionFor(state.Camera);

            var shiftedByCameraX = NewCameraPos.X - cameraPos.X;
            var shiftedByCameraY = NewCameraPos.Y - cameraPos.Y;

            previousRoom = PreviousRoom;
            previousCameraPos = PreviousCameraPos;
            cameraCurrentlyShiftBy = new Vector(shiftedByCameraX, shiftedByCameraY);
            return true;
        }

        /// <summary>
        /// Given a start, stop, current step, and final step, does a 
        ///   linear interpolation to the current point between the
        ///   two given points.
        ///   
        /// Models with subpixels internally, but expects pixel coordinates.
        /// </summary>
        private static (int X, int Y) GetInterimPoint(int startX, int startY, int stopX, int stopY, int curStep, int finalStep)
        {
            // need to special case this, so we don't lose any pixels to rounding
            // 
            // it's really important players _end_ where they're supposed to
            if (curStep == finalStep)
            {
                return (stopX, stopY);
            }

            var stepX_SubPixels = ((stopX - startX) * PositionComponent.SUBPIXELS_PER_PIXEL) / finalStep;
            var stepY_SubPixels = ((stopY - startY) * PositionComponent.SUBPIXELS_PER_PIXEL) / finalStep;

            var deltaX_SubPixels = stepX_SubPixels * curStep;
            var deltaY_SubPixels = stepY_SubPixels * curStep;

            var deltaX = deltaX_SubPixels / PositionComponent.SUBPIXELS_PER_PIXEL;
            var deltaY = deltaY_SubPixels / PositionComponent.SUBPIXELS_PER_PIXEL;

            var retX = startX + deltaX;
            var retY = startY + deltaY;

            return (retX, retY);
        }

        /// <summary>
        /// Clear out all the room state, and nuke any entities that we
        ///    marked as "needing to die" when the transition started.
        /// </summary>
        private void EndScrolling(GameState state)
        {
            var manager = state.EntityManager;

            // release control of the camera
            state.CameraSystem.ResetCamera();

            // clear out all the system's state
            ExitRequested = null;
            Scrolling = null;

            // nuke all the entities we only kept around for the transition
            foreach (var e in manager.EntitiesWith(FlagComponent.CullAfterTransition))
            {
                manager.ReleaseEntity(e);
            }
        }

        /// <summary>
        /// The player tried to exit the current room, 
        ///    figure out where they're going and setup
        ///    everything needed for them to transition
        ///    to the new room.
        ///    
        /// This includes making the new room, marking
        ///    old entities as needing culling, figure out
        ///    where the player and camera are going to 
        ///    end up, etc.
        /// </summary>
        private void HandleExitRequested(GameState state)
        {
            var request = ExitRequested.Value;
            // clear it, so we're done with this particular request
            ExitRequested = null;

            RoomNames? scrollTo;
            switch (request)
            {
                case ExitDirection.East: scrollTo = state.CurrentRoom.Template.RightExit; break;
                case ExitDirection.West: scrollTo = state.CurrentRoom.Template.LeftExit; break;
                case ExitDirection.North: scrollTo = state.CurrentRoom.Template.TopExit; break;
                case ExitDirection.South: scrollTo = state.CurrentRoom.Template.BottomExit; break;
                case ExitDirection.Door:
                    HandleDoorExitRequested(state);
                    return;
                case ExitDirection.Stairs:
                    HandleStairsExitRequested(state);
                    return;
                case ExitDirection.Pit:
                    HandlePitExitRequested(state);
                    return;
                default:
                    // glitch: ???
                    scrollTo = null;
                    break;
            }

            if (scrollTo == null)
            {
                // there is no such exit for the current room
                return;
            }

            // the idea here is, make a new room
            //   move all the _current_ entities
            //   to the new room but after updating
            //   their positions so they're positioned
            //   relative to the new room
            // we also mark the old entities as needing
            //   culling post-transition
            // then we swap the old room for the new room

            var manager = state.EntityManager;
            var roomMeasurer = state.RoomManager;

            // we're about to allocate a lot, so free up space
            if (manager.NextAvailableEntity - 1 != manager.NumLiveEntities)
            {
                state.CompactEntities();
            }

            foreach (var entity in manager.AllEntities())
            {
                // player bits stay around, but nothing else
                if (entity.Id == state.Player_Head.Id) continue;
                if (entity.Id == state.Player_Body.Id) continue;
                if (entity.Id == state.Player_Feet.Id) continue;
                // don't destroy the camera!
                if (entity.Id == state.Camera.Id) continue;

                manager.AddComponent(entity, FlagComponent.CullAfterTransition);
            }

            // create the new room (which makes a whole bunch of entities)
            var newRoom = state.CreateRoom(scrollTo.Value);

            // figure out how much we need to shift the _old_ entities,
            //   such that they're in the appropriate coordinate system
            var oldRoomDims = roomMeasurer.Measure(state.CurrentRoom.Name);
            var newRoomDims = roomMeasurer.Measure(newRoom.Name);

            var cameraPos = manager.GetPositionFor(state.Camera);
            var oldCameraPos = (X: cameraPos.X, Y: cameraPos.Y);

            int shiftX, shiftY, scaleX, scaleY;
            switch (request)
            {
                case ExitDirection.North:
                    shiftY = newRoomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL;
                    scaleY = 1;
                    shiftX = 0;
                    scaleX = (newRoomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL) / (oldRoomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL);
                    break;
                case ExitDirection.South:
                    shiftY = -oldRoomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL;
                    scaleY = 1;
                    shiftX = 0;
                    scaleX = (newRoomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL) / (oldRoomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL);
                    break;
                case ExitDirection.East:
                    shiftY = 0;
                    scaleY = (newRoomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL) / (oldRoomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL);
                    shiftX = -oldRoomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL;
                    scaleX = 1;
                    break;
                case ExitDirection.West:
                    shiftY = 0;
                    scaleY = (newRoomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL) / (oldRoomDims.Height * PositionComponent.SUBPIXELS_PER_PIXEL);
                    shiftX = newRoomDims.Width * PositionComponent.SUBPIXELS_PER_PIXEL;
                    scaleX = 1;
                    break;
                default: throw new InvalidOperationException($"Unexpected {nameof(ExitDirection)}: {request}");
            }

            // move everything into the new coordinate system
            foreach (var e in manager.EntitiesWith(FlagComponent.CullAfterTransition))
            {
                var p = manager.GetPositionFor(e);
                if (p == null) continue;

                p.X_SubPixel = p.X_SubPixel * scaleX + shiftX;
                p.Y_SubPixel = p.Y_SubPixel * scaleY + shiftY;
            }

            // have to update player parts separately, because we don't mark them
            var playerPos = manager.GetPositionFor(state.Player_Feet);
            playerPos.X_SubPixel = playerPos.X_SubPixel * scaleX + shiftX;
            playerPos.Y_SubPixel = playerPos.Y_SubPixel * scaleY + shiftY;
            // don't need to update the camera, because the Camera system will take care of it

            // record what we came out of
            var previousRoom = state.CurrentRoom.Name;
            var previousCameraPos = new Point(oldCameraPos.X, oldCameraPos.Y);
            var newCameraPos = new Point(
                (((int)previousCameraPos.X * PositionComponent.SUBPIXELS_PER_PIXEL) * scaleX + shiftX) / PositionComponent.SUBPIXELS_PER_PIXEL,
                (((int)previousCameraPos.Y * PositionComponent.SUBPIXELS_PER_PIXEL) * scaleY + shiftY) / PositionComponent.SUBPIXELS_PER_PIXEL
            );

            // swap the rooms
            state.CurrentRoom = newRoom;

            // determine where we want the _player_ to end up
            var initialPlayerPos = new Point(playerPos.X, playerPos.Y);
            var playerFeetDims = state.AssetMeasurer.Measure(AssetNames.Player_Feet);
            var playerBodyDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
            var playerHeadDims = state.AssetMeasurer.Measure(AssetNames.Player_Head);

            var playerWidth = Math.Max(playerFeetDims.Width, Math.Max(playerBodyDims.Width, playerHeadDims.Width));
            var playerHeight = playerFeetDims.Height + playerBodyDims.Height + playerHeadDims.Height;

            var desiredPlayerPos = DeterminePlayerDestination(newRoomDims, playerPos, playerWidth, playerHeight);
            var finalPlayerPos = new Point(desiredPlayerPos.X, desiredPlayerPos.Y);

            // we need to calculate the top left corner of the _whole_ player
            //    because the camera cares about that, whereas the above code
            //    is only concerned about their feet
            var cameraPlayerX = finalPlayerPos.X;
            var cameraPlayerY = finalPlayerPos.Y - playerBodyDims.Height - playerHeadDims.Height;

            // determine where the _camera_ is going to end up
            var desiredCamera =
                CameraSystem.CalculateNewPosition(
                    newRoomDims.Width,
                    newRoomDims.Height,
                    (int)cameraPlayerX,
                    (int)cameraPlayerY,
                    playerWidth,
                    playerHeight,
                    GameState.WIDTH_HACK,
                    GameState.HEIGHT_HACK
                );

            var finalCameraPos = new Point(desiredCamera.X, desiredCamera.Y);

            SetScrollingState(request, previousRoom, previousCameraPos, newCameraPos, finalCameraPos, initialPlayerPos, finalPlayerPos);
        }

        /// <summary>
        /// An exit was requested, and it was a door.
        /// 
        /// Set our state appropriately.
        /// </summary>
        private void HandleDoorExitRequested(GameState state)
        {
            Step = 0;
            Fading = FadingType.Out;
        }

        /// <summary>
        /// An exit was requested, and it was via some stairs.
        /// 
        /// Set our state appropriately.
        /// </summary>
        private void HandleStairsExitRequested(GameState state)
        {
            Step = 0;
            Fading = FadingType.Out;

            switch (FadingStairsDirection)
            {
                case StairDirections.Up:
                    state.PlayerStateSystem.RequestAnimation(
                        AnimationNames.Player_Feet_WalkingUpStairs1,
                        AnimationNames.Player_Body_WalkingUpStairs1,
                        AnimationNames.Player_Head_WalkingUpStairs1
                    );
                    break;
                case StairDirections.Down:
                    state.PlayerStateSystem.RequestAnimation(
                        AnimationNames.Player_Feet_WalkingDownStairs1,
                        AnimationNames.Player_Body_WalkingDownStairs1,
                        AnimationNames.Player_Head_WalkingDownStairs1
                    );
                    break;
            }

            var playerPos = state.EntityManager.GetPositionFor(state.Player_Feet);
            if (playerPos == null)
            {
                // glitch: ???
                return;
            }

            // record where the player is coming from...
            InitialPlayerPos = new Point(playerPos.X, playerPos.Y);

            // determine where the player is going to
            var playerFeetDims = state.AssetMeasurer.Measure(AssetNames.Player_Feet);
            var playerBodyDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
            var playerHeadDims = state.AssetMeasurer.Measure(AssetNames.Player_Head);

            var playerWidth = Math.Max(playerFeetDims.Width, Math.Max(playerBodyDims.Width, playerHeadDims.Width));
            var playerHeight = playerFeetDims.Height + playerBodyDims.Height + playerHeadDims.Height;

            var destSquareX_SubPixels = (int)FadingDestinationSquare.X * RoomTemplate.TILE_WIDTH_PIXELS * PositionComponent.SUBPIXELS_PER_PIXEL;
            var destSquareY_SubPixels = (int)FadingDestinationSquare.Y * RoomTemplate.TILE_WIDTH_PIXELS * PositionComponent.SUBPIXELS_PER_PIXEL;

            var doorFrameSize = state.AssetMeasurer.Measure(AssetNames.StairsDoorFrame);

            // we want the player to end up outside of the stairs, so we put them between the
            //   posts of the door frame but outside of the bottom of the stairs
            var centerInX = doorFrameSize.Width;
            var pushLeft_SubPixels = ((centerInX - playerWidth) / 2) * PositionComponent.SUBPIXELS_PER_PIXEL;
            var pushDown_SubPixels = doorFrameSize.Height * PositionComponent.SUBPIXELS_PER_PIXEL;

            var newPlayerPosX_SubPixels = destSquareX_SubPixels + pushLeft_SubPixels;
            var newPlayerPosY_SubPixels = destSquareY_SubPixels + pushDown_SubPixels;

            var newPlayerPosX = newPlayerPosX_SubPixels / PositionComponent.SUBPIXELS_PER_PIXEL;
            var newPlayerPosY = newPlayerPosY_SubPixels / PositionComponent.SUBPIXELS_PER_PIXEL;

            FinalPlayerPos = new Point(newPlayerPosX, newPlayerPosY);
        }

        /// <summary>
        /// An exit was requested, and it was via falling into a pit.
        /// 
        /// Set our state appropriately.
        /// </summary>
        private void HandlePitExitRequested(GameState state)
        {
            Step = 0;
            Falling = FallingType.IntoFloor;

            state.PlayerStateSystem.RequestAnimation(
                AnimationNames.Empty,
                AnimationNames.Empty,
                AnimationNames.Player_FallingPit
            );

            var playerPos = state.EntityManager.GetPositionFor(state.Player_Feet);
            if (playerPos == null)
            {
                // glitch: ???
                return;
            }

            // determine player dimensions
            var playerDims = state.AssetMeasurer.Measure(AssetNames.Player_FallingPit1);
            var playerWidth = playerDims.Width;
            var playerHeight = playerDims.Height;

            // determine player dimensions
            var pitDims = state.AssetMeasurer.Measure(AssetNames.Pit);

            // we need to move the player entirely into the pit
            var pitX = (int)PitPos.X;
            var pitY = (int)PitPos.Y;

            var shiftX = pitDims.Width / 2 - playerWidth / 2;
            var shiftY = -(pitDims.Height / 2 - playerHeight / 2);

            var playerCenteredX = pitX + shiftX;
            var playerCenteredY = pitY + shiftY;

            var playerCenteredX_SubPixels = playerCenteredX * PositionComponent.SUBPIXELS_PER_PIXEL;
            var playerCenteredY_SubPixels = playerCenteredY * PositionComponent.SUBPIXELS_PER_PIXEL;

            playerPos.X_SubPixel = playerCenteredX_SubPixels;
            playerPos.Y_SubPixel = playerCenteredY_SubPixels;

            // determine where the player is going to
            var destSquareX_SubPixels = (int)FadingDestinationSquare.X * RoomTemplate.TILE_WIDTH_PIXELS * PositionComponent.SUBPIXELS_PER_PIXEL;
            var destSquareY_SubPixels = (int)FadingDestinationSquare.Y * RoomTemplate.TILE_WIDTH_PIXELS * PositionComponent.SUBPIXELS_PER_PIXEL;

            FinalPlayerPos = new Point(destSquareX_SubPixels / PositionComponent.SUBPIXELS_PER_PIXEL, destSquareY_SubPixels / PositionComponent.SUBPIXELS_PER_PIXEL);
        }

        /// <summary>
        /// For a player currently outside a room, figure out where we'd eventually put them _inside_ the room.
        /// </summary>
        private (int X, int Y) DeterminePlayerDestination((int Width, int Height) roomDims, PositionComponent playerPos, int playerWidth, int playerHeight)
        {
            var x = playerPos.X;
            var y = playerPos.Y;

            int pLeft, pTop, pRight, pBottom;
            pLeft = pTop = pRight = pBottom = -1;

            Update();

            // off to right of the room
            if (pLeft >= roomDims.Width)
            {
                x = roomDims.Width - playerWidth - 1;
                Update();
            }

            // off to the left of the room
            if (pRight <= 0)
            {
                x = 1;
                Update();
            }

            // off above the room
            if (pBottom <= 0)
            {
                y = 1;
                Update();
            }

            if (pTop >= roomDims.Height)
            {
                y = roomDims.Height - playerHeight - 1;
                Update();
            }

            return (x, y);

            // Set the player bounding box
            void Update()
            {
                pLeft = x;
                pTop = y;
                pRight = pLeft + playerWidth;
                pBottom = pTop + playerHeight;
            }
        }
    }
}