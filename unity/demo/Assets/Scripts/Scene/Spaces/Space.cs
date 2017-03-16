using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Spaces
{
    internal abstract class Space
    {
        public readonly TileController TileController;
        public readonly GestureStrategy GestureStrategy;

        protected readonly Transform Pivot;
        protected readonly Camera Camera;
        protected readonly Transform Light;

        public Space(TileController tileController, GestureStrategy gestureStrategy, Transform pivot)
        {
            TileController = tileController;
            GestureStrategy = gestureStrategy;
            
            Pivot = pivot;
            Camera = pivot.Find("Camera").GetComponent<Camera>();
            Light = pivot.Find("Directional Light");
        }

        public abstract void Enter();

        public abstract void Leave();
    }
}
