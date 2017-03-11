using UnityEngine;

namespace UtyMap.Unity.Animations
{
    /// <summary> Provides the way to properly update animation. </summary>
    public abstract class Animator : MonoBehaviour
    {
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
