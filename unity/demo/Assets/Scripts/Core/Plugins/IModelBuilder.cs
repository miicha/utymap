using UtyMap.Unity;

namespace Assets.Scripts.Core.Plugins
{
    /// <summary> Responsible for building Unity's game objects from data received from library. </summary>
    internal interface IModelBuilder
    {
        /// <summary> Builds from element representation. </summary>
        /// <param name="tile"> Tile where element is located. </param>
        /// <param name="element"> Element. </param>
        void BuildElement(Tile tile, Element element);

        /// <summary> Builds from mesh representation. </summary>
        /// <param name="tile"> Tile where mesh is located. </param>
        /// <param name="mesh"> Mesh. </param>
        void BuildMesh(Tile tile, Mesh mesh);
    }
}
