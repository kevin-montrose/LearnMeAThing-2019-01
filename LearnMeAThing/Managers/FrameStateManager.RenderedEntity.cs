using LearnMeAThing.Assets;

namespace LearnMeAThing.Managers
{
    readonly struct RenderedEntity
    {
        public int X { get; }
        public int Y { get; }
        public AssetNames ToRender { get; }
        public byte Level { get; }

        public RenderedEntity(int x, int y, byte level, AssetNames toRender)
        {
            X = x;
            Y = y;
            Level = level;
            ToRender = toRender;
        }
    }
}
