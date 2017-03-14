using System;

namespace UtyMap.Unity.Animations
{
    /// <summary> Provides the way to handle animation. </summary>
    public abstract class Animator
    {
        private Animation _animation;

        /// <summary> Animates to given coordinate using default interpolators. </summary>
        public abstract void AnimateTo(GeoCoordinate coordinate, float zoom, TimeSpan duration);

        /// <summary> Notifies animator about frame update. </summary>
        public void Update(float deltaTime)
        {
            if (_animation == null || !_animation.IsRunning)
                return;

            _animation.OnUpdate(deltaTime);
        }

        /// <summary> True if there is running animation. </summary>
        public bool IsRunningAnimation
        {
            get { return _animation != null && _animation.IsRunning; }
        }

        /// <summary> Cancels outstanding animation. </summary>
        public void Cancel()
        {
            if (_animation != null)
                _animation.Stop();
        }

        /// <summary> Sets animation. </summary>
        /// <remarks> Stops existing animation. </remarks>
        protected void SetAnimation(Animation animation)
        {
            Cancel();
            _animation = animation;
        }
    }
}
