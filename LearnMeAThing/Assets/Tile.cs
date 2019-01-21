namespace LearnMeAThing.Assets
{
    readonly struct Tile
    {
        private readonly ushort _X;
        public ushort X => _X;
        private readonly ushort _Y;
        public ushort Y => _Y;
        private readonly int _TileMapIndex;
        public int TileMapIndex => _TileMapIndex;

        public Tile(ushort x, ushort y, int ix)
        {
            _X = x;
            _Y = y;
            _TileMapIndex = ix;
        }

        public override string ToString() => $"@({X:N0}, {Y:N0}) = {TileMapIndex:N0}";
    }
}
