using LearnMeAThing.Assets;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Tests
{
    class _IHitMapManager : IHitMapManager
    {
        private readonly (AssetNames Name, ConvexPolygonPattern[] Polygon, (int Width, int Height) Dimensions)[] Responses;

        public _IHitMapManager(params (AssetNames Name, ConvexPolygonPattern[] Polygon, (int Width, int Height) Dimensions)[] responses)
        {
            Responses = responses;
        }
        
        public ConvexPolygonPattern[] GetFor(AssetNames name)
        {
            if (Responses == null) return default;

            foreach(var r in Responses)
            {
                if (r.Name == name) return r.Polygon;
            }

            return default;
        }

        public (int Width, int Height) Measure(AssetNames name)
        {
            if (Responses == null) return default;

            foreach (var r in Responses)
            {
                if (r.Name == name) return r.Dimensions;
            }

            return default;
        }
    }
}
