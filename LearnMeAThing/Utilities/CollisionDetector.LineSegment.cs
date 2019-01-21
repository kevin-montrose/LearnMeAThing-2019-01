using System;

namespace LearnMeAThing.Utilities
{
    readonly struct LineSegment1D: IEquatable<LineSegment1D>
    {
        public readonly FixedPoint Start;
        public readonly FixedPoint Stop;

        public FixedPoint Length => Stop - Start;

        public LineSegment1D(FixedPoint start, FixedPoint stop)
        {
            if (start > stop) throw new InvalidOperationException($"Line segement must have positive length, {nameof(start)} must be <= {nameof(stop)}");

            Start = start;
            Stop = stop;
        }

        /// <summary>
        /// Returns true if this line segment overlaps with the other, to 
        ///   within the given tolerance.
        /// </summary>
        public bool Overlaps(LineSegment1D other, FixedPoint tolerance)
        {
            // |-----|  |----|
            //   this    other
            //       ^  ^
            //       a  b
            // if (|a-b| < tolerance) let a = b;
            //
            // |-------|   |------|
            //   other       this
            //         ^   ^
            //         c   d
            // if (|c-d| < tolerance) let d = c;

            // fudge the two line segments closer together if
            //   they're pretty close to each other
            var tCopy = this;
            var oCopy = other;
            {
                var stopStartDiff = tCopy.Stop - oCopy.Start;
                if(stopStartDiff.Abs() <= tolerance)
                {
                    tCopy = new LineSegment1D(tCopy.Start, oCopy.Start);
                }

                var startStopDiff = tCopy.Start - oCopy.Stop;
                if(startStopDiff.Abs() <= tolerance)
                {
                    tCopy = new LineSegment1D(oCopy.Stop, tCopy.Stop);
                }
            }

            var thisIsLeftOfOther = tCopy.Stop < oCopy.Start;
            var thisIsRightOfOther = tCopy.Start > oCopy.Stop;

            var collides = !(thisIsLeftOfOther || thisIsRightOfOther);

            return collides;
        }

        public bool Contains(FixedPoint p) => p >= Start && p <= Stop;

        public FixedPoint MeasureOverlap(LineSegment1D other)
        {
            // possibilities
            // 1. this is left of other -> 0
            // 2. this is right of other -> 0
            // 3. this is inside other -> |this|
            // 4. other is inside this -> |other|
            // 5. this overlaps the left side of other
            //    -> this.Stop - other.Start
            // 6. this overlaps the right side of other
            //    -> other.Stop - this.Start
            // 7. other overlaps the left side of this
            //    -> other.Stop - this.Start
            // 8. other overlaps the right side of this
            //    -> this.Stop - other.Start
            // 9. this == other -> |this|

            if (!Overlaps(other, FixedPoint.Zero))
            {
                // cases 1 & 2
                return FixedPoint.Zero;
            }

            var thisStartsInOther = other.Contains(this.Start);
            var thisStopsInOther = other.Contains(this.Stop);
            var otherStartsInThis = this.Contains(other.Start);
            var otherStopsInThis = this.Contains(other.Stop);

            var thisInsideOther = thisStartsInOther && thisStopsInOther;
            if (thisInsideOther)
            {
                // case 3
                return this.Length;
            }

            var otherInsideThis = otherStartsInThis && otherStopsInThis;
            if (otherInsideThis)
            {
                // case 4;
                return other.Length;
            }

            var thisOverlapsLeftOfOther = !thisStartsInOther && thisStopsInOther;
            if (thisOverlapsLeftOfOther)
            {
                // case 5
                return this.Stop - other.Start;
            }

            var thisOverlapsRightOfOther = thisStartsInOther && !thisStopsInOther;
            if (thisOverlapsRightOfOther)
            {
                // case 6
                return other.Stop - this.Start;
            }

            var otherOverlapsLeftOfThis = !otherStartsInThis && otherStopsInThis;
            if(otherOverlapsLeftOfThis)
            {
                // case 7
                return other.Stop - this.Start;
            }

            var otherOverlapsRightOfThis = otherStartsInThis && !otherStopsInThis;
            if (otherOverlapsRightOfThis)
            {
                // case 8
                return this.Stop - other.Stop;
            }

            if (this.Start == other.Start && this.Stop == other.Stop)
            {
                // case 9
                return this.Length;
            }

            throw new Exception("Shouldn't be possible");
        }

        public override string ToString() => $"{nameof(Start)}={Start}, {nameof(Stop)}={Stop}";

        public bool Equals(LineSegment1D other) => other.Start == Start && other.Stop == Stop;
        public override bool Equals(object obj)
        {
            if (!(obj is LineSegment1D)) return false;

            return Equals((LineSegment1D)obj);
        }
        public override int GetHashCode()
        {
            var ret = 17;
            ret *= 23;
            ret += Start.GetHashCode();
            ret *= 23;
            ret += Stop.GetHashCode();

            return ret;
        }
    }

    readonly struct LineSegment2D: IEquatable<LineSegment2D>
    {
        private readonly Point _P1;
        public Point P1 => _P1;
        private readonly Point _P2;
        public Point P2 => _P2;

        public LineSegment2D(Point p1, Point p2)
        {
            _P1 = p1;
            _P2 = p2;
        }
        
        public Vector Normal()
        {
            const int MAX_TRIES = 5;

            var v = new Vector(P2.X - P1.X, P2.Y - P1.Y);
            // make sure the line isn't so large that we overflow
            var @try = 0;
            while(@try < MAX_TRIES && !v.TryMagnitude(out _))
            {
                v = new Vector(v.DeltaX / 2, v.DeltaY / 2);
                @try++;
            }

            // make sure the line is large enough for the math to work
            @try = 0;
            while(true)
            {
                if (@try == MAX_TRIES) continue;

                // whelp
                if (!v.TryMagnitude(out var vMag)) break;
                
                if (vMag.IsZero)
                {
                    v = new Vector(v.DeltaX * 2, v.DeltaY * 2);
                    @try++;
                }
                else
                {
                    break;
                }
            }

            return v.Normal();
        }

        public static bool operator ==(LineSegment2D a, LineSegment2D b) => a.Equals(b);
        public static bool operator !=(LineSegment2D a, LineSegment2D b) => !(a.Equals(b));

        public bool Equals(LineSegment2D other)
        {
            return other.P1 == P1 && other.P2 == P2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LineSegment2D)) return false;

            return Equals((LineSegment2D)obj);
        }

        public override int GetHashCode()
        {
            var ret = 17;
            ret *= 23;
            ret += P1.GetHashCode();
            ret *= 23;
            ret += P2.GetHashCode();

            return ret;
        }

        public override string ToString() => $"{nameof(P1)}=({P1}), {nameof(P2)}=({P2})";
    }
}
