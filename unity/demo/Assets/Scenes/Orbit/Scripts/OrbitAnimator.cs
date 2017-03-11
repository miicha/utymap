using System;
using System.Collections.Generic;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations;
using UtyMap.Unity.Animations.Time;
using Animator = UtyMap.Unity.Animations.Animator;
using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scenes.Orbit.Scripts
{
    internal class OrbitAnimator : Animator
    {
        private Animation _animation;
        private Transform _pivot;
        private Transform _cam;

        /// <inheritdoc />
        public override void AnimateTo(GeoCoordinate coordinate, float height, TimeSpan duration)
        {
            // create position change animation
            var points = new List<Vector3>()
            {
                _cam.transform.position,
                new Vector3(0, 0, -height)
            };
            var pathAnimation = new PathAnimation(
                        _cam.transform,
                        new DecelerateInterpolator(),
                        new UtyMap.Unity.Animations.Path.LinearInterpolator(points),
                        duration);

            // create rotation change animation
            var rotations = new List<Quaternion>()
            {
                _pivot.transform.rotation,
                Quaternion.Euler(new Vector3((float)coordinate.Latitude, 270 - (float) coordinate.Longitude, 0))
            };
            var rotationAnimation = new RotationAnimation(
              _pivot.transform,
              new DecelerateInterpolator(),
              new UtyMap.Unity.Animations.Rotation.LinearInterpolator(rotations),
              duration);

            // compose animation into one composite
            _animation = new CompositeAnimation(new List<Animation>() { pathAnimation, rotationAnimation});
            _animation.Start();
        }

        /// <inheritdoc />
        public override bool HasRunningAnimations { get { return _animation != null && _animation.IsRunning; } }

        /// <inheritdoc />
        public override void CancelAnimations()
        {
            if (_animation != null)
                _animation.Stop();
        }

        #region Unity lifecycle methods

        void Awake()
        {
            _pivot = transform.Find("Pivot");
            _cam = transform.Find("Pivot/Camera");
        }

        void Update()
        {
            // Update camera position
            UpdateAnimation(_animation, Time.deltaTime);
            // TODO update pivot rotation
        }

        #endregion
    }
}
