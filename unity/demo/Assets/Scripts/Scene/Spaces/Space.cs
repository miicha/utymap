using System;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
{
    internal abstract class Space : IDisposable
    {
        public readonly TileController TileController;
        public readonly GestureStrategy GestureStrategy;

        protected readonly Transform Pivot;
        protected readonly Camera Camera;
        protected readonly Transform Light;

        public Space(TileController tileController, GestureStrategy gestureStrategy, Transform pivot)
        {
            TileController = tileController;
            GestureStrategy = gestureStrategy;
            
            Pivot = pivot;
            Camera = pivot.Find("Camera").GetComponent<Camera>();
            Light = pivot.Find("Directional Light");
        }

        public abstract void Enter();

        public abstract void Leave();

        /// <inheritdoc />
        public void Dispose()
        {
            TileController.Dispose();
        }
    }
}
