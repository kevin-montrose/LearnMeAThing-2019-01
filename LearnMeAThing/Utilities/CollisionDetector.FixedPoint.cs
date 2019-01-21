using System;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// A fixed point represention of a number.
    /// </summary>
    readonly struct FixedPoint : IEquatable<FixedPoint>, IComparable<FixedPoint>
    {
        // 2^20, which should make the multiplications and divisions cheaper
        private const long SCALE = 1_048_576;

        public static readonly FixedPoint Zero = new FixedPoint(0);
        public static readonly FixedPoint One = new FixedPoint(SCALE);

        private static readonly long SCALE_SQRT = IntSqrt(SCALE);
        private static readonly long SCALE_SQUARED = SCALE * SCALE;

        public readonly bool IsZero;

        private readonly long Scaled;

        private FixedPoint(long preScaled)
        {
            Scaled = preScaled;
            IsZero = Scaled == 0;
        }

        public int Sign()
        {
            if (Scaled == 0) return 0;
            if (Scaled < 0) return -1;
            return 1;
        }

        public static FixedPoint FromInt(int value) => new FixedPoint(value * SCALE);

        public static explicit operator int(FixedPoint a) =>(int)(a.Scaled / SCALE);

        public static FixedPoint operator +(FixedPoint a, FixedPoint b) => new FixedPoint(a.Scaled + b.Scaled);
        public static FixedPoint operator +(FixedPoint a, int b) => a + FromInt(b);
        public static FixedPoint operator +(int a, FixedPoint b) => FromInt(a) + b;

        public static FixedPoint operator -(FixedPoint a, FixedPoint b) => new FixedPoint(a.Scaled - b.Scaled);
        public static FixedPoint operator -(FixedPoint a, int b) => a - FromInt(b);
        public static FixedPoint operator -(int a, FixedPoint b) => FromInt(a) - b;

        public static FixedPoint operator *(FixedPoint a, FixedPoint b)
        {
            var aRaw = a.Scaled;    // aRaw = a * SCALE
            var bRaw = b.Scaled;    // bRaw = b * SCALE

            // we want cRaw = c * SCALE = (a*b) * SCALE
            // aRaw * bRaw = (a*SCALE) * (b*SCALE) = a*b*SCALE*SCALE
            //   so at the end we need to de-scale once

            var cRaw = (aRaw * bRaw) / SCALE;

            return new FixedPoint(cRaw);
        }
        public static FixedPoint operator *(FixedPoint a, int b)
        {
            var aRaw = a.Scaled;    // aRaw = a * SCALE

            // we want cRaw = c * SCALE = (a * b) * SCALE
            //   so we can get away with a simple multiply here

            var cRaw = aRaw * b;
            return new FixedPoint(cRaw);
        }
        public static FixedPoint operator *(int a, FixedPoint b)
        {
            var bRaw = b.Scaled;    // bRaw = b * SCALE

            // we want cRaw = c * SCALE = (a * b) * SCALE
            //   so we can get away with a simple multiply here

            var cRaw = a * bRaw;
            return new FixedPoint(cRaw);
        }

        public static FixedPoint operator /(FixedPoint a, FixedPoint b)
        {
            if (b.IsZero) throw new DivideByZeroException();

            var aRaw = a.Scaled;    // aRaw = a * SCALE
            var bRaw = b.Scaled;    // bRaw = b * SCALE

            // we want cRaw = c * SCALE = (a/b) * SCALE
            // aRaw / bRaw = (a * SCALE) / (b * SCALE) = a/b
            // so first, scale aRaw again

            var aRawScale = aRaw * SCALE;

            var cRaw = aRawScale / bRaw;
            return new FixedPoint(cRaw);
        }
        public static FixedPoint operator /(FixedPoint a, int b)
        {
            if (b == 0) throw new DivideByZeroException();

            var aRaw = a.Scaled;    // aRaw = a * SCALE

            // we want cRaw = c * SCALE = (a/b) * SCALE
            // aRaw / b = (a * SCALE) / (b) = (a/b) * SCALE
            //   so we can take it as is

            var cRaw = aRaw / b;
            return new FixedPoint(cRaw);
        }
        public static FixedPoint operator /(int a, FixedPoint b)
        {
            if(b.IsZero) throw new DivideByZeroException();

            var bRaw = b.Scaled;    // bRaw = b * SCALE

            // we want cRaw = c * SCALE = (a/b) * SCALE
            // a / bRaw = a / (b * SCALE) ~= 0
            //   so we need to scale a twice

            var aDoubleRaw = a * SCALE_SQUARED;

            var cRaw = aDoubleRaw / bRaw;
            return new FixedPoint(cRaw);
        }

        public static bool operator ==(FixedPoint a, FixedPoint b) => a.Scaled == b.Scaled;
        public static bool operator ==(FixedPoint a, int b) => a == FromInt(b);
        public static bool operator ==(int a, FixedPoint b) => FromInt(a) == b;

        public static bool operator !=(FixedPoint a, FixedPoint b) => a.Scaled != b.Scaled;
        public static bool operator !=(FixedPoint a, int b) => a != FromInt(b);
        public static bool operator !=(int a, FixedPoint b) => FromInt(a) != b;

        public static FixedPoint operator -(FixedPoint a) => new FixedPoint(-a.Scaled);

        public static bool operator <(FixedPoint a, FixedPoint b) => a.Scaled < b.Scaled;
        public static bool operator <(FixedPoint a, int b) => a < FromInt(b);
        public static bool operator <(int a, FixedPoint b) => FromInt(a) < b;

        public static bool operator <=(FixedPoint a, FixedPoint b) => a.Scaled <= b.Scaled;
        public static bool operator <=(FixedPoint a, int b) => a <= FromInt(b);
        public static bool operator <=(int a, FixedPoint b) => FromInt(a) <= b;

        public static bool operator >(FixedPoint a, FixedPoint b) => a.Scaled > b.Scaled;
        public static bool operator >(FixedPoint a, int b) => a > FromInt(b);
        public static bool operator >(int a, FixedPoint b) => FromInt(a) > b;

        public static bool operator >=(FixedPoint a, FixedPoint b) => a.Scaled >= b.Scaled;
        public static bool operator >=(FixedPoint a, int b) => a >= FromInt(b);
        public static bool operator >=(int a, FixedPoint b) => FromInt(a) >= b;

        public static FixedPoint Min(FixedPoint a, FixedPoint b) => a.Scaled < b.Scaled ? a : b;

        public static FixedPoint Max(FixedPoint a, FixedPoint b) => a.Scaled > b.Scaled ? a : b;

        public static FixedPoint Dot(FixedPoint x1, FixedPoint y1, FixedPoint x2, FixedPoint y2)
        {
            // for X1 = x1 * SCALE
            //     Y1 = y1 * SCALE
            //     X2 = x2 * SCALE
            //     Y2 = y2 * SCALE
            //
            // we want to calculate
            //    [(x1 * x2) + (y1 * y2)] * SCALE
            //
            // naively
            //    X1 * X2 + Y1 * Y2
            //    = (x1 * x2) * (SCALE * SCALE) + (y1 * y2) * (SCALE * SCALE)
            //    = (SCALE * SCALE) * [(x1 * x2) + (y1 * y2)]
            //
            // so we can just divide by SCALE and get the result we want

            var naively = x1.Scaled * x2.Scaled + y1.Scaled * y2.Scaled;
            var ret = naively / SCALE;

            return new FixedPoint(ret);
        }

        public static bool TryMagnitude(FixedPoint x, FixedPoint y, out FixedPoint res)
        {
            // for X = x * SCALE
            //     Y = y * SCALE
            //
            // we want to calculate
            //    [(x * x) + (y *y)]^(1/2) * SCALE
            //
            // naively
            //    [(X * X) + (Y * Y)]^(1/2)
            //    = [(SCALE * SCALE)*(x*x + y*y)]^(1/2)
            //    = [SCALE * SCALE)^(1/2)*(x*x + y*y)^(1/2)
            //    = [(x*x) + (y*y)]^(1/2) * SCALE
            //
            // which is exactly what we want

            var naive = x.Scaled * x.Scaled + y.Scaled * y.Scaled;
            if(naive < 0)
            {
                res = FixedPoint.Zero;
                return false;
            }
            var ret = IntSqrt(naive);

            res = new FixedPoint(ret);
            return true;
        }

        // todo: replace all Magnitudes with TryMagnitude calls
        public static FixedPoint Magnitude(FixedPoint x, FixedPoint y)
        {
            // for X = x * SCALE
            //     Y = y * SCALE
            //
            // we want to calculate
            //    [(x * x) + (y *y)]^(1/2) * SCALE
            //
            // naively
            //    [(X * X) + (Y * Y)]^(1/2)
            //    = [(SCALE * SCALE)*(x*x + y*y)]^(1/2)
            //    = [SCALE * SCALE)^(1/2)*(x*x + y*y)^(1/2)
            //    = [(x*x) + (y*y)]^(1/2) * SCALE
            //
            // which is exactly what we want

            var naive = x.Scaled * x.Scaled + y.Scaled * y.Scaled;
            var ret = IntSqrt(naive);

            return new FixedPoint(ret);
        }

        public static void Normalize(FixedPoint x, FixedPoint y, out FixedPoint xNew, out FixedPoint yNew)
        {
            // for X = x * SCALE
            //     Y = y * SCALE
            //
            // we want to calculate
            //     x / ([(x * x) + (y * y)]^(1/2)) * SCALE
            //     y / ([(x * x) + (y * y)]^(1/2)) * SCALE
            //
            // naively 
            //     X / ( [(X * X) + (Y * Y)]^(1/2) )
            //     = (x * SCALE) / ( [(x * SCALE * x * SCALE) + (y * SCALE * y * SCALE)]^(1/2) )
            //     = (x * SCALE) / ( [(SCALE * SCALE) * (x * x + y * y)]^(1/2) )
            //     = (x * SCALE) / ( SCALE * (x * x + y * y)^(1/2) )
            //     = x / [(x * x + y * y)^(1/2)]

            // so if we just do the naive thing, and then multipl by SCALE we 
            //    can skip lots of intermediate work

            var xScaled = x.Scaled;
            var yScaled = y.Scaled;

            // todo: this is incorrect
            var denom = xScaled * xScaled + yScaled * yScaled;
            while(denom < 0)
            {
                xScaled /= 2;
                yScaled /= 2;

                denom = xScaled * xScaled + yScaled * yScaled;
            }
            // end incorrect bits

            denom = IntSqrt(denom);
            var retX = xScaled * SCALE / denom;
            var retY = yScaled * SCALE / denom;

            xNew = new FixedPoint(retX);
            yNew = new FixedPoint(retY);
        }

        public FixedPoint Abs()
        {
            if (this >= 0) return this;

            return new FixedPoint(-Scaled);
        }

        public FixedPoint Sqrt()
        {
            // todo: replace this with a lookup table if possible

            // our current value is SCALE * a
            // we want to get to SCALE * (a^(1/2))
            // the following will calculate (SCALE * a)^(1/2)

            // res = (SCALE * a)^(1/2) = SCALED^(1/2) * a^(1/2)
            var res = IntSqrt(Scaled);
            // so now multiply by SCALE^(1/2) to get to SCALED * a^(1/2)
            res *= SCALE_SQRT;

            return new FixedPoint(res);
        }
        
        private static long IntSqrt(long val)
        {
            // based on: https://en.wikipedia.org/wiki/Integer_square_root

            if (val < 0)
            {
                throw new InvalidOperationException("Cannot compute square root of negative number");
            }

            // square root of 0 is 0 and 1 is 1
            if (val < 2)
            {
                return val;
            }

            var n = val;
            var shift = 2;
            var nShifted = n >> shift;
            while (nShifted != 0 && nShifted != n)
            {
                shift += 2;
                nShifted = n >> shift;
            }
            shift -= 2;

            var result = 0L;
            while (shift >= 0)
            {
                result = result << 1;
                var candidateResult = result + 1;
                if (candidateResult * candidateResult <= (n >> shift))
                {
                    result = candidateResult;
                }
                shift -= 2;
            }

            return result;
        }

        public override string ToString()
        {
            var sign = Scaled < 0 ? -1 : 1;

            var noSignScaled = Math.Abs(Scaled);

            var leftOfPoint = noSignScaled / SCALE;
            var rightOfPoint = noSignScaled % SCALE;

            if (leftOfPoint == 0 && rightOfPoint == 0)
            {
                return "0";
            }

            if (leftOfPoint == 0)
            {
                if (sign < 0)
                {
                    return $"-({rightOfPoint:N0}/{SCALE:N0})";
                }

                return $"{rightOfPoint:N0}/{SCALE:N0}";
            }

            if (rightOfPoint == 0)
            {
                if (sign < 0)
                {
                    return $"-({leftOfPoint:N0})";
                }
                else
                {
                    return leftOfPoint.ToString("N0");
                }
            }

            if (sign < 0)
            {
                return $"-({leftOfPoint:N0} + {rightOfPoint:N0}/{SCALE:N0})";
            }
            else
            {
                return $"{leftOfPoint:N0} + {rightOfPoint:N0}/{SCALE:N0}";
            }
        }

        public bool Equals(FixedPoint other) => this.Scaled == other.Scaled;
        public override bool Equals(object obj)
        {
            if (!(obj is FixedPoint)) return false;

            return Equals((FixedPoint)obj);
        }
        public override int GetHashCode() => Scaled.GetHashCode();

        public int CompareTo(FixedPoint other) => Scaled.CompareTo(other.Scaled);
    }
}
