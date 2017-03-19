using System;
using System.Collections.Generic;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations.Time;
using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scripts.Scene.Animations
{
    /// <summary> Handles surface animations. </summary>
    internal sealed class SurfaceAnimator : SpaceAnimator
    {
        public SurfaceAnimator(TileController tileController) :
            base(tileController, new DecelerateInterpolator())
        {
        }

        /// <inheritdoc />
        protected override Animation CreateAnimationTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            var position = Pivot.localPosition;
            return CreatePathAnimation(Pivot, duration, new List<Vector3>()
            {
                position,
                new Vector3(position.x, TileController.GetHeight(zoom), position.z)
            });
        }
    }
}
