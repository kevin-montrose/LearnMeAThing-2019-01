namespace LearnMeAThing.Assets
{
    enum RoomNames
    {
        //NONE = 0,

        Kakariko
    }
    
    /// <summary>
    /// Describes the parts of a room, 
    ///   can be used to create a Room instance
    ///   but is not itself a game "object"
    /// </summary>
    readonly struct RoomTemplate
    {
        public const int TILE_WIDTH_PIXELS =16 * 4;
        public const int TILE_HEIGHT_PIXELS = TILE_WIDTH_PIXELS;

        private readonly RoomNames _Name;
        public RoomNames Name => _Name;
        private readonly RoomNames? _RoomBelow;
        /// <summary>
        /// Reference to the room logically below this one, if any
        /// </summary>
        public RoomNames? RoomBelow => _RoomBelow;
        private readonly RoomNames? _RoomAbove;
        /// <summary>
        /// Reference to the room logically above this one, if any
        /// </summary>
        public RoomNames? RoomAbove => _RoomAbove;

        /// <summary>
        /// Room to the left, if any.
        /// </summary>
        private readonly RoomNames? _LeftExit;
        public RoomNames? LeftExit => _LeftExit;

        /// <summary>
        /// Room to the right, if any.
        /// </summary>
        private readonly RoomNames? _RightExit;
        public RoomNames? RightExit => _RightExit;

        /// <summary>
        /// Room to the north, if any.
        /// </summary>
        private readonly RoomNames? _TopExit;
        public RoomNames? TopExit => _TopExit;

        /// <summary>
        /// Room to the south, if any.
        /// </summary>
        private readonly RoomNames? _BottomExit;
        public RoomNames? BottomExit => _BottomExit;

        private readonly int _WidthInTiles;
        public int WidthInTiles => _WidthInTiles;
        private readonly int _HeightInTiles;
        public int HeightInTiles => _HeightInTiles;

        private readonly TileMap _TileMap;
        /// <summary>
        /// Reference to the tilemap that should be used to render this room.
        /// </summary>
        public TileMap TileMap => _TileMap;

        private readonly Tile[] _BackgroundTiles;
        /// <summary>
        /// The tiles making up this room, which are only meaningful when interpreted with the associated TileMap
        /// </summary>
        public Tile[] BackgroundTiles => _BackgroundTiles;

        private readonly RoomObject[] _ObjectsOnFloor;
        public RoomObject[] ObjectsOnFloor => _ObjectsOnFloor;
        
        public RoomTemplate(
            RoomNames name, 
            RoomNames? roomBelow, 
            RoomNames? roomAbove, 
            RoomNames? leftExit,
            RoomNames? rightExit,
            RoomNames? topExit,
            RoomNames? bottomExit,
            int width, 
            int height, 
            TileMap tileMap, 
            Tile[] background, 
            RoomObject[] objectsOnFloor
        )
        {
            _Name = name;
            _RoomBelow = roomBelow;
            _RoomAbove = roomAbove;
            _LeftExit = leftExit;
            _RightExit = rightExit;
            _TopExit = topExit;
            _BottomExit = bottomExit;
            _WidthInTiles = width;
            _HeightInTiles = height;
            _TileMap = tileMap;
            _BackgroundTiles = background;
            _ObjectsOnFloor = objectsOnFloor;
        }

        public override string ToString() => Name.ToString();
    }
}
