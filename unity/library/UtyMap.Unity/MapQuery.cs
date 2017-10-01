using UtyMap.Unity.Infrastructure.Primitives;

namespace UtyMap.Unity
{
    /// <summary> Defines query to map data. </summary>
    public struct MapQuery
    {
        /// <summary> Logical "not": result should not include any of these terms. </summary>
        public string NotTerms { get; set; }

        /// <summary> Logical "and": result has to include all of these terms. </summary>
        public string AndTerms { get; set; }

        /// <summary>  Logical "or": result might include any of these terms. </summary>
        public string OrTerms { get; set; }

        /// <summary> Bounding box constraint. </summary>
        public BoundingBox BoundingBox { get; set; }

        /// <summary> LOD range constraint. </summary>
        public Range<int> LodRange { get; set; }
    }
}
