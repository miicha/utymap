using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;
using Animator = UtyMap.Unity.Animations.Animator;

namespace Assets.Scripts.Scene.Spaces
{
    internal sealed class SphereSpace : Space
    {
        private readonly GameObject _planet;

        /// <inheritdoc />
        public override Animator Animator { get; protected set; }

        public SphereSpace(TileController tileController, GestureStrategy gestureStrategy, Transform planet) :
            base(tileController, gestureStrategy)
        {
            _planet = planet.gameObject;
            Animator = new SurfaceAnimator(tileController);
        }

        /// <inheritdoc />
        public override void Enter()
        {
            Camera.fieldOfView = TileController.FieldOfView;
            Camera.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Light.transform.localRotation = Quaternion.Euler(0, 0, 0);

            _planet.SetActive(true);
        }

        /// <inheritdoc />
        public override void Leave()
        {
            _planet.SetActive(false);
            TileController.Dispose();
        }
    }
}
