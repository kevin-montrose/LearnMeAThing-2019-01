using System;

namespace LearnMeAThing.Utilities
{
    [Flags]
    enum DebugFlags
    {
        None = 0,
        SpriteBoundingBoxes = 1,
        HitMapBoundingBoxes = 2,
        HitMapPolygons = 4,
        SystemTimings = 8,
        FramesPerSecond = 16
    }
}
