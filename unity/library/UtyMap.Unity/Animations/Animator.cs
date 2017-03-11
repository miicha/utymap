using System;
using UnityEngine;

namespace UtyMap.Unity.Animations
{
    /// <summary> Provides the way to properly update animation. </summary>
    public abstract class Animator : MonoBehaviour
    {
        /// <summary> True if there is at least one running animation scheduled by this animator. </summary>
        public abstract bool HasRunningAnimations { get; }

        /// <summary> Starts animation to given geocoordinate an height. </summary>
        public abstract void AnimateTo(GeoCoordinate coordinate, float height, TimeSpan duration);

        /// <summary> Cancels all outstanding animations. </summary>
        public abstract void CancelAnimations();

        /// <summary> Notifies target animation about update. </summary>
        /// <remarks> Should be called from Update method. </remarks>
        protected void UpdateAnimation(Animation anim, float deltaTime)
        {
            if (anim == null || !anim.IsRunning)
                return;

            anim.OnUpdate(deltaTime);
        }
    }
}
