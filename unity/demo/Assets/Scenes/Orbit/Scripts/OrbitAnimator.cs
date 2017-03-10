using System;
using System.Collections.Generic;
using UnityEngine;
using UtyMap.Unity.Animations.Time;
using Animator = UtyMap.Unity.Animations.Animator;
using Animation = UtyMap.Unity.Animations.Animation;
using LinearInterpolator = UtyMap.Unity.Animations.Path.LinearInterpolator;

namespace Assets.Scenes.Orbit.Scripts
{
    internal class OrbitAnimator : Animator
    {
        private Animation _animation;

        public void AnimateTo(Vector3 point, TimeSpan duration)
        {
            _animation = new Animation(
                new DecelerateInterpolator(),
                new LinearInterpolator(new List<Vector3>() { transform.position, point }),
                (float) duration.TotalSeconds);
            _animation.Start();
        }

        void Update()
        {
            UpdateAnimation(transform, _animation, Time.deltaTime);
        }

        public void Cancel()
        {
            if (_animation != null)
                _animation.Cancel();
        }
    }
}
