using System;
using Assets.Scenes.Details.Scripts;
using Assets.Scripts;
using Assets.Scripts.Scene.Controllers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Primitives;

namespace Assets.Scenes.Surface.Scripts
{
    class SurfaceCameraController : TileGridBehaviour
    {
        /// <summary> Max distance from origin before moving back. </summary>
        private const float MaxDistance = 2000;
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

        /// <inheritdoc />
        protected override bool OnPositionUpdated(Vector3 position)
        {
            if (IsCloseToDetail(position))
            {
                DetailCameraController.TileController.GeoOrigin = _tileController.GeoOrigin;
                TileController.Dispose();
                SceneManager.LoadScene("Detail");
                return false;
            }

            if (IsCloseToOrbit(position))
            {
                // TODO set proper orientation and position.
                TileController.Dispose();
                SceneManager.LoadScene("Orbit");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override float MaxOriginDistance()
        {
            return MaxDistance;
        }

        /// <inheritdoc />
        protected override TileGridController GetTileController()
        {
            return TileController;
        }

        private bool IsCloseToOrbit(Vector3 position)
        {
            if (_tileController.CurrentLevelOfDetail == MinLod)
            {
                var range = _tileController.GetHeightRange(new Vector3(position.x, float.MaxValue, position.z));
                var threshold = range.Minimum * 1.2f;
                return position.y > threshold;
            }
            return false;
        }

        private bool IsCloseToDetail(Vector3 position)
        {
            // TODO
            return false;
        }
    }
}
