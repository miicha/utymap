using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
{
    internal sealed class SphereSpace : Space
    {
        /// <inheritdoc />
        public override SpaceAnimator Animator { get; protected set; }

        public SphereSpace(SphereTileController tileController, SphereGestureStrategy gestureStrategy, Transform planet) :
            base(tileController, gestureStrategy, planet)
        {
            Animator = new SphereAnimator(tileController);
        }
    }
}
