using UnityEngine;

namespace UtyMap.Unity.Animations.Rotation
{
    public interface IRotationInterpolator
    {
        Quaternion GetRotation(float time);
    }
}
