using Assets.Scripts.Core.Plugins;
using UnityEngine;
using UtyMap.Unity;

namespace Assets.Scripts.Scenes.Map.Plugins
{
    class ImportElementBuilder : IElementBuilder
    {
        public GameObject Build(Tile tile, Element element)
        {
            var modelName = @"Prefabs/" + element.Styles["model"];
            var gameObject = GameObject.Instantiate(Resources.Load<GameObject>(modelName));
            
            // TODO calculate centroid for polygons
            gameObject.transform.position = tile.Projection.Project(element.Geometry[0], element.Heights[0]);
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            return gameObject;
        }
    }
}
