using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
{
    internal sealed class SurfaceSpace : Space
    {
        private readonly GameObject _surface;

        /// <inheritdoc />
        public override SpaceAnimator Animator { get; protected set; }

        public SurfaceSpace(SurfaceTileController tileController, SurfaceGestureStrategy gestureStrategy, Transform surface) :
            base(tileController, gestureStrategy)
        {
            _surface = surface.gameObject;
            Animator = new SurfaceAnimator(tileController);
        }

        public override void Enter()
        {
            base.Enter();

            Camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Light.transform.localRotation = Quaternion.Euler(90, 0, 0);

            _surface.SetActive(true);
        }

        public override void Leave()
        {
            base.Leave();

            _surface.SetActive(false);
        }
    }
}
