using UnityEngine;

namespace UtyMap.Unity.Animations
{
    /// <summary> Implements basic animation behaviour. </summary>
    public abstract class Animator : MonoBehaviour
    {
        protected virtual void UpdateAnimation(Transform trans, Animation anim, float deltaTime)
        {
            if (anim == null || anim.IsFinished)
                return;

            anim.OnUpdate(trans, deltaTime);
        }
    }
}
