using LearnMeAThing.Utilities;
using System;
using System.Collections.Generic;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class PolygonTests
    {
        private Buffer<Point> ScratchPoints1 = new Buffer<Point>(100);
        private Buffer<Point> ScratchPoints2 = new Buffer<Point>(100);
        private Buffer<Point> ScratchPoints3 = new Buffer<Point>(100);

        [Theory]
        // triangles
        [InlineData("(0,0); (1,0); (1,1)", TurnType.CounterClockwise)]
        [InlineData("(0,0); (1,1); (1,0)", TurnType.Clockwise)]
        // squares
        [InlineData("(0,0); (1,0); (1,1); (0,1)", TurnType.CounterClockwise)]
        [InlineData("(0,0); (0, 1); (1,1); (1,0)", TurnType.Clockwise)]
        // dented rectangle
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", TurnType.Clockwise)]
        [InlineData("(1,6); (1,2); (3,3); (4,2); (4,6); (3,4)", TurnType.CounterClockwise)]
        public void Turn(string ptsStr, TurnType expected)
        {
            var polygon = _ParsePattern(ptsStr);
            Assert.Equal(expected, polygon.Turn);
        }

        [Theory]
        // triangle (never reflexive)
        [InlineData("(0,0); (1,0); (1,1)", 0, false)]
        [InlineData("(0,0); (1,0); (1,1)", 1, false)]
        [InlineData("(0,0); (1,0); (1,1)", 2, false)]
        // squares (no reflexive)
        [InlineData("(0,0); (1,0); (1,1); (0,1)", 0, false)]
        [InlineData("(0,0); (1,0); (1,1); (0,1)", 1, false)]
        [InlineData("(0,0); (1,0); (1,1); (0,1)", 2, false)]
        [InlineData("(0,0); (1,0); (1,1); (0,1)", 3, false)]
        // dented rectangle (two reflexive)
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", 0, false)]
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", 1, true)]
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", 2, false)]
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", 3, false)]
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", 4, true)]
        [InlineData("(1,6); (3,4); (4,6); (4,2); (3,3); (1,2)", 5, false)]
        public void IsReflexive(string ptsStr, int ix, bool expected)
        {
            var polygon = _ParsePattern(ptsStr);
            var val = polygon.IsReflexVertex(ix);
            Assert.Equal(expected, val);
        }

        [Theory]
        [InlineData("(0,0); (1,1)", "(0,2); (3,2)", PolygonPattern.IntersectResult.NotIntersecting)]
        [InlineData("(0,0); (1,1)", "(1,1); (2,2)", PolygonPattern.IntersectResult.CoLinear)]
        [InlineData("(0,0); (1,1)", "(1,0); (0,1)", PolygonPattern.IntersectResult.Intersecting)]
        [InlineData("(0,0); (2,2)", "(1,0); (1,2)", PolygonPattern.IntersectResult.Intersecting)]
        internal void Intersects(string line1, string line2, PolygonPattern.IntersectResult expected)
        {
            var l1 = _ParseLine(line1);
            var l2 = _ParseLine(line2);

            var val = PolygonPattern.Intersect(l1, l2, out var _, false);
            Assert.Equal(expected, val);
        }

        [Theory]
        // triangle
        [InlineData("(0,0); (1,0); (1,1)", "(1,1);(1,0);(0,0)")]
        // square (no reflexive points)
        [InlineData("(0,0); (1,0); (1,1); (0,1)", "(0,1);(1,1);(1,0);(0,0)")]
        // dented square (1 reflexive point)
        [InlineData("(2,6); (3,4); (5,6); (5,3); (2,3)", "(3,4);(5,6);(5,3);(2,3)|(2,3);(2,6);(3,4)")]
        // dented square (1 reflexive point), other way
        [InlineData("(2,3); (5,3); (5,6); (3,4); (2,6)", "(3,4);(5,6);(5,3);(2,3)|(2,3);(2,6);(3,4)")]
        // star, square with 4 triangles attached
        [InlineData("(1,5);(3,6);(4,8);(5,6);(7,5);(5,4);(4,2);(3,4)", "(3,6);(4,8);(5,6)|(5,4);(4,2);(3,4)|(3,4);(1,5);(3,6);(5,6);(7,5);(5,4)")]
        // tree base
        [InlineData("(73, 81); (78, 71); (78, 67); (68, 49); (71, 35); (71, 20); (61, 20); (53, 30); (43, 0); (38, 0); (28, 31); (20, 20); (10, 20); (10, 36); (14, 49); (4, 67); (4, 71); (9, 82)", "(14,49);(4,67);(4,71);(9,82);(41,81 + 524,288/1,048,576)|(41,81 + 524,288/1,048,576);(41,41 + 42/1,048,576);(11 + 564,616/1,048,576,40 + 1,048,570/1,048,576);(14,49)|(28,31);(20,20);(10,20)|(28,31);(10,20);(10,36);(11 + 564,616/1,048,576,40 + 1,048,570/1,048,576);(41,41 + 42/1,048,576)|(41,41 + 42/1,048,576);(41 + 2/1,048,576,0);(38,0);(28,31)|(68,49);(69 + 805,154/1,048,576,40 + 786,444/1,048,576);(41 + 1/1,048,576,40 + 786,432/1,048,576);(41,81 + 524,288/1,048,576);(73,81)|(73,81);(78,71);(78,67);(68,49)|(53,30);(43,0);(41 + 2/1,048,576,0)|(41 + 2/1,048,576,0);(41 + 1/1,048,576,40 + 786,432/1,048,576);(69 + 805,154/1,048,576,40 + 786,444/1,048,576);(53,30)|(69 + 805,154/1,048,576,40 + 786,444/1,048,576);(71,35);(71,20);(61,20);(53,30)")]
        // double reflexive
        [InlineData("(1, 1); (2, 2); (2, 3); (1, 4); (3, 4); (3, 1)", "(2,3);(1,4);(3,4)|(3,4);(3,1);(2,2);(2,3)|(3,1);(1,1);(2,2)")]
        public void DecomposeIntoConvexPolygons(string ptsStr, string expected)
        {
            var poly = _ParsePattern(ptsStr);

            var convex = poly.DecomposeIntoConvexPolygons(ScratchPoints1, ScratchPoints2, ScratchPoints3);

            var vals = new List<string>();
            foreach(var cp in convex)
            {
                var pts = new List<string>();
                foreach(var pt in cp.Vertices)
                {
                    pts.Add("(" + pt.X + "," + pt.Y + ")");
                }

                vals.Add(string.Join(";", pts));
            }

            var val = string.Join("|", vals);
            Assert.Equal(expected, val);
        }

        [Theory]
        // hour-glass
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", "(1,5);(3,5);(3,1);(1,1);(2,3)", "(3,5);(5,5);(4,2);(5,1);(3,1)")]
        // spike backwards L
        [InlineData("(1,1);(3,3);(3,6);(5,8);(5,1)", "(3,3);(3,6);(5,8);(5,3 + 6/1,048,576)", "(1,1);(3,3);(5,3 + 6/1,048,576);(5,1)")]
        // tree!
        [InlineData(
            "(3632, 4064);(3888, 3552);(3888, 3328);(3376, 2432);(3568, 1760);(3568, 1024);(3072, 1024);(2656, 1536);(2160, 0);(1920, 16);(1424, 1552);(1008, 1040);(512, 1040);(512, 1776);(704, 2448);(192, 3344);(192, 3568);(448, 4080)",
            "(448,4,080);(192,3,568);(192,3,344);(704,2,448);(587 + 449,472/1,048,576,2,040 + 288/1,048,576);(3,488 + 128/1,048,576,2,039 + 1,048,128/1,048,576);(3,376,2,432);(3,888,3,328);(3,888,3,552);(3,632,4,064)",
            "(587 + 449,472/1,048,576,2,040 + 288/1,048,576);(512,1,776);(512,1,040);(1,008,1,040);(1,424,1,552);(1,920,16);(2,160,0);(2,656,1,536);(3,072,1,024);(3,568,1,024);(3,568,1,760);(3,488 + 128/1,048,576,2,039 + 1,048,128/1,048,576)"
        )]
        public void SplitNaively(string ptsStr, string poly1Expected, string poly2Expected)
        {
            var poly = _ParsePattern(ptsStr);

            var polys = poly.SplitNaively(ScratchPoints1, ScratchPoints2, ScratchPoints3);
            var poly1 = polys[0];
            var poly2 = polys[1];

            var pts = new List<string>();
            foreach (var pt in poly1.Vertices)
            {
                pts.Add("(" + pt.X + "," + pt.Y + ")");
            }
            var poly1Pts = string.Join(";", pts);

            pts.Clear();
            foreach (var pt in poly2.Vertices)
            {
                pts.Add("(" + pt.X + "," + pt.Y + ")");
            }
            var poly2Pts = string.Join(";", pts);

            Assert.Equal(poly1Expected, poly1Pts);
            Assert.Equal(poly2Expected, poly2Pts);
        }

        [Theory]
        // hour-glass looking thing
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 0, null, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 1, null, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 2, null, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 3, null, true)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 4, null, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 5, null, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 6, null, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 0, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 1, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 2, true)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 3, true)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 4, true)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 5, false)]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", null, 6, false)]
        public void CanIntersect(string ptsStr, int? x, int? y, bool expected)
        {
            if(x.HasValue && y.HasValue) throw new Exception();
            if (!x.HasValue && !y.HasValue) throw new Exception();

            var poly = _ParsePattern(ptsStr);

            bool val;
            if (x.HasValue)
            {
                val = poly.CanDivideAlongVertical(FixedPoint.FromInt(x.Value));
            }
            else
            {
                val = poly.CanDivideAlongHorizontal(FixedPoint.FromInt(y.Value));
            }

            Assert.Equal(expected, val);
        }

        [Theory]
        // hour glass thing-y
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 3, "(1,5);(3,5);(3,1);(1,1);(2,3)", "(3,5);(5,5);(4,2);(5,1);(3,1)")]
        // hour glass, but backwards
        [InlineData("(2,3);(1,1);(5,1);(4,2);(5,5);(1,5)", 3, "(1,5);(3,5);(3,1);(1,1);(2,3)", "(3,5);(5,5);(4,2);(5,1);(3,1)")]
        // spiky reverse l
        [InlineData("(1,1);(3,3);(3,6);(5,8);(5,1)", 2, "(1,1);(2,2);(2,1)", "(2,2);(3,3);(3,6);(5,8);(5,1);(2,1)")]
        [InlineData("(1,1);(3,3);(3,6);(5,8);(5,1)", 4, "(1,1);(3,3);(3,6);(4,7);(4,1)", "(4,7);(5,8);(5,1);(4,1)")]
        // spiky reverse l, but counter clockwise
        [InlineData("(5,1);(5,8);(3,6);(3,3);(1,1)", 2, "(1,1);(2,2);(2,1)", "(2,2);(3,3);(3,6);(5,8);(5,1);(2,1)")]
        [InlineData("(5,1);(5,8);(3,6);(3,3);(1,1)", 4, "(1,1);(3,3);(3,6);(4,7);(4,1)", "(4,7);(5,8);(5,1);(4,1)")]
        public void SplitVertically(string ptsStr, int x, string leftPtsExpected, string rightPtsExpected)
        {
            var poly = _ParsePattern(ptsStr);

            var split = poly.SplitVertically(FixedPoint.FromInt(x), ScratchPoints1, ScratchPoints2, ScratchPoints3);
            var left = split[0];
            var right = split[1];

            var pts = new List<string>();
            foreach (var pt in left.Vertices)
            {
                pts.Add("(" + pt.X + "," + pt.Y + ")");
            }
            var leftPts = string.Join(";", pts);

            pts.Clear();
            foreach (var pt in right.Vertices)
            {
                pts.Add("(" + pt.X + "," + pt.Y + ")");
            }
            var rightPts = string.Join(";", pts);
            
            Assert.Equal(leftPtsExpected, leftPts);
            Assert.Equal(rightPtsExpected, rightPts);
        }

        [Theory]
        // hour glass thing-y
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 2, "(1,5);(5,5);(4,2);(1 + 524,288/1,048,576,2);(2,3)", "(4,2);(5,1);(1,1);(1 + 524,288/1,048,576,2)")]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 3, "(1,5);(5,5);(4 + 349,526/1,048,576,3 + 2/1,048,576);(2,3)", "(4 + 349,526/1,048,576,3 + 2/1,048,576);(4,2);(5,1);(1,1);(2,3)")]
        [InlineData("(1,5);(5,5);(4,2);(5,1);(1,1);(2,3)", 4, "(1,5);(5,5);(4 + 699,051/1,048,576,4 + 1/1,048,576);(1 + 524,288/1,048,576,4)", "(4 + 699,051/1,048,576,4 + 1/1,048,576);(4,2);(5,1);(1,1);(2,3);(1 + 524,288/1,048,576,4)")]
        // hour glass thing-y, backwards
        [InlineData("(2,3);(1,1);(5,1);(4,2);(5,5);(1,5)", 2, "(1,5);(5,5);(4,2);(1 + 524,288/1,048,576,2);(2,3)", "(4,2);(5,1);(1,1);(1 + 524,288/1,048,576,2)")]
        [InlineData("(2,3);(1,1);(5,1);(4,2);(5,5);(1,5)", 3, "(1,5);(5,5);(4 + 349,526/1,048,576,3 + 2/1,048,576);(2,3)", "(4 + 349,526/1,048,576,3 + 2/1,048,576);(4,2);(5,1);(1,1);(2,3)")]
        [InlineData("(2,3);(1,1);(5,1);(4,2);(5,5);(1,5)", 4, "(1,5);(5,5);(4 + 699,051/1,048,576,4 + 1/1,048,576);(1 + 524,288/1,048,576,4)", "(4 + 699,051/1,048,576,4 + 1/1,048,576);(4,2);(5,1);(1,1);(2,3);(1 + 524,288/1,048,576,4)")]
        public void SplitHorizontally(string ptsStr, int y, string topPtsExpected, string bottomPtsExpected)
        {
            var poly = _ParsePattern(ptsStr);

            var split = poly.SplitHorizontally(FixedPoint.FromInt(y), ScratchPoints1, ScratchPoints2, ScratchPoints3);
            var top = split[0];
            var bottom = split[1];

            var pts = new List<string>();
            foreach (var pt in top.Vertices)
            {
                pts.Add("(" + pt.X + "," + pt.Y + ")");
            }
            var topPts = string.Join(";", pts);

            pts.Clear();
            foreach (var pt in bottom.Vertices)
            {
                pts.Add("(" + pt.X + "," + pt.Y + ")");
            }
            var bottomPts = string.Join(";", pts);

            Assert.Equal(topPtsExpected, topPts);
            Assert.Equal(bottomPtsExpected, bottomPts);
        }

        private static LineSegment2D _ParseLine(string ptsStr)
        {
            var pts = _ParsePoints(ptsStr);

            if (pts.Length != 2) throw new InvalidOperationException();

            return new LineSegment2D(pts[0], pts[1]);
        }

        private static PolygonPattern _ParsePattern(string ptsStr)
        {
            var pts = _ParsePoints(ptsStr);
            FixedPoint? maxY, minY;
            maxY = minY = null;

            for(var i = 0; i < pts.Length; i++)
            {
                var pt = pts[i];
                if (maxY == null || pt.Y > maxY) maxY = pt.Y;
                if (minY == null || pt.Y < minY) minY = pt.Y;
            }

            var height = (int)(maxY.Value - minY.Value);

            var polygon = new PolygonPattern(pts, height);
            return polygon;
        }

        private static Point[] _ParsePoints(string ptsStr)
        {
            var pts = new List<Point>();
            foreach (var ptStr in ptsStr.Split(';'))
            {
                var ptParts = ptStr.Split(',');
                if (ptParts.Length != 2) throw new Exception();

                var xStr = ptParts[0].Trim('(', ' ');
                var yStr = ptParts[1].Trim(')', ' ');

                var x = int.Parse(xStr);
                var y = int.Parse(yStr);

                pts.Add(new Point(x, y));
            }

            return pts.ToArray();
        }
    }
}
