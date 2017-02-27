using System;
using Assets.Scripts;
using Assets.Scripts.Scene.Controllers;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Primitives;

namespace Assets.Scenes.Surface.Scripts
{
    class SurfaceCameraController : MonoBehaviour
    {
        /// <summary> Max distance from origin before moving back. </summary>
        private const float MaxOriginDistance = 2000;
        private const float Scale = 0.01f;
        private const int MinLod = 9;
        private const int MaxLod = 15;

        private static TileGridController _tileController;
        /// <summary> Gets controller responsible for tile loading. </summary>
        public static TileGridController TileController
        {
            get
            {
                if (_tileController == null)
                {
                    var appManager = ApplicationManager.Instance;
                    appManager.InitializeFramework(ConfigBuilder.GetDefault());
                    _tileController = new TileGridController(
                        appManager.GetService<IMapDataStore>(),
                        appManager.GetService<Stylesheet>(),
                        ElevationDataType.Grid,
                        new Range<int>(MinLod, MaxLod),
                        Scale);
                    _tileController.GeoOrigin = new GeoCoordinate(52.53171, 13.38721);
                }

                return _tileController;
            }
        }

        public GameObject Pivot;
        public GameObject Planet;

        private Camera _camera;
        
        private Vector3 _lastPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        /// <summary> Performs framework initialization once, before any Start() is called. </summary>
        void Awake()
        {
            _camera = GetComponent<Camera>();
            TileController.UpdateCamera(_camera, transform.position);
            TileController.MoveOrigin(Vector3.zero);
        }

        void Update()
        {
            // no movements
            if (_lastPosition == transform.position)
                return;

            _lastPosition = transform.position;

            TileController.UpdateCamera(_camera, _lastPosition);
            TileController.Build(Planet, _lastPosition);

            KeepOrigin();
        }

        void OnGUI()
        {
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                String.Format("Position:{0}\nGeo:{1}\nQuadKey: {2}\nLOD:{3}\nScreen: {4}:{5}\nFoV: {6}",
                    transform.position,
                    TileController.Projection.Project(transform.position),
                    TileController.CurrentQuadKey,
                    TileController.CurrentLevelOfDetail,
                    Screen.width, Screen.height,
                    _camera.fieldOfView));
        }

        private void KeepOrigin()
        {
            var position = transform.position;
            if (!IsFar(position))
                return;

            Pivot.transform.position = TileController.WorldOrigin;
            Planet.transform.position += new Vector3(position.x, 0, position.z) * -1;
            _lastPosition = transform.position;

            TileController.MoveOrigin(position);
        }

        public bool IsFar(Vector3 position)
        {
            return Vector2.Distance(new Vector2(position.x, position.z), TileController.WorldOrigin) > MaxOriginDistance;
        }
    }
}
