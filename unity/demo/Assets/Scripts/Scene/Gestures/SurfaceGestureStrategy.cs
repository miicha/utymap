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
            var speed = Mathf.Max(ZoomMaxSpeed * InterpolateByZoom(ZoomFactor), ZoomMinSpeed);
            pivot.localPosition += Vector3.up * (1 - TwoFingerMoveGesture.DeltaScale) * speed;
        }
    }
}
