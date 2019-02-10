using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Core.Plugins;
using UtyMap.Unity;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyRx;

namespace Assets.Scripts.Core.Interop
{
    /// <summary> Il2Cpp scripting backend specific implementation. </summary>
    partial class MapDataLibrary
    {
#if ENABLE_IL2CPP
        private static IList<UtyRx.IObserver<MapData>> _tileObservers;
        private static IList<UtyRx.IObserver<Element>> _queryObservers;
        private static MaterialProvider _sMaterialProvider;

        private static readonly SafeDictionary<int, Tile> Tiles = new SafeDictionary<int, Tile>();
        private static readonly SafeDictionary<int, MapQuery> Queries = new SafeDictionary<int, MapQuery>();

        private static ITrace _trace;

        /// <inheritdoc />
        public UtyRx.IObservable<int> Get(Tile tile, IList<UtyRx.IObserver<MapData>> observers)
        {
            // NOTE workaround for static methods requirement
            if (_sMaterialProvider == null)
            {
                lock (this)
                {
                    _tileObservers = observers;
                    _sMaterialProvider = _materialProvider;
                }
            }

            var tag = tile.GetHashCode();
            Tiles.TryAdd(tag, tile);
            var observable = Get(tile, tag, OnMeshBuiltHandler, OnElementLoadedHandler, OnErrorHandler);
            Tiles.TryRemove(tag);
            return observable;
        }

        /// <inheritdoc />
        public UtyRx.IObservable<int> Get(MapQuery query, IList<UtyRx.IObserver<Element>> observers)
        {
            lock (this)
            {
                _queryObservers = observers;
            }

            var tag = query.GetHashCode();
            Queries.TryAdd(tag, query);
            var observable = Get(query, 0, OnElementFoundHandler, OnErrorHandler);
            Queries.TryRemove(tag);
            return observable;
        }

        private delegate void OnNewDirectory(string directory);

        private delegate void OnMeshBuilt(int tag, string name,
            IntPtr vertexPtr, int vertexCount,
            IntPtr trianglePtr, int triangleCount,
            IntPtr colorPtr, int colorCount,
            IntPtr uvPtr, int uvCount,
            IntPtr uvMapPtr, int uvMapCount);

        private delegate void OnElementLoaded(int tag, long id,
            IntPtr tagPtr, int tagCount,
            IntPtr vertexPtr, int vertexCount,
            IntPtr stylePtr, int styleCount);

        private delegate void OnError(string message);

        [AOT.MonoPInvokeCallback(typeof(OnError))]
        private static void OnCreateDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        [AOT.MonoPInvokeCallback(typeof(OnError))]
        private static void OnMeshBuiltHandler(int tag, string name, IntPtr vertexPtr, int vertexCount,
            IntPtr trianglePtr, int triangleCount, IntPtr colorPtr, int colorCount,
            IntPtr uvPtr, int uvCount, IntPtr uvMapPtr, int uvMapCount)
        {
            Tile tile;
            if (!Tiles.TryGetValue(tag, out tile) || tile.IsDisposed)
                return;

            // NOTE ideally, arrays should be marshalled automatically which could enable some optimizations,
            // especially, for il2cpp. However, I was not able to make it work using il2cpp setting: all arrays
            // were passed to this method with just one element. I gave up and decided to use manual marshalling 
            // here and in AdaptElement method below.
            var vertices = MarshalUtils.ReadDoubles(vertexPtr, vertexCount);
            var triangles = MarshalUtils.ReadInts(trianglePtr, triangleCount);
            var colors = MarshalUtils.ReadInts(colorPtr, colorCount);
            var uvs = MarshalUtils.ReadDoubles(uvPtr, uvCount);
            var uvMap = MarshalUtils.ReadInts(uvMapPtr, uvMapCount);
            MapDataAdapter.AdaptMesh(tile, _sMaterialProvider, _tileObservers, _trace, name, vertices, triangles, colors, uvs, uvMap);
        }

        [AOT.MonoPInvokeCallback(typeof(OnError))]
        private static void OnElementLoadedHandler(int tag, long id, IntPtr tagPtr, int tagCount,
            IntPtr vertexPtr, int vertexCount, IntPtr stylePtr, int styleCount)
        {
            Tile tile;
            if (!Tiles.TryGetValue(tag, out tile) || tile.IsDisposed)
                return;

            // NOTE see note above
            var vertices = MarshalUtils.ReadDoubles(vertexPtr, vertexCount);
            var tags = MarshalUtils.ReadStrings(tagPtr, tagCount);
            var styles = MarshalUtils.ReadStrings(stylePtr, styleCount);

            MapDataAdapter.AdaptElement(tile, _sMaterialProvider, _tileObservers, _trace, id, vertices, tags, styles);
        }

        [AOT.MonoPInvokeCallback(typeof(OnError))]
        private static void OnElementFoundHandler(int tag, long id, IntPtr tagPtr, int tagCount,
            IntPtr vertexPtr, int vertexCount, IntPtr stylePtr, int styleCount)
        {
            // NOTE see note above
            var vertices = MarshalUtils.ReadDoubles(vertexPtr, vertexCount);
            var tags = MarshalUtils.ReadStrings(tagPtr, tagCount);
            var styles = MarshalUtils.ReadStrings(stylePtr, styleCount);

            Element element = MapDataAdapter.AdaptElement(id, tags, vertices, styles);
            foreach (var observer in _queryObservers)
                observer.OnNext(element);
        }

        [AOT.MonoPInvokeCallback(typeof(OnError))]
        private static void OnErrorHandler(string message)
        {
            throw new InvalidOperationException(message);
        }
#endif
    }
}
