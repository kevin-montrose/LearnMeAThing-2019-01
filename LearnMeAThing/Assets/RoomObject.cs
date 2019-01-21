using System;

namespace LearnMeAThing.Assets
{
    readonly struct RoomObjectProperty
    {
        private readonly string _Name;
        public string Name => _Name;
        private readonly string _Value;
        public string Value => _Value;

        public RoomObjectProperty(string name, string value)
        {
            _Name = name;
            _Value = value;
        }

        public override string ToString() => $"{Name}={Value}";
    }

    public enum RoomObjectTypes
    {
        NONE = 0,

        FlowerTopLeft,
        FlowerTopLeftBottomRight,

        Tree,

        BushWall_TopBottom,

        Bush,

        Door,

        Stairs,

        Pit,

        SwordKnight
    }

    readonly struct RoomObject
    {
        private readonly RoomObjectTypes _Type;
        public RoomObjectTypes Type => _Type;

        private readonly int _X;
        public int X => _X;
        private readonly int _Y;
        public int Y => _Y;

        private readonly RoomObjectProperty[] _Properties;
        public RoomObjectProperty[] Properties => _Properties;

        public RoomObject(RoomObjectTypes type, int x, int y, RoomObjectProperty[] properties)
        {
            _Type = type;
            _X = x;
            _Y = y;
            _Properties = properties;
        }

        public string GetProperty(string name)
        {
            foreach(var p in Properties)
            {
                if(p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return p.Value;
                }
            }

            return null;
        }

        public override string ToString() => $"{Type}: {string.Join(", ", Properties)}";
    }
}
