using System;
using Assets.Scenes.Surface.Scripts;
using Assets.Scripts;
using Assets.Scripts.Scene.Controllers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Primitives;

namespace Assets.Scenes.Orbit.Scripts
{
    internal sealed class OrbitCameraController : MonoBehaviour
    {
        /// <summary> Scaled radius of Earth in meters. </summary>
        /// <remarks> So far, we use 1:1000 scale. </remarks>
        private const float Radius = 6371;

        /// <summary> Minimal supported LOD. </summary>
        private const int MinLod = 1;

        /// <summary> Maximal supported LOD. </summary>
        private const int MaxLod = 8;

        /// <summary> Closest distance to sphere's surface. </summary>
        private const float MinDistance = Radius * 1.1f;

        private const float RotationSensivity = 5f;
        private const float HeightSensivity = 100f;

        public GameObject Planet;
        public bool ShowState = true;

        private float _lastHeight = float.MaxValue;
        private Vector3 _lastOrientation;

        private static TileSphereController _tileController;
        /// <summary> Gets controller responsible for tile loading. </summary>
        public static TileSphereController TileController
        {
            get
            {
                if (_tileController == null)
                {
                    var appManager = ApplicationManager.Instance;
                    appManager.InitializeFramework(ConfigBuilder.GetDefault());
                    _tileController = new TileSphereController(
                        appManager.GetService<IMapDataStore>(),
                        appManager.GetService<Stylesheet>(),
                        ElevationDataType.Flat,
                        new Range<int>(MinLod, MaxLod),
                        Radius,
                        MinDistance);
                }

                return _tileController;
            }
        }

        #region Unity's callbacks

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

            if (IsCloseToSurface(position))
            {
                SurfaceCameraController.TileController.GeoOrigin = _tileController.GetCoordinate(_lastOrientation);
                _tileController.Dispose();

                SceneManager.LoadScene("Surface");
                return;
            }

            _tileController.Build(Planet, position, _lastOrientation);
        }

        void OnGUI()
        {
            if (ShowState)
            {
                var orientation = transform.rotation.eulerAngles;
                var labelText = String.Format("Position: {0}\nDistance: {1:0.#}km\nLOD: {2}",
                    _tileController.GetCoordinate(orientation),
                    _tileController.DistanceToSurface(transform.position),
                    _tileController.CurrentLevelOfDetail);

                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText);
            }
        }

        #endregion

        /// <summary> Checks whether position is close to surface. </summary>
        private bool IsCloseToSurface(Vector3 position)
        {
            return Vector3.Distance(position, TileController.Origin) < MinDistance;
        }
    }
}
