using System;
using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Spaces
{
    internal class SurfaceSpace : Space
    {
        private readonly GameObject _surface;

        public SurfaceSpace(TileController tileController, GestureStrategy gestureStrategy, Transform pivot, Transform surface) :
            base(tileController, gestureStrategy, pivot)
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
