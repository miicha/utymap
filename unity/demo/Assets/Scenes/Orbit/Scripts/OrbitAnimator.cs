using UnityEngine;

using Animator = UtyMap.Unity.Animations.Animator;
using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scenes.Orbit.Scripts
{
    internal class OrbitAnimator : Animator
    {
        /// <summary> Target animation </summary>
        public Animation Animation { get; set; }

        void Start()
        {
            
        }

        void Update()
        {
            UpdateAnimation(transform, Animation, Time.deltaTime);
        }
    }
}
