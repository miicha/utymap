using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;

namespace Assets.Scripts.Scene.Spaces
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
