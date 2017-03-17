using System;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;
using Animator = UtyMap.Unity.Animations.Animator;

namespace Assets.Scripts.Scene.Spaces
{
    internal abstract class Space : IDisposable
    {
        public readonly TileController TileController;
        public readonly GestureStrategy GestureStrategy;
        public abstract Animator Animator { get; protected set; }

        protected readonly Camera Camera;
        protected readonly Transform Light;

        public Space(TileController tileController, GestureStrategy gestureStrategy)
        {
            TileController = tileController;
            GestureStrategy = gestureStrategy;

            Camera = tileController.Pivot.Find("Camera").GetComponent<Camera>();
            Light = tileController.Pivot.Find("Directional Light");
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
