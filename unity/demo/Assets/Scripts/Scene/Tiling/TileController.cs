using System;
using System.Linq;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Primitives;

namespace Assets.Scripts.Scene.Tiling
{
    internal abstract class TileController : IDisposable
    {
        private readonly IMapDataStore _dataStore;
        private readonly Stylesheet _stylesheet;
        private readonly ElevationDataType _elevationType;

        /// <summary> Gets distance to origin. </summary>
        protected abstract float DistanceToOrigin { get; }

        /// <summary> Contains LOD values mapped for height ranges. </summary>
        protected RangeTree<float, int> LodTree;

        /// <summary> Scaled height range. </summary>
        public abstract Range<float> HeightRange { get; protected set; }

        /// <summary> Pivot. </summary>
        public readonly Transform Pivot;

        /// <summary> Level of detail range handled by the controller. </summary>
        public readonly Range<int> LodRange;

        /// <summary> Gets camera's field of view. </summary>
        public abstract float FieldOfView { get; protected set; }

        /// <summary> Current used projection. </summary>
        public abstract IProjection Projection { get; protected set; }

        /// <summary> Gets current zoom level. </summary>
        public abstract float ZoomLevel { get; }

        /// <summary> Is above maximum zoom level. </summary>
        public abstract bool IsAboveMax { get; }

        /// <summary> Is belove minimum zoom level </summary>
        public abstract bool IsBelowMin { get; }

        // <summary> Gets current geo coordinate. </summary>
        public abstract GeoCoordinate Coordinate { get; }

        /// <summary> Updates target. </summary>
        public abstract void Update(Transform target);

        /// <inheritdoc />
        public abstract void Dispose();

        protected TileController(IMapDataStore dataStore, Stylesheet stylesheet, ElevationDataType elevationType,
            Transform pivot, Range<int> lodRange)
        {
            _dataStore = dataStore;
            _stylesheet = stylesheet;
            _elevationType = elevationType;

            Pivot = pivot;
            LodRange = lodRange;
        }

        /// <summary> Gets height in scaled world coordinates for given zoom. </summary>
        public float GetHeight(float zoom)
        {
            var startLod = Math.Max((int)Math.Floor(zoom), LodRange.Minimum);
            var endLod = Math.Min((int)Math.Ceiling(zoom), LodRange.Maximum);

            var startHeight = float.MinValue;
            var endHeight = float.MinValue;

            foreach (var rangeValuePair in LodTree)
            {
                if (rangeValuePair.Value == startLod)
                    startHeight = rangeValuePair.To;
                if (rangeValuePair.Value == endLod)
                    endHeight = rangeValuePair.From;
            }

            // NOTE extrapolate height for zoom outside lod range.
            if (Math.Abs(startHeight - float.MinValue) < float.Epsilon ||
                Math.Abs(endHeight - float.MinValue) < float.Epsilon)
            {
                var ratio = (HeightRange.Maximum - HeightRange.Minimum) / (LodRange.Maximum - LodRange.Minimum + 1);
                return zoom > LodRange.Maximum
                    ? HeightRange.Minimum - (zoom - LodRange.Maximum) * ratio
                    : HeightRange.Maximum - (zoom - LodRange.Minimum) * ratio;
            }

            return endHeight - (endHeight - startHeight) * (zoom - startLod);
        }

        /// <summary> Creates tile for given quadkey. </summary>
        protected Tile CreateTile(QuadKey quadKey, GameObject parent)
        {
            return new Tile(quadKey, _stylesheet, Projection, _elevationType, parent);
        }

        /// <summary> Loads given tile. </summary>
        protected void LoadTile(Tile tile)
        {
            _dataStore.OnNext(tile);
        }

        /// <summary> Calculates target zoom level for given distance. </summary>
        protected float CalculateZoom(float distance)
        {
            if (IsAboveMax)
                return LodRange.Maximum + 0.999f;

            if (IsBelowMin)
                return LodRange.Minimum;

            var lodRange = LodTree[distance].Single();
            return lodRange.Value + (lodRange.To - distance) / (lodRange.To - lodRange.From);
        }
    }
}
