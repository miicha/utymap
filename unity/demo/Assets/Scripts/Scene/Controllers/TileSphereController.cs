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
    /// <summary> Controlls how tiles are loaded on sphere. </summary>
    internal sealed class TileSphereController : IDisposable
    {
        private readonly IMapDataStore _dataStore;
        private readonly Stylesheet _stylesheet;
        private readonly ElevationDataType _elevationType;
        private readonly Range<int> _lodRange;
        private readonly float _minDistance;
        private readonly IProjection _projection;
        private readonly float _lodStep;

        private Dictionary<QuadKey, Tile> _loadedQuadKeys = new Dictionary<QuadKey, Tile>();

        public int CurrentLevelOfDetail { get; private set; }
        public float Radius { get; private set; }
        public Vector3 Origin { get; private set; }

        public TileSphereController(IMapDataStore dataStore, Stylesheet stylesheet,
            ElevationDataType elevationType, Range<int> lodRange, float radius, float minDistance)
        {
            _dataStore = dataStore;
            _stylesheet = stylesheet;
            _elevationType = elevationType;
            _projection = new SphericalProjection(radius);
            _lodRange = lodRange;
            _minDistance = minDistance;

            Radius = radius;
            Origin = Vector3.zero;

            _lodStep = (2 * Radius - minDistance) / (lodRange.Maximum - lodRange.Minimum);
        }

        /// <summary> Gets coordinate from given rotation in euler angles. </summary>
        public GeoCoordinate GetCoordinate(Vector3 eulerAngles)
        {
            var latitude = eulerAngles.x;
            var longitude = (-90 - eulerAngles.y) % 360;

            if (latitude > 90) latitude -= 360;
            if (longitude < -180) longitude += 360;

            return new GeoCoordinate(latitude, longitude);
        }

        /// <summary> Calculates distance to surface. </summary>
        public float DistanceToSurface(Vector3 position)
        {
            return Vector3.Distance(position, Origin) - Radius;
        }

        /// <summary> Builds tiles for given position and orientation. </summary>
        public void Build(GameObject planet, Vector3 position, Vector3 orientation)
        {
            CurrentLevelOfDetail = CalculateLevelOfDetail(position);

            if (_loadedQuadKeys.Any())
                BuildIfNecessary(planet, orientation);
            else
                BuildInitial(planet);
        }

        /// <summary> Builds quadkeys if necessary. Decision is based on visible quadkey and lod level. </summary>
        private void BuildIfNecessary(GameObject planet, Vector3 orientation)
        {
            var actualGameObject = GetActual(planet, GetCoordinate(orientation));
            if (actualGameObject == planet)
                return;

            var actualQuadKey = QuadKey.FromString(actualGameObject.name);
            var actualName = actualGameObject.name;

            var parent = planet;
            var quadKeys = new List<QuadKey>();

            // zoom in
            if (actualQuadKey.LevelOfDetail < CurrentLevelOfDetail)
            {
                quadKeys.AddRange(GetChildren(actualQuadKey));
                var oldParent = actualGameObject.transform.parent;
                SafeDestroy(actualQuadKey, actualName);

                parent = new GameObject(actualName);
                parent.transform.parent = oldParent;
                Resources.UnloadUnusedAssets();
            }
            // zoom out
            else if (actualQuadKey.LevelOfDetail > CurrentLevelOfDetail)
            {
                string name = actualName.Substring(0, actualName.Length - 1);
                var quadKey = QuadKey.FromString(name);
                // destroy all siblings
                foreach (var child in GetChildren(quadKey))
                    SafeDestroy(child, child.ToString());
                // destroy current as it might be just placeholder.
                SafeDestroy(actualQuadKey, name);
                parent = GetParent(planet, quadKey);
                quadKeys.Add(quadKey);
                Resources.UnloadUnusedAssets();
            }

            BuildQuadKeys(parent, quadKeys);
        }

        /// <summary> Builds planet on initial lod. </summary>
        private void BuildInitial(GameObject planet)
        {
            var quadKeys = new List<QuadKey>();
            var maxQuad = GeoUtils.CreateQuadKey(new GeoCoordinate(-89.99, 179.99), _lodRange.Minimum);
            for (int y = 0; y <= maxQuad.TileY; ++y)
                for (int x = 0; x <= maxQuad.TileX; ++x)
                    quadKeys.Add(new QuadKey(x, y, _lodRange.Minimum));

            BuildQuadKeys(planet, quadKeys);
        }

        /// <summary> Builds quadkeys </summary>
        private void BuildQuadKeys(GameObject parent, IEnumerable<QuadKey> quadKeys)
        {
            foreach (var quadKey in quadKeys)
            {
                if (_loadedQuadKeys.ContainsKey(quadKey))
                    continue;

                var tileGameObject = new GameObject(quadKey.ToString());
                tileGameObject.transform.parent = parent.transform;
                var tile = new Tile(quadKey, _stylesheet, _projection, _elevationType, tileGameObject);
                _loadedQuadKeys.Add(quadKey, tile);
                _dataStore.OnNext(tile);
            }
        }

        /// <summary> Destroys gameobject by its name if it exists. </summary>
        private void SafeDestroy(QuadKey quadKey, string name = null)
        {
            if (_loadedQuadKeys.ContainsKey(quadKey))
            {
                _loadedQuadKeys[quadKey].Dispose();
                _loadedQuadKeys.Remove(quadKey);
                return;
            }

            var go = GameObject.Find(name);
            if (go != null)
                GameObject.Destroy(go);
        }

        /// <summary> Gets childrent for quadkey. </summary>
        private IEnumerable<QuadKey> GetChildren(QuadKey quadKey)
        {
            // TODO can be optimized to avoid string allocations.
            var quadKeyName = quadKey.ToString();
            yield return QuadKey.FromString(quadKeyName + "0");
            yield return QuadKey.FromString(quadKeyName + "1");
            yield return QuadKey.FromString(quadKeyName + "2");
            yield return QuadKey.FromString(quadKeyName + "3");
        }

        /// <summary> Gets actual loaded quadkey's gameobject for given coordinate. </summary>
        private GameObject GetActual(GameObject planet, GeoCoordinate coordinate)
        {
            var expectedQuadKey = GeoUtils.CreateQuadKey(coordinate, CurrentLevelOfDetail);

            if (_loadedQuadKeys.ContainsKey(expectedQuadKey))
                return _loadedQuadKeys[expectedQuadKey].GameObject;

            var expectedGameObject = GameObject.Find(expectedQuadKey.ToString());
            return expectedGameObject == null
                ? GetParent(planet, expectedQuadKey)  // zoom in
                : GetLastParent(expectedGameObject); // zoom out or pan
        }

        /// <summary> Gets parent game object for given quadkey. Creates hierarchy if necessary. </summary>
        private GameObject GetParent(GameObject planet, QuadKey quadKey)
        {
            // recursion end
            if (quadKey.LevelOfDetail <= _lodRange.Minimum)
                return planet;

            string quadKeyName = quadKey.ToString();
            string parentName = quadKeyName.Substring(0, quadKeyName.Length - 1);
            var parent = GameObject.Find(parentName);
            return parent != null
                ? parent
                : GetParent(planet, QuadKey.FromString(parentName));
        }

        /// <summary> Gets the last descendant game object with children. </summary>
        private GameObject GetLastParent(GameObject go)
        {
            return go.transform.childCount == 0
                ? go.transform.parent.gameObject
                : GetLastParent(go.transform.GetChild(0).gameObject);
        }

        /// <summary> Calculates LOD for given position </summary>
        private int CalculateLevelOfDetail(Vector3 position)
        {
            // TODO make it better: handle non-uniformly
            var distance = Vector3.Distance(position, Origin) - _minDistance;
            return Math.Max(_lodRange.Maximum - (int)Math.Round(distance / _lodStep), _lodRange.Minimum);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var quadkey in _loadedQuadKeys.Keys)
                SafeDestroy(quadkey);
            Resources.UnloadUnusedAssets();
        }
    }
}
