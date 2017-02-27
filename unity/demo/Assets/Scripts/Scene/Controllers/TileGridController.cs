using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Utils;

namespace Assets.Scripts.Scene.Controllers
{
    // TODO move to library project once tested.
    /// <summary> Controlls how tiles are loaded using grid structure. </summary>
    internal sealed class TileGridController : IDisposable
    {
        private Dictionary<QuadKey, Tile> _loadedQuadKeys = new Dictionary<QuadKey, Tile>();

        private readonly IMapDataStore _dataStore;
        private readonly Stylesheet _stylesheet;
        private readonly ElevationDataType _elevationType;
        private readonly Range<int> _lodRange;
        private RangeTree<float, int> _lodTree;

        /// <summary> World scale. </summary>
        public float Scale { get; private set; }

        /// <summary> Origin in world coordinates. </summary>
        public Vector3 WorldOrigin { get; private set; }

        /// <summary> Origin as GeoCoordinate. </summary>
        public GeoCoordinate GeoOrigin { get; set; }

        /// <summary> Current level of detail. </summary>
        public int CurrentLevelOfDetail { get; private set; }

        /// <summary> Current quadkey. </summary>
        public QuadKey CurrentQuadKey { get; private set; }

        /// <summary> Current used projection. </summary>
        public IProjection Projection { get; private set; }

        /// <summary> Creates instance of <see cref="TileGridController"/>. </summary>
        /// <param name="dataStore"> Data store for loading tiles. </param>
        /// <param name="stylesheet"> Used stylesheet. </param>
        /// <param name="elevationType"> Elevation type. </param>
        /// <param name="lodRange"> Level of detail range. </param>
        /// <param name="scale"> Coordinate scale. </param>
        public TileGridController(IMapDataStore dataStore, Stylesheet stylesheet,
            ElevationDataType elevationType, Range<int> lodRange, float scale)
        {
            _dataStore = dataStore;
            _stylesheet = stylesheet;
            _elevationType = elevationType;

            _lodRange = lodRange;

            WorldOrigin = Vector3.zero;
            Scale = scale;
        }

        /// <summary> Builds quadkeys if necessary. Decision is based on current position and lod level. </summary>
        public void Build(GameObject parent, Vector3 position)
        {
            var oldLod = CurrentQuadKey.LevelOfDetail;
            CurrentQuadKey = GetQuadKey(position);

            // zoom in/out
            if (oldLod != CurrentLevelOfDetail)
            {
                foreach (var tile in _loadedQuadKeys.Values)
                    tile.Dispose();

                Resources.UnloadUnusedAssets();
                _loadedQuadKeys.Clear();

                foreach (var quadKey in GetNeighbours(CurrentQuadKey))
                    BuildQuadKey(parent, quadKey);
            }
            // pan
            else
            {
                var quadKeys = new HashSet<QuadKey>(GetNeighbours(CurrentQuadKey));
                var newlyLoadedQuadKeys = new Dictionary<QuadKey, Tile>();

                foreach (var quadKey in quadKeys)
                    newlyLoadedQuadKeys.Add(quadKey, _loadedQuadKeys.ContainsKey(quadKey)
                        ? _loadedQuadKeys[quadKey]
                        : BuildQuadKey(parent, quadKey));

                foreach (var quadKeyPair in _loadedQuadKeys)
                    if (!quadKeys.Contains(quadKeyPair.Key))
                        quadKeyPair.Value.Dispose();

                Resources.UnloadUnusedAssets();
                _loadedQuadKeys = newlyLoadedQuadKeys;
            }
        }

        /// <summary> Moves origin to position. </summary>
        public void MoveOrigin(Vector3 position)
        {
            GeoOrigin = GeoUtils.ToGeoCoordinate(GeoOrigin, new Vector2(position.x, position.z) / Scale);
            Projection = GetProjection();
        }

        /// <summary> Updates camera dependend values. </summary>
        public void UpdateCamera(Camera camera, Vector3 position)
        {
            if (_lodTree == null)
                _lodTree = GetLodTree(camera, position);

            CurrentLevelOfDetail = _lodTree[position.y].First().Value;
        }

        /// <summary> Gets zoom speed ratio. </summary>
        public float GetZoomSpeedRatio(int lod)
        {
            return Mathf.Pow(2, -(lod - _lodRange.Minimum)) * Scale;
        }

        /// <summary> Gets pan speed ratio. </summary>
        public float GetPanSpeedRatio(int lod)
        {
            return Mathf.Pow(2, -(lod - _lodRange.Minimum)) * Scale;
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
        private Tile BuildQuadKey(GameObject parent, QuadKey quadKey)
        {
            var tileGameObject = new GameObject(quadKey.ToString());
            tileGameObject.transform.parent = parent.transform;
            var tile = new Tile(quadKey, _stylesheet, Projection, _elevationType, tileGameObject);
            _loadedQuadKeys.Add(quadKey, tile);
            _dataStore.OnNext(tile);
            return tile;
        }

        /// <summary> Gets quadkey for position. </summary>
        private QuadKey GetQuadKey(Vector3 position)
        {
            var currentPosition = GeoUtils.ToGeoCoordinate(GeoOrigin, new Vector2(position.x, position.z) / Scale);
            return GeoUtils.CreateQuadKey(currentPosition, CurrentLevelOfDetail);
        }

        /// <summary> Gets GetProjection for current georigin and scale. </summary>
        private IProjection GetProjection()
        {
            IProjection projection = new CartesianProjection(GeoOrigin);
            return Scale > 0
                ? new ScaledProjection(projection, Scale)
                : projection;
        }

        #region Lod calculations

        /// <summary> Gets range (interval) tree with LODs </summary>
        /// <remarks> Modifies camera's field of view. </remarks>
        private RangeTree<float, int> GetLodTree(Camera camera, Vector3 position)
        {
            const float sizeRatio = 0.75f;
            var tree = new RangeTree<float, int>();

            var maxDistance = position.y - 1;

            var aspectRatio = sizeRatio * (Screen.height < Screen.width ? 1 / camera.aspect : 1);

            var fov = GetFieldOfView(GeoUtils.CreateQuadKey(GeoOrigin, _lodRange.Minimum), maxDistance, aspectRatio);

            tree.Add(maxDistance, float.MaxValue, _lodRange.Minimum);
            for (int lod = _lodRange.Minimum + 1; lod <= _lodRange.Maximum; ++lod)
            {
                var frustumHeight = GetFrustumHeight(GeoUtils.CreateQuadKey(GeoOrigin, lod), aspectRatio);
                var distance = frustumHeight * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                tree.Add(distance, maxDistance, lod - 1);
                maxDistance = distance;
            }
            tree.Add(float.MinValue, maxDistance, _lodRange.Maximum);

            camera.fieldOfView = fov;

            return tree;
        }

        /// <summary> Gets height of camera's frustum. </summary>
        private float GetFrustumHeight(QuadKey quadKey, float aspectRatio)
        {
            return GetGridSize(quadKey) * aspectRatio;
        }

        /// <summary> Gets field of view for given quadkey and distance. </summary>
        private float GetFieldOfView(QuadKey quadKey, float distance, float aspectRatio)
        {
            return 2.0f * Mathf.Rad2Deg * Mathf.Atan(GetFrustumHeight(quadKey, aspectRatio) * 0.5f / distance);
        }

        /// <summary> Get side size of grid consists of 9 quadkeys. </summary>
        private float GetGridSize(QuadKey quadKey)
        {
            var bbox = GeoUtils.QuadKeyToBoundingBox(quadKey);
            var bboxWidth = bbox.MaxPoint.Longitude - bbox.MinPoint.Longitude;
            var minPoint = new GeoCoordinate(bbox.MinPoint.Latitude, bbox.MinPoint.Longitude - bboxWidth);
            var maxPoint = new GeoCoordinate(bbox.MinPoint.Latitude, bbox.MaxPoint.Longitude + bboxWidth);
            return (float)GeoUtils.Distance(minPoint, maxPoint) * Scale;
        }

        #endregion

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var tile in _loadedQuadKeys.Values)
                tile.Dispose();

            Resources.UnloadUnusedAssets();
        }
    }
}
