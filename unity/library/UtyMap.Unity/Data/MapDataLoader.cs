using System;
using System.Collections.Generic;
using UtyDepend.Config;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Infrastructure.IO;
using UtyRx;

namespace UtyMap.Unity.Data
{
    /// <summary> Loads data from core library. </summary>
    internal class MapDataLoader : ISubject<Tile, MapData>, IObservable<Tile>, IConfigurable
    {
        private const string TraceCategory = "mapdata.loader";

        private readonly List<IObserver<MapData>> _dataObservers = new List<IObserver<MapData>>();
        private readonly List<IObserver<Tile>> _tileObservers = new List<IObserver<Tile>>();

        private readonly IPathResolver _pathResolver;
        private readonly ITrace _trace;

        public MapDataLoader(IPathResolver pathResolver, ITrace trace)
        {
            _pathResolver = pathResolver;
            _trace = trace;
        }

        /// <inheritdoc />
        public void OnCompleted()
        {
            _dataObservers.ForEach(o => o.OnCompleted());
            _tileObservers.ForEach(o => o.OnCompleted());
        }

        /// <inheritdoc />
        public void OnError(Exception error)
        {
            _dataObservers.ForEach(o => o.OnError(error));
            _tileObservers.ForEach(o => o.OnError(error));
        }

        /// <inheritdoc />
        public void OnNext(Tile tile)
        {
            var stylesheetPathResolved = _pathResolver.Resolve(tile.Stylesheet.Path);

            _trace.Info(TraceCategory, "loading tile: {0} using style: {1}", tile.ToString(), stylesheetPathResolved);

            var adapter = new MapDataAdapter(tile, _dataObservers, _trace);
            CoreLibrary.LoadQuadKey(
                stylesheetPathResolved,
                tile.QuadKey,
                tile.ElevationType,
                adapter.AdaptMesh,
                adapter.AdaptElement,
                adapter.AdaptError,
                tile.CancelationToken);
            _trace.Info(TraceCategory, "tile loaded: {0}", tile.ToString());

            _tileObservers.ForEach(o => o.OnNext(tile));
        }

        /// <summary> Subscribes on mesh/element data loaded events. </summary>
        public IDisposable Subscribe(IObserver<MapData> observer)
        {
            _dataObservers.Add(observer);
            return Disposable.Empty;
        }

        /// <summary> Subscribes on tile fully load event. </summary>
        public IDisposable Subscribe(IObserver<Tile> observer)
        {
            _tileObservers.Add(observer);
            return Disposable.Empty;
        }

        /// <inheritdoc />
        public void Configure(IConfigSection configSection)
        {
            // empty so far
        }
    }
}
