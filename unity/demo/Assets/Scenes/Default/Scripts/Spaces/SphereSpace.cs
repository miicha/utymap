using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Spaces
{
    internal sealed class SphereSpace : Space
    {
        private readonly GameObject _planet;

        public SphereSpace(TileController tileController, GestureStrategy gestureStrategy, Transform planet) :
            base(tileController, gestureStrategy)
        {
            _planet = planet.gameObject;
        }

        /// <inheritdoc />
        public override void Enter()
        {
            _planet.SetActive(true);
        }

        /// <inheritdoc />
        public override void Leave()
        {
            _planet.SetActive(false);
            TileController.Dispose();
        }
    }
}
