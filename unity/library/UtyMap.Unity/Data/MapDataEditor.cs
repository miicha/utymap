using System;
using UtyDepend;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyRx;

namespace UtyMap.Unity.Data
{
    /// <summary> Specifies behavior of element editor. </summary>
    public interface IMapDataEditor
    {
        /// <summary> Adds element. </summary>
        void Add(string storageKey, Element element, Range<int> levelOfDetails);

        /// <summary> Edits element. </summary>
        void Edit(string storageKey, Element element, Range<int> levelOfDetails);

        /// <summary> Marks element with given id. </summary>
        void Delete(string storageKey, long elementId, Range<int> levelOfDetails);
    }

    /// <summary>
    ///     Default implementation of <see cref="IMapDataEditor"/> which works with in-memory store.
    /// </summary>
    internal class MapDataEditor : IMapDataEditor
    {
        private readonly IMapDataLibrary _mapDataLibrary;
        private readonly Stylesheet _stylesheet;

        [Dependency]
        public MapDataEditor(IMapDataLibrary mapDataLibrary, Stylesheet stylesheet)
        {
            _mapDataLibrary = mapDataLibrary;
            _stylesheet = stylesheet;
        }

        /// <inheritdoc />
        public void Add(string storageKey, Element element, Range<int> levelOfDetails)
        {
            _mapDataLibrary.AddTo(storageKey, element, _stylesheet, levelOfDetails, new CancellationToken())
                .Wait();
        }

        /// <inheritdoc />
        public void Edit(string storageKey, Element element, Range<int> levelOfDetails)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Delete(string storageKey, long elementId, Range<int> levelOfDetails)
        {
            throw new NotImplementedException();
        }
    }
}
