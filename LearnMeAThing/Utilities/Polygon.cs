using System;

namespace LearnMeAThing.Utilities
{
    public enum TurnType
    {
        NONE = 0,

        Clockwise,
        CounterClockwise
    }

    sealed class PolygonPattern
    {
        private readonly Point[] _Vertices;
        public Point[] Vertices => _Vertices;

        private readonly TurnType _TurnType;
        public TurnType Turn => _TurnType;

        private readonly int _OriginalHeight;
        public int OriginalHeight => _OriginalHeight;

        public PolygonPattern(Point[] vertices, int originalHeight)
        {
            if (vertices.Length < 3) throw new InvalidOperationException("Cannot create a polygon with fewer than three points");
            if (originalHeight <= 0) throw new InvalidOperationException("Original height must be positive");

            _OriginalHeight = originalHeight;

            for (var i = 0; i < vertices.Length; i++)
            {
                var v1 = vertices[i];
                for(var j = 0; j < vertices.Length; j++)
                {
                    if (i == j) continue;
                    var v2 = vertices[j];

                    if(v1.X == v2.X && v1.Y == v2.Y)
                    {
                        throw new InvalidOperationException("Tried to create a polygon with duplicate points");
                    }
                }
            }
            
            _Vertices = vertices;
            _TurnType = DetermineTurnType(vertices);
        }

        private static TurnType DetermineTurnType(Point[] vs)
        {
            var sum = FixedPoint.Zero;
            for(var i = 0; i < vs.Length; i++)
            {
                var next = i + 1;
                if(next == vs.Length)
                {
                    next = 0;
                }

                var v1 = vs[i];
                var v2 = vs[next];

                var a = (v2.X - v1.X) * (v2.Y + v1.Y);
                sum += a;
            }

            if (sum < 0) return TurnType.CounterClockwise;

            return TurnType.Clockwise;
        }

        public ConvexPolygonPattern[] DecomposeIntoConvexPolygons(Buffer<Point> scratch1, Buffer<Point> scratch2, Buffer<Point> scratch3)
        {
            const int MAX_TO_DIRECTLY_DECOMPOSE = 9;

            if(Turn == TurnType.CounterClockwise)
            {
                scratch1.Clear();
                for(var i = 0; i < Vertices.Length; i++)
                {
                    scratch1.Add(Vertices[i]);
                }
                scratch1.Reverse();
                var copy = scratch1.ToArray();

                var poly = new PolygonPattern(copy, OriginalHeight);
                return poly.DecomposeIntoConvexPolygons(scratch1, scratch2, scratch3);
            }

            if (Vertices.Length >= MAX_TO_DIRECTLY_DECOMPOSE)
            {
                var naiveParts = SplitNaively(scratch1, scratch2, scratch3);
                var ret = new Buffer<ConvexPolygonPattern>(100);
                foreach(var part in naiveParts)
                {
                    var covering = part.DecomposeIntoConvexPolygons(scratch1, scratch2, scratch3);
                    foreach (var p in covering)
                    {
                        ret.Add(p);
                    }
                }

                return ret.ToArray();
            }

            return DecomposeSmallPolygon(scratch1);
        }

        /// <summary>
        /// Looks at a variety of dividers along the x and y axis, returning a split that 
        ///   breaks the polygon into smaller pieces.
        /// </summary>
        internal PolygonPattern[] SplitNaively(Buffer<Point> scratch1, Buffer<Point> scratch2, Buffer<Point> scratch3)
        {
            const int START_SPLIT = 2;
            const int END_SPLIT = 8;

            GetBounds(out var minX, out var minY, out var maxX, out var maxY);

            PolygonPattern[] best = null;

            var xDiff = maxX - minX;
            var yDiff = maxY - minY;

            for(var split = START_SPLIT; split <= END_SPLIT; split++)
            {
                var xStep = xDiff / split;
                var yStep = yDiff / split;

                for(var i = 1; i < split; i++)
                {
                    var xSplit = minX + xStep * i;
                    var ySplit = minY + yStep * i;

                    if(CanDivideAlongVertical(xSplit))
                    {
                        var candidate = SplitVertically(xSplit, scratch1, scratch2, scratch3);
                        TakeIfBetter(candidate);
                    }

                    if (CanDivideAlongHorizontal(ySplit))
                    {
                        var candidate = SplitHorizontally(ySplit, scratch1, scratch2, scratch3);
                        TakeIfBetter(candidate);
                    }
                }
            }

            return best;

            void TakeIfBetter(PolygonPattern[] candidate)
            {
                if(best == null)
                {
                    best = candidate;
                    return;
                }

                var oldDiff = Math.Abs(best[0].Vertices.Length - best[1].Vertices.Length);
                var newDiff = Math.Abs(candidate[0].Vertices.Length - candidate[1].Vertices.Length);

                if(newDiff < oldDiff)
                {
                    best = candidate;
                }
            }
        }

        internal PolygonPattern[] SplitVertically(FixedPoint x, Buffer<Point> scratchVertices, Buffer<Point> scratchLeft, Buffer<Point> scratchRight)
        {
            scratchVertices.Clear();
            for(var i = 0; i < Vertices.Length; i++)
            {
                scratchVertices.Add(Vertices[i]);
            }
            if(Turn == TurnType.CounterClockwise)
            {
                scratchVertices.Reverse();
            }

            GetBounds(out var _, out var minY, out var _, out var maxY);
            var splitLine = new LineSegment2D(new Point(x, minY - 1), new Point(x, maxY + 1));

            scratchLeft.Clear();
            scratchRight.Clear();

            // assume we're going clockwise
            for (var vIx = 0; vIx < scratchVertices.Count; vIx++)
            {
                var nextIx = vIx + 1;
                if (nextIx == scratchVertices.Count)
                {
                    nextIx = 0;
                }

                var v = scratchVertices[vIx];
                var next = scratchVertices[nextIx];

                if (v.X == x)
                {
                    // the point _lies_ on the intersection line, by definition
                    //    it appears in both polygons
                    scratchLeft.Add(v);
                    scratchRight.Add(v);
                }
                else
                {
                    var lineSeg = new LineSegment2D(v, next);
                    if (Intersect(lineSeg, splitLine, out var pt, false) != IntersectResult.Intersecting)
                    {
                        if (IsLeft(v))
                        {
                            scratchLeft.Add(v);
                        }
                        else
                        {
                            scratchRight.Add(v);
                        }
                    }
                    else
                    {
                        if (IsLeft(v))
                        {
                            scratchLeft.Add(v);
                            scratchLeft.Add(pt.Value);
                            scratchRight.Add(pt.Value);
                        }
                        else
                        {
                            scratchRight.Add(v);
                            scratchRight.Add(pt.Value);
                            scratchLeft.Add(pt.Value);
                        }
                    }
                }
            }

            var leftPoly = new PolygonPattern(scratchLeft.ToArray(), OriginalHeight);
            var rightPoly = new PolygonPattern(scratchRight.ToArray(), OriginalHeight);

            return new[] { leftPoly, rightPoly };

            bool IsLeft(Point pt)
            {
                return pt.X < x;
            }
        }

        internal PolygonPattern[] SplitHorizontally(FixedPoint y, Buffer<Point> scratchVertices, Buffer<Point> scratchTop, Buffer<Point> scratchBottom)
        {
            scratchVertices.Clear();
            for(var i = 0; i < Vertices.Length; i++)
            {
                scratchVertices.Add(Vertices[i]);
            }
            if (Turn == TurnType.CounterClockwise)
            {
                scratchVertices.Reverse();
            }

            GetBounds(out var minX, out var _, out var maxX, out var _);
            var splitLine = new LineSegment2D(new Point(minX - 1, y), new Point(maxX + 1, y));

            scratchTop.Clear();
            scratchBottom.Clear();

            // assume we're going clockwise
            for (var vIx = 0; vIx < scratchVertices.Count; vIx++)
            {
                var nextIx = vIx + 1;
                if (nextIx == scratchVertices.Count)
                {
                    nextIx = 0;
                }

                var v = scratchVertices[vIx];
                var next = scratchVertices[nextIx];
                
                if (v.Y == y)
                {
                    // the point _lies_ on the intersection line, by definition
                    //    it appears in both polygons
                    scratchTop.Add(v);
                    scratchBottom.Add(v);
                }
                else
                {
                    var lineSeg = new LineSegment2D(v, next);
                    if (Intersect(lineSeg, splitLine, out var pt, false) != IntersectResult.Intersecting)
                    {
                        if (IsAbove(v))
                        {
                            scratchTop.Add(v);
                        }
                        else
                        {
                            scratchBottom.Add(v);
                        }
                    }
                    else
                    {
                        if (IsAbove(v))
                        {
                            scratchTop.Add(v);
                            scratchTop.Add(pt.Value);
                            scratchBottom.Add(pt.Value);
                        }
                        else
                        {
                            scratchBottom.Add(v);
                            scratchBottom.Add(pt.Value);
                            scratchTop.Add(pt.Value);
                        }
                    }
                }
            }

            var topPoly = new PolygonPattern(scratchTop.ToArray(), OriginalHeight);
            var bottomPoly = new PolygonPattern(scratchBottom.ToArray(), OriginalHeight);

            return new[] { topPoly, bottomPoly };
            
            bool IsAbove(Point v)
            {
                return v.Y > y;
            }
        }

        internal bool CanDivideAlongVertical(FixedPoint x)
        {
            var onVertex = false;
            for (var i = 0; i < Vertices.Length; i++)
            {
                var v = Vertices[i];
                if (v.X == x)
                {
                    onVertex = true;
                    break;
                }
            }

            if (onVertex)
            {
                // so, the bounds check is gonna be screwy
                // 
                // let's nudge it _slightly_ off on either side
                //   and if those both say yes, then we can

                var left = CanDivideAlongVertical(x - FixedPoint.One / 1_000);
                var right = CanDivideAlongVertical(x + FixedPoint.One / 1_000);

                return left && right;
            }

            GetBounds(out var _, out var minY, out var _, out var maxY);

            var lineSegment = new LineSegment2D(new Point(x, minY - 1), new Point(x, maxY + 1));

            return CanDivideAlongLine(lineSegment);
        }

        internal bool CanDivideAlongHorizontal(FixedPoint y)
        {
            var onVertex = false;
            for (var i = 0; i < Vertices.Length; i++)
            {
                var v = Vertices[i];
                if (v.Y == y)
                {
                    onVertex = true;
                    break;
                }
            }

            if (onVertex)
            {
                // so, the bounds check is gonna be screwy
                // 
                // let's nudge it _slightly_ off on either side
                //   and if those both say yes, then we can

                var below = CanDivideAlongHorizontal(y - FixedPoint.One / 1_000);
                var above = CanDivideAlongHorizontal(y + FixedPoint.One / 1_000);

                return below && above;
            }

            GetBounds(out var minX, out var _, out var maxX, out var _);

            var lineSegment = new LineSegment2D(new Point(minX-1, y), new Point(maxX+1, y));

            return CanDivideAlongLine(lineSegment);
        }

        internal bool CanDivideAlongLine(LineSegment2D line)
        {
            var linesCrossed = 0;

            for (var i = 0; i < Vertices.Length; i++)
            {
                var next = i + 1;
                if (next == Vertices.Length)
                {
                    next = 0;
                }

                var seg = new LineSegment2D(Vertices[i], Vertices[next]);

                var intersect = Intersect(line, seg, out var _, true);
                if (intersect == IntersectResult.CoLinear) return false;

                if (intersect == IntersectResult.Intersecting)
                {
                    linesCrossed++;
                }
            }

            return linesCrossed == 2;
        }

        private void GetBounds(out FixedPoint minX, out FixedPoint minY, out FixedPoint maxX, out FixedPoint maxY)
        {
            FixedPoint? xMin, xMax, yMin, yMax;
            xMin = xMax = yMin = yMax = null;

            foreach (var pt in Vertices)
            {
                if (xMin == null || pt.X < xMin)
                {
                    xMin = pt.X;
                }

                if (yMin == null || pt.Y < yMin)
                {
                    yMin = pt.Y;
                }

                if (xMax == null || pt.X > xMax)
                {
                    xMax = pt.X;
                }

                if (yMax == null || pt.Y > yMax)
                {
                    yMax = pt.Y;
                }
            }

            minX = xMin.Value;
            minY = yMin.Value;
            maxX = xMax.Value;
            maxY = yMax.Value;
        }
        
        private ConvexPolygonPattern[] DecomposeSmallPolygon(Buffer<Point> scratch)
        {
            // based on: https://gamedev.stackexchange.com/a/67901/155

            var parts = new Buffer<ConvexPolygonPattern>(100);

            if (Vertices.Length > 3)
            {
                for (var i = 0; i < Vertices.Length; i++)
                {
                    var v1Ix = i;
                    if (!IsReflexVertex(v1Ix))
                    {
                        continue;
                    }
                    
                    for (var v2Ix = 0; v2Ix < Vertices.Length; v2Ix++)
                    {
                        var isSameVertex = v2Ix == i;
                        if (isSameVertex) continue;

                        var prevIx = v2Ix - 1;
                        if (prevIx < 0) prevIx = Vertices.Length - 1;
                        var nextIx = v2Ix + 1;
                        if (nextIx == Vertices.Length) nextIx = 0;

                        var isAdjacentVertex = prevIx == i || nextIx == i;
                        if (isAdjacentVertex) continue;

                        var canSee = CanSee(v1Ix, v2Ix, scratch);

                        if (canSee)
                        {
                            var left = PolygonFromVertices(v1Ix, v2Ix, scratch);
                            if (left.Turn == TurnType.CounterClockwise) throw new Exception();
                            var right = PolygonFromVertices(v2Ix, v1Ix, scratch);
                            if (right.Turn == TurnType.CounterClockwise) throw new Exception();

                            ConvexPolygonPattern[] leftConvex;
                            ConvexPolygonPattern[] rightConvex;

                            if (left.Vertices.Length == 3)
                            {
                                leftConvex = new[] { new ConvexPolygonPattern(left.Vertices, OriginalHeight) };
                            }
                            else
                            {
                                leftConvex = left.DecomposeSmallPolygon(scratch);
                            }

                            if (right.Vertices.Length == 3)
                            {
                                
                                rightConvex = new[] { new ConvexPolygonPattern(right.Vertices, OriginalHeight) };
                            }
                            else
                            {
                                rightConvex = right.DecomposeSmallPolygon(scratch);
                            }

                            var num = leftConvex.Length + rightConvex.Length;
                            if (parts.Count == 0 || num < parts.Count)
                            {
                                parts.Clear();
                                foreach (var l in leftConvex) parts.Add(l);
                                foreach (var r in rightConvex) parts.Add(r);
                            }
                        }
                    }
                }
            }

            if (parts.Count == 0)
            {
                // no reflexive found
                return new[] { new ConvexPolygonPattern(Vertices, OriginalHeight) };
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Returns true if you can draw a line between these two vertices
        ///    without intersecting any edge, and the line lies within the polygon.
        /// </summary>
        internal bool CanSee(int v1Ix, int v2Ix, Buffer<Point> scratch)
        {
            var v1 = Vertices[v1Ix];
            var v2 = Vertices[v2Ix];

            var diagonal = new LineSegment2D(v1, v2);
            
            for(var i = 0; i < Vertices.Length; i++)
            {
                var next = i + 1;
                if(next == Vertices.Length)
                {
                    next = 0;
                }
                var vI = Vertices[i];
                var vNext = Vertices[next];

                var edge = new LineSegment2D(vI, vNext);

                if (Intersect(edge, diagonal, out var _, false) == IntersectResult.Intersecting) return false;
            }

            // we now know that the line doesn't intersect any edges, but it could still be
            //   outside the polygon, which would mean the points cannot "see" each other

            var slope = new Vector(v2.X - v1.X, v2.Y - v1.Y);
            var midPoint = new Point(v1.X + slope.DeltaX / 2, v1.Y + slope.DeltaY / 2);

            if (!InsidePolygon(midPoint, scratch)) return false;

            return true;
        }

        
        /// <summary>
        /// Returns true if the point lies inside the polygon.
        /// </summary>
        internal bool InsidePolygon(Point pt, Buffer<Point> scratchUniquePoints)
        {
            // the easiest way to check this is to take the center point on the line
            //   (which we know is in the same state as the rest of the line, because
            //    we just disproved intersections) and extend a line to the right
            //    and count the number of faces it intersects with.  If the count
            //    is even (or zero), it's outside the polygon; if odd it's inside

            FixedPoint? smallestDist = null;

            for(var i = 0; i < Vertices.Length; i++)
            {
                var v = Vertices[i];
                var dist = (v.X - pt.X).Abs();

                if (smallestDist == null || dist < smallestDist) smallestDist = dist;
            }

            GetBounds(out var minX, out var minY, out var maxX, out var maxY);
            var width = maxX - minX;

            var lineSegWidth = smallestDist.Value + width + 1;

            // this line should be long enough that any point lying outside but 
            //   to the _left_ should cross through the whole polygon
            var horizontalLine = new LineSegment2D(pt, new Point(pt.X + lineSegWidth, pt.Y));

            scratchUniquePoints.Clear();

            for (var i = 0; i < Vertices.Length; i++)
            {
                var next = i + 1;
                if (next == Vertices.Length)
                {
                    next = 0;
                }
                var vI = Vertices[i];
                var vNext = Vertices[next];

                var edge = new LineSegment2D(vI, vNext);

                if (Intersect(edge, horizontalLine, out var intersectionPoint, true) == IntersectResult.Intersecting)
                {
                    if (!scratchUniquePoints.Contains(intersectionPoint.Value))
                    {
                        scratchUniquePoints.Add(intersectionPoint.Value);
                    }
                }
            }

            var intersections = scratchUniquePoints.Count;
            var isInside = (intersections % 2) == 1;

            return isInside;
        }

        internal enum IntersectResult
        {
            Parallel,
            CoLinear,
            Intersecting,
            NotIntersecting
        }

        internal static IntersectResult Intersect(LineSegment2D a, LineSegment2D b, out Point? intersectionPoint, bool inclusive)
        {
            // based on: https://stackoverflow.com/a/565282/80572

            var p = a.P1;
            var r = new Vector(a.P2.X - p.X, a.P2.Y - p.Y);

            var q = b.P1;
            var s = new Vector(b.P2.X - q.X, b.P2.Y - q.Y);

            // t = (q − p) × s / (r × s)
            var rCrossS = Cross(r, s);
            
            var qMinusP = new Vector(q.X - p.X, q.Y - p.Y);
            var qMinusPCrossS = Cross(qMinusP, s);

            // they're parallel, are the on the same line?
            if (rCrossS.IsZero)
            {
                intersectionPoint = null;
                if(qMinusPCrossS.IsZero) return IntersectResult.CoLinear;

                return IntersectResult.Parallel;
            }

            var t = qMinusPCrossS / rCrossS;

            // u = (q − p) × r / (r × s)
            var qMinusPCrossR = Cross(qMinusP, r);
            var u = qMinusPCrossR / rCrossS;

            // exclude the end points
            if (inclusive)
            {
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                {
                    var offset = t * r;
                    intersectionPoint = new Point(p.X + offset.DeltaX, p.Y + offset.DeltaY);
                    return IntersectResult.Intersecting;
                }

                intersectionPoint = null;
                return IntersectResult.NotIntersecting;
            }

            if (t > 0 && t < 1 && u > 0 && u < 1)
            {
                var offset = t * r;
                intersectionPoint = new Point(p.X + offset.DeltaX, p.Y + offset.DeltaY);
                return IntersectResult.Intersecting;
            }

            intersectionPoint = null;
            return IntersectResult.NotIntersecting;

            FixedPoint Cross(Vector v, Vector w)
            {
                return (v.DeltaX * w.DeltaY) - (v.DeltaY * w.DeltaX);
            }
        }

        internal bool IsReflexVertex(int vIx)
        {
            GetLineSegmentsOffVertex(vIx, out var left, out var right);
            var leftCrossRight = left.DeltaX * right.DeltaY - left.DeltaY * right.DeltaX;

            switch (Turn)
            {
                case TurnType.Clockwise: return leftCrossRight > 0;
                case TurnType.CounterClockwise: return leftCrossRight < 0;
                default: throw new InvalidOperationException($"Unexpected Turn: {Turn}");
            }
        }

        private void GetLineSegmentsOffVertex(int vIx, out Vector fromBefore, out Vector toAfter)
        {
            var beforeVIx = vIx - 1;
            if (beforeVIx == -1)
            {
                beforeVIx = Vertices.Length - 1;
            }
            var afterVIx = vIx + 1;
            if (afterVIx == Vertices.Length)
            {
                afterVIx = 0;
            }

            var beforeV = Vertices[beforeVIx];
            var v = Vertices[vIx];
            var afterV = Vertices[afterVIx];

            fromBefore = new Vector(v.X - beforeV.X, v.Y - beforeV.Y);
            toAfter = new Vector(afterV.X - v.X, afterV.Y - v.Y);
        }

        /// <summary>
        /// Creates a polygon containing the vertices (inclusive) between the given indexes.
        /// </summary>
        private PolygonPattern PolygonFromVertices(int v1Ix, int v2Ix, Buffer<Point> scratch)
        {
            scratch.Clear();

            var cur = v1Ix;
            var keepGoing = true;
            while (keepGoing)
            {
                scratch.Add(Vertices[cur]);
                if(cur == v2Ix)
                {
                    keepGoing = false;
                }

                cur++;
                if(cur == Vertices.Length)
                {
                    cur = 0;
                }
            }

            if (scratch.Count < 3)
            {
                throw new Exception();
            }

            var vs = scratch.ToArray();
            var pRet = new PolygonPattern(vs, OriginalHeight);

            if(pRet.Turn == TurnType.CounterClockwise)
            {
                scratch.Reverse();
                vs = scratch.ToArray();
                pRet = new PolygonPattern(vs, OriginalHeight);
            }

            return pRet;
        }

        public override string ToString() => $"{Vertices.Length} Vertices, {Turn}";
    }
}
