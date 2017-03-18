using System;
using System.Collections.Generic;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations;
using UtyMap.Unity.Animations.Time;
using Animator = UtyMap.Unity.Animations.Animator;
using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scripts.Scene.Animations
{
    internal abstract class SpaceAnimator : Animator
    {
        protected readonly Transform Pivot;
        protected readonly Transform Camera;
        protected readonly TileController TileController;

        private readonly ITimeInterpolator _timeInterpolator;

        protected abstract Animation CreateAnimationTo(GeoCoordinate coordinate, float zoom, TimeSpan duration);

        protected SpaceAnimator(TileController tileController, ITimeInterpolator timeInterpolator)
        {
            Pivot = tileController.Pivot;
            Camera = Pivot.Find("Camera").transform;
            TileController = tileController;
            _timeInterpolator = timeInterpolator;
        }

        /// <inheritdoc />
        public override void AnimateTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            SetAnimation(CreateAnimationTo(coordinate, zoom, duration));
            Start();
        }

        protected PathAnimation CreatePathAnimation(Transform target, TimeSpan duration, IEnumerable<Vector3> points)
        {
            return new PathAnimation(target, _timeInterpolator,
                new UtyMap.Unity.Animations.Path.LinearInterpolator(points),
                duration);
        }

        protected RotationAnimation CreateRotationAnimation(Transform target, TimeSpan duration, IEnumerable<Quaternion> rotations)
        {
            return new RotationAnimation(target, _timeInterpolator,
                new UtyMap.Unity.Animations.Rotation.LinearInterpolator(rotations),
                duration);
        }
    }
}
