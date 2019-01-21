using LearnMeAThing.Assets;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace LearnMeAThing.Managers
{
    /// <summary>
    /// The bits of room management that are needed by the actual engine,
    ///    ignoring all the graphics-y parts.
    /// </summary>
    interface IRoomManager
    {
        void Initialize();
        RoomTemplate Get(RoomNames name);
        (int Width, int Height) Measure(RoomNames name);
    }

    /// <summary>
    /// Handles loading RoomTemplates, looking them up,
    ///   and mapping them to graphics-y bits (while hiding
    ///   those details from the rest of the engine).
    /// </summary>
    sealed class RoomManager<TProcessed> : IRoomManager
    {
        private readonly string _RoomPath;
        public string RoomPath => _RoomPath;

        private RoomTemplate[] Templates;
        private readonly Func<RoomTemplate, TProcessed> Map;
        private readonly Action<TProcessed> Free;

        private TProcessed[] MappedRooms;

        public RoomManager(string roomPath, Func<RoomTemplate, TProcessed> map, Action<TProcessed> free)
        {
            _RoomPath = roomPath;
            Map = map;
            Free = free;
        }

        /// <summary>
        /// Returns the template for the given room.
        /// </summary>
        public RoomTemplate Get(RoomNames name) => Templates[(int)name];
        
        /// <summary>
        /// Determins the size of the given room, in pixels.
        /// </summary>
        public (int Width, int Height) Measure(RoomNames name)
        {
            var template = Get(name);
            return (template.WidthInTiles * RoomTemplate.TILE_WIDTH_PIXELS, template.HeightInTiles * RoomTemplate.TILE_HEIGHT_PIXELS);
        }

        /// <summary>
        /// Gets the "background" for the given room.
        /// 
        /// Exactly what constitutes a background is handled by whatever mapping function
        ///   was passed to the RoomManager constructor.
        /// </summary>
        public TProcessed GetBackground(RoomNames name) => MappedRooms[(int)name];

        /// <summary>
        /// Spin it up!
        /// </summary>
        public void Initialize()
        {
            Templates = LoadTemplates(RoomPath);
            MappedRooms = new TProcessed[Templates.Length];
            for (var i = 0; i < Templates.Length; i++)
            {
                MappedRooms[i] = Map(Templates[i]);
            }
        }

        /// <summary>
        /// Force a reload of all RoomTemplates, and freeing all mapped
        ///   "backgrounds" from the old templates.
        /// </summary>
        public void Reload()
        {
            var oldMapped = MappedRooms;

            // load up the new stuff from disk
            var newTemplates = LoadTemplates(RoomPath);
            var newMapped = new TProcessed[newTemplates.Length];

            for (var i = 0; i < newTemplates.Length; i++)
            {
                newMapped[i] = Map(newTemplates[i]);
            }

            // swap over
            Templates = newTemplates;
            MappedRooms = newMapped;

            // free all the old stuff
            for (var i = 0; i < oldMapped.Length; i++)
            {
                Free(oldMapped[i]);
            }
        }

        private static RoomTemplate[] LoadTemplates(string folder)
        {
            int maxRooms = 0;
            foreach (RoomNames room in Enum.GetValues(typeof(RoomNames)))
            {
                var asInt = (int)room;
                if (asInt > maxRooms)
                {
                    maxRooms = asInt;
                }
            }

            var tileMaps = LoadTileMaps(folder);
            var rts = new RoomTemplate[maxRooms + 1];

            foreach (var file in Directory.EnumerateFiles(folder, "*.tmx"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!Enum.TryParse<RoomNames>(name, ignoreCase: true, result: out var parsedName)) continue;

                var template = LoadTemplate(tileMaps, parsedName, file);

                rts[(int)parsedName] = template;
            }

            return rts;
        }

        private static TileMap[] LoadTileMaps(string folder)
        {
            var ix = 0;
            var ret = new TileMap[4];
            foreach (var file in Directory.EnumerateFiles(folder, "*.tsx"))
            {
                if (ret.Length == ix)
                {
                    Array.Resize(ref ret, ret.Length * 2);
                }

                var map = LoadTileMap(file);
                ret[ix] = map;
                ix++;
            }

            Array.Resize(ref ret, ix);

            return ret;
        }

        private static TileMap LoadTileMap(string file)
        {
            var name = Path.GetFileNameWithoutExtension(file);

            var maxIx = -1;
            var tiles = new AssetNames[32];

            using (var fs = File.OpenRead(file))
            using (var xml = XmlReader.Create(fs))
            {
                while (xml.Read())
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xml.Name == "tile")
                            {
                                var el = (XElement)XElement.ReadFrom(xml);
                                ReadTile(el);
                            }
                            break;
                    }
                }
            }

            Array.Resize(ref tiles, maxIx + 1);

            return new TileMap(name, tiles);

            // Reads a tile element and adds it's details to tiles (and updates maxIx)
            void ReadTile(XElement tile)
            {
                var id = tile.Attribute("id")?.Value;
                if (!int.TryParse(id, out var ix)) throw new InvalidOperationException($"Could not parse tile id, expected positive integer, found: {id}");

                XElement imageEl = null;
                foreach (var e in tile.Nodes())
                {
                    if (e.NodeType != XmlNodeType.Element) continue;

                    var maybeImageEl = (XElement)e;
                    if (maybeImageEl.Name == "image")
                    {
                        imageEl = maybeImageEl;
                        break;
                    }
                }

                if (imageEl == null) throw new InvalidOperationException("Could not find image node of tile");

                var assetPath = imageEl.Attribute("source")?.Value;
                if (string.IsNullOrEmpty(assetPath)) throw new InvalidOperationException("No source found on tile image");

                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (!Enum.TryParse<AssetNames>(assetName, ignoreCase: true, result: out var parsedAssetName))
                {
                    throw new InvalidOperationException($"Could not create asset name from: {assetName}");
                }

                while (tiles.Length <= ix)
                {
                    Array.Resize(ref tiles, tiles.Length * 2);
                }

                tiles[ix] = parsedAssetName;
                maxIx = Math.Max(maxIx, ix);
            }
        }

        private static RoomTemplate LoadTemplate(TileMap[] tileMaps, RoomNames name, string file)
        {
            var (w, h, m, b, oof, le, re, te, be) = LoadTemplateDetails(file);

            if (b.Length != (w * h)) throw new InvalidOperationException("Incorrect number of tiles specified for room size");

            var tiles = new Tile[b.Length];
            for (ushort y = 0; y < h; y++)
            {
                for (ushort x = 0; x < w; x++)
                {
                    var ix = y * w + x;
                    tiles[ix] = new Tile(x, y, b[ix]);
                }
            }

            TileMap? map = null;
            for (var i = 0; i < tileMaps.Length; i++)
            {
                var tm = tileMaps[i];
                if (tm.Name == m)
                {
                    map = tm;
                    break;
                }
            }

            if (map == null) throw new InvalidOperationException($"Could not find tilemap: {m}");

            return new RoomTemplate(name, null, null, le, re, te, be, w, h, map.Value, tiles, oof);
        }

        private static 
            (
                ushort Width, 
                ushort Height, 
                string TileMap, 
                int[] BackgroundTiles, 
                RoomObject[] ObjectsOnFloor, 
                RoomNames? left, 
                RoomNames? right,
                RoomNames? top,
                RoomNames? bottom
            ) 
            LoadTemplateDetails(string file)
        {
            string orientation, renderorder, width, height;
            orientation = renderorder = width = height = null;

            string tileSetName = null;

            int[] backgroundTiles = null;
            RoomObjectProperty[] backgroundProps = null;
            RoomObject[] objectsOnFloor = null;

            using (var fs = File.OpenRead(file))
            using (var xml = XmlReader.Create(fs))
            {
                while (xml.Read())
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xml.Name == "map")
                            {
                                ReadMap(xml);
                                continue;
                            }

                            if (xml.Name == "tileset")
                            {
                                var el = (XElement)XElement.ReadFrom(xml);
                                ReadTileSet(el);
                                continue;
                            }

                            if (xml.Name == "layer")
                            {
                                var el = (XElement)XElement.ReadFrom(xml);
                                var layerName = el.Attribute("name")?.Value;
                                if ("background".Equals(layerName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (backgroundTiles != null) throw new InvalidOperationException("Multiple background layers defined");
                                    (backgroundTiles, backgroundProps) = ReadLayer(el);
                                    if (backgroundTiles == null) throw new InvalidOperationException("Couldn't load background layer");

                                    continue;
                                }

                                // todo: other layers

                                continue;
                            }

                            if (xml.Name == "objectgroup")
                            {
                                var el = (XElement)XElement.ReadFrom(xml);
                                var groupName = el.Attribute("name")?.Value;
                                if ("onfloor".Equals(groupName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (objectsOnFloor != null) throw new InvalidOperationException("Multiple onfloor object layers defined");
                                    objectsOnFloor = ReadObjectLayer(el);
                                    if (objectsOnFloor == null) throw new InvalidOperationException("Couldn't load onfloor layer");
                                }
                            }

                            break;
                    }
                }
            }

            if (backgroundTiles == null) throw new InvalidOperationException("No background found");
            if (tileSetName == null) throw new InvalidOperationException("No tileset found");

            RoomNames? lExit, rExit, tExit, bExit;
            lExit = rExit = tExit = bExit = null;

            if (backgroundProps != null)
            {
                foreach (var prop in backgroundProps)
                {
                    if (prop.Name.Equals("BottomExit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!Enum.TryParse<RoomNames>(prop.Value, ignoreCase: true, out var parsedRoom)) throw new InvalidOperationException($"Couldn't parse room name: {prop.Value}");
                        if (bExit != null) throw new InvalidOperationException("Multiple bottom exits listed");
                        bExit = parsedRoom;
                        continue;
                    }

                    if (prop.Name.Equals("TopExit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!Enum.TryParse<RoomNames>(prop.Value, ignoreCase: true, out var parsedRoom)) throw new InvalidOperationException($"Couldn't parse room name: {prop.Value}");
                        if (tExit != null) throw new InvalidOperationException("Multiple top exits listed");
                        tExit = parsedRoom;
                        continue;
                    }

                    if (prop.Name.Equals("LeftExit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!Enum.TryParse<RoomNames>(prop.Value, ignoreCase: true, out var parsedRoom)) throw new InvalidOperationException($"Couldn't parse room name: {prop.Value}");
                        if (lExit != null) throw new InvalidOperationException("Multiple left exits listed");
                        lExit = parsedRoom;
                        continue;
                    }

                    if (prop.Name.Equals("RightExit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!Enum.TryParse<RoomNames>(prop.Value, ignoreCase: true, out var parsedRoom)) throw new InvalidOperationException($"Couldn't parse room name: {prop.Value}");
                        if (rExit != null) throw new InvalidOperationException("Multiple right exits listed");
                        rExit = parsedRoom;
                        continue;
                    }
                }
            }

            return (ushort.Parse(width), ushort.Parse(height), tileSetName, backgroundTiles, objectsOnFloor, lExit, rExit, tExit, bExit);

            // reads an object layer, producing a bunch of objects
            RoomObject[] ReadObjectLayer(XElement el)
            {
                var ret = new RoomObject[4];
                var retIx = 0;
                foreach (var n in el.Nodes())
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        var maybeObject = (XElement)n;
                        if (maybeObject.Name == "object")
                        {
                            if (retIx == ret.Length)
                            {
                                Array.Resize(ref ret, ret.Length * 2);
                            }

                            var obj = ReadObject(maybeObject);
                            ret[retIx] = obj;
                            retIx++;
                        }
                    }
                }

                Array.Resize(ref ret, retIx);
                return ret;
            }

            // reads an object from the xml
            RoomObject ReadObject(XElement obj)
            {
                var name = obj.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name)) throw new InvalidOperationException($"Couldn't load {nameof(name)} for object");
                var x = obj.Attribute("x")?.Value;
                if (string.IsNullOrEmpty(x)) throw new InvalidOperationException($"Couldn't load {nameof(x)} for object");
                var y = obj.Attribute("y")?.Value;
                if (string.IsNullOrEmpty(y)) throw new InvalidOperationException($"Couldn't load {nameof(y)} for object");

                RoomObjectProperty[] properties = null;

                foreach (var n in obj.Nodes())
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        var maybeProperties = (XElement)n;
                        if (maybeProperties.Name == "properties")
                        {
                            properties = ReadObjectProperties(maybeProperties);
                            break;
                        }
                    }
                }

                if (properties == null) throw new InvalidOperationException("Could not load properties for object");

                if (!int.TryParse(x, out var parsedX)) throw new InvalidOperationException($"Couldn't parse object {nameof(x)}, expected integer, found: {x}");
                if (!int.TryParse(y, out var parsedY)) throw new InvalidOperationException($"Couldn't parse object {nameof(y)}, expected integer, found: {y}");

                RoomObjectTypes? type = null;
                for (var i = 0; i < properties.Length; i++)
                {
                    var p = properties[i];
                    if ("objecttype".Equals(p.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var typeStr = p.Value;
                        if (!Enum.TryParse<RoomObjectTypes>(typeStr, ignoreCase: true, out var typeParsed)) throw new InvalidOperationException($"Couldn't parse object, found: {typeStr}");

                        type = typeParsed;
                        break;
                    }
                }

                if (type == null) throw new InvalidOperationException("Couldn't determine type of object");

                return new RoomObject(type.Value, parsedX, parsedY, properties);
            }

            // read properties out of xml
            RoomObjectProperty[] ReadObjectProperties(XElement el)
            {
                var ret = new RoomObjectProperty[4];
                var retIx = 0;
                foreach (var n in el.Nodes())
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        var maybeProperty = (XElement)n;
                        if (maybeProperty.Name == "property")
                        {
                            var name = maybeProperty.Attribute("name")?.Value;
                            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException($"Could not determine {nameof(name)} of property");
                            var value = maybeProperty.Attribute("value")?.Value;
                            if (string.IsNullOrEmpty(value)) throw new InvalidOperationException($"Could not determine {nameof(value)} of property");

                            if (ret.Length == retIx)
                            {
                                Array.Resize(ref ret, ret.Length * 2);
                            }

                            // todo: maybe dedupe name & value?
                            //       would keep the object graph smaller
                            ret[retIx] = new RoomObjectProperty(name, value);
                            retIx++;
                        }
                    }
                }

                Array.Resize(ref ret, retIx);
                return ret;
            }

            // reads a tileset element, updating tileSetName
            void ReadTileSet(XElement el)
            {
                if (!string.IsNullOrEmpty(tileSetName)) throw new InvalidOperationException("tileset has already been read");

                var tilesetFileName = el.Attribute("source")?.Value;
                if (tilesetFileName == null) throw new InvalidOperationException("Could not find source on tileset");

                tileSetName = Path.GetFileNameWithoutExtension(tilesetFileName);
                if (string.IsNullOrEmpty(tileSetName)) throw new InvalidOperationException("Could not extract tileset name");
            }

            // reads a layer element into a Tile[]
            (int[] Tiles, RoomObjectProperty[] Properties) ReadLayer(XElement el)
            {
                XElement dataEl = null;
                XElement propEl = null;
                foreach (var n in el.Nodes())
                {
                    if (n.NodeType != XmlNodeType.Element) continue;

                    var maybeDataElement = (XElement)n;
                    if (maybeDataElement.Name == "data")
                    {
                        dataEl = maybeDataElement;
                        continue;
                    }

                    if(maybeDataElement.Name == "properties")
                    {
                        propEl = maybeDataElement;
                        continue;
                    }
                }

                var props = propEl != null ? ReadObjectProperties(propEl) : null;

                if (dataEl == null)
                {
                    throw new InvalidOperationException("Couldn't find data element for layer");
                }

                if (dataEl.Attribute("encoding")?.Value != "csv")
                {
                    throw new InvalidOperationException("Unexpected encoding for layer, only CSV is supported");
                }

                var content = dataEl.Value.Replace("\r", "").Replace("\n", "");
                var parts = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var ret = new int[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    var p = parts[i];
                    if (!int.TryParse(p, out var pTile))
                    {
                        throw new InvalidOperationException($"Unexpected tile reference: {p}");
                    }

                    ret[i] = pTile - 1; // tileset map is 0 based, but layer is 1 based
                }

                return (ret, props);
            }

            // Reads the metadata needed off a map element
            //   and sets the appropriate variables in the outer scope
            void ReadMap(XmlReader el)
            {
                if (orientation != null || renderorder != null || width != null || height != null)
                {
                    throw new InvalidOperationException($"map element already processed");
                }

                orientation = el.GetAttribute(nameof(orientation));
                renderorder = el.GetAttribute(nameof(renderorder));
                width = el.GetAttribute(nameof(width));
                height = el.GetAttribute(nameof(height));

                if (string.IsNullOrEmpty(orientation) || string.IsNullOrEmpty(renderorder) || string.IsNullOrEmpty(width) || string.IsNullOrEmpty(height))
                {
                    throw new InvalidOperationException($"map element is missing required attributes");
                }

                if (orientation != "orthogonal") throw new InvalidOperationException($"Unexpected {nameof(orientation)}, only orthogonal is supported, found: {orientation}");
                if (renderorder != "right-down") throw new InvalidOperationException($"Unexpected {nameof(renderorder)}, only right-down is supported, found: {renderorder}");

                if (!ushort.TryParse(width, out var _)) throw new InvalidOperationException($"Unexpected {nameof(width)}, expected an integer, found: {width}");
                if (!ushort.TryParse(height, out var __)) throw new InvalidOperationException($"Unexpected {nameof(height)}, expected an integer, found: {height}");
            }
        }
    }
}