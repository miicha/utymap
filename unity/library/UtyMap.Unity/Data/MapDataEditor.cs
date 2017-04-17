using System;
using UtyDepend;
using UtyMap.Unity.Infrastructure.IO;
using UtyMap.Unity.Infrastructure.Primitives;

namespace UtyMap.Unity.Data
{
    /// <summary> Specifies behavior of element editor. </summary>
    public interface IMapDataEditor
    {
        /// <summary> Adds element. </summary>
        void Add(MapDataStorageType dataStorageType, Element element, Range<int> levelOfDetails);

        /// <summary> Edits element. </summary>
        void Edit(MapDataStorageType dataStorageType, Element element, Range<int> levelOfDetails);

        /// <summary> Marks element with given id. </summary>
        void Delete(MapDataStorageType dataStorageType, long elementId, Range<int> levelOfDetails);
    }

    /// <summary>
    ///     Default implementation of <see cref="IMapDataEditor"/> which
    ///     works with in-memory store.
    /// </summary>
    internal class MapDataEditor : IMapDataEditor
    {
        private readonly Stylesheet _stylesheet;
        private readonly IPathResolver _resolver;

        [Dependency]
        public MapDataEditor(Stylesheet stylesheet, IPathResolver resolver)
        {
            _stylesheet = stylesheet;
            _resolver = resolver;
        }

        /// <inheritdoc />
        public void Add(MapDataStorageType dataStorageType, Element element, Range<int> levelOfDetails)
        {
            var errorMsg = CoreLibrary.AddElementToStore(dataStorageType,
                _resolver.Resolve(_stylesheet.Path),
                element, levelOfDetails);

            if (!String.IsNullOrEmpty(errorMsg))
                throw new MapDataException(errorMsg);
        }

        /// <inheritdoc />
        public void Edit(MapDataStorageType dataStorageType, Element element, Range<int> levelOfDetails)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Delete(MapDataStorageType dataStorageType, long elementId, Range<int> levelOfDetails)
        {
            throw new NotImplementedException();
        }
    }
}
