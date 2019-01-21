using System;

namespace LearnMeAThing.Utilities
{
    class ConvexPolygonPattern: IEquatable<ConvexPolygonPattern>
    {
        /// <summary>
        /// X component of top left corner of polygons bounding.
        /// 
        /// Coordinates are in caretesian plane.
        /// </summary>
        public readonly FixedPoint BoundingX;

        /// <summary>
        /// Y component of top left corner of polygons bounding.
        /// 
        /// Coordinates are in caretesian plane.
        /// </summary>
        public readonly FixedPoint BoundingY;

        /// <summary>
        /// Width of the bounding box for this polygon.
        /// </summary>
        public readonly FixedPoint BoundingWidth;

        /// <summary>
        /// Height of the bounding box for this polygon.
        /// </summary>
        public readonly FixedPoint BoundingHeight;

        private readonly Point[] _Vertices;
        // the vertices of the polygon, in a clockwise order, for a convex polygon
        public Point[] Vertices => _Vertices;

        private readonly Vector[] _Normals;
        public Vector[] Normals => _Normals;

        private readonly LineSegment2D[] _LineSegements;
        public LineSegment2D[] LineSegments => _LineSegements;

        public readonly int OriginalHeight;

        public ConvexPolygonPattern(Point[] vertices, int originalHeight)
        {
            if (vertices.Length < 3) throw new InvalidOperationException("Cannot create a polygon with fewer than three points");
            if (originalHeight <= 0) throw new InvalidOperationException("Must have positive original height");

            for (var i = 0; i < vertices.Length; i++)
            {
                var v1 = vertices[i];
                for (var j = 0; j < vertices.Length; j++)
                {
                    if (i == j) continue;
                    var v2 = vertices[j];

                    if (v1.X == v2.X && v1.Y == v2.Y)
                    {
                        throw new InvalidOperationException("Tried to create a polygon with duplicate points");
                    }
                }
            }

            OriginalHeight = originalHeight;
            _Vertices = vertices;

            FixedPoint? minX = null;
            FixedPoint? minY = null;
            FixedPoint? maxX = null;
            FixedPoint? maxY = null;

            for (var i = 0; i < vertices.Length; i++)
            {
                var pt = vertices[i];
                if (minX == null || pt.X < minX)
                {
                    minX = pt.X;
                }
                if (minY == null || pt.Y < minY)
                {
                    minY = pt.Y;
                }
                if (maxX == null || pt.X > maxX)
                {
                    maxX = pt.X;
                }
                if (maxY == null || pt.Y > maxY)
                {
                    maxY = pt.Y;
                }
            }

            BoundingX = minX.Value;
            BoundingY = maxY.Value;
            BoundingWidth = (maxX.Value - minX.Value);
            BoundingHeight = (maxY.Value - minY.Value);

            var normalIx = 0;
            var normals = new Vector[vertices.Length];
            for(var i = 0; i < vertices.Length; i++)
            {
                var v1 = vertices[i];
                var v2Ix = i + 1;
                if (v2Ix == vertices.Length)
                {
                    v2Ix = 0;
                }
                var v2 = vertices[v2Ix];

                var line = new LineSegment2D(v1, v2);
                var normal = line.Normal();
                var keep = true;

                for(var j = 0; j < normalIx; j++)
                {
                    var oldNormal = normals[j];
                    if (oldNormal.IsParallel(normal))
                    {
                        keep = false;
                        break;
                    }
                }

                if (keep)
                {
                    normals[normalIx] = normal;
                    normalIx++;
                }
            }

            Array.Resize(ref normals, normalIx);
            _Normals = normals;

            var lineSegs = new LineSegment2D[vertices.Length];
            for(var i = 0; i < lineSegs.Length; i++)
            {
                var vNextIx = i + 1;
                if(vNextIx == Vertices.Length)
                {
                    vNextIx = 0;
                }
                var v1 = Vertices[i];
                var v2 = Vertices[vNextIx];

                var line = new LineSegment2D(v1, v2);
                lineSegs[i] = line;
            }
            _LineSegements = lineSegs;
        }

        public override string ToString() => $"{Vertices.Length} Vertices, Bounding Box [{BoundingX}, {BoundingY}, {BoundingX + BoundingWidth}, {BoundingY + BoundingHeight}]";
        
        public bool Equals(ConvexPolygonPattern other)
        {
            if (this.Vertices.Length != other.Vertices.Length) return false;
            if (this.OriginalHeight != other.OriginalHeight) return false;

            for(var i = 0; i < this.Vertices.Length; i++)
            {
                var thisV = this.Vertices[i];
                var otherV = other.Vertices[i];

                if (!thisV.Equals(otherV)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var ret = 17;
            for(var i = 0; i < this.Vertices.Length; i++)
            {
                ret *= 23;
                ret += this.Vertices[i].GetHashCode();
            }
            ret *= 23;
            ret += this.OriginalHeight.GetHashCode();

            return ret;
        }
    }
    
    readonly struct ConvexPolygon: IEquatable<ConvexPolygon>
    {
        public struct NormalEnumerator: IDisposable
        {
            private Vector _Current;
            public Vector Current => _Current;

            private readonly Vector[] Normals1;
            private readonly Vector[] Normals2;

            private int N1Index, N2Index;

            public NormalEnumerator(Vector[] n1, Vector[] n2)
            {
                _Current = default;
                Normals1 = n1;
                Normals2 = n2;
                N1Index = N2Index = 0;
            }

            public bool MoveNext()
            {
                if(N1Index < Normals1.Length)
                {
                    _Current = Normals1[N1Index];
                    N1Index++;
                    return true;
                }

                while(N2Index < Normals2.Length)
                {
                    var n2 = Normals2[N2Index];
                    var keep = true;
                    for(var i = 0; i < Normals1.Length; i++)
                    {
                        var n1 = Normals1[i];
                        if (n1.IsParallel(n2))
                        {
                            keep = false;
                            break;
                        }
                    }
                    if (keep)
                    {
                        for(var i = 0; i < N2Index; i++)
                        {
                            var oldN2 = Normals2[i];
                            if (oldN2.IsParallel(n2))
                            {
                                keep = false;
                                break;
                            }
                        }
                    }

                    N2Index++;

                    if (!keep)
                    {
                        continue;
                    }

                    _Current = n2;
                    return true;
                }

                return false;
            }

            public void Dispose() { }
        }

        public readonly struct NormalEnumerable
        {
            private readonly Vector[] Normals1;
            private readonly Vector[] Normals2;

            public NormalEnumerable(Vector[] n1, Vector[] n2)
            {
                Normals1 = n1;
                Normals2 = n2;
            }

            public NormalEnumerator GetEnumerator() => new NormalEnumerator(Normals1, Normals2);
        }
        
        public FixedPoint BoundingX => Pattern.BoundingX + Translation.X;
        public FixedPoint BoundingY => Pattern.BoundingY + Translation.Y;
        public FixedPoint BoundingWidth => Pattern.BoundingWidth;
        public FixedPoint BoundingHeight => Pattern.BoundingHeight;

        private readonly ConvexPolygonPattern Pattern;
        private readonly Point Translation;

        public int NumVertices => Pattern.Vertices.Length;

        public int NumLineSegments => NumVertices;

        public Vector[] Normals => Pattern.Normals;

        public int OriginalHeight => Pattern.OriginalHeight;

        public ConvexPolygon(ConvexPolygonPattern pattern) : this(pattern, new Point(0, 0)) { }

        private ConvexPolygon(ConvexPolygonPattern pattern, Point translation)
        {
            Pattern = pattern;
            Translation = translation;
        }

        public Point GetVertex(int ix)
        {
            var v = Pattern.Vertices[ix];
            var ret = new Point(v.X + Translation.X, v.Y + Translation.Y);

            return ret;
        }
        
        public ConvexPolygon Translate(int x, int y) => Translate(FixedPoint.FromInt(x), FixedPoint.FromInt(y));

        public ConvexPolygon Translate(FixedPoint x, FixedPoint y)
        {
            var newPt = new Point(x + Translation.X, y + Translation.Y);
            return new ConvexPolygon(Pattern, newPt);
        }

        public LineSegment2D GetLineSegment(int ix)
        {
            var l = Pattern.LineSegments[ix];

            var p1 = new Point(l.P1.X + Translation.X, l.P1.Y + Translation.Y);
            var p2 = new Point(l.P2.X + Translation.X, l.P2.Y + Translation.Y);
            var ret = new LineSegment2D(p1, p2);

            return ret;
        }
        
        public static NormalEnumerable GetUniqueNormals(ConvexPolygon a, ConvexPolygon b)
        {
            var n1 = a.Pattern.Normals;
            var n2 = b.Pattern.Normals;

            return new NormalEnumerable(n1, n2);
        }

        public LineSegment1D ProjectOnto(Vector axis)
        {
            if (!axis.TryMagnitude(out var axisMag)) throw new InvalidOperationException("Couldn't take magnitude of an axis...");
            
            FixedPoint? min = null;
            FixedPoint? max = null;
            
            for(var i = 0; i < NumLineSegments; i++)
            {
                var line = GetLineSegment(i);

                var start1D = axis.Dot(line.P1);
                var stop1D = axis.Dot(line.P2);
                
                if(min == null || start1D < min)
                {
                    min = start1D;
                }
                if(min == null || stop1D < min)
                {
                    min = stop1D;
                }
                if(max == null || start1D > max)
                {
                    max = start1D;
                }
                if (max == null || stop1D > max)
                {
                    max = stop1D;
                }
            }

            return new LineSegment1D(min.Value, max.Value);
        }

        public override string ToString() => $"{Pattern.Vertices.Length} Vertices, Bounding Box [{BoundingX}, {BoundingY}, {BoundingX + BoundingWidth}, {BoundingY + BoundingHeight}]";

        public override int GetHashCode()
        {
            var ret = 13;
            ret *= 23;
            ret += Pattern.GetHashCode();
            ret *= 23;
            ret += Translation.GetHashCode();

            return ret;
        }

        public bool Equals(ConvexPolygon other)
        {
            return other.Translation.Equals(this.Translation) && other.Pattern.Equals(this.Pattern);
        }
    }
}
