using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Managers
{
    /// <summary>
    /// Represents a layer of debug information that
    ///   can be rendered over the top of the game
    /// </summary>
    readonly struct DebugOverlay
    {
        // camera!
        private readonly int _CameraX;
        public int CameraX => _CameraX;
        private readonly int _CameraY;
        public int CameraY => _CameraY;

        // entities!
        public int NumEntities => EntityLocations.Count;

        private readonly Buffer<Point> _EntityLocations;
        public Buffer<Point> EntityLocations => _EntityLocations;

        private readonly Buffer<(int Width, int Height)> _EntitySpriteDimensions;
        public Buffer<(int Width, int Height)> EntitySpriteDimensions => _EntitySpriteDimensions;

        // hit maps!

        public int NumHitMaps => HitMapLocations.Count;

        private readonly Buffer<Point> _HitMapLocations;
        public Buffer<Point> HitMapLocations => _HitMapLocations;

        private readonly Buffer<(int Width, int Height)> _HitMapDimensions;
        public Buffer<(int Width, int Height)> HitMapDimensions => _HitMapDimensions;
        private readonly Buffer<ConvexPolygon> _HitMapPolygonsCartesianSubPixels;
        public Buffer<ConvexPolygon> HitMapPolygonsCartesianSubPixels => _HitMapPolygonsCartesianSubPixels;

        // timings!
        private readonly Timings _SystemTimings;
        public Timings SystemTimings => _SystemTimings;

        // memory / gc
        private readonly long _AllocatedBytes;
        public long AllocatedBytes => _AllocatedBytes;

        private readonly int[] _CollectionsPerGeneration;
        public int[] CollectionsPerGeneration => _CollectionsPerGeneration;

        public DebugOverlay(
            int cameraX,
            int cameraY,
            Buffer<Point> entityLocations,
            Buffer<(int Width, int Height)> entitySpriteDimensions,
            Buffer<Point> hitMapLocations,
            Buffer<(int Width, int Height)> hitMapDimensions,
            Buffer<ConvexPolygon> hitMapPolygons,
            Timings systemTimings,
            long allocatedBytes,
            int[] gcCollections
        )
        {
            _CameraX = cameraX;
            _CameraY = cameraY;

            _EntityLocations = entityLocations;
            _EntitySpriteDimensions = entitySpriteDimensions;

            _HitMapLocations = hitMapLocations;
            _HitMapDimensions = hitMapDimensions;
            _HitMapPolygonsCartesianSubPixels = hitMapPolygons;

            _SystemTimings = systemTimings;

            _AllocatedBytes = allocatedBytes;
            _CollectionsPerGeneration = gcCollections;
        }

        public static DebugOverlay CaptureFrom(
            GameState state, 
            int windowWidth, 
            int windowHeight,
            Buffer<Point> entityLocationBuffer,
            Buffer<(int Width, int Height)> entityDimensionBuffer,
            Buffer<Point> hitMapLocationBuffer,
            Buffer<(int Width, int Height)> hitMapDimensionBuffer,
            long allocatedBytes,
            int[] gcCollectionBuffer
        )
        {
            var manager = state.EntityManager;
            var measurer = state.AssetMeasurer;

            var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);

            Entity? camera = null;
            foreach(var e in manager.EntitiesWith(FlagComponent.Camera))
            {
                camera = e;
                break;
            }

            var cameraPos = camera != null ? manager.GetPositionFor(camera.Value) : null;
            
            entityLocationBuffer.Clear();
            entityDimensionBuffer.Clear();
            hitMapLocationBuffer.Clear();
            hitMapDimensionBuffer.Clear();

            if (cameraPos != null)
            {
                // it is intentional that we go over everything 
                //   and don't exclude stuff off screen
                // this is for debugging, and less is more here
                foreach (var t in manager.EntitiesWithAnimation())
                {
                    var e = t.Entity;
                    var c = t.Component;
                    var pos = manager.GetPositionFor(e);
                    if (pos == null) continue;

                    var shiftedByCameraX = pos.X - cameraPos.X;
                    var shiftedByCameraY = pos.Y - cameraPos.Y;

                    entityLocationBuffer.Add(new Point(shiftedByCameraX, shiftedByCameraY));
                    var frame = c.GetCurrentFrame(state.AnimationManager);
                    var dim = measurer.Measure(frame);
                    entityDimensionBuffer.Add(dim);
                }

                for(var i = 0; i < state.CollisionDetectionSystem.LastUsedPolygonsCartesian.Count; i++)
                {
                    var t = state.CollisionDetectionSystem.LastUsedPolygonsCartesian[i];
                    var locCartesian = new Point(t.BoundingX / PositionComponent.SUBPIXELS_PER_PIXEL, t.BoundingY / PositionComponent.SUBPIXELS_PER_PIXEL);
                    var locScreen = ConvertToScreen(roomDims.Height, locCartesian.X, locCartesian.Y);

                    var shiftedByCameraX = locScreen.X - cameraPos.X;
                    var shiftedByCameraY = locScreen.Y - cameraPos.Y;
                    hitMapLocationBuffer.Add(new Point(shiftedByCameraX, shiftedByCameraY));

                    var dim = ((int)(t.BoundingWidth / PositionComponent.SUBPIXELS_PER_PIXEL), (int)(t.BoundingHeight / PositionComponent.SUBPIXELS_PER_PIXEL));
                    hitMapDimensionBuffer.Add(dim);
                }
            }

            return
                new DebugOverlay(
                    (int)cameraPos.X,
                    (int)cameraPos.Y,
                    entityLocationBuffer,
                    entityDimensionBuffer,
                    hitMapLocationBuffer,
                    hitMapDimensionBuffer,
                    state.CollisionDetectionSystem.LastUsedPolygonsCartesian,
                    state.Timings,
                    allocatedBytes,
                    gcCollectionBuffer
                );
        }
        
        private static Point ConvertToScreen(int roomHeight, FixedPoint x, FixedPoint y)
        {
            return new Point(x, roomHeight - y);
        }
    }
}
