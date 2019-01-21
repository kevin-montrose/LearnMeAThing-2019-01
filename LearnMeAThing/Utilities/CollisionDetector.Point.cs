using System;

namespace LearnMeAThing.Utilities
{
    readonly struct Point: IEquatable<Point>
    {
        public readonly FixedPoint X;
        public readonly FixedPoint Y;

        public Point(FixedPoint x, FixedPoint y)
        {
            X = x;
            Y = y;
        }

        public Point(int x, int y)
        {
            X = FixedPoint.FromInt(x);
            Y = FixedPoint.FromInt(y);
        }

        public Point ProjectOnto(Vector b)
        {
            var bNorm = b.Normalize();

            var a = new Vector(X, Y);
            var res = (a.Dot(bNorm)) * bNorm;

            return new Point(res.DeltaX, res.DeltaY);
        }

        public static bool operator ==(Point a, Point b) => a.Equals(b);
        public static bool operator !=(Point a, Point b) => !a.Equals(b);

        public bool Equals(Point other)
        {
            var diffX = other.X - X;
            var diffY = other.Y - Y;

            var diff = new Vector(diffX, diffY);

            if (!diff.TryMagnitude(out var diffMag)) return false;
            
            return diffMag == FixedPoint.Zero;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point)) return false;

            return Equals((Point)obj);
        }

        public override string ToString() => $"{nameof(X)}={X}, {nameof(Y)}={Y}";

        public override int GetHashCode()
        {
            var ret = 17;
            ret *= 23;
            ret += X.GetHashCode();
            ret *= 23;
            ret += Y.GetHashCode();
            return ret;
        }
    }
}
