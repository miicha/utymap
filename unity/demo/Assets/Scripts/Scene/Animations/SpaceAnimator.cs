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

        /// <summary> Keeps track of the last animation. </summary>
        private AnimationState _state;

        /// <summary> Creates animation for given coordinate and zoom level with given duration. </summary>
        protected abstract Animation CreateAnimationTo(GeoCoordinate coordinate, float zoom, TimeSpan duration);

        protected SpaceAnimator(TileController tileController, ITimeInterpolator timeInterpolator)
        {
            Pivot = tileController.Pivot;
            Camera = Pivot.Find("Camera").transform;
            TileController = tileController;
            _timeInterpolator = timeInterpolator;
        }

        /// <inheritdoc />
        public sealed override void AnimateTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            _state = new AnimationState(coordinate, zoom, duration);

            SetAnimation(CreateAnimationTo(coordinate, zoom, duration));
            Start();
        }

        /// <summary> Continues animation.. </summary>
        public void ContinueFrom(SpaceAnimator other)
        {
            var state = other._state;
            AnimateTo(state.Coordinate, state.Zoom, TimeSpan.FromSeconds(state.TimeLeft));
        }

        /// <inheritdoc />
        protected sealed override void OnAnimationUpdate(float deltaTime)
        {
            _state.OnUpdate(deltaTime);
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

        #region Nested classes

        /// <summary> Keeps state of last animation. </summary>
        /// <remarks> Introduced to support animation transition from one space to another. </remarks>
        struct AnimationState
        {
            public readonly GeoCoordinate Coordinate;
            public readonly float Zoom;
            public float TimeLeft;

            public AnimationState(GeoCoordinate coordinate, float zoom, TimeSpan duration)
            {
                Coordinate = coordinate;
                Zoom = zoom;
                TimeLeft = (float) duration.TotalSeconds;
            }

            public void OnUpdate(float deltaTime) { TimeLeft -= deltaTime; }
        }

        #endregion
    }
}
