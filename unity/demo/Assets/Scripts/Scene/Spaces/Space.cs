using System;
using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
{
    internal abstract class Space : IDisposable
    {
        public readonly TileController TileController;
        public readonly GestureStrategy GestureStrategy;
        public abstract SpaceAnimator Animator { get; protected set; }

        protected readonly Transform Pivot;
        protected readonly Camera Camera;
        protected readonly Transform Light;

        public Space(TileController tileController, GestureStrategy gestureStrategy)
        {
            TileController = tileController;
            GestureStrategy = gestureStrategy;

            Pivot = tileController.Pivot;
            Camera = tileController.Pivot.Find("Camera").GetComponent<Camera>();
            Light = tileController.Pivot.Find("Directional Light");
        }

        /// <summary> Simply resets pivot, camera, light to zero values. </summary>
        protected void ResetTransforms()
        {
            Camera.transform.localPosition = Vector3.zero;
            Camera.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Pivot.localPosition = Vector3.zero;
            Pivot.localRotation = Quaternion.Euler(0, 0, 0);
            Light.localPosition = Vector3.zero;
            Light.localRotation = Quaternion.Euler(0, 0, 0);
        }

        /// <summary> Performs init actions. </summary>
        public virtual void Enter()
        {
            ResetTransforms();
            Camera.fieldOfView = TileController.FieldOfView;
        }

        /// <summary> Performs cleanup actions. </summary>
        public virtual void Leave()
        {
            Animator.Cancel();
            TileController.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            TileController.Dispose();
        }
    }
}
