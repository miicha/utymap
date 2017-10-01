using System;
using System.Collections.Generic;
using System.Linq;
using UtyDepend;
using UtyDepend.Config;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyRx;

namespace UtyMap.Unity.Data
{
    /// <summary> Defines behavior of class responsible of mapdata processing. </summary>
    public interface IMapDataStore : IObserver<Tile>, IObserver<MapQuery>,
        IObservable<MapData>, IObservable<Tile>, IObservable<Element>
    {
        /// <summary> Registers in-memory data storage with given key. </summary>
        /// <param name="storageKey"> Storage key.</param>
        void Register(string storageKey);

        /// <summary> Registers persistent data storage with given key. </summary>
        /// <param name="storageKey"> Storage key.</param>
        /// <param name="indexPath"> Persistent index path. </param>
        void Register(string storageKey, string indexPath);

        /// <summary> Adds mapdata to the specific data storage. </summary>
        /// <param name="storageKey"> Storage key. </param>
        /// <param name="dataPath"> Path to mapdata. </param>
        /// <param name="stylesheet"> Stylesheet which to use during import. </param>
        /// <param name="levelOfDetails"> Which level of details to use. </param>
        /// <returns> Returns progress status. </returns>
        IObservable<int> AddTo(string storageKey, string dataPath, Stylesheet stylesheet, Range<int> levelOfDetails);

        /// <summary> Adds mapdata to the specific data storage. </summary>
        /// <param name="storageKey"> Storage key. </param>
        /// <param name="dataPath"> Path to mapdata. </param>
        /// <param name="stylesheet"> Stylesheet which to use during import. </param>
        /// <param name="quadKey"> QuadKey to add. </param>
        /// <returns> Returns progress status. </returns>
        IObservable<int> AddTo(string storageKey, string dataPath, Stylesheet stylesheet, QuadKey quadKey);
    }

    /// <summary> Default implementation of map data store. </summary>
    internal class MapDataStore : IMapDataStore, IDisposable, IConfigurable
    {
        private readonly IMapDataProvider _mapDataProvider;
        private readonly IMapDataLibrary _mapDataLibrary;

        private readonly List<string> _storageKeys = new List<string>();
        private readonly List<IObserver<MapData>> _dataObservers = new List<IObserver<MapData>>();
        private readonly List<IObserver<Tile>> _tileObservers = new List<IObserver<Tile>>();
        private readonly List<IObserver<Element>> _elementObservers = new List<IObserver<Element>>();

        [Dependency]
        public MapDataStore(IMapDataProvider mapDataProvider, IMapDataLibrary mapDataLibrary)
        {
            _mapDataLibrary = mapDataLibrary;

            _mapDataProvider = mapDataProvider;
            _mapDataProvider
                .ObserveOn(Scheduler.ThreadPool)
                .Subscribe(value =>
                {
                    // We have map data in store.
                    if (String.IsNullOrEmpty(value.Item2))
                        _mapDataLibrary.Get(value.Item1, _dataObservers);
                    else
                        // NOTE store data in the first registered store
                        AddTo(_storageKeys.First(), value.Item2, value.Item1.Stylesheet, value.Item1.QuadKey)
                            .Subscribe(progress => { }, () => _mapDataLibrary.Get(value.Item1, _dataObservers));
                });
        }

        #region Interface implementations

        /// <inheritdoc />
        public void Register(string storageKey)
        {
            _storageKeys.Add(storageKey);
            _mapDataLibrary.Register(storageKey);
        }

        /// <inheritdoc />
        public void Register(string storageKey, string indexPath)
        {
            _storageKeys.Add(storageKey);
            _mapDataLibrary.Register(storageKey, indexPath);
        }

        /// <inheritdoc />
        public IObservable<int> AddTo(string storageKey, string dataPath, Stylesheet stylesheet, Range<int> levelOfDetails)
        {
            return _mapDataLibrary.AddTo(storageKey, dataPath, stylesheet, levelOfDetails);
        }

        /// <inheritdoc />
        public IObservable<int> AddTo(string storageKey, string dataPath, Stylesheet stylesheet, QuadKey quadKey)
        {
            return _mapDataLibrary.Exists(quadKey)
                ? Observable.Return<int>(100)
                : _mapDataLibrary.AddTo(storageKey, dataPath, stylesheet, quadKey);
        }

        /// <inheritdoc />
        void IObserver<MapQuery>.OnCompleted()
        {
            _dataObservers.ForEach(o => o.OnCompleted());
        }

        /// <inheritdoc />
        void IObserver<MapQuery>.OnError(Exception error)
        {
            _dataObservers.ForEach(o => o.OnError(error));
        }

         /// <inheritdoc />
         void IObserver<Tile>.OnCompleted()
         {
             _tileObservers.ForEach(o => o.OnCompleted());
         }

         /// <inheritdoc />
         void IObserver<Tile>.OnError(Exception error)
         {
             _tileObservers.ForEach(o => o.OnError(error));
         }

        /// <summary> Triggers loading data for given tile. </summary>
        public void OnNext(Tile tile)
        {
            _mapDataProvider.OnNext(tile);
        }

        /// <summary> Triggers search of elements matching query query. </summary>
        public void OnNext(MapQuery value)
        {
            _mapDataLibrary.Get(value, _elementObservers);
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

        /// <summary> Subscribes on element search event. </summary>
        public IDisposable Subscribe(IObserver<Element> observer)
        {
            _elementObservers.Add(observer);
            return Disposable.Empty;
        }

        /// <inheritdoc />
        public void Configure(IConfigSection configSection)
        {
            _mapDataLibrary.Configure(configSection.GetString("data/index"));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _mapDataLibrary.Dispose();
        }

        #endregion
    }
}
