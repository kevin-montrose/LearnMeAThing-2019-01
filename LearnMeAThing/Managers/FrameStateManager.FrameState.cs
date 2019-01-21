using LearnMeAThing.Assets;
using LearnMeAThing.Utilities;
using System;
namespace LearnMeAThing.Managers
{
    /// <summary>
    /// Represents a frame of the game.
    /// </summary>
    sealed class FrameState : IDisposable
    {
        /// <summary>
        /// Compares RenderedEntities, placing lower levels first, followed up lower ys, followed by lower xs.
        /// </summary>
        private sealed class RenderedEntityComparer : System.Collections.Generic.IComparer<RenderedEntity>
        {
            public static readonly RenderedEntityComparer Instance = new RenderedEntityComparer();

            private RenderedEntityComparer() { }

            public int Compare(RenderedEntity a, RenderedEntity b)
            {
                var lvlC = a.Level.CompareTo(b.Level);
                if (lvlC != 0) return lvlC;
                var yC = a.Y.CompareTo(b.Y);
                if (yC != 0) return yC;

                return a.X.CompareTo(b.X);
            }
        }

        private readonly FrameStateManager Manager;

        public int EntityCount => UsedEntities;

        private int UsedEntities;
        private readonly RenderedEntity[] Entities;

        public int OffsetInWindowX;
        public int OffsetInWindowY;
        
        public RoomNames Background;
        public int BackgroundOffsetX;
        public int BackgroundOffsetY;
        public int BackgroundHeightSubPixels;
        public int BackgroundWidthSubPixels;

        public RoomNames? TransitionBackground;
        public int TransitionBackgroundSourceX;
        public int TransitionBackgroundSourceY;
        public int TransitionBackgroundDestX;
        public int TransitionBackgroundDestY;

        public int FadePercent;

        public DebugOverlay Overlay;

        private Buffer<Point> Debug_EntityLocationsBuffer;
        private Buffer<(int Width, int Height)> Debug_EntityDimensionsBuffer;
        private Buffer<Point> Debug_HitMapLocationsBuffer;
        private Buffer<(int Width, int Height)> Debug_HitMapDimensionsBuffer;

        private int[] Debug_CollectionsPerGeneration;

        public FrameState(FrameStateManager manager, int maxEntities, int maxHitMapsPerEntity)
        {
            Manager = manager;
            UsedEntities = 0;
            Entities = new RenderedEntity[maxEntities];

            Debug_EntityLocationsBuffer = new Buffer<Point>(maxEntities);
            Debug_EntityDimensionsBuffer = new Buffer<(int Width, int Height)>(maxEntities);
            Debug_HitMapLocationsBuffer = new Buffer<Point>(maxEntities * maxHitMapsPerEntity);
            Debug_HitMapDimensionsBuffer = new Buffer<(int Width, int Height)>(maxEntities * maxHitMapsPerEntity);

            Debug_CollectionsPerGeneration = new int[GC.MaxGeneration + 1];
        }

        public void SetOffsetInWindow(int x, int y)
        {
            OffsetInWindowX = x;
            OffsetInWindowY = y;
        }

        public void SetBackground(RoomNames handle, int xOffset, int yOffset, int roomWidthSubPixels, int roomHeightSubPixels)
        {
            Background = handle;
            BackgroundOffsetX = xOffset;
            BackgroundOffsetY = yOffset;
            BackgroundWidthSubPixels = roomWidthSubPixels;
            BackgroundHeightSubPixels = roomHeightSubPixels;
        }

        public void SetTransitionBackground(RoomNames handle, int sourceX, int sourceY, int destX, int destY)
        {
            TransitionBackground = handle;
            TransitionBackgroundSourceX = sourceX;
            TransitionBackgroundSourceY = sourceY;
            TransitionBackgroundDestX = destX;
            TransitionBackgroundDestY = destY;
        }

        public void MakeDebugOverlay(GameState state, int windowWidth, int windowHeight, long allocatedBytes)
        {
            for(var i = 0; i <= GC.MaxGeneration; i++)
            {
                Debug_CollectionsPerGeneration[i] = GC.CollectionCount(i);
            }

            Overlay = 
                DebugOverlay.CaptureFrom(
                    state, 
                    windowWidth, 
                    windowHeight,
                    Debug_EntityLocationsBuffer,
                    Debug_EntityDimensionsBuffer,
                    Debug_HitMapLocationsBuffer,
                    Debug_HitMapDimensionsBuffer,
                    allocatedBytes,                     // passed because _something_ has to keep track of a Process object
                    Debug_CollectionsPerGeneration
                );
        }

        public void Push(int x, int y, byte level, AssetNames handle)
        {
            if (UsedEntities == Entities.Length) throw new InvalidOperationException("Tried to push too many entities to this frame");

            Entities[UsedEntities] = new RenderedEntity(x, y, level, handle);
            UsedEntities++;
        }

        public RenderedEntity Get(int ix)
        {
            if (ix < 0) throw new IndexOutOfRangeException($"{nameof(ix)}={ix}, is less than 0");
            if (ix >= UsedEntities) throw new IndexOutOfRangeException($"{nameof(ix)}={ix}, is greater than or equal to {UsedEntities}");

            return Entities[ix];
        }

        public void SortEntities()
        {
            Array.Sort(Entities, 0, UsedEntities, RenderedEntityComparer.Instance);
        }

        public void Dispose()
        {
            UsedEntities = 0;
            Background = 0;
            BackgroundOffsetX = 0;
            BackgroundOffsetY = 0;
            OffsetInWindowX = 0;
            OffsetInWindowY = 0;
            TransitionBackground = null;
            TransitionBackgroundDestX = 0;
            TransitionBackgroundDestY = 0;
            TransitionBackgroundSourceX = 0;
            TransitionBackgroundSourceY = 0;
            Overlay = default;
            Manager.Return(this);
        }
    }
}
