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
            var endLod = startLod + 1;

            var minHeight = float.MinValue;
            var maxHeight = float.MinValue;

            foreach (var rangeValuePair in LodTree)
            {
                if (rangeValuePair.Value == startLod)
                {
                    minHeight = rangeValuePair.From;
                    maxHeight = rangeValuePair.To;
                    break;
                }
            }

            // NOTE extrapolate height for zoom outside lod range.
            // This happens in case of inter-space animation.
            if (Math.Abs(minHeight - float.MinValue) < float.Epsilon ||
                Math.Abs(maxHeight - float.MinValue) < float.Epsilon)
            {
                var ratio = (HeightRange.Maximum - HeightRange.Minimum) / (LodRange.Maximum - LodRange.Minimum + 1);
                return zoom > LodRange.Maximum
                    ? HeightRange.Minimum - (zoom - LodRange.Maximum) * ratio
                    : HeightRange.Maximum - (zoom - LodRange.Minimum) * ratio;
            }

            // NOTE: we clamp with some tolerance to prevent issues with float precision when
            // the distance is huge (planet level). Theoretically, double type will fix 
            // the problem but it will force to use casting to float in multiple places.
            var range = maxHeight - minHeight;
            var tolerance = range * 0.00001f;
            return Mathf.Clamp(minHeight + range * (endLod - zoom), minHeight + tolerance, maxHeight - tolerance);
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
