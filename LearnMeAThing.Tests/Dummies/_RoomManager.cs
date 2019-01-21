using LearnMeAThing.Assets;
using LearnMeAThing.Managers;

namespace LearnMeAThing.Tests
{
    class _RoomManager : IRoomManager
    {
        private readonly (RoomNames Room, RoomTemplate Response, int? Width, int? Height)[] Responses;

        public _RoomManager(params (RoomNames Room, RoomTemplate Response, int? Width, int? Height)[] responses)
        {
            Responses = responses;
        }

        public RoomTemplate Get(RoomNames name)
        {
            if (Responses == null) return default;

            for (var i = 0; i < Responses.Length; i++)
            {
                var r = Responses[i];
                if (r.Room == name) return r.Response;
            }

            return default;
        }
        
        public void Initialize() { }

        public (int Width, int Height) Measure(RoomNames name)
        {
            if (Responses == null) return (0, 0);

            for (var i = 0; i < Responses.Length; i++)
            {
                var r = Responses[i];
                if (r.Room == name)
                {
                    if (r.Width.HasValue) return (r.Width.Value, r.Height.Value);

                    return (r.Response.WidthInTiles * RoomTemplate.TILE_WIDTH_PIXELS, r.Response.WidthInTiles * RoomTemplate.TILE_HEIGHT_PIXELS);
                }
            }

            return (0, 0);
        }
    }
}
