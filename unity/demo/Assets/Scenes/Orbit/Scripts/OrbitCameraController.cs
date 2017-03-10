using System;
using System.Collections.Generic;
using Assets.Scenes.Surface.Scripts;
using Assets.Scripts;
using Assets.Scripts.Scene.Controllers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtyMap.Unity;
using UtyMap.Unity.Animations.Time;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Primitives;
using Animation = UtyMap.Unity.Animations.Animation;

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

        private const float RotationSensivity = 5f;
        private const float HeightSensivity = 100f;

        public GameObject Planet;
        public bool ShowState = true;
        public bool FreezeLod = false;

        private Vector3 _lastPosition;
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
                        Radius);
                }

                return _tileController;
            }
        }

        #region Unity's callbacks

        void Start()
        {
            var animation = new Animation(new DecelerateInterpolator(), 
                new UtyMap.Unity.Animations.Path.LinearInterpolator(new List<Vector3>()
                {
                   transform.position,
                   transform.position + (transform.position - Vector3.zero).normalized * -5000,
                }), 10);

            GetComponent<OrbitAnimator>().Animation = animation;
            animation.Start();
        }

        void Update()
        {
            if (FreezeLod)
                return;

            var trans = transform;
            var position = trans.position;
            var rotation = trans.rotation;

            if (Vector3.Distance(_lastOrientation, rotation.eulerAngles) < RotationSensivity &&
                Vector3.Distance(position, TileController.Origin) < HeightSensivity)
                return;

            _lastPosition = position;
            _lastOrientation = rotation.eulerAngles;

            if (IsCloseToSurface(position))
            {
                SurfaceCameraController.TileController.GeoOrigin = TileController.GetCoordinate(_lastOrientation);
                _tileController.Dispose();
                SceneManager.LoadScene("Surface");
                return;
            }

            TileController.Build(Planet, position, _lastOrientation);
        }

        void OnGUI()
        {
            if (ShowState)
            {
                var orientation = transform.rotation.eulerAngles;
                var labelText = String.Format("Position: {0}\nDistance: {1:0.#}km\nLOD: {2}",
                    TileController.GetCoordinate(orientation),
                    TileController.DistanceToSurface(transform.position),
                    TileController.CurrentLevelOfDetail);

                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText);
            }
        }

        #endregion

        /// <summary> Checks whether position is close to surface. </summary>
        private bool IsCloseToSurface(Vector3 position)
        {
            return TileController.CurrentLevelOfDetail == MaxLod && TileController.IsUnderMin(position);
        }
    }
}
