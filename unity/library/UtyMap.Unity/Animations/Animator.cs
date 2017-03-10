using UnityEngine;

namespace UtyMap.Unity.Animations
{
    /// <summary> Runs animation with proper timing. </summary>
    public class Animator : MonoBehaviour
    {
        /// <summary> Target animation </summary>
        public Animation Animation { get; set; }

        void Update()
        {
            if (Animation == null || Animation.IsFinished)
                return;

            Animation.OnUpdate(transform, UnityEngine.Time.deltaTime);
        }
    }
}
