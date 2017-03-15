using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Space(TileController tileController, GestureStrategy gestureStrategy, Transform pivot, Camera camera)
        {
            TileController = tileController;
            GestureStrategy = gestureStrategy;
            
            Pivot = pivot;
            Camera = camera;
        }

        public abstract void Enter();

        public abstract void Leave();
    }
}
