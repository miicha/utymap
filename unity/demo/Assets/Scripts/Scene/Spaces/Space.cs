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

        protected readonly Transform Target;
        protected readonly Transform Pivot;
        protected readonly Camera Camera;
        protected readonly Transform Light;

        public Space(TileController tileController, GestureStrategy gestureStrategy, Transform target)
        {
            Target = target;
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

        /// <summary> Enters space from top. </summary>
        public virtual void EnterTop() { Enter(); }

        /// <summary> Enters space from bottom. </summary>
        public virtual void EnterBottom() { Enter(); }

        /// <summary> Notifies space about time since last update. </summary>
        public void Update(float deltaTime)
        {
            Animator.Update(deltaTime);
            TileController.Update(Target);
        }

        /// <summary> Performs cleanup actions. </summary>
        public virtual void Leave()
        {
            Animator.Cancel();
            TileController.Dispose();
            Target.gameObject.SetActive(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            TileController.Dispose();
        }

        /// <summary> Performs init actions. </summary>
        private void Enter()
        {
            Target.gameObject.SetActive(true);
            ResetTransforms();
            Camera.fieldOfView = TileController.FieldOfView;
        }
    }
}
