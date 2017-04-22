using System;
using Assets.Scripts.Scene.Tiling;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;

namespace Assets.Scripts.Scene.Gestures
{
    internal class SurfaceGestureStrategy : GestureStrategy
    {
        private const float PanMaxSpeed = 1f;
        private const float PanMinSpeed = 0.005f;
        private const float PanFactor = 0.05f;

        private const float ZoomMaxSpeed = 100f;
        private const float ZoomMinSpeed = 1f;
        private const float ZoomFactor = 0.5f;

        private const float TintMax = 0f;
        private const float TintMin = -22.5f;
        private const float TintSpeed = 0.1f;

        public SurfaceGestureStrategy(TileController tileController, 
                                      ScreenTransformGesture twoFingerMoveGesture,
                                      ScreenTransformGesture manipulationGesture) :
            base(tileController, twoFingerMoveGesture, manipulationGesture)
        {
        }

        public override void OnManipulationTransform(Transform pivot, Transform camera)
        {
            var speed = Mathf.Max(PanMaxSpeed * InterpolateByZoom(PanFactor), PanMinSpeed);
            pivot.localPosition += new Vector3(ManipulationGesture.DeltaPosition.x, 0, ManipulationGesture.DeltaPosition.y) * -speed;
        }

        public override void OnTwoFingerTransform(Transform pivot, Transform camera)
        {
            if (SetTint(pivot, camera))
                return;

            var speed = Mathf.Max(ZoomMaxSpeed * InterpolateByZoom(ZoomFactor), ZoomMinSpeed);
            pivot.localPosition += Vector3.up * (1 - TwoFingerMoveGesture.DeltaScale) * speed;  
        }

        /// <remarks>
        ///     Experimental. Should be replaced with custom gesture.
        /// </remarks>
        private bool SetTint(Transform pivot, Transform camera)
        {
            var pointer1 = TwoFingerMoveGesture.ActivePointers[0];
            var pointer2 = TwoFingerMoveGesture.ActivePointers[1];

            var delta1 = pointer1.Position - pointer1.PreviousPosition;
            var delta2 = pointer2.Position - pointer2.PreviousPosition;

            // different direction
            if (delta1.y < 0 != delta2.y < 0)
                return false;

            // ignore small values
            if (Mathf.Abs(delta1.y) < 2f || Mathf.Abs(delta2.y) < 2f)
                return false;

            // rather zoom than tint
            if (Mathf.Abs(delta1.x / delta1.y) > 0.5f || Mathf.Abs(delta2.x / delta2.y) > 0.5f)
                return false;

            // fingers are too far
            if (Mathf.Abs(delta1.y - delta2.y) > 1)
                return false;

            var angle = pivot.rotation.eulerAngles.x + (delta1.y + delta2.y) * TintSpeed / 2 - 360;
            pivot.rotation = Quaternion.Euler(Mathf.Clamp(angle, TintMin, TintMax), 0, 0);

            return true;
        }
    }
}
