using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Utilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LearnMeAThing.Managers
{
    interface IHitMapManager
    {
        (int Width, int Height) Measure(AssetNames name);
        ConvexPolygonPattern[] GetFor(AssetNames name);
    }

    sealed class HitMapManager: IHitMapManager
    {
        private class PixelsComparer : System.Collections.Generic.IComparer<(int X, int Y, int Value)>
        {
            public static readonly PixelsComparer Instance = new PixelsComparer();

            private PixelsComparer() { }
            public int Compare((int X, int Y, int Value) x, (int X, int Y, int Value) y) => x.Value.CompareTo(y.Value);
        }

        public string HitMapPath { get; private set; }

        ConvexPolygonPattern[][] Patterns;
        (int Width, int Height)[] Dimensions;

        private readonly Buffer<Utilities.Point> PointScratchSpace1;
        private readonly Buffer<Utilities.Point> PointScratchSpace2;
        private readonly Buffer<Utilities.Point> PointScratchSpace3;

        public HitMapManager(string path, int maxVertices)
        {
            HitMapPath = path;
            PointScratchSpace1 = new Buffer<Utilities.Point>(maxVertices);
            PointScratchSpace2 = new Buffer<Utilities.Point>(maxVertices);
            PointScratchSpace3 = new Buffer<Utilities.Point>(maxVertices);
        }

        public ConvexPolygonPattern[] GetFor(AssetNames name) => Patterns[(int)name];
        public (int Width, int Height) Measure(AssetNames name) => Dimensions[(int)name];

        public void Initialize()
        {
            (Patterns, Dimensions) = LoadAllHitMaps(HitMapPath, PointScratchSpace1, PointScratchSpace2, PointScratchSpace3);
        }

        public void Reload()
        {
            (Patterns, Dimensions) = LoadAllHitMaps(HitMapPath, PointScratchSpace1, PointScratchSpace2, PointScratchSpace3);
        }

        private static (ConvexPolygonPattern[][] Patterns, (int Width, int Height)[] Dimensions) LoadAllHitMaps(string path, Buffer<Utilities.Point> scratch1, Buffer<Utilities.Point> scratch2, Buffer<Utilities.Point> scratch3)
        {
            var max = 0;
            foreach(AssetNames n in Enum.GetValues(typeof(AssetNames)))
            {
                var asInt = (int)n;
                if(asInt > max)
                {
                    max = asInt;
                }
            }

            var ret = new ConvexPolygonPattern[max + 1][];
            var dims = new (int Width, int Height)[max + 1];
            foreach(var file in Directory.EnumerateFiles(path))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!Enum.TryParse<AssetNames>(name, ignoreCase: true, result: out var parsedName)) continue;

                ConvexPolygonPattern[] poly = null;
                (int Width, int Height) dim = default;

                var ext = Path.GetExtension(file);
                if(".bmp".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                {
                    (poly, dim) = LoadHitMapFromBitmap(file, scratch1, scratch2, scratch3);
                }

                if(".png".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                {
                    (poly, dim) = LoadHitMapFromPNG(file, scratch1, scratch2, scratch3);
                }

                if (poly == null) continue;

                ret[(int)parsedName] = poly;
                dims[(int)parsedName] = dim;
            }

            return (ret, dims);
        }

        private static (ConvexPolygonPattern[] SubPolys, (int Width, int Height) Dimensions) LoadHitMapFromBitmap(string file, Buffer<Utilities.Point> scratch1, Buffer<Utilities.Point> scratch2, Buffer<Utilities.Point> scratch3) => LoadHitMapImpl(file, scratch1, scratch2, scratch3);
        private static (ConvexPolygonPattern[] SubPolys, (int Width, int Height) Dimensions) LoadHitMapFromPNG(string file, Buffer<Utilities.Point> scratch1, Buffer<Utilities.Point> scratch2, Buffer<Utilities.Point> scratch3) => LoadHitMapImpl(file, scratch1, scratch2, scratch3);

        private static (ConvexPolygonPattern[] SubPolys, (int Width, int Height) Dimensions) LoadHitMapImpl(
            string path, 
            Buffer<Utilities.Point> scratch1,
            Buffer<Utilities.Point> scratch2,
            Buffer<Utilities.Point> scratch3
        )
        {
            (int Width, int Height) dim;

            using (var img = (Bitmap)Image.FromFile(path))
            {
                dim = (img.Width, img.Height);

                var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                var toCopyInto = new int[img.Width * img.Height];
                Marshal.Copy(data.Scan0, toCopyInto, 0, toCopyInto.Length);

                img.UnlockBits(data);

                var diskPts = new (int X, int Y, int Value)[4];
                var diskPtsIx = 0;

                for(var i = 0; i < toCopyInto.Length; i++)
                {
                    var pix = toCopyInto[i];
                    var alpha = (pix & 0xFF_00_00_00) >> 24;

                    if (alpha == 0) continue;

                    var y = i / img.Width;
                    var x = i % img.Width;

                    var noAlpha = pix & 0x00_FF_FF_FF;

                    if(diskPtsIx == diskPts.Length)
                    {
                        Array.Resize(ref diskPts, diskPts.Length * 2);
                    }

                    diskPts[diskPtsIx] = (x, y, noAlpha);
                    diskPtsIx++;
                }

                if (diskPtsIx < 2) throw new InvalidOperationException($"Hitmap defined without enough points, minimum is 2");

                Array.Sort(diskPts, 0, diskPtsIx, PixelsComparer.Instance);
                
                var cartesianPts = new Utilities.Point[diskPtsIx];
                for(var i = 0; i < diskPtsIx; i++)
                {
                    var screenPt = diskPts[i];
                    var cartesianX = screenPt.X;
                    var cartesianY = img.Height - screenPt.Y;

                    cartesianPts[i] = new Utilities.Point(cartesianX, cartesianY);
                }

                var rawPolygon = new PolygonPattern(cartesianPts, img.Height);
                var unScaled = rawPolygon.DecomposeIntoConvexPolygons(scratch1, scratch2, scratch3);
                
                var ret = new ConvexPolygonPattern[unScaled.Length];
                for(var i = 0; i < unScaled.Length; i++)
                {
                    var unScaledPoly = unScaled[i];
                    var scaledPts = new Utilities.Point[unScaledPoly.Vertices.Length];
                    for(var j = 0; j < unScaledPoly.Vertices.Length; j++)
                    {
                        var unscaledPt = unScaledPoly.Vertices[j];
                        var scaledPt = 
                            new Utilities.Point(
                                unscaledPt.X * PositionComponent.SUBPIXELS_PER_PIXEL, 
                                unscaledPt.Y * PositionComponent.SUBPIXELS_PER_PIXEL
                        );
                        scaledPts[j] = scaledPt;
                    }

                    var scaledPoly = new ConvexPolygonPattern(scaledPts, unScaledPoly.OriginalHeight * PositionComponent.SUBPIXELS_PER_PIXEL);
                    ret[i] = scaledPoly;
                }
                
                return (ret, dim);
            }
        }
    }
}
