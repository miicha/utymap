using System;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Gestures
{
    internal class SphereGestureStrategy : GestureStrategy
    {
        /// <summary> Value depends on radius and camera settings. </summary>
        private const float MagicAngleLimitCoeff = 2;

        private const float RotationSpeed = 100f;
        
        private const float ZoomSpeed = 200f;
        
        private readonly float _radius;

        public SphereGestureStrategy(ScreenTransformGesture twoFingerMoveGesture,
                                     ScreenTransformGesture manipulationGesture,
                                     float radius) :
            base(twoFingerMoveGesture, manipulationGesture)
        {
            _radius = radius;
        }

        /// <inheritdoc />
        public override void OnManipulationTransform(Transform pivot, Transform camera)
        {
            var rotation = Quaternion.Euler(
                          ManipulationGesture.DeltaPosition.y / Screen.height * RotationSpeed,
                          -ManipulationGesture.DeltaPosition.x / Screen.width * RotationSpeed,
                          ManipulationGesture.DeltaRotation);

            SetRotation(pivot, camera, rotation);
        }

        /// <inheritdoc />
        public override void OnTwoFingerTransform(Transform pivot, Transform camera)
        {
            var rotation = Quaternion.Euler(
            TwoFingerMoveGesture.DeltaPosition.y / Screen.height * RotationSpeed,
             -TwoFingerMoveGesture.DeltaPosition.x / Screen.width * RotationSpeed,
             TwoFingerMoveGesture.DeltaRotation);

            SetRotation(pivot, camera, rotation);

            camera.transform.localPosition += Vector3.forward * (TwoFingerMoveGesture.DeltaScale - 1f) * ZoomSpeed;
        }

        /// <summary> Sets rotation to pivot with limit. </summary>
        private void SetRotation(Transform pivot, Transform camera, Quaternion rotation)
        {
            pivot.localRotation *= rotation;
            pivot.localEulerAngles = new Vector3(
                LimitAngle(pivot.eulerAngles.x, CalculateLimit(camera)),
                pivot.eulerAngles.y,
                LimitAngle(pivot.eulerAngles.z, 10));
        }

        private static float LimitAngle(float angle, float limit)
        {
            angle = angle > 180 ? angle - 360 : angle;
            var sign = angle < 0 ? -1 : 1;
            return limit - sign * angle > 0 ? angle : sign * limit;
        }

        private float CalculateLimit(Transform camera)
        {
            var pole = new Vector3(0, _radius, 0);
            var center = Vector3.zero;
            var position = new Vector3(0, 0, Vector3.Distance(camera.transform.position, center));

            var a = Vector3.Distance(position, center);
            var b = Vector3.Distance(position, pole);
            var c = _radius;

            var cosine = (a * a + b * b - c * c) / (2 * a * b);
            return (float) Math.Acos(cosine) * Mathf.Rad2Deg * MagicAngleLimitCoeff;
        }
    }
}
