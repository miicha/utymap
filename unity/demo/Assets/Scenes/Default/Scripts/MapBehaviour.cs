using System;
using System.Collections.Generic;
using Assets.Scenes.Default.Scripts.Animations;
using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;
using Assets.Scripts;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Primitives;

using Animator = UtyMap.Unity.Animations.Animator;

namespace Assets.Scenes.Default.Scripts
{
    internal class MapBehaviour: MonoBehaviour
    {
        public double Latitude = 52.53171;
        public double Longitude = 13.38721;

        public bool ShowState = true;

        public Transform Planet;
        public Transform Pivot;
        public Camera Camera;

        public ScreenTransformGesture TwoFingerMoveGesture;
        public ScreenTransformGesture ManipulationGesture;

        private int _currentSpaceIndex;
        private List<Space> _spaces;
        private List<Animator> _animators;

        #region Unity lifecycle methods

        void Start()
        {
            var appManager = ApplicationManager.Instance;
            appManager.InitializeFramework(ConfigBuilder.GetDefault());

            var mapDataStore = appManager.GetService<IMapDataStore>();
            var stylesheet = appManager.GetService<Stylesheet>();
            var geoOrigin = new GeoCoordinate(Latitude, Longitude);

            // scaled radius of Earth in meters, approx. 1:1000
            const float PlanetRadius = 6371f;
            float aspect = Camera.aspect;

            _spaces = new List<Space>()
            {
                // Orbit
                new Space(new SphereTileController(mapDataStore, stylesheet, ElevationDataType.Flat, new Range<int>(1, 8), PlanetRadius),
                          new SphereGestureStrategy(TwoFingerMoveGesture, ManipulationGesture, PlanetRadius)),
                // Surface
                new Space(new SurfaceTileController(mapDataStore, stylesheet, ElevationDataType.Grid, new Range<int>(9, 15), geoOrigin, aspect, 0.01f, 2000),
                          new SurfaceGestureStrategy(TwoFingerMoveGesture, ManipulationGesture)),
                // Detail
                new Space(new SurfaceTileController(mapDataStore, stylesheet, ElevationDataType.Grid, new Range<int>(16, 16), geoOrigin, aspect, 1f, 3000),
                          new SurfaceGestureStrategy(TwoFingerMoveGesture, ManipulationGesture))
            };

            _animators = new List<Animator>()
            {
                // Orbit
                new SphereAnimator(Pivot, Camera.transform, _spaces[0].TileController),
                // Surface
                new SurfaceAnimator(),
                // Detail
                new SurfaceAnimator()
            };

            OnTransition(null, _spaces[_currentSpaceIndex]);
        }

        void OnEnable()
        {
            TwoFingerMoveGesture.Transformed += TwoFingerTransformHandler;
            ManipulationGesture.Transformed += ManipulationTransformedHandler;
        }

        void OnDisable()
        {
            TwoFingerMoveGesture.Transformed -= TwoFingerTransformHandler;
            ManipulationGesture.Transformed -= ManipulationTransformedHandler;
        }

        void Update()
        {
            if (_animators[_currentSpaceIndex].IsRunningAnimation)
            {
                _animators[_currentSpaceIndex].Update(Time.deltaTime);
                return;
            }

            _spaces[_currentSpaceIndex].TileController.OnUpdate(Planet, Camera.transform.localPosition, Pivot.rotation.eulerAngles);
        }

        void OnGUI()
        {
            if (ShowState)
            {
                var tileController = _spaces[_currentSpaceIndex].TileController;
                var labelText = String.Format("Position: {0}\nDistance: {1:0.#}km\nZoom: {2}",
                    tileController.Coordinate,
                    tileController.DistanceToSurface / 1000f,
                    tileController.ZoomLevel);
                
                GUI.contentColor = Color.red;
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText);
            }
        }

        #endregion

        #region Touch handles

        private void ManipulationTransformedHandler(object sender, EventArgs e)
        {
            _spaces[_currentSpaceIndex].GestureStrategy.OnManipulationTransform(Pivot, Camera.transform);
        }

        private void TwoFingerTransformHandler(object sender, EventArgs e)
        {
            _spaces[_currentSpaceIndex].GestureStrategy.OnTwoFingerTransform(Pivot, Camera.transform);
        }

        #endregion

        #region Space change logic

        /// <summary> Performs transition from one space to another. </summary>
        private void OnTransition(Space from, Space to)
        {
            Camera.fieldOfView = to.TileController.FieldOfView;

            var coordinate = from == null
                ? ApplicationManager.Instance.DefaultCoordinate
                : from.TileController.Coordinate;

            var lodRange = to.TileController.LodRange;

            var zoom = from != null && from.TileController.LodRange.Maximum > lodRange.Maximum
                ? lodRange.Maximum
                : lodRange.Minimum;
            
            _animators[_currentSpaceIndex].AnimateTo(coordinate, zoom, TimeSpan.Zero);
        }

        private class Space
        {
            public readonly TileController TileController;
            public readonly GestureStrategy GestureStrategy;

            public Space(TileController tileController, GestureStrategy gestureStrategy)
            {
                TileController = tileController;
                GestureStrategy = gestureStrategy;
            }
        }

        #endregion
    }
}
