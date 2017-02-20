using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Scene;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Utils;
using UtyRx;

namespace Assets.Scenes.Surface.Scripts
{
    class SurfaceCameraController : MonoBehaviour
    {
        private const string TraceCategory = "scene.surface";

        private QuadKey _currentQuadKey;
        private Vector3 _lastPosition = Vector3.zero;
        private RangeTree<float, int> _lodTree;

        public GameObject Pivot;
        public GameObject Planet;

        private HashSet<QuadKey> _loadedQuadKeys = new HashSet<QuadKey>();

        private IMapDataStore _dataStore;
        private IProjection _projection;
        private Stylesheet _stylesheet;

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
               .Where(r => r.Item1.GameObject.activeInHierarchy)
               .Subscribe(r => r.Item2.Match(
                               e => modelBuilder.BuildElement(r.Item1, e),
                               m => modelBuilder.BuildMesh(r.Item1, m)),
                          ex => trace.Error(TraceCategory, ex, "cannot process mapdata."),
                          () => trace.Warn(TraceCategory, "stop listening mapdata."));

            _dataStore = appManager.GetService<IMapDataStore>();
            _stylesheet = appManager.GetService<Stylesheet>();
            _projection = new CartesianProjection(SurfaceCalculator.GeoOrigin);
        }

        void Start()
        {
            _lodTree = SurfaceCalculator.GetLodTree(GetComponent<Camera>(), transform.position);

            UpdateLod();
        }

        void Update()
        {
            // no movements
            if (_lastPosition == transform.position)
                return;

            _lastPosition = transform.position;

            UpdateLod();

            BuildIfNecessary();

            KeepOrigin();
        }

        void OnGUI()
        {
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                String.Format("Position:{0}\nGeo:{1}\nQuadKey: {2}\nLOD:{3}\nScreen: {4}:{5}\nFoV: {6}",
                    transform.position,
                    GeoUtils.ToGeoCoordinate(SurfaceCalculator.GeoOrigin, new Vector2(transform.position.x, transform.position.z)),
                    _currentQuadKey,
                    SurfaceCalculator.CurrentLevelOfDetails,
                    Screen.width, Screen.height,
                    GetComponent<Camera>().fieldOfView));
        }

        private void KeepOrigin()
        {
            if (!SurfaceCalculator.IsFar(transform.position))
                return;

            Vector3 direction = new Vector3(transform.position.x, 0, transform.position.z) - SurfaceCalculator.Origin;

            Pivot.transform.position = SurfaceCalculator.Origin;
            Planet.transform.position += direction * -1;

            SurfaceCalculator.GeoOrigin = GeoUtils.ToGeoCoordinate(SurfaceCalculator.GeoOrigin, new Vector2(direction.x, direction.z));
            _projection = new CartesianProjection(SurfaceCalculator.GeoOrigin);
        }

        /// <summary> Updates current lod level based on current position. </summary>
        private void UpdateLod()
        {
            SurfaceCalculator.CurrentLevelOfDetails = _lodTree[transform.position.y].First().Value;
        }

        /// <summary> Builds quadkeys if necessary. Decision is based on current position and lod level. </summary>
        private void BuildIfNecessary()
        {
            var oldLod = _currentQuadKey.LevelOfDetail;
            var currentPosition = GeoUtils.ToGeoCoordinate(SurfaceCalculator.GeoOrigin, new Vector2(_lastPosition.x, _lastPosition.z));
            _currentQuadKey = GeoUtils.CreateQuadKey(currentPosition, SurfaceCalculator.CurrentLevelOfDetails);

            // zoom in/out
            if (oldLod != SurfaceCalculator.CurrentLevelOfDetails)
            {
                foreach (var quadKey in _loadedQuadKeys)
                    SafeDestroy(quadKey.ToString());

                _loadedQuadKeys.Clear();

                foreach (var quadKey in GetNeighbours(_currentQuadKey))
                    BuildQuadKey(Planet, quadKey);
            }
            // pan
            else
            {
                var quadKeys = new HashSet<QuadKey>(GetNeighbours(_currentQuadKey));

                foreach (var quadKey in quadKeys)
                {
                    if (_loadedQuadKeys.Contains(quadKey))
                        continue;

                    BuildQuadKey(Planet, quadKey);
                }

                foreach (var quadKey in _loadedQuadKeys)
                    if (!quadKeys.Contains(quadKey))
                        SafeDestroy(quadKey.ToString());

                _loadedQuadKeys = quadKeys;
            }
        }

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

        private void BuildQuadKey(GameObject parent, QuadKey quadKey)
        {
            var tileGameObject = new GameObject(quadKey.ToString());
            tileGameObject.transform.parent = parent.transform;
            _dataStore.OnNext(new Tile(quadKey, _stylesheet, _projection, ElevationDataType.Grid, tileGameObject));
            //QuadTreeSystem.BuildQuadTree(tileGameObject, quadKey, _projection);
            _loadedQuadKeys.Add(quadKey);
        }

        private static void SafeDestroy(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(false);
                GameObject.Destroy(go);
            }
        }
    }
}
