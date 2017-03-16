using System;
using System.Collections.Generic;
using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Spaces;
using Assets.Scripts.Scene.Tiling;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Primitives;
using Animator = UtyMap.Unity.Animations.Animator;
using Space = Assets.Scripts.Scene.Spaces.Space;

namespace Assets.Scripts.Scene
{
    /// <summary> Provides an entry point for building the map and reacting on user interaction with it. </summary>
    internal class MapBehaviour: MonoBehaviour
    {
        #region User controlled settings

        public double StartLatitude = 52.53171;
        public double StartLongitude = 13.38721;
        [Range(1, 16)]
        public float StartZoom = 1;

        public Transform Planet;
        public Transform Surface;
        public Transform Pivot;
        public Camera Camera;

        public ScreenTransformGesture TwoFingerMoveGesture;
        public ScreenTransformGesture ManipulationGesture;

        public bool ShowPosition = true;
        public bool ShowConsole = false;

        #endregion

        private CompositionRoot _compositionRoot;
        private int _currentSpaceIndex;
        private List<Range<int>> _lodRanges;
        private List<Space> _spaces;
        private List<Animator> _animators;

        #region Unity lifecycle methods

        #region Public properties

        /// <summary> Gets current tile controller. </summary>
        public TileController TileController { get { return _spaces[_currentSpaceIndex].TileController; } }

        #endregion

        void Start()
        {
            _compositionRoot = MapInitTask.Run(ShowConsole);

            var mapDataStore = _compositionRoot.GetService<IMapDataStore>();
            var stylesheet = _compositionRoot.GetService<Stylesheet>();
            var startCoordinate = new GeoCoordinate(StartLatitude, StartLongitude);

            // scaled radius of Earth in meters, approx. 1:1000
            const float planetRadius = 6371f;
            const float surfaceScale = 0.01f;
            const float detailScale = 1f;
            const float maxDistance = 3000;
            float aspect = Camera.aspect;

            _lodRanges = new List<Range<int>>()
            {
                new Range<int>(1, 8),
                new Range<int>(9, 15),
                new Range<int>(16, 16)
            };

            _spaces = new List<Space>()
            {
                new SphereSpace(new SphereTileController(mapDataStore, stylesheet, ElevationDataType.Flat, _lodRanges[0], planetRadius),
                                new SphereGestureStrategy(TwoFingerMoveGesture, ManipulationGesture, planetRadius), Pivot, Planet),
                new SurfaceSpace(new SurfaceTileController(mapDataStore, stylesheet, ElevationDataType.Grid, _lodRanges[1], startCoordinate, aspect, surfaceScale, maxDistance),
                                 new SurfaceGestureStrategy(TwoFingerMoveGesture, ManipulationGesture), Pivot, Surface),
                new SurfaceSpace(new SurfaceTileController(mapDataStore, stylesheet, ElevationDataType.Grid, _lodRanges[2], startCoordinate, aspect, detailScale, maxDistance),
                                 new SurfaceGestureStrategy(TwoFingerMoveGesture, ManipulationGesture), Pivot, Surface)
            };

            _animators = new List<Animator>()
            {
                new SphereAnimator(Pivot, _spaces[0].TileController),
                new SurfaceAnimator(Pivot, _spaces[1].TileController),
                new SurfaceAnimator(Pivot, _spaces[2].TileController)
            };

            OnTransition(startCoordinate, StartZoom);
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

            _spaces[_currentSpaceIndex].TileController.OnUpdate(_currentSpaceIndex == 0 ? Planet : Surface, 
                Camera.transform.localPosition, Pivot.rotation.eulerAngles);
        }

        void OnGUI()
        {
            if (ShowPosition)
            {
                var tileController = _spaces[_currentSpaceIndex].TileController;
                var labelText = String.Format("Position: {0}\nZoom: {1}",
                    tileController.Coordinate,
                    tileController.ZoomLevel);
                
                GUI.contentColor = Color.red;
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), labelText);
            }
        }

        void OnDestroy()
        {
            foreach (var space in _spaces)
                space.Dispose();

            _compositionRoot.Dispose();
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

        /// <summary> Performs transition to unknown space based on zoom level and lod ranges. </summary>
        private void OnTransition(GeoCoordinate coordinate, float zoom)
        {
            for (int i = 0; i < _lodRanges.Count; ++i)
            {
                if (_lodRanges[i].Contains((int) zoom))
                {
                    _currentSpaceIndex = i;
                    break;
                }
            }

            OnTransition(_spaces[_currentSpaceIndex], coordinate, zoom);
        }

        /// <summary> Performs transition from one space to another. </summary>
        private void OnTransition(Space from, Space to)
        {
            // calculate "to"-space parameters
            var coordinate = from.TileController.Coordinate;
            var zoom = from.TileController.LodRange.Maximum > to.TileController.LodRange.Maximum
                ? to.TileController.LodRange.Maximum
                : to.TileController.LodRange.Minimum;

            from.Leave();

            OnTransition(to, coordinate, zoom);
        }

        /// <summary> Performs transition to the new space. </summary>
        private void OnTransition(Space to, GeoCoordinate coordinate, float zoom)
        {
            // prepare scen for "to"-state by making transition
            to.Enter();
            // make an instant animation to given position
            _animators[_currentSpaceIndex].AnimateTo(coordinate, zoom, TimeSpan.Zero);
        }

        #endregion
    }
}
