using Assets.Scripts.Plugins;
using Assets.Scripts.Scene.Animations;
using Assets.Scripts.Scene.Gestures;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;
using UtyMap.Unity;

namespace Assets.Scripts.Scene.Spaces
{
    internal sealed class SurfaceSpace : Space
    {
        private readonly SurfaceTileController _tileController;

        /// <inheritdoc />
        public override SpaceAnimator Animator { get; protected set; }

        public SurfaceSpace(SurfaceTileController tileController, SurfaceGestureStrategy gestureStrategy, 
            Transform surface, MaterialProvider materialProvider) :
                base(tileController, gestureStrategy, surface, materialProvider)
        {
            _tileController = tileController;
            Animator = new SurfaceAnimator(tileController);
        }

        protected override void OnEnter(GeoCoordinate coordinate, bool isFromTop)
        {
            Camera.GetComponent<Skybox>().material = MaterialProvider.GetSharedMaterial(@"Skyboxes/Surface/Skybox");

            Camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Light.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Pivot.localPosition = new Vector3(0, isFromTop
                ? TileController.HeightRange.Maximum
                : TileController.HeightRange.Minimum,
                0);

            // surface specific
            _tileController.MoveGeoOrigin(coordinate);
            TileController.Update(Target);
        }

        protected override void OnExit()
        {
        }
    }
}
