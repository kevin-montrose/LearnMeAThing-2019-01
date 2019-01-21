using LearnMeAThing.Assets;

namespace LearnMeAThing.Tests
{
    class _AssetMeasurer : IAssetMeasurer
    {
        private readonly (AssetNames Name, (int Width, int Height) Dimensions)[] Responses;

        public _AssetMeasurer(params (AssetNames Name, (int Width,int Height) Dimensions)[] responses)
        {
            Responses = responses;
        }

        public (int Width, int Height) Measure(AssetNames name)
        {
            if (Responses == null) return (0, 0);

            foreach(var item in Responses)
            {
                if (name == item.Name) return item.Dimensions;
            }

            return (0, 0);
        }
    }
}
