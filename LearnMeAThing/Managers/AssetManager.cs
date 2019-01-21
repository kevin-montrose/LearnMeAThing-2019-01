using LearnMeAThing.Assets;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LearnMeAThing.Managers
{
    /// <summary>
    /// Dumb custom pixel struct, just to abstract over everyhting else
    /// </summary>
    public struct Pixel
    {
        private int Backing;

        public byte Red => (byte)((Backing & 0x00_FF_00_00) >> 16);
        public byte Green => (byte)((Backing & 0x00_00_FF_00) >> 8);
        public byte Blue => (byte)((Backing & 0x00_00_00_FF) >> 0);
        public byte Alpha => (byte)((Backing & 0xFF_00_00_00) >> 24);

        public Pixel(int packed)
        {
            Backing = packed;
        }

        public Pixel(byte a, byte r, byte g, byte b)
        {
            Backing = (a << 24) | (r << 16) | (g << 8) | b;
        }
    }

    /// <summary>
    /// Handles loading and mapping raw assets.
    /// 
    /// The level of indirection allows us to dynamically reload things
    ///   without recompiling.
    /// </summary>
    sealed class AssetManager<TProcessed>: IAssetMeasurer
    {   
        public string AssetPath { get; private set; }

        private readonly Func<int[], ushort, ushort, TProcessed> Map;
        private readonly Action<TProcessed> Free;

        private (ushort Width, ushort Height)[] Dimensions;
        private TProcessed[] LoadedAssets;

        public AssetManager(string assetPath, Func<int[], ushort, ushort, TProcessed> map, Action<TProcessed> free)
        {
            if (!Directory.Exists(assetPath)) throw new InvalidOperationException($"Directory does not exist: {assetPath}");
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (free == null) throw new ArgumentNullException(nameof(free));

            AssetPath = assetPath;
            Map = map;
            Free = free;
        }

        /// <summary>
        /// Spin it up!
        /// </summary>
        public void Initialize()
        {
            var pixels = LoadAllFrom(AssetPath);
            (LoadedAssets, Dimensions) = MapAll(pixels.Pixels, pixels.Dimensions);
        }

        /// <summary>
        /// Reload assets
        /// </summary>
        public void Reload()
        {
            var old = LoadedAssets;
            var pixels = LoadAllFrom(AssetPath);
            (LoadedAssets, Dimensions) = MapAll(pixels.Pixels, pixels.Dimensions);
            
            for(var i = 0; i < old.Length; i++)
            {
                Free(old[i]);
            }
        }

        public TProcessed Get(AssetNames asset)
        {
            if (asset < 0 || ((int)asset) >= LoadedAssets.Length)
            {
                // glitch: this makes sense I guess?
                //   we could do something more fun
                asset = AssetNames.NONE;
            }

            return LoadedAssets[(int)asset];
        }

        public (int Width, int Height) Measure(AssetNames asset)
        {
            if(asset < 0 || ((int)asset) >= LoadedAssets.Length)
            {
                // glitch: symetry with Get
                asset = AssetNames.NONE;
            }

            return Dimensions[(int)asset];
        }

        /// <summary>
        /// Take raw pixels and turn them into "whatever" it is the
        ///    consumer needs.
        /// </summary>
        private (TProcessed[] Assets, (ushort Width, ushort Height)[] Dimensions) MapAll(int[][] pixels, (ushort Width, ushort Height)[] dimensions)
        {
            if (pixels[(int)AssetNames.NONE] == null)
            {
                throw new InvalidOperationException($"No asset loaded for {AssetNames.NONE}, which is bananas");
            }

            var data = new TProcessed[pixels.Length];

            // need to make a new dimensions list, so we can fill in any gaps
            var dims = new (ushort Width, ushort Height)[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixs = pixels[i];
                var dim = dimensions[i];
                if (pixs == null)
                {
                    // make sure _everything_ has a valid pixel map
                    pixs = pixels[(int)AssetNames.NONE];
                    dim = dimensions[(int)AssetNames.NONE];
                }

                data[i] = Map(pixs, dim.Width, dim.Height);
                dims[i] = dim;
            }

            return (data, dims);
        }
        
        private static (int[][] Pixels, (ushort Width, ushort Height)[] Dimensions) LoadAllFrom(string path)
        {
            AssetNames max = 0;
            foreach (AssetNames v in Enum.GetValues(typeof(AssetNames)))
            {
                if (v > max)
                {
                    max = v;
                }
            }

            var loaded = new int[(int)max + 1][];
            var dims = new (ushort Width, ushort Height)[loaded.Length];

            foreach (var file in Directory.EnumerateFiles(path))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var ext = Path.GetExtension(file);
                if (string.IsNullOrEmpty(ext)) continue;
                if (!Enum.TryParse<AssetNames>(name, ignoreCase: true, result: out var parsed)) continue;

                int[] pixels = null;
                ushort width = 0;
                ushort height = 0;
                if (ext.Equals(".bmp", StringComparison.InvariantCultureIgnoreCase))
                {
                    (pixels, width, height) = LoadPixelsBitmap(file);
                }

                if (ext.Equals(".png", StringComparison.InvariantCulture))
                {
                    (pixels, width, height) = LoadPixelsPNG(file);
                }

                if (pixels == null) continue;

                if (width == 0 || height == 0) throw new InvalidOperationException("Found invalid dimensions for an asset");

                loaded[(int)parsed] = pixels;
                dims[(int)parsed] = (width, height);
            }

            return (loaded, dims);
        }

        // separate impls in case we need to move of System.Drawing and do this "custom"
        private static (int[] Data, ushort Width, ushort Height) LoadPixelsBitmap(string path) => LoadPixelsImpl(path);
        private static (int[] Data, ushort Width, ushort Height) LoadPixelsPNG(string path) => LoadPixelsImpl(path);

        private static (int[] Data, ushort Width, ushort Height) LoadPixelsImpl(string path)
        {
            using (var img = (Bitmap)Image.FromFile(path))
            {
                var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                var toCopyInto = new int[img.Width * img.Height];
                Marshal.Copy(data.Scan0, toCopyInto, 0, toCopyInto.Length);

                img.UnlockBits(data);

                return (toCopyInto, (ushort)img.Width, (ushort)img.Height);
            }
        }
    }
}
