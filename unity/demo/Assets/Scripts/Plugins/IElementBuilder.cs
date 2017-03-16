using UnityEngine;
using UtyMap.Unity;

namespace Assets.Scripts.Scene.Builders
{
    /// <summary> Provides the way to build custom representation of map data. </summary>
    public interface IElementBuilder
    {
        /// <summary> Builds gameobject from Element for given tile. </summary>
        GameObject Build(Tile tile, Element element);
    }
}