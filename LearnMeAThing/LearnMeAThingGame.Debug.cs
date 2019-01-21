using LearnMeAThing.Components;
using LearnMeAThing.Managers;
using LearnMeAThing.Systems;
using LearnMeAThing.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace LearnMeAThing
{
    public partial class LearnMeAThingGame
    {
        private Texture2D CollisionDot_HACK;

        private Texture2D DebugColorBlack_HACK;
        private Texture2D DebugColor1_HACK;
        private Texture2D DebugColor2_HACK;
        private Texture2D DebugColor3_HACK;
        private Texture2D DebugColor4_HACK;
        private Texture2D DebugColor5_HACK;
        private Texture2D DebugColor6_HACK;
        private Texture2D DebugColor7_HACK;
        private Texture2D DebugColor8_HACK;
        private Texture2D DebugColor9_HACK;
        private Texture2D DebugColor10_HACK;
        private Texture2D DebugColor11_HACK;
        private Texture2D DebugColor12_HACK;
        private Texture2D DebugColor13_HACK;
        private Texture2D DebugColor14_HACK;
        private Texture2D DebugColor15_HACK;
        private Texture2D DebugColor16_HACK;

        private DebugFlags DebugSettings;

        private bool CanAcceptDebug = true;
        private void CheckDebugRequests()
        {
            var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            if (CanAcceptDebug)
            {
                var dDown = state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D);

                if (!dDown) return;

                var one = state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1);
                var two = state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2);
                var three = state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3);
                var four = state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4);
                var five = state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D5);

                var toFlip = DebugFlags.None;
                if (one) toFlip |= DebugFlags.SpriteBoundingBoxes;
                if (two) toFlip |= DebugFlags.HitMapBoundingBoxes;
                if (three) toFlip |= DebugFlags.HitMapPolygons;
                if (four) toFlip |= DebugFlags.SystemTimings;
                if (five) toFlip |= DebugFlags.FramesPerSecond;

                DebugSettings ^= toFlip;

                CanAcceptDebug = false;
            }
            else
            {
                var dUp = state.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.D);
                if (dUp)
                {
                    CanAcceptDebug = true;
                }
            }
        }

        private Texture2D GetDebugColorForIndex(int i)
        {
            Texture2D color;
            switch (i % 16)
            {
                case 0: color = DebugColor1_HACK; break;
                case 1: color = DebugColor2_HACK; break;
                case 2: color = DebugColor3_HACK; break;
                case 3: color = DebugColor4_HACK; break;
                case 4: color = DebugColor5_HACK; break;
                case 5: color = DebugColor6_HACK; break;
                case 6: color = DebugColor7_HACK; break;
                case 7: color = DebugColor8_HACK; break;
                case 8: color = DebugColor9_HACK; break;
                case 9: color = DebugColor10_HACK; break;
                case 10: color = DebugColor11_HACK; break;
                case 11: color = DebugColor12_HACK; break;
                case 12: color = DebugColor13_HACK; break;
                case 13: color = DebugColor14_HACK; break;
                case 14: color = DebugColor15_HACK; break;
                case 15: color = DebugColor16_HACK; break;
                default: throw new InvalidOperationException();
            }

            return color;
        }

        private void RenderDebugOverlay(FrameState renderState)
        {
            if (DebugSettings.HasFlag(DebugFlags.SpriteBoundingBoxes))
            {
                RenderSpriteBoundingBoxes(renderState);
            }

            if (DebugSettings.HasFlag(DebugFlags.HitMapBoundingBoxes))
            {
                RenderHitMapBoundingBoxes(renderState);
            }

            if (DebugSettings.HasFlag(DebugFlags.HitMapPolygons))
            {
                RenderHitMapPolygons(renderState);
            }

            if (DebugSettings.HasFlag(DebugFlags.SystemTimings))
            {
                RenderTimingsGraph(renderState);
            }

            if (DebugSettings.HasFlag(DebugFlags.FramesPerSecond))
            {
                RenderFramesPerSecond(renderState);
            }

            RenderDebugLayerNames();
        }

        private void RenderFramesPerSecond(FrameState renderState)
        {
            var fpsY = 0;
            var avgRenderY = Font.CHARACTER_HEIGHT;
            var allocY = 2 * Font.CHARACTER_HEIGHT;
            var collectionY = 3 * Font.CHARACTER_HEIGHT;
            
            // how much time are we spending drawing?
            {
                var avgRenderMs = FPSTracker.AverageRenderTimeMilliseconds;
                if (avgRenderMs != null)
                {
                    var strLen = MeasureTextWidth("Render: ");
                    var numLen = MeasureTextWidth(avgRenderMs.Value);
                    var suffixLen = MeasureTextWidth("ms");

                    var avgRenderX = GameState.WIDTH_HACK - strLen - numLen - suffixLen;

                    DrawText("Render: ", new Utilities.Point(avgRenderX, avgRenderY));
                    avgRenderX += strLen;
                    DrawText(avgRenderMs.Value, new Utilities.Point(avgRenderX, avgRenderY));
                    avgRenderX += numLen;
                    DrawText("ms", new Utilities.Point(avgRenderX, avgRenderY));
                }
            }

            // how many frames are we rendering a second?
            {
                var fps = FPSTracker.FramesPerSecond;
                if (fps != null)
                {
                    var strLen = MeasureTextWidth("FPS: ");
                    var numLen = MeasureTextWidth(fps.Value);
                    var fpsX = GameState.WIDTH_HACK - strLen - numLen;
                    DrawText("FPS: ", new Utilities.Point(fpsX, fpsY));
                    fpsX += MeasureTextWidth("FPS: ");
                    DrawText(fps.Value, new Utilities.Point(fpsX, fpsY));
                }
            }

            // how many bytes have we allocated?
            {
                var allocBytes = renderState.Overlay.AllocatedBytes;
                var strLen = MeasureTextWidth("Alloc: ");
                var numLen = MeasureTextWidth(allocBytes);

                var allocX = GameState.WIDTH_HACK - strLen - numLen;
                DrawText("Alloc: ", new Utilities.Point(allocX, allocY));
                allocX += strLen;
                DrawText(allocBytes, new Utilities.Point(allocX, allocY));
            }

            // how many collections have we performed?
            {
                var strLen = MeasureTextWidth("GC: ");
                var valueLen = 0;
                
                for (var i = 0; i < renderState.Overlay.CollectionsPerGeneration.Length; i++)
                {
                    if (i != 0)
                    {
                        valueLen += MeasureTextWidth("-");
                    }

                    var cs = renderState.Overlay.CollectionsPerGeneration[i];
                    valueLen += MeasureTextWidth(cs);
                }

                var collectionX = GameState.WIDTH_HACK - strLen - valueLen;
                DrawText("GC: ", new Utilities.Point(collectionX, collectionY));
                collectionX += strLen;

                for (var i = 0; i < renderState.Overlay.CollectionsPerGeneration.Length; i++)
                {
                    if (i != 0)
                    {
                        DrawText("-", new Utilities.Point(collectionX, collectionY));
                        collectionX += MeasureTextWidth("-");
                    }

                    var cs = renderState.Overlay.CollectionsPerGeneration[i];
                    DrawText(cs, new Utilities.Point(collectionX, collectionY));

                    collectionX += MeasureTextWidth(cs);
                }
            }
        }

        private void RenderTimingsGraph(FrameState state)
        {
            // this is ~1/60th of a second, so we should scale everything such that it's relative to that
            const double MAX_TICKS_PER_FRAME = TimeSpan.TicksPerMillisecond * 16;
            const int DESIRED_COLUMN_HEIGHT = 128;
            const int DESIRED_COLUMN_WIDTH = 4;

            var overlay = state.Overlay;
            var timings = overlay.SystemTimings;

            var totalWidth = timings.NumPointsTracked * DESIRED_COLUMN_WIDTH;

            // fill in the back, so we can see rough percents
            DrawRectangle(new Utilities.Point(GameState.WIDTH_HACK - totalWidth, GameState.HEIGHT_HACK - DESIRED_COLUMN_HEIGHT), totalWidth, DESIRED_COLUMN_HEIGHT, DebugColorBlack_HACK);

            var lastLabelY = GameState.HEIGHT_HACK;
            var alreadyDrawn = new System.Collections.Generic.HashSet<SystemType>();

            for (var pointIx = 0; pointIx < timings.NumPointsTracked; pointIx++)
            {
                var columnX = GameState.WIDTH_HACK - totalWidth + pointIx * DESIRED_COLUMN_WIDTH;
                var heightSoFar = 0;
                foreach (SystemType system in Enum.GetValues(typeof(SystemType)))
                {
                    if (system == SystemType.NONE) continue;

                    var ix = (int)system;
                    var color = GetDebugColorForIndex(ix);

                    var ticksAtTime = timings.GetTicksForSystem(system)[pointIx];

                    // I'm ok with using floating point here
                    //     because these numbers are potentially quite large
                    //     and only for debugging; so FixedPoint isn't a good
                    //     fit.
                    var percHeight = ((double)ticksAtTime) / (double)MAX_TICKS_PER_FRAME;
                    var height = (int)Math.Round(DESIRED_COLUMN_HEIGHT * percHeight);
                    if (height <= 0) continue;

                    if (alreadyDrawn.Add(system))
                    {
                        // label the thing
                        var text = system.ToString();
                        var textX = GameState.WIDTH_HACK - totalWidth - text.Length * Font.CHARACTER_WIDTH;
                        var textY = lastLabelY - Font.CHARACTER_HEIGHT;
                        DrawText(text, new Utilities.Point(textX, textY));
                        lastLabelY = textY;

                        // draw a dot to illistrate the color
                        var dotX = textX - 2 * Font.CHARACTER_WIDTH / 3;
                        var dotY = textY + Font.CHARACTER_HEIGHT / 3;
                        var dotWidth = Font.CHARACTER_WIDTH / 3;
                        var dotHeight = Font.CHARACTER_HEIGHT / 3;
                        DrawRectangle(new Utilities.Point(dotX, dotY), dotWidth, dotHeight, color);
                    }

                    var topLeft = new Utilities.Point(columnX, GameState.HEIGHT_HACK - heightSoFar - height);

                    DrawRectangle(topLeft, DESIRED_COLUMN_WIDTH, height, color);

                    heightSoFar += height;
                }
            }

            void DrawRectangle(Utilities.Point topLeft, int width, int height, Texture2D color)
            {
                SpriteBatch.Draw(color, new Rectangle((int)topLeft.X, (int)topLeft.Y, width, height), Color.White);
            }
        }

        private void RenderDebugLayerNames()
        {
            var text = "";
            if (DebugSettings.HasFlag(DebugFlags.HitMapPolygons)) text += (text.Length > 0 ? ", " : "") + nameof(DebugFlags.HitMapPolygons);
            if (DebugSettings.HasFlag(DebugFlags.SpriteBoundingBoxes)) text += (text.Length > 0 ? ", " : "") + nameof(DebugFlags.SpriteBoundingBoxes);
            if (DebugSettings.HasFlag(DebugFlags.HitMapBoundingBoxes)) text += (text.Length > 0 ? ", " : "") + nameof(DebugFlags.HitMapBoundingBoxes);
            if (DebugSettings.HasFlag(DebugFlags.SystemTimings)) text += (text.Length > 0 ? ", " : "") + nameof(DebugFlags.SystemTimings);
            // intentionally not doing FPS, it has it's own label

            if (text.Length == 0) return;

            DrawText("Debug: " + text, new Utilities.Point(0, 0));
        }

        private void RenderSpriteBoundingBoxes(FrameState renderState)
        {
            var overlay = renderState.Overlay;
            for (var i = 0; i < overlay.NumEntities; i++)
            {
                var pt = overlay.EntityLocations[i];
                var dims = overlay.EntitySpriteDimensions[i];

                var color = GetDebugColorForIndex(i);
                DrawSquare(renderState, pt, dims, color);
            }
        }

        private void RenderHitMapBoundingBoxes(FrameState renderState)
        {
            var overlay = renderState.Overlay;

            for (var i = 0; i < overlay.NumHitMaps; i++)
            {
                var pt = overlay.HitMapLocations[i];
                var dims = overlay.HitMapDimensions[i];

                var color = GetDebugColorForIndex(i);
                DrawSquare(renderState, pt, dims, color);
            }
        }

        private void RenderHitMapPolygons(FrameState renderState)
        {
            var roomHeightSubPixels = renderState.BackgroundHeightSubPixels;

            var overlay = renderState.Overlay;

            for (var i = 0; i < overlay.NumHitMaps; i++)
            {
                var color = GetDebugColorForIndex(i);
                var polyCartesianSubPixels = overlay.HitMapPolygonsCartesianSubPixels[i];

                for (var j = 0; j < polyCartesianSubPixels.NumVertices; j++)
                {
                    var nextIx = j + 1;
                    if (nextIx == polyCartesianSubPixels.NumVertices)
                    {
                        nextIx = 0;
                    }

                    var v1Cartesian = polyCartesianSubPixels.GetVertex(j);
                    var v2Cartesian = polyCartesianSubPixels.GetVertex(nextIx);

                    var v1Screen = new Utilities.Point(v1Cartesian.X, roomHeightSubPixels - v1Cartesian.Y);
                    var v2Screen = new Utilities.Point(v2Cartesian.X, roomHeightSubPixels - v2Cartesian.Y);

                    var v1Pixels = new Utilities.Point(v1Screen.X / PositionComponent.SUBPIXELS_PER_PIXEL, v1Screen.Y / PositionComponent.SUBPIXELS_PER_PIXEL);
                    var v2Pixels = new Utilities.Point(v2Screen.X / PositionComponent.SUBPIXELS_PER_PIXEL, v2Screen.Y / PositionComponent.SUBPIXELS_PER_PIXEL);

                    var v1AfterCamera = new Utilities.Point(v1Pixels.X - overlay.CameraX, v1Pixels.Y - overlay.CameraY);
                    var v2AfterCamera = new Utilities.Point(v2Pixels.X - overlay.CameraX, v2Pixels.Y - overlay.CameraY);

                    DrawLine(v1AfterCamera, v2AfterCamera, color);
                }
            }
        }
    }
}
