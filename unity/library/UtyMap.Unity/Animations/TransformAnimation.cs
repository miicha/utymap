using UnityEngine;
using UtyMap.Unity.Animations.Path;
using UtyMap.Unity.Animations.Time;

namespace UtyMap.Unity.Animations
{
    /// <summary> Represents animation for transform. </summary>
    public class TransformAnimation : Animation
    {
        private readonly Transform _transform;
        private readonly ITimeInterpolator _timeInterpolator;
        private readonly IPathInterpolator _pathInterpolator;
        private readonly float _duration;
        private readonly bool _isLoop;
        private float _time;

        public TransformAnimation(Transform transform,
                                  ITimeInterpolator timeInterpolator,
                                  IPathInterpolator pathInterpolator,
                                  float duration = 2, bool isLoop = false)
        {
            _transform = transform;
            _timeInterpolator = timeInterpolator;
            _pathInterpolator = pathInterpolator;
            _duration = duration;
            _isLoop = isLoop;
        }

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

            // interpolate time
            var t = _timeInterpolator.GetTime(_time);
            // interpolate position
            _transform.position = _pathInterpolator.GetPoint(t);
            // TODO interpolate rotation
        }
    }
}
