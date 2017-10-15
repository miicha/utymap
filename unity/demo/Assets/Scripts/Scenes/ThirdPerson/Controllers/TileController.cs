using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Utils;

namespace Assets.Scripts.Scenes.ThirdPerson.Controllers
{
    internal sealed class TileController: IDisposable
    {
        private readonly IMapDataStore _dataStore;
        private readonly Stylesheet _stylesheet;
        private readonly ElevationDataType _elevationDataType;
        private readonly int _levelOfDetail;

        private GeoCoordinate _geoOrigin;

        private Vector3 _position;

        private int _disposedTilesCounter = 0;
        private Dictionary<QuadKey, Tile> _loadedQuadKeys = new Dictionary<QuadKey, Tile>();

        public GeoCoordinate RelativeNullPoint { get { return _geoOrigin;  } }

        public TileController(IMapDataStore dataStore, Stylesheet stylesheet,
            ElevationDataType elevationDataType, GeoCoordinate origin, int levelOfDetail)
        {
            _dataStore = dataStore;
            _stylesheet = stylesheet;
            _elevationDataType = elevationDataType;
            _geoOrigin = origin;
            _levelOfDetail = levelOfDetail;
        }

        /// <summary> Controls tiles on map. </summary>
        /// <param name="parent"> Parent of tiles. </param>
        /// <param name="position"> Current position. </param>
        public void Update(Transform parent, Vector3 position)
        {
            if (Vector3.Distance(position, _position) < float.Epsilon)
                return;

            _position = position;

            var currentPosition = GeoUtils.ToGeoCoordinate(_geoOrigin, new Vector2(_position.x, _position.z));
            var currentQuadKey = GeoUtils.CreateQuadKey(currentPosition, _levelOfDetail);

            var quadKeys = new HashSet<QuadKey>(GetNeighbours(currentQuadKey));
            var newlyLoadedQuadKeys = new Dictionary<QuadKey, Tile>();

            foreach (var quadKey in quadKeys)
                newlyLoadedQuadKeys.Add(quadKey, _loadedQuadKeys.ContainsKey(quadKey)
                    ? _loadedQuadKeys[quadKey]
                    : BuildQuadKey(parent, quadKey));

            int tilesDisposed = 0;
            foreach (var quadKeyPair in _loadedQuadKeys)
                if (!quadKeys.Contains(quadKeyPair.Key))
                {
                    ++tilesDisposed;
                    quadKeyPair.Value.Dispose();
                }

            UnloadAssets(tilesDisposed);
            _loadedQuadKeys = newlyLoadedQuadKeys;
        }

        // TODO call this method when tile is moved too far.
        /// <summary> Moves geo origin to specific world position. </summary>
        private void MoveGeoOrigin(Vector3 position)
        {
            _geoOrigin = GeoUtils.ToGeoCoordinate(_geoOrigin, new Vector2(position.x, position.z));
        }

        /// <summary> Get tiles surrounding given. </summary>
        private IEnumerable<QuadKey> GetNeighbours(QuadKey quadKey)
        {
            yield return new QuadKey(quadKey.TileX, quadKey.TileY, quadKey.LevelOfDetail);

            yield return new QuadKey(quadKey.TileX - 1, quadKey.TileY, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX - 1, quadKey.TileY + 1, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX, quadKey.TileY + 1, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX + 1, quadKey.TileY + 1, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX + 1, quadKey.TileY, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX + 1, quadKey.TileY - 1, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX, quadKey.TileY - 1, quadKey.LevelOfDetail);
            yield return new QuadKey(quadKey.TileX - 1, quadKey.TileY - 1, quadKey.LevelOfDetail);
        }

        /// <summary> Build specific quadkey. </summary>
        private Tile BuildQuadKey(Transform parent, QuadKey quadKey)
        {
            var tileGameObject = new GameObject(quadKey.ToString());
            tileGameObject.transform.parent = parent.transform;
            var tile = CreateTile(quadKey, tileGameObject);
            _loadedQuadKeys.Add(quadKey, tile);
            LoadTile(tile);
            return tile;
        }

        /// <summary> Creates tile for given quadkey. </summary>
        private Tile CreateTile(QuadKey quadKey, GameObject parent)
        {
            return new Tile(quadKey, _stylesheet, new CartesianProjection(_geoOrigin), _elevationDataType, parent);
        }

        /// <summary> Loads given tile. </summary>
        private void LoadTile(Tile tile)
        {
            _dataStore.OnNext(tile);
        }

        /// <summary> Unloads assets if necessary. </summary>
        /// <remarks> This method calls Resources.UnloadUnusedAssets which is expensive to do frequently. </remarks>
        private void UnloadAssets(int tilesDisposed)
        {
            const int disposedTileThreshold = 20;

            _disposedTilesCounter += tilesDisposed;

            if (_disposedTilesCounter > disposedTileThreshold)
            {
                _disposedTilesCounter = 0;
                Resources.UnloadUnusedAssets();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var tile in _loadedQuadKeys.Values.ToArray())
                tile.Dispose();

            UnloadAssets(int.MaxValue);
        }
    }
}
