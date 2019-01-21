using LearnMeAThing.Assets;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Reflection;

namespace LearnMeAThing
{
    public partial class LearnMeAThingGame : Game
    {
        private GraphicsDeviceManager Graphics;
        private SpriteBatch SpriteBatch;
        private GameState State;
        private FrameStateManager FrameManager;

        private Texture2D Black;

        private HotReloadWatcher ReloadWatcher;

        private AssetManager<Texture2D> Assets;
        private RoomManager<Texture2D> Rooms;
        private AnimationManager Animations;
        private HitMapManager HitMaps;
        
        private readonly string ContentPath;

        private readonly Stopwatch DrawTimer;
        private readonly FPS FPSTracker;

        private readonly Process ThisProcess;

        public LearnMeAThingGame(string contentOverride)
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            var exePath = Assembly.GetExecutingAssembly().Location;
            var defaultContentDirectory = System.IO.Path.GetDirectoryName(exePath);
            if (contentOverride != null && System.IO.Directory.Exists(contentOverride))
            {
                ContentPath = contentOverride;
            }
            else
            {
                ContentPath = System.IO.Path.Combine(defaultContentDirectory, "Content");
            }

            DebugSettings = DebugFlags.None;
            DrawTimer = new Stopwatch();
            FPSTracker = new FPS(60);
            ThisProcess = Process.GetCurrentProcess();
        }
        
        protected override void Initialize()
        {
#if DEBUG
            Window.Title = "Learn Me A Thing (DEBUG)";
#else
            Window.Title = "Learn Me A Thing (RELEASE)";
#endif

            var imgPath = System.IO.Path.Combine(ContentPath, "Images");
            var roomPath  = System.IO.Path.Combine(ContentPath, "Rooms");
            var animationPath = System.IO.Path.Combine(ContentPath, "Animations");
            var hitMapPath = System.IO.Path.Combine(ContentPath, "HitMaps");

            State = new GameState();
            Assets = new AssetManager<Texture2D>(imgPath, PixelsToTexture, FreeTexture);
            Rooms = new RoomManager<Texture2D>(roomPath, RoomToTexture, FreeTexture);
            Animations = new AnimationManager(animationPath);
            HitMaps = new HitMapManager(hitMapPath, 100);
            FrameManager = new FrameStateManager(GameState.MAX_ENTITIES, 20);

            ReloadWatcher =
                new HotReloadWatcher(
                    ContentPath,
                    TimeSpan.FromSeconds(1),
                    (folder) =>
                    {
                        switch (folder.ToLowerInvariant())
                        {
                            case "images":  Assets.Reload(); break;
                            case "rooms": Rooms.Reload(); break;
                            case "animations": Animations.Reload(); break;
                            case "hitmaps": HitMaps.Reload(); break;
                        }
                    }
                );

            base.Initialize();

            // map our rooms' backgrounds to one big texture
            Texture2D RoomToTexture(RoomTemplate room)
            {
                var tileMap = room.TileMap;

                var widthPixels = room.WidthInTiles * RoomTemplate.TILE_WIDTH_PIXELS;
                var heightPixels = room.WidthInTiles * RoomTemplate.TILE_HEIGHT_PIXELS;

                var ret = new RenderTarget2D(GraphicsDevice, widthPixels, heightPixels);
                GraphicsDevice.SetRenderTarget(ret);
                
                GraphicsDevice.Clear(Color.Black);
                SpriteBatch.Begin(blendState: BlendState.NonPremultiplied);

                foreach (var tile in room.BackgroundTiles)
                {
                    var tileX = tile.X * RoomTemplate.TILE_WIDTH_PIXELS;
                    var tileY = tile.Y * RoomTemplate.TILE_HEIGHT_PIXELS;

                    var tileAsset = tileMap[tile.TileMapIndex];
                    var tileImg = Assets.Get(tileAsset);

                    SpriteBatch.Draw(tileImg, new Vector2(tileX, tileY), Color.White);
                }

                SpriteBatch.End();

                GraphicsDevice.SetRenderTarget(null);

                return ret;
            }

            // map our pixels to MonoGame pixels
            Texture2D PixelsToTexture(int[] pixs, ushort width, ushort height)
            {
                var ret = new Texture2D(GraphicsDevice, width, height);
                var data = new Color[width * height];
                for(var y = 0; y < height; y++)
                {
                    for(var x = 0; x < width; x++)
                    {
                        var ix = y * width + x;
                        var pix = new Pixel(pixs[ix]);
                        
                        data[ix] = new Color(pix.Red, pix.Green, pix.Blue, pix.Alpha);
                    }
                }

                ret.SetData(data);

                return ret;
            }

            // release MonoGame things
            void FreeTexture(Texture2D txt)
            {
                txt.Dispose();
            }
        }
        
        protected override void LoadContent()
        {
            // create our sprite batch for writing
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // initialize the asset manager
            Assets.Initialize();

            // initialize the room manager
            //   note that this _depends_ on Assets
            //   having finished initializing
            Rooms.Initialize();

            // intialize the animation manager
            Animations.Initialize();

            // initialize the hitmap manager
            HitMaps.Initialize();

            // initialize the game state
            State.Initialize(Rooms, MonoGameHardwareInputAdapter.Instance, Assets, Animations, HitMaps);

            // resize the window
            Graphics.PreferredBackBufferWidth = GameState.WIDTH_HACK;
            Graphics.PreferredBackBufferHeight = GameState.HEIGHT_HACK;
            Graphics.ApplyChanges();

            // get our black box ready
            Black = MakeSolidDot(Color.Black);

            // HACK
            CollisionDot_HACK = new Texture2D(GraphicsDevice, 5, 5);
            {
                var data = new Color[5 * 5];
                for (int i = 0; i < data.Length; ++i) data[i] = Color.Azure;
                CollisionDot_HACK.SetData(data);
            }

            DebugColorBlack_HACK = MakeSolidDot(Color.Black);
            DebugColor1_HACK = MakeSolidDot(Color.Cyan);
            DebugColor2_HACK = MakeSolidDot(Color.Magenta);
            DebugColor3_HACK = MakeSolidDot(Color.Yellow);
            DebugColor4_HACK = MakeSolidDot(Color.Chartreuse);
            DebugColor5_HACK = MakeSolidDot(Color.DeepPink);
            DebugColor6_HACK = MakeSolidDot(Color.FloralWhite);
            DebugColor7_HACK = MakeSolidDot(Color.ForestGreen);
            DebugColor8_HACK = MakeSolidDot(Color.Gold);
            DebugColor9_HACK = MakeSolidDot(Color.GreenYellow);
            DebugColor10_HACK = MakeSolidDot(Color.IndianRed);
            DebugColor11_HACK = MakeSolidDot(Color.Lime);
            DebugColor12_HACK = MakeSolidDot(Color.Maroon);
            DebugColor13_HACK = MakeSolidDot(Color.MintCream);
            DebugColor14_HACK = MakeSolidDot(Color.Olive);
            DebugColor15_HACK = MakeSolidDot(Color.Orange);
            DebugColor16_HACK = MakeSolidDot(Color.Pink);
            
            //State.MakeCollisionTest_HACK();
            // END HACK

#if DEBUG
            ReloadWatcher.Start();
#endif

            Texture2D MakeSolidDot(Color c)
            {
                var ret = new Texture2D(GraphicsDevice, 1, 1);
                var data = new Color[1];
                for (int i = 0; i < data.Length; ++i) data[i] = c;
                ret.SetData(data);

                return ret;
            }
        }

        protected override void UnloadContent() { }
        
        protected override void Update(GameTime gameTime)
        {
            State.Update(gameTime.ElapsedGameTime);

            CheckDebugRequests();

            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            DrawTimer.Restart();

#if DEBUG
            ReloadWatcher.Check();
#endif
            
            GraphicsDevice.Clear(Color.Black);
            SpriteBatch.Begin(blendState: BlendState.NonPremultiplied);

            var allocatedBytes = ThisProcess.PrivateMemorySize64;

            using (var renderState = FrameManager.CaptureFrom(State, Graphics.PreferredBackBufferWidth, Graphics.PreferredBackBufferHeight, allocatedBytes))
            {
                var background = Rooms.GetBackground(renderState.Background);
                var visibleWidth = Math.Min(Graphics.PreferredBackBufferWidth, background.Width - renderState.BackgroundOffsetX);
                var visibleHeight = Math.Min(Graphics.PreferredBackBufferHeight, background.Height - renderState.BackgroundOffsetY);
                var backgroundSourceRect =
                    new Rectangle(
                        renderState.BackgroundOffsetX,
                        renderState.BackgroundOffsetY,
                        visibleWidth,
                        visibleHeight
                    );
                var backgroundDestPoint = new Vector2(renderState.OffsetInWindowX, renderState.OffsetInWindowY);

                SpriteBatch.Draw(background, backgroundDestPoint, backgroundSourceRect, Color.White);

                if (renderState.TransitionBackground.HasValue)
                {
                    var transitionBackground = Rooms.GetBackground(renderState.TransitionBackground.Value);
                    var transitionSourceRect = 
                        new Rectangle(
                            renderState.TransitionBackgroundSourceX, 
                            renderState.TransitionBackgroundSourceY, 
                            transitionBackground.Width - renderState.TransitionBackgroundSourceX,
                            transitionBackground.Height - renderState.TransitionBackgroundSourceY
                    );

                    var transitionDestPoint = 
                        new Vector2(
                            renderState.TransitionBackgroundDestX + renderState.OffsetInWindowX, 
                            renderState.TransitionBackgroundDestY + renderState.OffsetInWindowY
                        );

                    SpriteBatch.Draw(transitionBackground, transitionDestPoint, transitionSourceRect, Color.White);
                }

                for (var i = 0; i < renderState.EntityCount; i++)
                {
                    var toRender = renderState.Get(i);
                    var vec = new Vector2(renderState.OffsetInWindowX + toRender.X, renderState.OffsetInWindowY + toRender.Y);
                    var sprite = Assets.Get(toRender.ToRender);

                    SpriteBatch.Draw(sprite, vec, Color.White);
                }
                
                if(renderState.FadePercent != 0)
                {
                    var scale = renderState.FadePercent / 100f;
                    var scaleColor = Color.White * scale;
                    var wholeScreen = new Rectangle(0, 0, GameState.WIDTH_HACK, GameState.HEIGHT_HACK);

                    SpriteBatch.Draw(Black, wholeScreen, scaleColor);
                }

                RenderDebugOverlay(renderState);
            }
            
            SpriteBatch.End();
            
            base.Draw(gameTime);

            DrawTimer.Stop();
            var renderTime = DrawTimer.Elapsed;

            FPSTracker.PushRenderTime(renderTime);
            FPSTracker.FrameFinished();
        }
        
        private void DrawText(string text, Utilities.Point topLeftScreen)
        {
            var fontSprite = Assets.Get(AssetNames.Font);
            var curPoint = topLeftScreen;
            foreach(var c in text)
            {
                Font.GetBounds(c, out var x, out var y);

                SpriteBatch.Draw(fontSprite, new Vector2((int)curPoint.X, (int)curPoint.Y), new Rectangle(x, y, Font.CHARACTER_WIDTH, Font.CHARACTER_HEIGHT), Color.White);

                curPoint = new Utilities.Point(curPoint.X + Font.CHARACTER_WIDTH, curPoint.Y);
            }
        }

        private void DrawText(long num, Utilities.Point topLeftScreen)
        {
            var fontSprite = Assets.Get(AssetNames.Font);
            var curPoint = topLeftScreen;
            
            // take care of the negative sign
            if(num < 0)
            {
                DrawText("-", topLeftScreen);
                curPoint = new Utilities.Point(curPoint.X + Font.CHARACTER_WIDTH, curPoint.Y);

                num = -num;
            }

            // now we move right-to-left
            var numWidth = MeasureTextWidth(num);
            curPoint = new Utilities.Point(curPoint.X + numWidth - Font.CHARACTER_WIDTH, curPoint.Y);

            var numChars = 0;
            do
            {
                // add commas every 3 digits
                if (numChars % 3 == 0 && numChars > 0)
                {
                    DrawText(",", curPoint);
                    curPoint = new Utilities.Point(curPoint.X - Font.CHARACTER_WIDTH, curPoint.Y);
                }

                // draw the current digit
                var digit = num % 10;
                switch (digit)
                {
                    case 0: DrawText("0", curPoint); break;
                    case 1: DrawText("1", curPoint); break;
                    case 2: DrawText("2", curPoint); break;
                    case 3: DrawText("3", curPoint); break;
                    case 4: DrawText("4", curPoint); break;
                    case 5: DrawText("5", curPoint); break;
                    case 6: DrawText("6", curPoint); break;
                    case 7: DrawText("7", curPoint); break;
                    case 8: DrawText("8", curPoint); break;
                    case 9: DrawText("9", curPoint); break;
                    default: throw new InvalidOperationException($"Unexpected digit: {digit}");
                }
                curPoint = new Utilities.Point(curPoint.X - Font.CHARACTER_WIDTH, curPoint.Y);
                numChars++;

                // shift a digit over
                num /= 10;
            } while (num > 0);
        }

        private int MeasureTextWidth(string str) => str.Length * Font.CHARACTER_WIDTH;
        private int MeasureTextWidth(long num)
        {
            var needsNegativeSign = false;
            // count the negative sign
            if(num < 0)
            {
                needsNegativeSign = true;
                num = -num;
            }

            var numChars = 0;

            do
            {
                // add commas every 3 digits
                if(numChars % 3 == 0 && numChars > 0)
                {
                    numChars++;
                }
                numChars++;
                num /= 10;
            } while (num > 0);

            if (needsNegativeSign) numChars++;

            return numChars * Font.CHARACTER_WIDTH;
        }

        
        private (int X, int Y) TranslateToRenderCoords(FrameState renderState, int screenX, int screenY)
        {
            var x = renderState.OffsetInWindowX + screenX;
            var y = renderState.OffsetInWindowY + screenY;

            return (x, y);
        }
        
        private void DrawSquare(FrameState renderState, Utilities.Point pt, (int Width, int Height) dims, Texture2D color)
        {
            var (renderLeftX, renderTopY) = TranslateToRenderCoords(renderState, (int)pt.X, (int)pt.Y);
            var renderRightX = renderLeftX + dims.Width;
            var renderBottomY = renderTopY + dims.Height;

            var topLeft = new Utilities.Point(renderLeftX, renderTopY);
            var topRight = new Utilities.Point(renderRightX, renderTopY);
            var bottomRight = new Utilities.Point(renderRightX, renderBottomY);
            var bottomLeft = new Utilities.Point(renderLeftX, renderBottomY);

            // draw the box
            DrawLine(topLeft, topRight, color);
            DrawLine(topRight, bottomRight, color);
            DrawLine(bottomRight, bottomLeft, color);
            DrawLine(bottomLeft, topLeft, color);

            // draw a dot
            var dotTopLeft = new Utilities.Point(topLeft.X - 1, topLeft.Y - 1);
            var dotTopRight = new Utilities.Point(topLeft.X + 1, dotTopLeft.Y);
            var dotBottomLeft = new Utilities.Point(dotTopLeft.X, topLeft.Y + 1);
            var dotBottomRight = new Utilities.Point(dotTopRight.X, dotBottomLeft.Y);
            DrawLine(dotTopLeft, dotTopRight, color);
            DrawLine(dotTopRight, dotBottomRight, color);
            DrawLine(dotBottomRight, dotBottomLeft, color);
            DrawLine(dotBottomLeft, dotTopLeft, color);
        }

        private void DrawLine(Utilities.Point p1, Utilities.Point p2, Texture2D color)
        {
            // from: https://gamedev.stackexchange.com/a/44016/155
            var deltaX = (int)(p2.X - p1.X);
            var deltaY = (int)(p2.Y - p1.Y);
            var vec = new Vector(deltaX, deltaY);
            var angle = (float)Math.Atan2(deltaY, deltaX);

            if (!vec.TryMagnitude(out var lenFP)) return;

            var len = (int)lenFP;

            SpriteBatch.Draw(
                color,
                new Rectangle(
                    (int)p1.X,
                    (int)p1.Y,
                    len,
                    1
                ),
                null,
                Color.White,
                angle,
                Vector2.Zero,
                SpriteEffects.None,
                0
            );
        }
    }
}
