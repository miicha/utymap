using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
{
    internal sealed class SphereSpace : Space
    {
        private readonly GameObject _planet;

        /// <inheritdoc />
        public override SpaceAnimator Animator { get; protected set; }

        public SphereSpace(SphereTileController tileController, SphereGestureStrategy gestureStrategy, Transform planet) :
            base(tileController, gestureStrategy)
        {
            _planet = planet.gameObject;
            Animator = new SphereAnimator(tileController);
        }

        /// <inheritdoc />
        public override void EnterTop()
        {
            base.EnterTop();
            _planet.SetActive(true);
        }

        /// <inheritdoc />
        public override void EnterBottom()
        {
            base.EnterBottom();
            _planet.SetActive(true);
        }

        /// <inheritdoc />
        public override void Leave()
        {
            base.Leave();

            _planet.SetActive(false);
        }
    }
}
