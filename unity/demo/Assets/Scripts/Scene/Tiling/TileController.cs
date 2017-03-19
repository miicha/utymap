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
        /// <summary> Specifies tile settings. </summary>
        public struct Settings
        {
            public readonly IMapDataStore DataStore;
            public readonly Stylesheet Stylesheet;
            public readonly ElevationDataType ElevationType;

            public Settings(IMapDataStore dataStore, Stylesheet stylesheet, ElevationDataType elevationType)
            {
                DataStore = dataStore;
                Stylesheet = stylesheet;
                ElevationType = elevationType;
            }
        }

        private readonly Settings _settings;

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

        // <summary> Gets current geo coordinate. </summary>
        public abstract GeoCoordinate Coordinate { get; }

        /// <summary> Is above maximum zoom level. </summary>
        public bool IsAboveMax { get { return HeightRange.Maximum < DistanceToOrigin; } }

        /// <summary> Is belove minimum zoom level </summary>
        public bool IsBelowMin { get { return HeightRange.Minimum > DistanceToOrigin; } }

        /// <summary> Updates target. </summary>
        public abstract void Update(Transform target);

        /// <inheritdoc />
        public abstract void Dispose();

        protected TileController(Settings settings, Transform pivot, Range<int> lodRange)
        {
            _settings = settings;

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
            return new Tile(quadKey, _settings.Stylesheet, Projection, _settings.ElevationType, parent);
        }

        /// <summary> Loads given tile. </summary>
        protected void LoadTile(Tile tile)
        {
            _settings.DataStore.OnNext(tile);
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
