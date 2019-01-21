using System;

namespace LearnMeAThing.Entities
{
    readonly struct Entity: IEquatable<Entity>
    {
        public readonly int Id;

        public Entity(int id)
        {
            Id = id;
        }

        public override string ToString() => $"{nameof(Id)}={Id:N0}";

        public bool Equals(Entity other)
        => Id == other.Id;

        public override bool Equals(object obj)
        {
            if (!(obj is Entity)) return false;

            return Equals((Entity)obj);
        }

        public override int GetHashCode()
         => Id;
    }
}
