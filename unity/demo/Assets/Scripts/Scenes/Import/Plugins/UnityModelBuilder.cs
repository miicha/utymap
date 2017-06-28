using Assets.Scripts.Core.Plugins;
using UnityEngine;
using UtyDepend;
using UtyMap.Unity;
using Mesh = UtyMap.Unity.Mesh;

namespace Assets.Scripts.Scenes.Import.Plugins
{
    /// <summary> Provides the way to build meshes received from library. </summary>
    internal class UnityModelBuilder: IModelBuilder
    {
        private readonly MaterialProvider _materialProvider;

        [Dependency]
        public UnityModelBuilder(MaterialProvider materialProvider)
        {
            _materialProvider = materialProvider;
        }

        /// <inheritdoc />
        public void BuildElement(Tile tile, Element element)
        {
            // NOTE ignore for this example
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
