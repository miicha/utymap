using System;
using UnityEngine;
using UtyMap.Unity.Animations.Path;
using UtyMap.Unity.Animations.Time;

namespace UtyMap.Unity.Animations
{
    /// <summary> Represents animation. </summary>
    public class Animation
    {
        private readonly ITimeInterpolator _timeInterpolator;
        private readonly IPathInterpolator _pathInterpolator;
        
        private readonly float _duration;
        private readonly bool _isLoop;

        private float _time;

        internal bool IsFinished { get; private set; }

        /// <summary> Called when animation is finished. </summary>
        public event EventHandler Finished;

        public Animation(ITimeInterpolator timeInterpolator, IPathInterpolator pathInterpolator,
            float duration = 2, bool isLoop = false)
        {
            _timeInterpolator = timeInterpolator;
            _pathInterpolator = pathInterpolator;
            _duration = duration;
            _isLoop = isLoop;

            IsFinished = true;
        }

        /// <summary> Starts animation. </summary>
        public void Start()
        {
            _time = 0;
            IsFinished = false;
        }

        /// <summary> Stops animation. </summary>
        public void Cancel()
        {
            IsFinished = true;
            OnFinished(EventArgs.Empty);
        }

        protected virtual void OnFinished(EventArgs e)
        {
            var @event = Finished;
            if (@event != null)
                @event(this, e);
        }

        internal void OnUpdate(Transform transform, float deltaTime)
        {
            _time += deltaTime / _duration;

            if (_time > 1)
            {
                if (_isLoop)
                    _time = 0;
                else
                {
                    Cancel();
                    return;
                }
            }

            // interpolate time
            var t = _timeInterpolator.GetTime(_time);
            // interpolate position
            transform.position = _pathInterpolator.GetPoint(t);
            // TODO interpolate rotation
        }
    }
}
