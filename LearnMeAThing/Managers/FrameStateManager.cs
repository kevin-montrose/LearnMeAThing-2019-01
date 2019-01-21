using LearnMeAThing.Components;
using System;
using System.Threading;

namespace LearnMeAThing.Managers
{
    /// <summary>
    /// Class responsible for taking the current game state and "rendering" it into a FrameState that can then be consumed
    ///    by whatever.
    /// </summary>
    sealed class FrameStateManager
    {
        const int DEFAULT_SIZE = 4;
        
        private int OverAllocated;

        private int MaximumEntities;
        private int MaxHitMapPolygonsPerEntity;
        private FrameState[] FrameStatePool;

        public FrameStateManager(int maximumEntities, int maxHitMapPolygonsPerEntity)
        {
            MaximumEntities = maximumEntities;
            MaxHitMapPolygonsPerEntity = maxHitMapPolygonsPerEntity;
            FrameStatePool = new FrameState[DEFAULT_SIZE];
            for(var i = 0; i < FrameStatePool.Length; i++)
            {
                FrameStatePool[i] = new FrameState(this, MaximumEntities, MaxHitMapPolygonsPerEntity);
            }

            OverAllocated = 0;
        }

        public FrameState CaptureFrom(GameState gameState, int windowWidth, int windowHeight, long allocatedBytes)
        {
            var ret = GetOrCreateState();

            var roomDim = gameState.RoomManager.Measure(gameState.CurrentRoom.Name);
            var cameraPos = GetCameraPosition(gameState);

            // the _room_ is smaller than the window, we want to offset the whole 
            //   camera into the window (so it's centered)
            // if the room is bigger than the window, we don't want to do anythings
            var offsetInWindowX = windowWidth / 2 - roomDim.Width / 2;
            var offsetInWindowY = windowHeight / 2 - roomDim.Height / 2;

            offsetInWindowX = Math.Max(0, offsetInWindowX);
            offsetInWindowY = Math.Max(0, offsetInWindowY);

            ret.SetOffsetInWindow(offsetInWindowX, offsetInWindowY);
            
            // the camera controls how much of the window is actually rendered
            ret.SetBackground(gameState.CurrentRoom.Name, cameraPos.X, cameraPos.Y, roomDim.Width * PositionComponent.SUBPIXELS_PER_PIXEL, roomDim.Height * PositionComponent.SUBPIXELS_PER_PIXEL);

            var exit = gameState.ExitSystem;
            if(exit.IsTransitioning)
            {
                if (exit.TryGetTransitionProgress(gameState, out var prevRoom, out var oldCameraPos, out var shiftedBy))
                {
                    // we're in a scrolling transition
                    ret.SetTransitionBackground(prevRoom, (int)oldCameraPos.X, (int)oldCameraPos.Y, (int)shiftedBy.DeltaX, (int)shiftedBy.DeltaY);
                }
                else
                {
                    // we're in a fading transition
                    ret.FadePercent = exit.GetFadeToBlackProgress();
                }
            }

            DrawAnimatedEntities(gameState, cameraPos, roomDim, ret);

            ret.MakeDebugOverlay(gameState, windowWidth, windowHeight, allocatedBytes);
            
            return ret;
        }

        private void DrawAnimatedEntities(GameState gameState, (int X, int Y) cameraPos, (int Width, int Height) roomDim, FrameState ret)
        {
            var res = gameState.EntityManager.EntitiesWithAnimation();
            foreach (var anim in res)
            {
                var e = anim.Entity;
                var c = anim.Component;
                var pos = gameState.EntityManager.GetPositionFor(e);
                if (pos == null) continue;

                var currentFrame = c.GetCurrentFrame(gameState.AnimationManager);
                var dim = gameState.AssetMeasurer.Measure(currentFrame);

                byte level = default;
                var lc = gameState.EntityManager.GetFlagComponentsForEntity(e);
                if (lc.Success)
                {
                    level =
                        (byte)
                        (lc.Value.HasFlag(FlagComponent.Level_Ceiling) ?
                            FlagComponent.Level_Ceiling :
                            (lc.Value.HasFlag(FlagComponent.Level_Top) ?
                                FlagComponent.Level_Top :
                                (lc.Value.HasFlag(FlagComponent.Level_Middle) ?
                                    FlagComponent.Level_Middle :
                                    (lc.Value.HasFlag(FlagComponent.Level_Floor) ?
                                        FlagComponent.Level_Floor :
                                        default
                                    )
                                )
                            )
                        );
                }

                var shiftedX = pos.X - cameraPos.X;
                var shiftedY = pos.Y - cameraPos.Y;

                var rightEdge = shiftedX + dim.Width;
                var bottomEdge = shiftedY + dim.Height;

                // off the left side of the screen
                if (rightEdge < 0) continue;
                // off the top side of the screen
                if (bottomEdge < 0) continue;
                // off the right side of the screen
                if (shiftedX > roomDim.Width) continue;
                // off the bottom side of the screen
                if (shiftedY > roomDim.Height) continue;

                ret.Push(shiftedX, shiftedY, level, currentFrame);
            }

            ret.SortEntities();
        }
        
        private FrameState GetOrCreateState()
        {
            for(var i = 0; i < FrameStatePool.Length; i++)
            {
                var item = FrameStatePool[i];
                if (item == null) continue;

                var res = Interlocked.CompareExchange(ref FrameStatePool[i], null, item);

                if(ReferenceEquals(item, res))
                {
                    return item;
                }
            }

            var @new = new FrameState(this, MaximumEntities, MaxHitMapPolygonsPerEntity);
            return @new;
        }

        public void Return(FrameState state)
        {
            for (var i = 0; i < FrameStatePool.Length; i++)
            {
                var item = FrameStatePool[i];
                if (item != null) continue;

                var res = Interlocked.CompareExchange(ref FrameStatePool[i], state, null);

                if (res == null)
                {
                    return;
                }
            }

            // we've been allocating too much, make a note
            Interlocked.Increment(ref OverAllocated);

            // TODO: we might want to resize FrameStatePool so we've got more time?
        }

        /// <summary>
        /// Extract the camera position from the given game state.
        /// </summary>
        private (int X, int Y) GetCameraPosition(GameState gameState)
        {
            var camera = gameState.Camera;
            var pos = gameState.EntityManager.GetPositionFor(camera);

            if(pos == null)
            {
                return (0, 0);
            }

            return (pos.X, pos.Y);
        }
    }
}
