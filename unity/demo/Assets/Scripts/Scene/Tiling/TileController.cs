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

        /// <summary> Contains LOD values mapped for height ranges. </summary>
        protected RangeTree<float, int> LodTree;

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
        public abstract bool IsAboveMax { get; }

        /// <summary> Is belove minimum zoom level </summary>
        public abstract bool IsBelowMin{ get; }

        /// <summary> Moves geo origin to specific world position. </summary>
        public abstract void MoveOrigin(Vector3 position);

        /// <summary> Updates position and rotation. </summary>
        public abstract void OnUpdate(Transform planet);

        /// <inheritdoc />
        public abstract void Dispose();

        protected TileController(IMapDataStore dataStore, Stylesheet stylesheet, 
            ElevationDataType elevationType, Transform pivot, Range<int> lodRange)
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

            if (Math.Abs(startHeight - float.MinValue) < float.Epsilon ||
                Math.Abs(endHeight - float.MinValue) < float.Epsilon)
                throw new ArgumentException(String.Format("Invalid lod: {0}.", zoom));

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
            var lodRange = LodTree[distance].Single();
            var zoom = lodRange.Value + (lodRange.To - distance) / (lodRange.To - lodRange.From);
            return Mathf.Clamp(zoom, LodRange.Minimum, LodRange.Maximum);
        }
    }
}
