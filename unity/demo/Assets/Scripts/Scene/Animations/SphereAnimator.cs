using System;
using System.Collections.Generic;
using Assets.Scripts.Scene.Tiling;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Animations;
using UtyMap.Unity.Animations.Time;
using Animation = UtyMap.Unity.Animations.Animation;

namespace Assets.Scripts.Scene.Animations
{
    /// <summary> Handles sphere animations. </summary>
    internal sealed class SphereAnimator : SpaceAnimator
    {
        public SphereAnimator(TileController tileController) :
            base(tileController, new DecelerateInterpolator())
        {
        }

        /// <inheritdoc />
        protected override Animation CreateAnimationTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            return new CompositeAnimation(new List<Animation>
            {
                CreatePathAnimation(Camera, duration, new List<Vector3>()
                {
                    Camera.localPosition,
                    new Vector3(0, 0, -TileController.GetHeight(zoom))
                }),
                CreateRotationAnimation(Pivot, duration, new List<Quaternion>()
                {
                    Pivot.rotation,
                    Quaternion.Euler(new Vector3((float) coordinate.Latitude, 270 - (float) coordinate.Longitude, 0))
                })
            });
        }
    }
}
