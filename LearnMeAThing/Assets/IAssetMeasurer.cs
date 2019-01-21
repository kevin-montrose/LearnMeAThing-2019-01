namespace LearnMeAThing.Assets
{
    interface IAssetMeasurer
    {
        (int Width, int Height) Measure(AssetNames name);
    }
}
