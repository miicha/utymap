using System;
using UtyMap.Unity;

using Animator = UtyMap.Unity.Animations.Animator;

namespace Assets.Scenes.Default.Scripts.Animations
{
    internal sealed class SurfaceAnimator : Animator
    {
        public override void AnimateTo(GeoCoordinate coordinate, float zoom, TimeSpan duration)
        {
            throw new NotImplementedException();
        }
    }
}
