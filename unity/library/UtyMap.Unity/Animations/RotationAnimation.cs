using UnityEngine;
using UtyMap.Unity.Animations.Rotation;
using UtyMap.Unity.Animations.Time;

namespace UtyMap.Unity.Animations
{
    public class RotationAnimation : TransformAnimation
    {
        private readonly IRotationInterpolator _rotationInterpolator;

        public RotationAnimation(Transform transform,
                                 ITimeInterpolator timeInterpolator,
                                 IRotationInterpolator rotationInterpolator,
                                 float duration = 2, bool isLoop = false)
            : base(transform, timeInterpolator, duration, isLoop)
        {
            _rotationInterpolator = rotationInterpolator;
        }

        protected override void UpdateTransform(Transform transform, float time)
        {
            transform.rotation = _rotationInterpolator.GetRotation(time);
        }
    }
}
