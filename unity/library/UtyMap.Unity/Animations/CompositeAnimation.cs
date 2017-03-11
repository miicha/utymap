using System;
using System.Collections.Generic;

namespace UtyMap.Unity.Animations
{
    /// <summary> Provides the way to compose more than one animation. </summary>
    public class CompositeAnimation : Animation
    {
        private readonly IEnumerable<Animation> _animations;

        public CompositeAnimation(IEnumerable<Animation> animations)
        {
            _animations = animations;
        }

        /// <inheritdoc />
        protected internal override void OnStarted()
        {
            foreach (var animation in _animations)
                animation.OnStarted();
        }

        /// <inheritdoc />
        protected internal override void OnStopped(EventArgs e)
        {
            foreach (var animation in _animations)
                animation.OnStopped(e);
        }

        /// <inheritdoc />
        protected internal override void OnUpdate(float deltaTime)
        {
            foreach (var animation in _animations)
                animation.OnUpdate(deltaTime);
        }
    }
}
