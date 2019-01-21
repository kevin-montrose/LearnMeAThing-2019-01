using LearnMeAThing.Utilities;
using System;
using System.Collections.Generic;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class CollisionTests
    {
        [Fact]
        public void FixedPointOperations()
        {
            // addition
            {
                var one = FixedPoint.One;
                var two = FixedPoint.FromInt(2);
                var oneHalf = one / two;

                var three = one + two;
                Assert.Equal(FixedPoint.FromInt(3), three);

                var oneAndAHalf = one + oneHalf;
                Assert.Equal("1 + 524,288/1,048,576", oneAndAHalf.ToString());

                var twoAndAHalf = two + oneHalf;
                Assert.Equal("2 + 524,288/1,048,576", twoAndAHalf.ToString());

                var threeAndAHalf = three + oneHalf;
                Assert.Equal("3 + 524,288/1,048,576", threeAndAHalf.ToString());
            }

            // subtraction
            {
                var one = FixedPoint.One;
                var two = FixedPoint.FromInt(2);
                var oneHalf = one / two;

                var negativeOne = one - two;
                Assert.Equal(FixedPoint.FromInt(-1), negativeOne);

                var aHalf = one - oneHalf;
                Assert.Equal("524,288/1,048,576", aHalf.ToString());

                var oneAndAHalf = two - oneHalf;
                Assert.Equal("1 + 524,288/1,048,576", oneAndAHalf.ToString());

                var negativeOneHalf = FixedPoint.Zero - oneHalf;
                Assert.Equal("-(524,288/1,048,576)", negativeOneHalf.ToString());
            }

            // division
            {
                var one = FixedPoint.One;
                Assert.Equal("1", one.ToString());

                var oneHalf1 = one / 2;
                var oneHalf2 = one / FixedPoint.FromInt(2);
                var oneHalf3 = 1 / FixedPoint.FromInt(2);
                Assert.Equal(oneHalf1, oneHalf2);
                Assert.Equal(oneHalf1, oneHalf3);
                Assert.Equal("524,288/1,048,576", oneHalf1.ToString());

                var nineteenDivideBySeven = FixedPoint.FromInt(19) / FixedPoint.FromInt(7);
                Assert.Equal("2 + 748,982/1,048,576", nineteenDivideBySeven.ToString());
                var sevenDividedByNineteen = FixedPoint.FromInt(7) / FixedPoint.FromInt(19);
                Assert.Equal("386,317/1,048,576", sevenDividedByNineteen.ToString());
            }

            // sqrt
            {
                var zero = FixedPoint.Zero;
                Assert.Equal("0", zero.Sqrt().ToString());
                var one = FixedPoint.One;
                Assert.Equal("1", one.Sqrt().ToString());

                var two = FixedPoint.FromInt(2);
                var sqrt2 = two.Sqrt();
                Assert.Equal("1 + 434,176/1,048,576", sqrt2.ToString());

                var big = FixedPoint.FromInt(1_444_579);
                var sqrtBig = big.Sqrt();
                Assert.Equal("1,201 + 950,272/1,048,576", sqrtBig.ToString());
            }
        }

        [Fact]
        public void VectorOperations()
        {
            // equality
            {
                var a = new Vector(FixedPoint.One, FixedPoint.FromInt(3));
                var b = new Vector(FixedPoint.One, FixedPoint.FromInt(3));
                var c = new Vector(FixedPoint.FromInt(2), FixedPoint.FromInt(2));

                Assert.True(a == b);
                Assert.True(a != c);
            }

            // add
            {
                var a = new Vector(FixedPoint.One, FixedPoint.FromInt(3));
                var b = new Vector(FixedPoint.FromInt(4), FixedPoint.FromInt(-10));

                var c = a + b;

                Assert.Equal("DeltaX=5, DeltaY=-(7)", c.ToString());
            }

            // subtract
            {
                var a = new Vector(FixedPoint.One, FixedPoint.FromInt(3));
                var b = new Vector(FixedPoint.FromInt(4), FixedPoint.FromInt(-10));

                var c = a - b;

                Assert.Equal("DeltaX=-(3), DeltaY=13", c.ToString());
            }

            // magnitude
            {
                var a = new Vector(FixedPoint.FromInt(15), FixedPoint.FromInt(-14));
                Assert.True(a.TryMagnitude(out var mag));
                Assert.Equal("20 + 543,460/1,048,576", mag.ToString());
            }

            // normal
            {
                var tolerance = FixedPoint.One / 10_000;

                var a = new Vector(FixedPoint.FromInt(-3), FixedPoint.FromInt(23));
                Assert.True(a.TryMagnitude(out var mPre));
                var n = a.Normalize();
                Assert.True(n.TryMagnitude(out var mPost));
                var b = n * mPre;

                var diff = (mPost - FixedPoint.One).Abs();
                Assert.True(diff < tolerance);

                var delta = a - b;
                Assert.True(delta.TryMagnitude(out diff));

                Assert.True(diff < tolerance);
            }

            // parallel
            {
                var a = new Vector(FixedPoint.FromInt(2), FixedPoint.FromInt(4));
                var b = a * FixedPoint.FromInt(5);
                var c = a * FixedPoint.FromInt(-1);

                Assert.True(a.IsParallel(b));
                Assert.True(b.IsParallel(a));
                Assert.True(a.IsParallel(c));

                var d = new Vector(FixedPoint.FromInt(-2), FixedPoint.FromInt(4));

                Assert.False(a.IsParallel(d));
                Assert.False(d.IsParallel(a));
            }

            // dot
            {
                var a = new Vector(FixedPoint.FromInt(4), FixedPoint.FromInt(-99));
                var b = new Vector(FixedPoint.FromInt(17), FixedPoint.FromInt(63));

                var d = a.Dot(b);
                Assert.Equal("-(6,169)", d.ToString());
            }
        }

        [Theory]
        [InlineData(1, 2, 3, 4, "DeltaX=-(741,455/1,048,576), DeltaY=741,455/1,048,576")]       // line: y=x+1              ; slope = 1       ; normal slope = -1
        [InlineData(3, 4, 1, 2, "DeltaX=741,455/1,048,576, DeltaY=-(741,455/1,048,576)")]       // line: y=x+1              ; slope = 1       ; normal slope = -1
        [InlineData(0, 0, 0, 2, "DeltaX=-(1), DeltaY=0")]                                       // line: vertical at x=0    ; slope = infinity; normal slope = any y = 2
        [InlineData(0, 2, 0, 0, "DeltaX=1, DeltaY=0")]                                          // line: vertical at x=0    ; slope = infinity; normal slope = any y = 2
        [InlineData(15, 15, 2, -2, "DeltaX=832,944/1,048,576, DeltaY=-(636,957/1,048,576)")]    // line: y=(17/13)*x - 60/13; slope = 17/13   ; normal slope = -13/17
        [InlineData(2, -2, 15, 15, "DeltaX=-(832,944/1,048,576), DeltaY=636,957/1,048,576")]    // line: y=(17/13)*x - 60/13; slope = 17/13   ; normal slope = -13/17
        [InlineData(4, 4, 8, 4, "DeltaX=0, DeltaY=1")]                                          // line: y=4; slope = 0; normal slope = infinity
        [InlineData(8, 4, 4, 4, "DeltaX=0, DeltaY=-(1)")]                                       // line: y=4; slope = 0; normal slope = infinity
        public void NormalOfALine(int p1X, int p1Y, int p2X, int p2Y, string expected)
        {
            var tolerance = FixedPoint.One / 10_000;

            var p1 = new Point(p1X, p1Y);
            var p2 = new Point(p2X, p2Y);

            var lineSeg = new LineSegment2D(p1, p2);

            var normal = lineSeg.Normal();
            Assert.True(normal.TryMagnitude(out var normalMag));

            var diffFromOne = (FixedPoint.One - normalMag).Abs();
            Assert.True(diffFromOne < tolerance);
            Assert.Equal(expected, normal.ToString());
        }

        [Theory]
        [InlineData(4, 3, 17, 0, "X=4, Y=0")]
        [InlineData(4, 3, 0, 22, "X=0, Y=3")]
        [InlineData(3, 21, 4, -8, "X=-(7 + 838,845/1,048,576), Y=15 + 629,114/1,048,576")]
        [InlineData(3, 21, 2, -4, "X=-(7 + 838,845/1,048,576), Y=15 + 629,114/1,048,576")]
        [InlineData(3, 21, -4, 8, "X=-(7 + 838,845/1,048,576), Y=15 + 629,114/1,048,576")]
        [InlineData(3, 21, -2, 4, "X=-(7 + 838,845/1,048,576), Y=15 + 629,114/1,048,576")]
        public void PointProjectOnto(int pX, int pY, int axisX, int axisY, string expected)
        {
            var pt = new Point(pX, pY);
            var axis = new Vector(axisX, axisY);

            var res = pt.ProjectOnto(axis);
            Assert.Equal(expected, res.ToString());
        }
        
        private static ConvexPolygon ForName(string name)
        {
            switch (name)
            {
                case "triangle": return new ConvexPolygon(new ConvexPolygonPattern(new[] { new Point(1, 1), new Point(2, 0), new Point(0, 0) }, 2));
                case "square": return new ConvexPolygon(new ConvexPolygonPattern(new[] { new Point(0, 1), new Point(1, 1), new Point(1, 0), new Point(0, 0) }, 1));
                case "bigsquare": return new ConvexPolygon(new ConvexPolygonPattern(new[] { new Point(0, 0), new Point(0, 2), new Point(2, 2), new Point(2, 0) }, 2));
                case "pentagon": return new ConvexPolygon(new ConvexPolygonPattern(new[] { new Point(0, 0), new Point(0, 2), new Point(2, 4), new Point(4, 2), new Point(4, 0) }, 4));
                case "octagon": return new ConvexPolygon(new ConvexPolygonPattern(new[] { new Point(0, 2), new Point(0, 5), new Point(2, 7), new Point(5, 7), new Point(7, 5), new Point(7, 2), new Point(5, 0), new Point(2, 0) }, 7));
                case "smalloctagon": return new ConvexPolygon(new ConvexPolygonPattern(new[] { new Point(0, 1), new Point(0, 2), new Point(1, 3), new Point(2, 3), new Point(3, 2), new Point(3, 1), new Point(2, 0), new Point(1, 0) }, 3));
                default: throw new InvalidOperationException($"Unexpected {nameof(name)}: {name}");
            }
        }

        [Fact]
        public void Normals()
        {
            // triangle
            {
                var p = ForName("triangle");
                var ns = p.Normals;

                Assert.Equal(3, ns.Length);
                Assert.Equal("DeltaX=741,455/1,048,576, DeltaY=741,455/1,048,576", ns[0].ToString());
                Assert.Equal("DeltaX=0, DeltaY=-(1)", ns[1].ToString());
                Assert.Equal("DeltaX=-(741,455/1,048,576), DeltaY=741,455/1,048,576", ns[2].ToString());
            }

            // square
            {
                var p = ForName("square");
                var ns = p.Normals;

                Assert.Equal(2, ns.Length);
                Assert.Equal("DeltaX=0, DeltaY=1", ns[0].ToString());
                Assert.Equal("DeltaX=1, DeltaY=0", ns[1].ToString());
            }

            // pentagon
            {
                var p = ForName("pentagon");
                var ns = p.Normals;

                Assert.Equal(4, ns.Length);
                Assert.Equal("DeltaX=-(1), DeltaY=0", ns[0].ToString());
                Assert.Equal("DeltaX=-(741,455/1,048,576), DeltaY=741,455/1,048,576", ns[1].ToString());
                Assert.Equal("DeltaX=741,455/1,048,576, DeltaY=741,455/1,048,576", ns[2].ToString());
                Assert.Equal("DeltaX=0, DeltaY=-(1)", ns[3].ToString());
            }

            // octagon
            {
                var p = ForName("octagon");
                var ns = p.Normals;

                Assert.Equal(4, ns.Length);
                Assert.Equal("DeltaX=-(1), DeltaY=0", ns[0].ToString());
                Assert.Equal("DeltaX=-(741,455/1,048,576), DeltaY=741,455/1,048,576", ns[1].ToString());
                Assert.Equal("DeltaX=0, DeltaY=1", ns[2].ToString());
                Assert.Equal("DeltaX=741,455/1,048,576, DeltaY=741,455/1,048,576", ns[3].ToString());
            }
        }

        [Theory]
        [InlineData(-2, 1, 10, 15, -4, "2 + 262,144/1,048,576")]    // left, moving right
        [InlineData(-2, 1, -15, -10, 4, "2")]                       // right, moving left
        [InlineData(1, 2, 4, 6, 1, "NEVER")]                        // right, moving right
        [InlineData(1, 2, -4, -3, -10, "NEVER")]                    // left, moving left
        [InlineData(1, 2, 3, 4, 0, "NEVER")]                        // no motion
        [InlineData(1, 4, 2, 3, 1, "NEVER")]                        // overlapping
        public void LineSegementCollision1D(int start1, int stop1, int start2, int stop2, int motion2, string expected)
        {
            var ls1 = new LineSegment1D(FixedPoint.FromInt(start1), FixedPoint.FromInt(stop1));
            var ls2 = new LineSegment1D(FixedPoint.FromInt(start2), FixedPoint.FromInt(stop2));

            var m = FixedPoint.FromInt(motion2);
            var time = CollisionDetector.DetermineCollisionTime(ls1, ls2, m);

            var res = time == null ? "NEVER" : time.Value.ToString();
            Assert.Equal(expected, res);
        }

        [Theory]
        [InlineData(    // two triangles, one above the other, sliding towards each over on the y axis
            0, 0,
            0, 5,
            0, -1,
            1,          // vertical axis (0 is up and right, 2 is up and left)
            "4"
        )]
        [InlineData(    // two triangles, one to the right and above the other, sliding towards each over along a up-and-to-the-right line
            0, 0,
            6, 6,
            -1, -1,
            0,          // up and right (1 is vertical, 2 is up and left)
            "5"
        )]
        [InlineData(    // two triangles, one to the right and above the other, sliding away from each over along a up-and-to-the-right line
            0, 0,
            6, 6,
            1, 1,
            0,          // up and right (1 is vertical, 2 is up and left)
            "NEVER"
        )]
        [InlineData(    // two triangles, one to the left and above the other, sliding towards each other along a up-and-to-the-left line
            0, 0,
            -7, 7,
            1, -1,
            2,          // up and left (0 is up and right, 1 vertical)
            "6"
        )]
        public void PolygonCollisionOnAxis_Triangles(
            int x1, int y1,
            int x2, int y2,
            int m2x, int m2y,
            int normalIndex,
            string expected
        ) => _PolygonCollionOnAxis("triangle", x1, y1, "triangle", x2, y2, m2x, m2y, normalIndex, expected);

        [Theory]
        [InlineData(                // two squares, sliding at each other along the x axis
            0, 0,
            5, 0,
            -1, 0,
            1,                      // horizontal axis (0 is vertical)
            "4"
        )]
        [InlineData(                // two squares, sliding at each other along the x axis
            0, 0,
            5, 0,
            -1, 0,
            0,                      // vertical axis (1 is horizontal)
            "NEVER"
        )]
        [InlineData(                // two squares, sliding away from each other along the x axis
            0, 0,
            5, 0,
            1, 0,
            1,                      // horizontal axis (0 is vertical)
            "NEVER"
        )]
        [InlineData(                // two squares, sliding away from each other along the x axis
            0, 0,
            5, 0,
            1, 0,
            0,                      // vertical axis (1 is horizontal)
            "NEVER"
        )]
        [InlineData(                // two squares, sliding away from each other along the x axis
            0, 0,
            10, 10,
            -1, -1,
            0,                      // vertical axis (1 is horizontal)
            "9"
        )]
        [InlineData(                // two squares, sliding away from each other along the x axis
            0, 0,
            10, 10,
            -1, -1,
            1,                      // vertical axis (1 is horizontal)
            "9"
        )]
        public void PolygonCollisionOnAxis_Squares(
            int x1, int y1,
            int x2, int y2,
            int m2x, int m2y,
            int normalIndex,
            string expected
        ) => _PolygonCollionOnAxis("square", x1, y1, "square", x2, y2, m2x, m2y, normalIndex, expected);
        
        private static void _PolygonCollionOnAxis(
            string poly1, int x1, int y1, 
            string poly2, int x2, int y2, 
            int m2x, int m2y, 
            int normalIndex, 
            string expected
        )
        {
            var p1 = ForName(poly1).Translate(x1, y1);
            var p2 = ForName(poly2).Translate(x2, y2);
            
            var m2 = new Vector(m2x, m2y);

            var normals = ConvexPolygon.GetUniqueNormals(p1, p2);

            Vector targetNormal = default;
            var ix = 0;
            foreach(var norm in normals)
            {
                if(ix == normalIndex)
                {
                    targetNormal = norm;
                    break;
                }
                ix++;
            }

            if (ix < normalIndex) throw new Exception("Couldn't find normal");

            var collisionTime = CollisionDetector.DetermineCollisionTime(p1, p2, m2, targetNormal);

            var res = collisionTime == null ? "NEVER" : collisionTime.Value.ToString();
            Assert.Equal(expected, res);
        }

        [Theory]
        // sloped lines
        [InlineData(0, 0, 5, 5, 4, 2, "@1 + 434,176/1,048,576; X=2 + 1,048,573/1,048,576, Y=2 + 1,048,573/1,048,576")]
        [InlineData(0, 0, 5, 5, -9, -6, "NEVER")]
        [InlineData(0, 0, 5, 5, 6, 6, "NEVER")]
        // vertical lines
        [InlineData(0, 0, 0, 3, 2, 2, "@2; X=0, Y=1 + 1,048,574/1,048,576")]
        [InlineData(1, 1, 0, 3, -4, -2, "NEVER")]
        // horizontal lines
        [InlineData(3, 5, 7, 5, 4, 3, "@2; X=4, Y=5")]
        [InlineData(3, 5, 7, 5, 4, 6, "@1; X=4, Y=5")]
        [InlineData(3, 5, 7, 5, 0, 5, "NEVER")]
        public void DetermineCollisionPointStationary(int x1, int y1, int x2, int y2, int px, int py, string expected)
        {
            var line = new LineSegment2D(new Point(x1, y1), new Point(x2, y2));
            var pt = new Point(px, py);
            var res = CollisionDetector.DetermineClosestPoint(line, pt);

            var val = res == null ? "NEVER" : $"@{res.Value.Distance}; {res.Value.Point}";
            Assert.Equal(expected, val);
        }

        [Theory]
        [InlineData(                // two squares moving towards each other on a up-right and down-left path
            "square", 0, 0,
            1, 1,
            "square", 5, 5,
            -2, -2,
            1,
            "X=2 + 524,287/1,048,576, Y=2 + 524,287/1,048,576"
        )]
        [InlineData(                // two squares moving towards each other on a up-right and down-left path
            "triangle", 0, 0,
            1, 1,
            "bigsquare", 0, 3,
            -1, -1,
            1,
            "X=1 + 262,115/1,048,576, Y=1 + 786,461/1,048,576"
        )]
        [InlineData(
            "octagon", 0, 0,
            0, 0,
            "square", 11, 3, 
            -1, 0,
            3,
            "X=7 + 524,288/1,048,576, Y=3 + 524,288/1,048,576"
        )]
        public void DetermineCollisionPointInMotion(
            string p1, int x1, int y1,
            int m1x, int m1y,
            string p2, int x2, int y2,
            int m2x, int m2y,
            int time,
            string expected
        )
        {
            var poly1 = ForName(p1);
            var poly1Trans = poly1.Translate(x1, y1);
            var poly2 = ForName(p2);
            var poly2Trans = poly2.Translate(x2, y2);

            var t = FixedPoint.FromInt(time);
            var v1 = new Vector(m1x, m1y);
            var v2 = new Vector(m2x, m2y);

            var detector = new CollisionDetector(64, 100);

            var collision = detector.DetermineCollisionPointInMotion(poly1Trans, v1, poly2Trans, v2, t);
            Assert.Equal(expected, collision.ToString());
        }

        [Fact]
        public void FullScene()
        {
            var step = FixedPoint.FromInt(1) / 1_000;

            var square1 = ForName("square");
            var bigSquare = ForName("bigsquare");
            var triangle = ForName("triangle");
            var square2 = ForName("square");
            var octagon = ForName("smalloctagon");

            var shapes = new List<ConvexPolygon>();
            shapes.Add(square1.Translate(1, 4));
            shapes.Add(bigSquare.Translate(5, 7));
            shapes.Add(triangle.Translate(5, 2));
            shapes.Add(square2.Translate(10, 2));
            shapes.Add(octagon.Translate(9, 5));

            var motion = new List<Vector>();
            motion.Add(new Vector(2, 1));
            motion.Add(new Vector(1, -1));
            motion.Add(new Vector(0, 2));
            motion.Add(new Vector(-1, 1));
            motion.Add(new Vector(-3, 0));
            
            var square1_BigSquare = new List<Collision>();
            var square1_Triangle = new List<Collision>();
            var square1_Square2 = new List<Collision>();
            var square1_Octagon = new List<Collision>();

            var bigSquare_Triangle = new List<Collision>();
            var bigSquare_Square2 = new List<Collision>();
            var bigSquare_Octagon = new List<Collision>();

            var triangle_Square2 = new List<Collision>();
            var triangle_Octagon = new List<Collision>();

            var square2_Octagon = new List<Collision>();

            var detector = new CollisionDetector(64, 100);
            using (var collisions = detector.FindCollisions(shapes.Count, shapes.ToArray(), motion.ToArray(), step))
            {
                // mux these into separate collections
                foreach (var c in collisions)
                {
                    if (c.FirstPolygonIndex == 0)
                    {
                        if (c.SecondPolygonIndex == 1)
                        {
                            square1_BigSquare.Add(c);
                            continue;
                        }

                        if (c.SecondPolygonIndex == 2)
                        {
                            square1_Triangle.Add(c);
                            continue;
                        }

                        if (c.SecondPolygonIndex == 3)
                        {
                            square1_Square2.Add(c);
                            continue;
                        }

                        if (c.SecondPolygonIndex == 4)
                        {
                            square1_Octagon.Add(c);
                            continue;
                        }

                        throw new Exception("Wut");
                    }

                    if (c.FirstPolygonIndex == 1)
                    {
                        if (c.SecondPolygonIndex == 2)
                        {
                            bigSquare_Triangle.Add(c);
                            continue;
                        }

                        if (c.SecondPolygonIndex == 3)
                        {
                            bigSquare_Square2.Add(c);
                            continue;
                        }

                        if (c.SecondPolygonIndex == 4)
                        {
                            bigSquare_Octagon.Add(c);
                            continue;
                        }

                        throw new Exception("Wut");
                    }

                    if (c.FirstPolygonIndex == 2)
                    {
                        if (c.SecondPolygonIndex == 3)
                        {
                            triangle_Square2.Add(c);
                            continue;
                        }

                        if (c.SecondPolygonIndex == 4)
                        {
                            triangle_Octagon.Add(c);
                            continue;
                        }

                        throw new Exception("Wut");
                    }

                    if (c.FirstPolygonIndex == 3)
                    {
                        if (c.SecondPolygonIndex == 4)
                        {
                            square2_Octagon.Add(c);
                            continue;
                        }

                        throw new Exception("Wut");
                    }

                    throw new Exception("Huh");
                }
            }

            Assert.Equal(0, square1_BigSquare.Count);
            Assert.Equal(1, square1_Triangle.Count);
            Assert.Equal("X=5 + 349,522/1,048,576, Y=5 + 699,049/1,048,576", square1_Triangle[0].CollisionAt.ToString());
            Assert.Equal("1 + 699,049/1,048,576", square1_Triangle[0].AtTime.ToString());
            Assert.Equal(0, square1_Square2.Count);
            Assert.Equal(1, square1_Octagon.Count);
            Assert.Equal("X=4 + 838,862/1,048,576, Y=6 + 209,715/1,048,576", square1_Octagon[0].CollisionAt.ToString());
            Assert.Equal("1 + 419,429/1,048,576", square1_Octagon[0].AtTime.ToString());

            Assert.Equal(1, bigSquare_Triangle.Count);
            Assert.Equal("X=6 + 524,287/1,048,576, Y=5 + 524,289/1,048,576", bigSquare_Triangle[0].CollisionAt.ToString());
            Assert.Equal("1 + 524,287/1,048,576", bigSquare_Triangle[0].AtTime.ToString());
            Assert.Equal(1, bigSquare_Square2.Count);
            Assert.Equal("X=8 + 524,288/1,048,576, Y=5", bigSquare_Square2[0].CollisionAt.ToString());
            Assert.Equal("1 + 1,048,575/1,048,576", bigSquare_Square2[0].AtTime.ToString());
            Assert.Equal(1, bigSquare_Octagon.Count);
            Assert.Equal("X=7 + 524,289/1,048,576, Y=6 + 786,433/1,048,576", bigSquare_Octagon[0].CollisionAt.ToString());
            Assert.Equal("524,287/1,048,576", bigSquare_Octagon[0].AtTime.ToString());
            

            Assert.Equal(0, triangle_Square2.Count);
            Assert.Equal(1, triangle_Octagon.Count);
            Assert.Equal("X=6 + 209,717/1,048,576, Y=5 + 209,714/1,048,576", triangle_Octagon[0].CollisionAt.ToString());
            Assert.Equal("1 + 209,714/1,048,576", triangle_Octagon[0].AtTime.ToString());

            Assert.Equal(0, square2_Octagon.Count);
        }

        [Theory]
        // no overlap
        [InlineData(1, 2, 3, 4, 0)]
        // no overlap
        [InlineData(3, 4, 1, 2, 0)]
        // overlaping on right
        [InlineData(1, 3, -1, 2, 1)]
        // overlaping on left
        [InlineData(-1, 2, 1, 3, 1)]
        // inside
        [InlineData(1, 5, 2, 4, 2)]
        // contains
        [InlineData(2, 4, 1, 5, 2)]
        public void TestOverlap(int a1, int a2, int b1, int b2, int expected)
        {
            var l1 = new LineSegment1D(FixedPoint.FromInt(a1), FixedPoint.FromInt(a2));
            var l2 = new LineSegment1D(FixedPoint.FromInt(b1), FixedPoint.FromInt(b2));

            var overlap1 = l1.MeasureOverlap(l2);
            var overlapInt1 = (int)overlap1;
            Assert.Equal(expected, overlapInt1);

            var overlap2 = l2.MeasureOverlap(l1);
            var overlapInt2 = (int)overlap2;
            Assert.Equal(expected, overlapInt2);
        }
    }
}
