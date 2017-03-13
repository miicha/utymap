using System;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Gestures
{
    internal class SurfaceGestureStrategy : GestureStrategy
    {
        private float _panSpeed;
        private float _zoomSpeed;

        public SurfaceGestureStrategy(ScreenTransformGesture twoFingerMoveGesture,
                                      ScreenTransformGesture manipulationGesture) :
            base(twoFingerMoveGesture, manipulationGesture)
        {
        }

        public override void OnManipulationTransform(Transform pivot, Transform camera)
        {
            camera.transform.localPosition += Vector3.up * (ManipulationGesture.DeltaScale - 1f) * _zoomSpeed;
        }

        public override void OnTwoFingerTransform(Transform pivot, Transform camera)
        {
            pivot.localPosition += new Vector3(TwoFingerMoveGesture.DeltaPosition.x, 0, TwoFingerMoveGesture.DeltaPosition.y) * _panSpeed;
        }
    }
}
