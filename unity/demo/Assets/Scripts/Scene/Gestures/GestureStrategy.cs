using Assets.Scripts.Scene.Tiling;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;

namespace Assets.Scripts.Scene.Gestures
{
    /// <summary> Encapsulates gesture processing. </summary>
    internal abstract class GestureStrategy
    {
        protected readonly TileController TileController;
        protected readonly ScreenTransformGesture TwoFingerMoveGesture;
        protected readonly ScreenTransformGesture ManipulationGesture;

        protected GestureStrategy(TileController tileController,
                                  ScreenTransformGesture twoFingerMoveGesture,
                                  ScreenTransformGesture manipulationGesture)
        {
            TileController = tileController;
            TwoFingerMoveGesture = twoFingerMoveGesture;
            ManipulationGesture = manipulationGesture;
        }

        public abstract void OnManipulationTransform(Transform pivot, Transform camera);

        public abstract void OnTwoFingerTransform(Transform pivot, Transform camera);

        /// <summary> Calculates interpolated value base on current zoom level. </summary>
        protected float InterpolateByZoom(float factor = 1f)
        {
            var lodRange = TileController.LodRange;
            var value = (lodRange.Maximum - TileController.ZoomLevel + 1) / (lodRange.Maximum - lodRange.Minimum + 1);
            value = Mathf.Clamp(value, 0, 1f);
            
            return Mathf.Abs(factor - 1.0f) < float.Epsilon
                ? 1.0f - (1.0f - value) * (1.0f - value)
                : 1.0f - Mathf.Pow(1.0f - value, 2 * factor);
        }
    }
}
