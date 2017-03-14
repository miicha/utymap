using System;
using System.Collections.Generic;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations;
using UtyMap.Unity.Animations.Time;

using Animation = UtyMap.Unity.Animations.Animation;
using Animator = UtyMap.Unity.Animations.Animator;

namespace Assets.Scenes.Default.Scripts.Animations
{
    /// <summary> Handles sphere animations. </summary>
    internal sealed class SphereAnimator : Animator
    {
        private readonly Transform _pivot;
        private readonly Transform _camera;

        private readonly TileController _tileController;
        private readonly ITimeInterpolator _timeInterpolator;

        public SphereAnimator(Transform pivot, Transform camera, TileController tileController)
        {
            _pivot = pivot;
            _camera = camera;
            _tileController = tileController;

            _timeInterpolator = new DecelerateInterpolator();
        }

        /// <inheritdoc />
        public override void AnimateTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            var animation = new CompositeAnimation(new List<Animation>
            {
                CreatePathAnimation(zoom, duration), 
                CreateRotationAnimation(coordinate, duration)
            });

            SetAnimation(animation);

            animation.Start();
        }

        private PathAnimation CreatePathAnimation(float zoom, TimeSpan duration)
        {
            var points = new List<Vector3>()
            {
                _camera.localPosition,
                new Vector3(0, 0, -_tileController.GetHeight(zoom))
            };
            return new PathAnimation(
                _camera.transform,
                _timeInterpolator,
                new UtyMap.Unity.Animations.Path.LinearInterpolator(points),
                duration);
        }

        private RotationAnimation CreateRotationAnimation(GeoCoordinate coordinate, TimeSpan duration)
        {
            var rotations = new List<Quaternion>()
            {
                _pivot.rotation,
                Quaternion.Euler(new Vector3((float) coordinate.Latitude, 270 - (float) coordinate.Longitude, 0))
            };
            return new RotationAnimation(
                _pivot.transform,
                _timeInterpolator,
                new UtyMap.Unity.Animations.Rotation.LinearInterpolator(rotations),
                duration);
        }
    }
}
