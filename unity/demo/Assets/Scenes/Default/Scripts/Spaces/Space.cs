using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;

namespace Assets.Scenes.Default.Scripts.Spaces
{
    internal abstract class Space
    {
        public readonly TileController TileController;
        public readonly GestureStrategy GestureStrategy;

        public Space(TileController tileController, GestureStrategy gestureStrategy)
        {
            TileController = tileController;
            GestureStrategy = gestureStrategy;
        }

        public abstract void Enter();

        public abstract void Leave();
    }
}
