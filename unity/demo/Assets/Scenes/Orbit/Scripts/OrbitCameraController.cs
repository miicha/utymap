using System;
using System.Collections.Generic;
using Assets.Scenes.Surface.Scripts;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Utils;

namespace Assets.Scenes.Orbit.Scripts
{
    internal sealed class OrbitCameraController : MonoBehaviour
    {
        private const float RotationSensivity = 5f;
        private const float HeightSensivity = 100f;

        public GameObject Planet;
        public bool ShowState = true;
        public bool FreezeLod = false;

        private int _currentLod;
        private float _lastHeight = float.MaxValue;
        private Vector3 _lastOrientation;
        private Dictionary<QuadKey, Tile> _loadedQuadKeys = new Dictionary<QuadKey, Tile>();

        private IMapDataStore _dataStore;
        private IProjection _projection;
        private Stylesheet _stylesheet;
        private ElevationDataType _elevationDataType = ElevationDataType.Flat;

        #region Unity's callbacks

        /// <summary> Performs framework initialization once, before any Start() is called. </summary>
        void Awake()
        {
            var appManager = ApplicationManager.Instance;
            appManager.InitializeFramework(ConfigBuilder.GetDefault());

            _dataStore = appManager.GetService<IMapDataStore>();
            _stylesheet = appManager.GetService<Stylesheet>();
            _projection = new SphericalProjection(OrbitCalculator.Radius);
        }

        void Start()
        {
            BuildInitial();
        }

        void Update()
        {
            var trans = transform;
            var position = trans.position;
            var rotation = trans.rotation;

            if (Vector3.Distance(_lastOrientation, rotation.eulerAngles) < RotationSensivity &&
                Math.Abs(_lastHeight - position.y) < HeightSensivity)
                return;

            _lastHeight = position.y;
            _lastOrientation = rotation.eulerAngles;

            if (OrbitCalculator.IsCloseToSurface(position))
            {
                SurfaceCalculator.GeoOrigin = OrbitCalculator.GetCoordinate(_lastOrientation);
                SceneManager.LoadScene("Surface");
                return;
            }

            UpdateLod();
            BuildIfNecessary();
        }

        void OnGUI()
        {
            if (ShowState)
            {
                var orientation = transform.rotation.eulerAngles;
                var labelText = String.Format("Position: {0}\nDistance: {1:0.#}km\nLOD: {2}",
                    OrbitCalculator.GetCoordinate(orientation),
                    OrbitCalculator.DistanceToSurface(transform.position),
                    OrbitCalculator.CalculateLevelOfDetail(transform.position));

                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText);
            }
        }

        #endregion

        /// <summary> Updates current lod level based on current position. </summary>
        private void UpdateLod()
        {
            if (FreezeLod)
            {
                _currentLod = Math.Max(1, _currentLod);
                return;
            }

            _currentLod = OrbitCalculator.CalculateLevelOfDetail(transform.position);
        }

        /// <summary> Builds planet on initial lod. </summary>
        private void BuildInitial()
        {
            var quadKeys = new List<QuadKey>();
            var maxQuad = GeoUtils.CreateQuadKey(new GeoCoordinate(-89.99, 179.99), OrbitCalculator.MinLod);
            for (int y = 0; y <= maxQuad.TileY; ++y)
                for (int x = 0; x <= maxQuad.TileX; ++x)
                    quadKeys.Add(new QuadKey(x, y, OrbitCalculator.MinLod));

            BuildQuadKeys(Planet, quadKeys);
        }

        /// <summary> Builds quadkeys if necessary. Decision is based on visible quadkey and lod level. </summary>
        private void BuildIfNecessary()
        {
            var orientation = transform.rotation.eulerAngles;
            var actualGameObject = GetActual(OrbitCalculator.GetCoordinate(orientation));
            if (actualGameObject == Planet)
                return;

            var actualQuadKey = QuadKey.FromString(actualGameObject.name);
            var actualName = actualGameObject.name;

            var parent = Planet;
            var quadKeys = new List<QuadKey>();

            // zoom in
            if (actualQuadKey.LevelOfDetail < _currentLod)
            {
                quadKeys.AddRange(GetChildren(actualQuadKey));
                var oldParent = actualGameObject.transform.parent;
                SafeDestroy(actualName, actualQuadKey);

                parent = new GameObject(actualName);
                parent.transform.parent = oldParent;
            }
             // zoom out
            else if (actualQuadKey.LevelOfDetail > _currentLod)
            {
                string name = actualName.Substring(0, actualName.Length - 1);
                var quadKey = QuadKey.FromString(name);
                // destroy all siblings
                foreach (var child in GetChildren(quadKey))
                    SafeDestroy(child.ToString(), child);
                // destroy current as it might be just placeholder.
                SafeDestroy(name, actualQuadKey);
                parent = GetParent(quadKey);
                quadKeys.Add(quadKey);
            }

            BuildQuadKeys(parent, quadKeys);
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
                var tile = new Tile(quadKey, _stylesheet, _projection, _elevationDataType, tileGameObject);
                _dataStore.OnNext(new Tile(quadKey, _stylesheet, _projection, _elevationDataType, tileGameObject));
                _loadedQuadKeys.Add(quadKey, tile);
            }
        }

        /// <summary> Destroys gameobject by its name if it exists. </summary>
        private void SafeDestroy(string name, QuadKey quadKey)
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
        private GameObject GetActual(GeoCoordinate coordinate)
        {
            var expectedQuadKey = GeoUtils.CreateQuadKey(coordinate, _currentLod);

            if (_loadedQuadKeys.ContainsKey(expectedQuadKey))
                return _loadedQuadKeys[expectedQuadKey].GameObject;

            var expectedGameObject = GameObject.Find(expectedQuadKey.ToString());
            return expectedGameObject == null
                ? GetParent(expectedQuadKey)  // zoom in
                : GetLastParent(expectedGameObject); // zoom out or pan
        }

        /// <summary> Gets parent game object for given quadkey. Creates hierarchy if necessary. </summary>
        private GameObject GetParent(QuadKey quadKey)
        {
            // recursion end
            if (quadKey.LevelOfDetail <= OrbitCalculator.MinLod)
                return Planet;

            string quadKeyName = quadKey.ToString();
            string parentName = quadKeyName.Substring(0, quadKeyName.Length - 1);
            var parent = GameObject.Find(parentName);
            return parent != null
                ? parent
                : GetParent(QuadKey.FromString(parentName));
        }

        /// <summary> Gets the last descendant game object with children. </summary>
        private GameObject GetLastParent(GameObject go)
        {
            return go.transform.childCount == 0
                ? go.transform.parent.gameObject
                : GetLastParent(go.transform.GetChild(0).gameObject);
        }
    }
}
