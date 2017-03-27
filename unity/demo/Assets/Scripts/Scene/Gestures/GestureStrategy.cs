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
    }
}
