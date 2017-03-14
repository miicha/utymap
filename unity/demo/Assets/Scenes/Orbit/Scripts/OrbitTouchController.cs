using System;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;
using Animator = UtyMap.Unity.Animations.Animator;

namespace Assets.Scenes.Orbit.Scripts
{
    internal sealed class OrbitTouchController : MonoBehaviour
    {
        /// <summary> Value depends on radius and camera settings. </summary>
        private const float MagicAngleLimitCoeff = 2;

        public ScreenTransformGesture TwoFingerMoveGesture;
        public ScreenTransformGesture ManipulationGesture;

        public float RotationSpeed = 100f;
        public float ZoomSpeed = 20f;

        private Transform _pivot;
        private Transform _cam;
        private Transform _light;

        private OrbitAnimator _animator;

        private void Awake()
        {
            _pivot = transform.Find("Pivot");
            _cam = transform.Find("Pivot/Camera");
            _light = transform.Find("Directional Light");

            //_animator = FindObjectOfType<OrbitAnimator>();
        }

        void Update()
        {
            if (_animator.HasRunningAnimations)
                _light.localRotation = _pivot.localRotation;
        }

        private void OnEnable()
        {
            TwoFingerMoveGesture.Transformed += twoFingerTransformHandler;
            ManipulationGesture.Transformed += manipulationTransformedHandler;
        }

        private void OnDisable()
        {
            TwoFingerMoveGesture.Transformed -= twoFingerTransformHandler;
            ManipulationGesture.Transformed -= manipulationTransformedHandler;
        }

        private void manipulationTransformedHandler(object sender, EventArgs e)
        {
            CancelAnimations();

            var rotation = Quaternion.Euler(
                          ManipulationGesture.DeltaPosition.y / Screen.height * RotationSpeed,
                          -ManipulationGesture.DeltaPosition.x / Screen.width * RotationSpeed,
                          ManipulationGesture.DeltaRotation);

            SetRotation(rotation);
            _light.localRotation = _pivot.localRotation;
        }

        private void twoFingerTransformHandler(object sender, EventArgs e)
        {
            CancelAnimations();

            var rotation = Quaternion.Euler(
              TwoFingerMoveGesture.DeltaPosition.y / Screen.height * RotationSpeed,
               -TwoFingerMoveGesture.DeltaPosition.x / Screen.width * RotationSpeed,
               TwoFingerMoveGesture.DeltaRotation);

            SetRotation(rotation);
            _cam.transform.localPosition += Vector3.forward * (TwoFingerMoveGesture.DeltaScale - 1f) * ZoomSpeed;
        }

        /// <summary> Sets rotation to pivot with limit. </summary>
        private void SetRotation(Quaternion rotation)
        {
            _pivot.localRotation *= rotation;
            _pivot.localEulerAngles = new Vector3(
                LimitAngle(_pivot.eulerAngles.x, CalculateLimit()),
                _pivot.eulerAngles.y,
                LimitAngle(_pivot.eulerAngles.z, 10));
        }

        private static float LimitAngle(float angle, float limit)
        {
            angle = angle > 180 ? angle - 360 : angle;
            var sign = angle < 0 ? -1 : 1;
            return limit - sign * angle > 0 ? angle : sign * limit;
        }

        private float CalculateLimit()
        {
            var radius = OrbitCameraController.TileController.Radius;
            var pole = new Vector3(0, radius, 0);
            var center = Vector3.zero;
            var position = new Vector3(0, 0, Vector3.Distance(_cam.transform.position, center));

            var a = Vector3.Distance(position, center);
            var b = Vector3.Distance(position, pole);
            var c = radius;

            var cosine = (a * a + b * b - c * c) / (2 * a * b);
            return (float) Math.Acos(cosine) * Mathf.Rad2Deg * MagicAngleLimitCoeff;
        }

        private void CancelAnimations()
        {
            if (_animator != null)
                _animator.CancelAnimations();
        }
    }
}
