using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
{
    internal class SurfaceSpace : Space
    {
        private readonly GameObject _surface;

        public SurfaceSpace(TileController tileController, GestureStrategy gestureStrategy, Transform surface) :
            base(tileController, gestureStrategy)
        {
            _surface = surface.gameObject;
        }

        public override void Enter()
        {
            Camera.fieldOfView = TileController.FieldOfView;
            Camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Light.transform.localRotation = Quaternion.Euler(90, 0, 0);

            _surface.SetActive(true);
        }

        public override void Leave()
        {
            _surface.SetActive(false);
            TileController.Dispose();
        }
    }
}
