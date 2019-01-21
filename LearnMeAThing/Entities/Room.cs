using LearnMeAThing.Assets;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Entities
{
    /// <summary>
    /// Represents an active instance of a room.
    /// </summary>
    struct Room
    {
        private readonly RoomTemplate _Template;
        public RoomTemplate Template => _Template;

        public RoomNames Name => Template.Name;
        
        public Room(RoomTemplate template)
        {
            _Template = template;
        }
    }
}
