using System.Collections.Generic;
using Assets.Scripts.Core.Plugins;
using UnityEngine;
using UtyDepend;
using UtyMap.Unity;
using Mesh = UtyMap.Unity.Mesh;

namespace Assets.Scripts.Scenes.Map.Plugins
{
    /// <summary> Responsible for building Unity game objects from meshes and elements. </summary>
    internal class UnityModelBuilder : IModelBuilder
    {
        private readonly MaterialProvider _materialProvider;

        private Dictionary<string, IElementBuilder> _elementBuilders = new Dictionary<string, IElementBuilder>();

        [Dependency]
        public UnityModelBuilder(MaterialProvider materialProvider)
        {
            _materialProvider = materialProvider;

            // register custom builders here.
            _elementBuilders.Add("info", new PlaceElementBuilder(_materialProvider));
            _elementBuilders.Add("label", new LabelElementBuilder());
            _elementBuilders.Add("import", new ImportElementBuilder());
        }

        /// <inheritdoc />
        public void BuildElement(Tile tile, Element element)
        {
            foreach (var pair in _elementBuilders)
            {
                if (!element.Styles["builders"].Contains(pair.Key))
                    continue;

                var gameObject = pair.Value.Build(tile, element);
                if (gameObject.transform.parent == null)
                    gameObject.transform.parent = tile.GameObject.transform;
            }
        }

        /// <inheritdoc />
        public void BuildMesh(Tile tile, Mesh mesh)
        {
            var gameObject = new GameObject(mesh.Name);

            var uMesh = new UnityEngine.Mesh();
            uMesh.vertices = mesh.Vertices;
            uMesh.triangles = mesh.Triangles;
            uMesh.colors = mesh.Colors;
            uMesh.uv = mesh.Uvs;
            uMesh.uv2 = mesh.Uvs2;
            uMesh.uv3 = mesh.Uvs3;

            uMesh.RecalculateNormals();

            gameObject.isStatic = true;
            gameObject.AddComponent<MeshFilter>().mesh = uMesh;
            // TODO use TextureIndex to select proper material.
            string texture = tile.QuadKey.LevelOfDetail == 16
                ? @"Materials/SurfaceTexturedColored"
                : @"Materials/SurfaceColored";

            gameObject.AddComponent<MeshRenderer>().sharedMaterial = _materialProvider.GetSharedMaterial(texture);
            gameObject.transform.parent = tile.GameObject.transform;
        }
    }
}
