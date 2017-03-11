using UnityEngine;
using UtyMap.Unity.Animations.Time;

namespace UtyMap.Unity.Animations
{
    /// <summary> Represents animation for updating transform. </summary>
    public abstract class TransformAnimation : Animation
    {
        private readonly Transform _transform;
        private readonly ITimeInterpolator _timeInterpolator;
        private readonly float _duration;
        private readonly bool _isLoop;
        private float _time;

        protected TransformAnimation(Transform transform,
                                     ITimeInterpolator timeInterpolator,
                                     float duration = 2, bool isLoop = false)
        {
            _transform = transform;
            _timeInterpolator = timeInterpolator;
            _duration = duration;
            _isLoop = isLoop;
        }

        /// <summary> Updates transform using interpolated time. </summary>
        protected abstract void UpdateTransform(Transform transform, float time);

        /// <inheritdoc />
        protected internal override void OnStarted()
        {
            _time = 0;
        }

        /// <inheritdoc />
        protected internal override void OnUpdate(float deltaTime)
        {
            _time += deltaTime / _duration;

            if (_time > 1)
            {
                if (_isLoop)
                    _time = 0;
                else
                {
                    Stop();
                    return;
                }
            }

            UpdateTransform(_transform, _timeInterpolator.GetTime(_time));
        }
    }
}
