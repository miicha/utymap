using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Utils;
using UtyRx;

namespace UtyMap.Unity.Data
{
    /// <summary> Adapts map tile data received from utymap API to the type used by the app. </summary>
    /// <remarks>
    ///     It has to be static as neither il2cpp nor mono AOT support marshaling delegates that point to 
    ///     instance methods to native code. A tag is used to match mapdata to specific tile object.
    ///     TODO refactor this class to reduce complexity and uglyness
    /// </remarks>
    internal static class MapDataAdapter
    {
        private const int VertexLimit = 64998;
        private const string TraceCategory = "mapdata.loader";
        private static readonly Regex ElementNameRegex = new Regex("^(building):([0-9]*)");

        private static readonly HashSet<IObserver<MapData>> Observers = new HashSet<IObserver<MapData>>();
        private static readonly SafeDictionary<int, Tile> Tiles = new SafeDictionary<int, Tile>();

        private static ITrace _trace = new DefaultTrace();

        public static void UseTrace(ITrace trace)
        {
            _trace = trace;
        }

        public static bool Add(IObserver<MapData> observer)
        {
            return Observers.Add(observer);
        }

        public static bool Remove(IObserver<MapData> observer)
        {
            return Observers.Remove(observer);
        }

        public static bool Add(Tile tile)
        {
            return Tiles.TryAdd(tile.GetHashCode(), tile);
        }

        public static bool Remove(Tile tile)
        {
            return Tiles.TryRemove(tile.GetHashCode());
        }

        public static void Clear()
        {
            Observers.Clear();
            Tiles.Clear();
        }

        /// <summary> Adapts mesh data received from utymap. </summary>
        public static void AdaptMesh(int tag, string name, double[] vertices, int vertexCount,
            int[] triangles, int triangleCount, int[] colors, int colorCount,
            double[] uvs, int uvCount, int[] uvMap, int uvMapCount)
        {
            Tile tile;
            if (!Tiles.TryGetValue(tag, out tile) || tile.IsDisposed)
                return;

            Vector3[] worldPoints;
            Color[] unityColors;

            Vector2[] unityUvs;
            Vector2[] unityUvs2;
            Vector2[] unityUvs3;

            // NOTE process terrain differently to emulate flat shading effect by avoiding 
            // triangles to share the same vertex. Remove "if" branch if you don't need it
            if (name.Contains("terrain"))
            {
                worldPoints = new Vector3[triangleCount];
                unityColors = new Color[triangleCount];

                unityUvs = new Vector2[triangleCount];
                unityUvs2 = new Vector2[triangleCount];
                unityUvs3 = new Vector2[triangleCount];

                var textureMapper = CreateTextureAtlasMapper(unityUvs, unityUvs2, unityUvs3, uvs, uvMap);

                for (int i = 0; i < triangles.Length; ++i)
                {
                    int vertIndex = triangles[i] * 3;
                    worldPoints[i] = tile.Projection
                        .Project(new GeoCoordinate(vertices[vertIndex + 1], vertices[vertIndex]), vertices[vertIndex + 2]);

                    unityColors[i] = ColorUtils.FromInt(colors[triangles[i]]);
                    textureMapper.SetUvs(i, triangles[i] * 2);
                    triangles[i] = i;
                }
            }
            else
            {
                long id;
                if (!ShouldLoad(tile, name, out id))
                    return;

                worldPoints = new Vector3[vertexCount / 3];
                for (int i = 0; i < vertices.Length; i += 3)
                    worldPoints[i / 3] = tile.Projection
                        .Project(new GeoCoordinate(vertices[i + 1], vertices[i]), vertices[i + 2]);

                unityColors = new Color[colorCount];
                for (int i = 0; i < colorCount; ++i)
                    unityColors[i] = ColorUtils.FromInt(colors[i]);

                if (uvCount > 0)
                {
                    unityUvs = new Vector2[uvCount/2];
                    unityUvs2 = new Vector2[uvCount/2];
                    unityUvs3 = new Vector2[uvCount/2];

                    var textureMapper = CreateTextureAtlasMapper(unityUvs, unityUvs2, unityUvs3, uvs, uvMap);
                    for (int i = 0; i < uvCount; i += 2)
                    {
                        unityUvs[i/2] = new Vector2((float) uvs[i], (float) uvs[i + 1]);
                        textureMapper.SetUvs(i/2, i);
                    }
                }
                else
                {
                    unityUvs = new Vector2[worldPoints.Length];
                    unityUvs2 = new Vector2[worldPoints.Length];
                    unityUvs3 = new Vector2[worldPoints.Length];
                }

                tile.Register(id);
            }

            BuildMesh(tile, name, worldPoints, triangles, unityColors, unityUvs, unityUvs2, unityUvs3);
        }

        /// <summary> Adapts element data received from utymap. </summary>
        public static void AdaptElement(int tag, long id, string[] tags, int tagCount, double[] vertices, int vertexCount, 
            string[] styles, int styleCount)
        {
            Tile tile;
            if (!Tiles.TryGetValue(tag, out tile) || tile.IsDisposed)
                return;

            var geometry = new GeoCoordinate[vertexCount / 3];
            var heights = new double[vertexCount/3];
            for (int i = 0; i < vertexCount; i += 3)
            {
                geometry[i/3] = new GeoCoordinate(vertices[i + 1], vertices[i]);
                heights[i / 3] = vertices[i + 2];
            }

            Element element = new Element(id, geometry, heights, ReadDict(tags), ReadDict(styles));
            NotifyObservers(new MapData(tile, new Union<Element, Mesh>(element)));
        }

        /// <summary> Adapts error message </summary>
        public static void AdaptError(string message)
        {
            var exception = new MapDataException(message);
            _trace.Error(TraceCategory, exception, "cannot adapt mapdata.");
            NotifyObservers(exception);
        }

        #region Private members

        private static Dictionary<string, string> ReadDict(string[] data)
        {
            var map = new Dictionary<string, string>(data.Length / 2);
            for (int i = 0; i < data.Length; i += 2)
                map.Add(data[i], data[i + 1]);
            return map;
        }

        private static bool ShouldLoad(Tile tile, string name, out long id)
        {
            var match = ElementNameRegex.Match(name);
            if (!match.Success)
            {
                id = 0;
                return true;
            }

            id = long.Parse(match.Groups[2].Value);
            return id == 0 || !tile.Has(id);
        }

        private static TextureAtlasMapper CreateTextureAtlasMapper(Vector2[] unityUvs, Vector2[] unityUvs2, Vector2[] unityUvs3,
                double[] uvs, int[] uvMap)
        {
            const int infoEntrySize = 8;
            var count = uvMap == null ? 0 : uvMap.Length;
            List<TextureAtlasInfo> infos = new List<TextureAtlasInfo>(count / infoEntrySize);
            for (int i = 0; i < count; )
            {
                var info = new TextureAtlasInfo();
                info.UvIndexRange = new Range<int>(!infos.Any() ? 0 : infos.Last().UvIndexRange.Maximum, uvMap[i++]);
                info.TextureIndex = uvMap[i++];

                int atlasWidth = uvMap[i++];
                int atlasHeight = uvMap[i++];
                float x = uvMap[i++];
                float y = uvMap[i++];
                float width = uvMap[i++];
                float height = uvMap[i++];

                bool isEmpty = atlasWidth == 0 || atlasHeight == 0;
                info.TextureSize = new Vector2(isEmpty ? 0 : width / atlasWidth, isEmpty ? 0 : height / atlasHeight);
                info.TextureOffset = new Vector2(isEmpty ? 0 : x / atlasWidth, isEmpty ? 0 : y / atlasHeight);

                infos.Add(info);
            }

            return new TextureAtlasMapper(unityUvs, unityUvs2, unityUvs3, uvs, infos);
        }

        /// <summary> Builds mesh object and notifies observers. </summary>
        /// <remarks> Unity has vertex count limit and spliiting meshes here is quite expensive operation. </remarks>
        private static void BuildMesh(Tile tile, string name, Vector3[] worldPoints, int[] triangles, Color[] unityColors, 
            Vector2[] unityUvs, Vector2[] unityUvs2, Vector2[] unityUvs3)
        {
            if (worldPoints.Length < VertexLimit)
            {
                Mesh mesh = new Mesh(name, 0, worldPoints, triangles, unityColors, unityUvs, unityUvs2, unityUvs3);
               NotifyObservers(new MapData(tile, new Union<Element, Mesh>(mesh)));
                return;
            }

            _trace.Warn(TraceCategory, "Mesh '{0}' has more vertices than allowed by Unity: {1}. Will try to split..",
                   name, worldPoints.Length.ToString());
            if (worldPoints.Length != triangles.Length)
            {
                // TODO handle this case properly
                _trace.Warn(TraceCategory, "Cannot split mesh {0}: vertecies count != triangles count", name);
                return;
            }

            int parts = (int) Math.Ceiling((float) worldPoints.Length / VertexLimit);
            for (int i = 0; i < parts; ++i)
            {
                var start = i * VertexLimit;
                var end = Math.Min(start + VertexLimit, worldPoints.Length);
                Mesh mesh = new Mesh(name + i, 0, 
                    worldPoints.Skip(start).Take(end - start).ToArray(),
                    i == 0
                        ? triangles.Skip(start).Take(end - start).ToArray()
                        : triangles.Skip(start).Take(end - start).Select(tri => tri - start).ToArray(),
                    unityColors.Skip(start).Take(end - start).ToArray(),
                    unityUvs.Skip(start).Take(end - start).ToArray(),
                    unityUvs2.Skip(start).Take(end - start).ToArray(),
                    unityUvs3.Skip(start).Take(end - start).ToArray());
                NotifyObservers(new MapData(tile, new Union<Element, Mesh>(mesh)));
            }
        }

        private static void NotifyObservers(MapData mapData)
        {
            foreach (var observer in Observers)
                observer.OnNext(mapData);
        }

        private static void NotifyObservers(Exception ex)
        {
            foreach (var observer in Observers)
                observer.OnError(ex);
        }

        #endregion

        #region Nested class

        private class TextureAtlasMapper
        {
            private readonly Vector2[] _unityUvs;
            private readonly Vector2[] _unityUvs2;
            private readonly Vector2[] _unityUvs3;
            private readonly double[] _uvs;
            private readonly List<TextureAtlasInfo> _infos;

            public TextureAtlasMapper(Vector2[] unityUvs, Vector2[] unityUvs2, Vector2[] unityUvs3, double[] uvs,
                List<TextureAtlasInfo> infos)
            {
                _unityUvs = unityUvs;
                _unityUvs2 = unityUvs2;
                _unityUvs3 = unityUvs3;
                _uvs = uvs;
                _infos = infos;
            }

            public void SetUvs(int resultIndex, int origIindex)
            {
                int begin = 0;
                int end = _infos.Count;

                while (begin < end)
                {
                    int middle = begin + (end - begin) / 2;
                    var info = _infos[middle];
                    if (info.UvIndexRange.Contains(origIindex))
                    {
                        _unityUvs[resultIndex] = new Vector2((float)_uvs[origIindex], (float)_uvs[origIindex + 1]);
                        _unityUvs2[resultIndex] = info.TextureSize;
                        _unityUvs3[resultIndex] = info.TextureOffset;
                        return;
                    }
                    if (info.UvIndexRange.Minimum > origIindex)
                        end = middle;
                    else
                        begin = middle + 1;
                }
            }
        }

        private struct TextureAtlasInfo
        {
            public int TextureIndex;
            public Range<int> UvIndexRange;
            public Vector2 TextureSize;
            public Vector2 TextureOffset;
        }

        #endregion
    }
}
