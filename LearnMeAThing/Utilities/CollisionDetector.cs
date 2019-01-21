using System;

namespace LearnMeAThing.Utilities
{
    readonly struct Collision
    {
        private readonly int _FirstPolygonIndex;
        public int FirstPolygonIndex => _FirstPolygonIndex;
        private readonly int _SecondPolygonIndex;
        public int SecondPolygonIndex => _SecondPolygonIndex;
        private readonly Point _CollisionAt;
        public Point CollisionAt => _CollisionAt;
        private readonly FixedPoint _AtTime;
        public FixedPoint AtTime => _AtTime;

        public Collision(int ix1, int ix2, Point pt, FixedPoint at)
        {
            _FirstPolygonIndex = ix1;
            _SecondPolygonIndex = ix2;
            _CollisionAt = pt;
            _AtTime = at;
        }

        public override string ToString() => $"{nameof(FirstPolygonIndex)}={FirstPolygonIndex:N0}, {nameof(SecondPolygonIndex)}={SecondPolygonIndex}, {nameof(CollisionAt)}={CollisionAt}, {nameof(AtTime)}={AtTime}";
    }

    readonly struct CollisionResults : IDisposable
    {
        public struct Enumerator : IDisposable
        {
            private Collision _Current;
            public Collision Current => _Current;

            private readonly int Count;
            private readonly Collision[] Results;

            private int Index;

            public Enumerator(int count, Collision[] results)
            {
                _Current = default;
                Count = count;
                Results = results;
                Index = 0;
            }

            public bool MoveNext()
            {
                if (Index == Count) return false;

                _Current = Results[Index];
                Index++;

                return true;
            }

            public void Dispose() { }
        }

        private readonly int _Count;
        public int Count => _Count;

        public Collision this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

                return Results[index];
            }
        }

        private readonly Collision[] Results;

        private readonly CollisionDetector Owner;

        public CollisionResults(int count, Collision[] results, CollisionDetector owner)
        {
            _Count = count;
            Results = results;
            Owner = owner;
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, Results);

        public void Dispose()
        {
            Owner.Return(Results);
        }
    }

    class CollisionDetector
    {
        private static readonly FixedPoint Tolerance = FixedPoint.One / 10_000;
        
        private Collision[] ResultsBuffer;
        private Buffer<Point> FirstPointBuffer;
        private Buffer<Point> SecondPointBuffer;
        private Buffer<FixedPoint> TimeBuffer;
        public CollisionDetector(int maximumSimultaneousCollisions, int maximumPolygonVertices)
        {
            ResultsBuffer = new Collision[maximumSimultaneousCollisions];
            FirstPointBuffer = new Buffer<Point>(maximumPolygonVertices);
            SecondPointBuffer = new Buffer<Point>(maximumPolygonVertices);
            TimeBuffer = new Buffer<FixedPoint>(maximumSimultaneousCollisions);
        }

        public void Return(Collision[] buffer)
        {
            ResultsBuffer = buffer;
        }

        /// <summary>
        /// Determines the smallest translation of the moving polygon that will result in the polygons no longer colliding.
        /// </summary>
        public static Vector? GetMinimumTranslationVector(ConvexPolygon stationary, ConvexPolygon moving)
        {
            FixedPoint? smallestOverlap = null;
            Vector bestNorm = default;

            var stationaryCenter = new Point(stationary.BoundingX + stationary.BoundingWidth / 2, stationary.BoundingY - stationary.BoundingHeight / 2);
            var movingCenter = new Point(moving.BoundingX + moving.BoundingWidth / 2, moving.BoundingY - moving.BoundingHeight / 2);

            var normsToCheck = ConvexPolygon.GetUniqueNormals(stationary, moving);
            foreach (var norm in normsToCheck)
            {
                var stationaryOnNorm = stationary.ProjectOnto(norm);
                var movingOnNorm = moving.ProjectOnto(norm);

                // not actually colliding
                if (!movingOnNorm.Overlaps(stationaryOnNorm, Tolerance))
                {
                    return null;
                }

                var overlapSize = movingOnNorm.MeasureOverlap(stationaryOnNorm);
                if (overlapSize.IsZero)
                {
                    overlapSize = FixedPoint.One;
                }

                if (smallestOverlap == null || overlapSize < smallestOverlap)
                {
                    smallestOverlap = overlapSize;
                    bestNorm = norm;
                }
            }

            var centerDiff = new Vector(movingCenter.X - stationaryCenter.X, movingCenter.Y - stationaryCenter.Y);
            if (centerDiff.Dot(bestNorm) < 0)
            {
                bestNorm = new Vector(-bestNorm.DeltaX, -bestNorm.DeltaY);
            }

            var ret = bestNorm * smallestOverlap.Value;
            return ret;
        }

        /// <summary>
        /// Takes a set of polygons in a particular kind of motion, and a time step to 'lock' to,
        ///    and returns a list of all collisions that would occur.
        /// </summary>
        internal CollisionResults FindCollisions(
            int count,
            ConvexPolygon[] polygons,
            Vector[] motion,
            FixedPoint timeStep,
            bool limitTime = false     // in practice this means we only look two "steps" forward
        )
        {
            if (count < 0) throw new ArgumentOutOfRangeException($"{count} must be >= 0");
            if (polygons == null) throw new ArgumentNullException(nameof(polygons));
            if (motion == null) throw new ArgumentNullException(nameof(motion));
            if (polygons.Length < count) throw new InvalidOperationException($"{nameof(polygons)} too small for given {nameof(count)}");
            if (motion.Length < count) throw new InvalidOperationException($"{nameof(motion)} too small for given {nameof(count)}");
            if (timeStep < 0) throw new ArgumentException("Must be positive", nameof(TimeSpan));

            var retIx = 0;
            var ret = ResultsBuffer;
            for (var i = 0; i < count; i++)
            {
                var p1 = polygons[i];
                var m1 = motion[i];
                for (var j = i + 1; j < count; j++)
                {
                    var p2 = polygons[j];
                    var m2 = motion[j];

                    if (m1.DeltaX.IsZero && m1.DeltaY.IsZero && m2.DeltaX.IsZero && m2.DeltaY.IsZero)
                    {
                        continue;
                    }

                    if (limitTime && !CouldCollide(p1, m1, p2, m2))
                    {
                        continue;
                    }

                    var offset = CenterPolygons(p1, p2, out var p1Centered, out var p2Centered);

                    // this is _raw_, i.e. very close to when they 
                    //    actually collide
                    // we can't actually use it, because the next
                    //    check will collide
                    var time = CollisionTime(p1Centered, m1, p2Centered, m2, ref TimeBuffer);
                    if (time == null) continue;

                    // round down to the nearest timeStep, which will tend to stop collisions
                    //    juuuusssssst short by design
                    var clippedTime = (time.Value / timeStep) * timeStep;
                    if (clippedTime == time)
                    {
                        clippedTime -= timeStep;
                    }

                    var ptCentered = DetermineCollisionPointInMotion(p1Centered, m1, p2Centered, m2, clippedTime);
                    var pt = new Point(ptCentered.X - offset.DeltaX, ptCentered.Y - offset.DeltaY);

                    if (retIx == ret.Length)
                    {
                        throw new InvalidOperationException($"Too many collisions to process");
                    }

                    ret[retIx] = new Collision(i, j, pt, clippedTime);
                    retIx++;
                }
            }

            var results = new CollisionResults(retIx, ret, this);
            ResultsBuffer = null;

            return results;
        }

        /// <summary>
        /// Creates two new polygons that have the same relative offset as p1 and p2, but are centered
        ///   closer to (0,0).
        ///   
        /// This is useful to prevent overflows and other irritating rounding errors.
        /// 
        /// Returns the offset, to return to the original coordinate system subtract the offset.
        /// </summary>
        internal static Vector CenterPolygons(ConvexPolygon p1, ConvexPolygon p2, out ConvexPolygon p1Centered, out ConvexPolygon p2Centered)
        {
            FixedPoint shiftX;
            if (p1.BoundingX.Abs() >= p2.BoundingX.Abs())
            {
                shiftX = -p1.BoundingX;
            }
            else
            {
                shiftX = -p2.BoundingX;
            }

            FixedPoint shiftY;
            if (p1.BoundingY.Abs() >= p2.BoundingY.Abs())
            {
                shiftY = -p1.BoundingY;
            }
            else
            {
                shiftY = -p2.BoundingY;
            }

            p1Centered = p1.Translate(shiftX, shiftY);
            p2Centered = p2.Translate(shiftX, shiftY);

            return new Vector(shiftX, shiftY);
        }

        /// <summary>
        /// Determines the point of collision between two moving polygons that we "know" 
        ///    occurred at the given time.
        ///    
        /// We've already made the assumption that a collision occurred, this method
        ///   just determines where that collision "happened".
        /// </summary>
        internal Point DetermineCollisionPointInMotion(ConvexPolygon p1, Vector v1, ConvexPolygon p2, Vector v2, FixedPoint collisionTime)
        {
            var pMotion1 = v1 * collisionTime;
            var pMotion2 = v2 * collisionTime;

            var p1Pos = p1.Translate(pMotion1.DeltaX, pMotion1.DeltaY);
            var p2Pos = p2.Translate(pMotion2.DeltaX, pMotion2.DeltaY);

            FirstPointBuffer.Clear();
            SecondPointBuffer.Clear();
            return DetermineCollisionPointStationary(p1Pos, p2Pos, ref FirstPointBuffer, ref SecondPointBuffer);
        }

        /// <summary>
        /// Determines the point between the two given polygons where a "collision occurred".
        /// 
        /// In other word
        ///   - it finds he closest point on one polygon to the other (both are checked)
        ///   - it draws a line from that point to the nearest line on the other polygon
        ///   - it takes the half way point on that line
        ///   
        /// We've already made the assumption that a collision occurred, this method
        ///   just determines where that collision "happened".
        /// </summary>
        internal static Point DetermineCollisionPointStationary(
            ConvexPolygon p1,
            ConvexPolygon p2,
            ref Buffer<Point> smallestCollidingPoint1,
            ref Buffer<Point> smallestCollidingPoint2)
        {
            FixedPoint? smallestDist = null;

            // go over the vertices in polygon 1
            for(var i = 0; i < p1.NumVertices; i++)
            {
                var p1Pt = p1.GetVertex(i);
                for(var j = 0; j < p2.NumLineSegments; j++)
                {
                    var seg = p2.GetLineSegment(j);
                    var res = DetermineClosestPoint(seg, p1Pt);
                    if (res == null) continue;

                    var (dist, pt) = res.Value;

                    UpdateSmallest(dist, p1Pt, pt, ref smallestCollidingPoint1, ref smallestCollidingPoint2);
                }
            }

            // go over the vertices in polygon 2
            for(var i = 0; i < p2.NumVertices; i++)
            {
                var p2Pt = p2.GetVertex(i);
                for(var j = 0; j < p1.NumLineSegments; j++)
                {
                    var seg = p1.GetLineSegment(j);
                    var res = DetermineClosestPoint(seg, p2Pt);
                    if (res == null) continue;

                    var (dist, pt) = res.Value;

                    UpdateSmallest(dist, p2Pt, pt, ref smallestCollidingPoint1, ref smallestCollidingPoint2);
                }
            }

            // if we're in this scenario, it means two _vertices_ are going to collide,
            //   not a vertex and a line
            // so we need to loop over all vertices and find the two closest ones
            if (smallestDist == null)
            {
                for(var i = 0; i < p1.NumVertices; i++)
                {
                    var v1 = p1.GetVertex(i);
                    for(var j = 0; j < p2.NumVertices; j++)
                    {
                        var v2 = p2.GetVertex(j);
                        var line = new Vector(v2.X - v1.X, v2.Y - v1.Y);

                        if (!line.TryMagnitude(out var dist)) continue;
                        
                        UpdateSmallest(dist, v1, v2, ref smallestCollidingPoint1, ref smallestCollidingPoint2);
                    }
                }
            }

            // let's just say the collision is half way between the vertex and the (point on the line | other vertex)
            var halfWay = smallestDist.Value / 2;

            var midPoint1_X = FixedPoint.Zero;
            var midPoint1_Y = FixedPoint.Zero;
            for (var i = 0; i < smallestCollidingPoint1.Count; i++)
            {
                var pt = smallestCollidingPoint1[i];
                midPoint1_X += pt.X;
                midPoint1_Y += pt.Y;
            }
            midPoint1_X /= smallestCollidingPoint1.Count;
            midPoint1_Y /= smallestCollidingPoint1.Count;

            var midPoint2_X = FixedPoint.Zero;
            var midPoint2_Y = FixedPoint.Zero;

            for (var i = 0; i < smallestCollidingPoint2.Count; i++)
            {
                var pt = smallestCollidingPoint2[i];
                midPoint2_X += pt.X;
                midPoint2_Y += pt.Y;
            }
            midPoint2_X /= smallestCollidingPoint2.Count;
            midPoint2_Y /= smallestCollidingPoint2.Count;

            var fromVertexToClosestPoint = new Vector(midPoint2_X - midPoint1_X, midPoint2_Y - midPoint1_Y);

            FixedPoint fromVertexToClosestPointMag = default;

            // if we get _veeery_ close we need to handle this
            while (!fromVertexToClosestPoint.TryMagnitude(out fromVertexToClosestPointMag))
            {
                fromVertexToClosestPoint = new Vector(fromVertexToClosestPoint.DeltaX / 2, fromVertexToClosestPoint.DeltaY / 2);
            }

            if (fromVertexToClosestPointMag.IsZero)
            {
                fromVertexToClosestPoint = Vector.Zero;
            }
            else
            {
                fromVertexToClosestPoint = fromVertexToClosestPoint.Normalize();
            }

            var midX = midPoint1_X + fromVertexToClosestPoint.DeltaX * halfWay;
            var midY = midPoint1_Y + fromVertexToClosestPoint.DeltaY * halfWay;

            var midPt = new Point(midX, midY);
            return midPt;


            // Updates smallestCollidingPoint1 & smallestCollidingPoint2 so that at they always
            //   contain the points for the smallest distance seen yet
            void UpdateSmallest(
                FixedPoint distance,
                Point collidingPoint1,
                Point collidingPoint2,
                ref Buffer<Point> points1,
                ref Buffer<Point> points2)
            {
                if (smallestDist == null || distance < smallestDist)
                {
                    points1.Clear();
                    points2.Clear();

                    smallestDist = distance;

                    points1.Add(collidingPoint1);
                    points2.Add(collidingPoint2);
                    return;
                }

                if (distance > smallestDist) return;

                if (distance == smallestDist)
                {
                    points1.Add(collidingPoint1);
                    points2.Add(collidingPoint2);
                }
            }
        }

        /// <summary>
        /// Determines the distance between the given line segement and the given point (if any), and the point on the _line_
        ///   that is closest to the given point.
        /// </summary>
        internal static (FixedPoint Distance, Point Point)? DetermineClosestPoint(LineSegment2D line, Point point)
        {
            // based on: http://paulbourke.net/geometry/pointlineplane/

            // u = ((x_3 - x_1)(x_2 - x_1) + (y_3 - y_1)(y_2 - y_1)) / (|(p2 - p1)|^2)

            var diff = new Vector(line.P2.X - line.P1.X, line.P2.Y - line.P1.Y);
            if (diff.DeltaX * diff.DeltaX + diff.DeltaY * diff.DeltaY < 0) return null;

            if (!diff.TryMagnitude(out var diffMag)) return null;
            if (diffMag.IsZero) return null;

            var diffMagSq = diffMag * diffMag;

            var num = (point.X - line.P1.X) * (line.P2.X - line.P1.X) + (point.Y - line.P1.Y) * (line.P2.Y - line.P1.Y);

            var u = num / diffMagSq;

            // not that close to the line
            if (u < 0 || u > 1) return null;

            var step = u * diff;

            var ptOnLine = new Point(line.P1.X + step.DeltaX, line.P1.Y + step.DeltaY);

            var diffX = point.X - ptOnLine.X;
            var diffY = point.Y - ptOnLine.Y;

            var mag = (diffX * diffX + diffY * diffY);
            if (mag < 0) return null;

            var dist = mag.Sqrt();

            return (dist, ptOnLine);
        }

        /// <summary>
        /// Determine the earliest time, if any, that the two given polygons (both in motion) will collide.
        /// </summary>
        internal static FixedPoint? CollisionTime(ConvexPolygon p1, Vector v1, ConvexPolygon p2, Vector v2, ref Buffer<FixedPoint> timeBuffer)
        {
            // treat p1 as stationary, so adjust p2's speed accordingly
            var v2New = v2 - v1;

            // enumerate the normals we need to check
            var normalsToCheck = ConvexPolygon.GetUniqueNormals(p1, p2);

            timeBuffer.Clear();

            // determine all the times that any given axis will become 
            //   overlapping, those are all the times we need to check
            foreach (var normal in normalsToCheck)
            {
                var t = DetermineCollisionTime(p1, p2, v2New, normal);
                if (t == null) continue;

                timeBuffer.Add(t.Value);
            }

            if (timeBuffer.Count == 0) return null;

            return DetermineEarliestTrueCollisionTime(ref timeBuffer, p1, v1, p2, v2);
        }

        /// <summary>
        /// Given a set of times and two polygons in motion, determines which (if
        ///   any) of those times is the earliest "true" collision.
        /// </summary>
        private static FixedPoint? DetermineEarliestTrueCollisionTime(ref Buffer<FixedPoint> times, ConvexPolygon p1, Vector v1, ConvexPolygon p2, Vector v2)
        {
            times.Sort();

            for (var i = 0; i < times.Count; i++)
            {
                var t = times[i];

                // move the polygons to where they will be at time t
                var fixedP1 = p1.Translate(v1.DeltaX * t, v1.DeltaY * t);
                var fixedP2 = p2.Translate(v2.DeltaX * t, v2.DeltaY * t);

                // build up the unique normals of the polygons
                var finalNorms = ConvexPolygon.GetUniqueNormals(p1, p2);

                // project, if _any_ normal isn't overlapping then
                //   the polygons are not colliding and we can move on
                //   to the next time
                var collides = true;
                foreach (var norm in finalNorms)
                {
                    var p1OnNorm = fixedP1.ProjectOnto(norm);
                    var p2OnNorm = fixedP2.ProjectOnto(norm);

                    if (!p1OnNorm.Overlaps(p2OnNorm, Tolerance))
                    {
                        collides = false;
                        break;
                    }
                }

                if (collides)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Return the earliest time, if any, that the two given polygons will intersect _on the given axis_ 
        ///   with p2 moving in the direction of p2Motion.
        ///   
        /// P1 is "fixed" and will not move.
        /// </summary>
        internal static FixedPoint? DetermineCollisionTime(ConvexPolygon p1, ConvexPolygon p2, Vector p2Motion, Vector onAxis)
        {
            var motionOnAxis = p2Motion.Dot(onAxis);

            // no motion means no collision
            if (motionOnAxis.IsZero) return null;

            var p1OnAxis = p1.ProjectOnto(onAxis);
            var p2OnAxis = p2.ProjectOnto(onAxis);

            return DetermineCollisionTime(p1OnAxis, p2OnAxis, motionOnAxis);
        }

        /// <summary>
        /// Return the first time, if any, that two line segements will intersect
        /// </summary>
        internal static FixedPoint? DetermineCollisionTime(LineSegment1D p1OnAxis, LineSegment1D p2OnAxis, FixedPoint p2MotionOnAxis)
        {
            // if p2 is left of p1
            //   if p2 is moving left, no collision
            //   if p2 is moving right, collision is at (p1.Start - p2.Stop) / motionOnAxis
            // if p2 is right of p1
            //   if p2 is moving right, no collision
            //   if p2 is moving left, collision is at (p2.Start - p1.Stop) / motionOnAxis

            var p2LeftOfP1 = p2OnAxis.Stop < p1OnAxis.Start;
            var p2RightOfP1 = p2OnAxis.Start > p1OnAxis.Stop;
            var overlapping = !(p2LeftOfP1 || p2RightOfP1);

            // don't consider already overlapping things colliding
            if (overlapping) return null;

            var p2MovingLeft = p2MotionOnAxis < FixedPoint.Zero;
            var p2MovingRight = !p2MovingLeft;

            if (p2LeftOfP1)
            {
                if (p2MovingLeft)
                {
                    return null;
                }

                var distance = p1OnAxis.Start - p2OnAxis.Stop;
                var t = distance / p2MotionOnAxis;            // distance is positive, and motion is positive (moving right)
                return t;
            }

            if (p2RightOfP1)
            {
                if (p2MovingRight)
                {
                    return null;
                }

                var distance = p2OnAxis.Start - p1OnAxis.Stop;
                var t = distance / (-p2MotionOnAxis);         // distance is positive, but motion is negative (moving left)
                return t;
            }

            throw new Exception("Shouldn't be possible");
        }

        /// <summary>
        /// Performs a quick check to see if it is even possible for two given objects to collide.
        /// </summary>
        private static bool CouldCollide(ConvexPolygon a, Vector aVec, ConvexPolygon b, Vector bVec)
        {
            var aBox = GetInMotionBoundingBox(a, aVec);
            var bBox = GetInMotionBoundingBox(b, bVec);

            // based on: https://stackoverflow.com/a/306332/80572

            var aLeftOfB = aBox.Right < bBox.Left;
            var aRightOfB = aBox.Left > bBox.Right;
            var aBelowB = aBox.Top < bBox.Bottom;
            var aAboveB = aBox.Bottom > bBox.Top;

            var overlapping = !(aLeftOfB || aRightOfB || aBelowB || aAboveB);

            return overlapping;
        }

        /// <summary>
        /// Returns a box that describes the entire area that this object could be found in given it's velocity and the elapsed time.
        /// 
        /// Box is in cartesian coordinates.
        /// </summary>
        private static (FixedPoint Left, FixedPoint Right, FixedPoint Top, FixedPoint Bottom) GetInMotionBoundingBox(
            ConvexPolygon a,
            Vector aVec
        )
        {
            var xMotion = aVec.DeltaX.Abs();
            var yMotion = aVec.DeltaY.Abs();

            var doubleXMotion = xMotion + xMotion;
            var doubleYMotion = yMotion + yMotion;

            var left = a.BoundingX - doubleXMotion - 1;
            var top = a.BoundingY + doubleYMotion + 1;
            var right = (a.BoundingX + a.BoundingWidth) + doubleXMotion + 1;
            var bottom = (a.BoundingY - a.BoundingHeight) - doubleYMotion - 1;

            return (left, right, top, bottom);
        }
    }
}