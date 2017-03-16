using System;
using System.Collections.Generic;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations;
using UtyMap.Unity.Animations.Time;

using Animator = UtyMap.Unity.Animations.Animator;
using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scenes.Default.Scripts.Animations
{
    internal abstract class SpaceAnimator : Animator
    {
        protected readonly Transform Pivot;
        protected readonly Camera Camera;
        protected readonly TileController TileController;

        private readonly ITimeInterpolator _timeInterpolator;

        protected abstract Animation CreateAnimationTo(GeoCoordinate coordinate, float zoom, TimeSpan duration);

        protected SpaceAnimator(Transform pivot, TileController tileController, ITimeInterpolator timeInterpolator)
        {
            Pivot = pivot;
            Camera = pivot.Find("Camera").GetComponent<Camera>();
            TileController = tileController;
            _timeInterpolator = timeInterpolator;
        }


        /// <inheritdoc />
        public override void AnimateTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            var animation = CreateAnimationTo(coordinate, zoom, duration);
            SetAnimation(animation);
            animation.Start();
        }

        protected PathAnimation CreatePathAnimation(TimeSpan duration, IEnumerable<Vector3> points)
        {
            return new PathAnimation(
                Camera.transform,
                _timeInterpolator,
                new UtyMap.Unity.Animations.Path.LinearInterpolator(points),
                duration);
        }

        protected RotationAnimation CreateRotationAnimation(TimeSpan duration, IEnumerable<Quaternion> rotations)
        {
            return new RotationAnimation(
                Pivot.transform,
                _timeInterpolator,
                new UtyMap.Unity.Animations.Rotation.LinearInterpolator(rotations),
                duration);
        }
    }
}
