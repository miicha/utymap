using System;
using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Spaces
{
    internal class SurfaceSpace : Space
    {
        private readonly GameObject _surface;

        public SurfaceSpace(TileController tileController, GestureStrategy gestureStrategy, Transform surface) 
            : base(tileController, gestureStrategy)
        {
            _surface = surface.gameObject;
        }

        public override void Enter()
        {
            _surface.SetActive(true);
        }

        public override void Leave()
        {
            _surface.SetActive(false);
            TileController.Dispose();
        }
    }
}
