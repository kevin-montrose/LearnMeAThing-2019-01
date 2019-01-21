using System;

namespace LearnMeAThing.Utilities
{
    readonly struct Vector : IEquatable<Vector>
    {
        public static readonly Vector Zero = new Vector(FixedPoint.Zero, FixedPoint.Zero);

        public readonly FixedPoint DeltaX;
        public readonly FixedPoint DeltaY;

        public Vector(FixedPoint deltaX, FixedPoint deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public Vector(int deltaX, int deltaY)
        {
            DeltaX = FixedPoint.FromInt(deltaX);
            DeltaY = FixedPoint.FromInt(deltaY);
        }

        public FixedPoint Dot(Vector other) => FixedPoint.Dot(DeltaX, DeltaY, other.DeltaX, other.DeltaY);
        
        public FixedPoint Dot(Point other) => FixedPoint.Dot(DeltaX, DeltaY, other.X, other.Y);

        public bool TryMagnitude(out FixedPoint val) => FixedPoint.TryMagnitude(DeltaX, DeltaY, out val);
        
        public Vector Normalize()
        {
            FixedPoint.Normalize(DeltaX, DeltaY, out var x, out var y);
            return new Vector(x, y);
        }
        
        public Vector Normal()
        {
            var normalized = Normalize();

            return new Vector(-normalized.DeltaY, normalized.DeltaX);
        }

        public bool IsParallel(Vector other)
        {
            var cross = other.DeltaX * DeltaY - other.DeltaY * DeltaX;

            return cross == FixedPoint.Zero;
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.DeltaX + b.DeltaX, a.DeltaY + b.DeltaY);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.DeltaX - b.DeltaX, a.DeltaY - b.DeltaY);
        }

        public static Vector operator *(Vector a, FixedPoint b)
        {
            return new Vector(a.DeltaX * b, a.DeltaY * b);
        }

        public static Vector operator *(FixedPoint a, Vector b)
        {
            return new Vector(b.DeltaX * a, b.DeltaY * a);
        }

        public static bool operator ==(Vector a, Vector b)
        {
            return a.DeltaX == b.DeltaX && a.DeltaY == b.DeltaY;
        }

        public static bool operator !=(Vector a, Vector b)
        {
            return (a.DeltaX != b.DeltaX || a.DeltaY != b.DeltaY);
        }

        public bool Equals(Vector other) => this == other;
        public override bool Equals(object obj)
        {
            if (!(obj is Vector)) return false;

            return Equals((Vector)obj);
        }

        public override int GetHashCode()
        {
            var a = DeltaX.GetHashCode();
            var b = DeltaY.GetHashCode();

            var ret = 17;
            ret *= 23;
            ret += a;
            ret *= 23;
            ret += b;

            return ret;
        }

        public override string ToString() => $"{nameof(DeltaX)}={DeltaX}, {nameof(DeltaY)}={DeltaY}";
    }
}
