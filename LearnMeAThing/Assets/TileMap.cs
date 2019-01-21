using System;

namespace LearnMeAThing.Assets
{
    /// <summary>
    /// Represents a mapping from tile map indexes to actual assets
    ///    that we can render.
    /// </summary>
    readonly struct TileMap
    {
        public AssetNames this[int ix]
        {
            get
            {
                if (ix < 0 || ix > _TileAssets.Length) throw new ArgumentOutOfRangeException(nameof(ix));

                return _TileAssets[ix];
            }
        }

        private readonly AssetNames[] _TileAssets;

        private readonly string _Name;
        public string Name => _Name;

        public TileMap(string name, AssetNames[] tiles)
        {
            _Name = name;
            _TileAssets = tiles;
        }
    }
}
