using Assets.Scenes.Default.Scripts.Gestures;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;

namespace Assets.Scenes.Default.Scripts.Spaces
{
    internal sealed class SphereSpace : Space
    {
        private readonly GameObject _planet;

        public SphereSpace(TileController tileController, GestureStrategy gestureStrategy, Transform pivot, Transform planet) :
            base(tileController, gestureStrategy, pivot)
        {
            _planet = planet.gameObject;
        }

        /// <inheritdoc />
        public override void Enter()
        {
            Camera.fieldOfView = TileController.FieldOfView;
            Camera.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Light.transform.localRotation = Quaternion.Euler(0, 0, 0);

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
