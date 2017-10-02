using UtyMap.Unity.Infrastructure.Primitives;

namespace UtyMap.Unity
{
    /// <summary> Defines query to map data. </summary>
    public struct MapQuery
    {
        /// <summary> Logical "not": result should not include any of these terms. </summary>
        public readonly string NotTerms;

        /// <summary> Logical "and": result has to include all of these terms. </summary>
        public readonly string AndTerms;

        /// <summary>  Logical "or": result might include any of these terms. </summary>
        public readonly string OrTerms;

        /// <summary> Bounding box constraint. </summary>
        public readonly BoundingBox BoundingBox;

        /// <summary> LOD range constraint. </summary>
        public readonly Range<int> LodRange;

        public MapQuery(string notTerms, string andTerms, string orTerms,
            BoundingBox boundingBox, Range<int> lodRange)
        {
            NotTerms = notTerms;
            AndTerms = andTerms;
            OrTerms = orTerms;
            BoundingBox = boundingBox;
            LodRange = lodRange;
        }
    }
}
