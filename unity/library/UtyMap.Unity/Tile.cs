using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Utils;

namespace UtyMap.Unity
{
    /// <summary> Represents map tile. </summary>
    public class Tile : IDisposable
    {
        /// <summary> Stores element ids loaded in this tile. </summary>
        private readonly SafeHashSet<long> _localIds = new SafeHashSet<long>();

        /// <summary> Stores element ids loaded for all tiles. </summary>
        private static readonly SafeHashSet<long> GlobalIds = new SafeHashSet<long>();

        /// <summary> Tile geo bounding box. </summary>
        public BoundingBox BoundingBox { get; private set; }

        /// <summary> Corresponding quadkey. </summary>
        public QuadKey QuadKey { get; private set; }

        /// <summary> Stylesheet. </summary>
        public Stylesheet Stylesheet { get; private set; }

        /// <summary> Used projection. </summary>
        public IProjection Projection { get; private set; }

        /// <summary> Specifies which elevation data type should be used. </summary>
        public ElevationDataType ElevationType { get; private set; }

        /// <summary> Sets game object which holds all children objects. </summary>
        public GameObject GameObject { get; private set; }

        /// <summary> True if tile was disposed. </summary>
        public bool IsDisposed { get; private set; }

        /// <summary> Used to cancel tile loading in native code. </summary>
        internal CancellationToken CancelationToken;

        /// <summary> Creates <see cref="Tile"/>. </summary>
        /// <param name="quadKey"></param>
        /// <param name="stylesheet"></param>
        /// <param name="projection"> Projection. </param>
        /// <param name="elevationType"> Elevation type. </param>
        /// <param name="gameObject"> Tile gameobject. </param>
        public Tile(QuadKey quadKey, Stylesheet stylesheet, IProjection projection, 
            ElevationDataType elevationType, GameObject gameObject = null)
        {
            QuadKey = quadKey;
            Stylesheet = stylesheet;
            Projection = projection;
            ElevationType = elevationType;
            GameObject = gameObject;

            BoundingBox = GeoUtils.QuadKeyToBoundingBox(quadKey);

            CancelationToken = new CancellationToken();
        }

        /// <summary> Checks whether element with specific id is registered. </summary>
        /// <param name="id">Element id.</param>
        /// <returns> True if registration is found. </returns>
        internal bool Has(long id)
        {
            return GlobalIds.Contains(id) || _localIds.Contains(id);
        }

        /// <summary> Register element with given id inside to prevent multiple loading. </summary>
        /// <remarks> 
        ///     Mostly used for objects which cross tile borders, but their geometry is 
        ///     not clipped (buildings one of examples) 
        /// </remarks>
        internal void Register(long id)
        {
            _localIds.Add(id);
            GlobalIds.Add(id);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return String.Format("({0},{1}:{2})", QuadKey.TileX, QuadKey.TileY, QuadKey);
        }

        #region IDisposable implementation

        /// <inheritdoc />
        public void Dispose()
        {
            // notify native code.
            CancelationToken.SetCancelled(true);

            // remove all registered ids from global list if they are in current registry
            foreach (var id in _localIds)
                GlobalIds.Remove(id);
            _localIds.Clear();

            if (GameObject != null)
                GameObject.Destroy(GameObject);

            IsDisposed = true;
        }

        #endregion
    }
}
