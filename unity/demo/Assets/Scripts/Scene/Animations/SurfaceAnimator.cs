using System;
using System.Collections.Generic;
using Assets.Scenes.Default.Scripts.Tiling;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations.Time;

using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scenes.Default.Scripts.Animations
{
    internal sealed class SurfaceAnimator : SpaceAnimator
    {
        public SurfaceAnimator(Transform pivot, TileController tileController) :
            base(pivot, tileController, new DecelerateInterpolator())
        {
        }

        protected override Animation CreateAnimationTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            return CreatePathAnimation(duration, new List<Vector3>()
            {
                Camera.transform.localPosition,
                new Vector3(0, 0, TileController.GetHeight(zoom))
            });
        }
    }
}
