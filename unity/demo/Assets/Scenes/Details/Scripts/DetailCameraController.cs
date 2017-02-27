using System;
using Assets.Scenes.Surface.Scripts;
using Assets.Scripts;
using Assets.Scripts.Scene.Behaviours;
using Assets.Scripts.Scene.Controllers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Primitives;

namespace Assets.Scenes.Details.Scripts
{
    internal sealed class DetailCameraController : TileGridBehaviour
    {
        /// <summary> Max distance from origin before moving back. </summary>
        private const float MaxDistance = 3000;
        private const float Scale = 1f;
        private const int MinLod = 16;
        private const int MaxLod = 16;

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
            if (IsCloseToSurface(position))
            {
                SurfaceCameraController.TileController.GeoOrigin = _tileController.GeoOrigin;
                TileController.Dispose();
                SceneManager.LoadScene("Surface");
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

        /// <summary> Checks whether position is close to surface scene. </summary>
        private bool IsCloseToSurface(Vector3 position)
        {
            return false;
        }
    }
}
