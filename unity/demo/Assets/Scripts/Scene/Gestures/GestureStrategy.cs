using TouchScript.Gestures.TransformGestures;
using UnityEngine;

namespace Assets.Scripts.Scene.Gestures
{
    /// <summary> Encapsulates gesture processing. </summary>
    internal abstract class GestureStrategy
    {
        protected readonly ScreenTransformGesture TwoFingerMoveGesture;
        protected readonly ScreenTransformGesture ManipulationGesture;

        protected GestureStrategy(ScreenTransformGesture twoFingerMoveGesture,
                                  ScreenTransformGesture manipulationGesture)
        {
            TwoFingerMoveGesture = twoFingerMoveGesture;
            ManipulationGesture = manipulationGesture;
        }

        public abstract void OnManipulationTransform(Transform pivot, Transform camera);

        public abstract void OnTwoFingerTransform(Transform pivot, Transform camera);
    }
}
