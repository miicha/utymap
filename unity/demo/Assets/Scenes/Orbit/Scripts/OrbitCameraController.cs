using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Utils;
using UtyRx;

namespace Assets.Scenes.Orbit.Scripts
{
    internal sealed class OrbitCameraController : MonoBehaviour
    {
        private const string TraceCategory = "scene.orbit";

        public GameObject Planet;
        public bool ShowState = true;
        public bool FreezeLod = false;

        private int _currentLod;

        private Vector3 _lastPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        private IMapDataStore _dataStore;
        private IProjection _projection;
        private Stylesheet _stylesheet;
        private ElevationDataType _elevationDataType = ElevationDataType.Flat;

        #region Unity's callbacks

        /// <summary> Performs framework initialization once, before any Start() is called. </summary>
        void Awake()
        {
            var appManager = ApplicationManager.Instance;
            appManager.InitializeFramework(ConfigBuilder.GetDefault(), init => { });

            var trace = appManager.GetService<ITrace>();
            var modelBuilder = appManager.GetService<UnityModelBuilder>();
            appManager.GetService<IMapDataStore>()
               .SubscribeOn(Scheduler.ThreadPool)
               .ObserveOn(Scheduler.MainThread)
               .Where(r => r.Item1.GameObject != null)
               .Subscribe(r => r.Item2.Match(
                               e => modelBuilder.BuildElement(r.Item1, e),
                               m => modelBuilder.BuildMesh(r.Item1, m)),
                          ex => trace.Error(TraceCategory, ex, "cannot process mapdata."),
                          () => trace.Warn(TraceCategory, "stop listening mapdata."));

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
            // no movements
            if (_lastPosition == transform.position)
                return;

            _lastPosition = transform.position;

            if (OrbitCalculator.IsCloseToSurface(_lastPosition))
            {
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
                // create empty parent and destroy old quadkey.
                parent = new GameObject(actualName);
                parent.transform.parent = actualGameObject.transform.parent;
                GameObject.Destroy(actualGameObject.gameObject);
            }
            // zoom out
            else if (actualQuadKey.LevelOfDetail > _currentLod)
            {
                string name = actualName.Substring(0, actualName.Length - 1);
                var quadKey = QuadKey.FromString(name);
                // destroy all siblings
                foreach (var child in GetChildren(quadKey))
                    SafeDestroy(child.ToString());
                // destroy current as it might be just placeholder.
                SafeDestroy(name);
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
                var tileGameObject = new GameObject(quadKey.ToString());
                tileGameObject.transform.parent = parent.transform;
                _dataStore.OnNext(new Tile(quadKey, _stylesheet, _projection, _elevationDataType, tileGameObject));
            }
        }

        /// <summary> Destroys gameobject by its name if it exists. </summary>
        private void SafeDestroy(string name)
        {
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
