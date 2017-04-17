using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UtyMap.Unity.Infrastructure.Primitives;

namespace UtyMap.Unity.Data
{
    /// <summary> Cancellation token to cancel processing in native code. </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class CancellationToken
    {
        public int IsCancelled;

        public void SetCancelled(bool isCancelled)
        {
            IsCancelled = (byte) (isCancelled ? 1 : 0);
        }
    }

    /// <summary> Provides the way to build tile encapsulating Utymap implementation. </summary>
    internal static class CoreLibrary
    {
        private const string InMemoryStoreKey = "InMemory";
        private const string PersistentStoreKey = "Persistent";

        private static object __lockObj = new object();

        // NOTE: protection flag against multiple configuration.
        // TODO: maybe support multiple calls?
        private static volatile bool _isConfigured;
        private static string _lastError;

        /// <summary> Configure utymap. Should be called before any core API usage. </summary>
        /// <param name="stringPath"> Path to string table. </param>
        /// <param name="mapDataPath">Path for map data. </param>
        public static string Configure(string stringPath, string mapDataPath)
        {
            lock (__lockObj)
            {
                // NOTE this directories should be created in advance (and some others..)
                if (!Directory.Exists(stringPath) || !Directory.Exists(mapDataPath))
                    throw new DirectoryNotFoundException(String.Format("Cannot find {0} or {1}", stringPath, mapDataPath));

                if (_isConfigured)
                    return null;

                ResetLastError();
                configure(stringPath, OnErrorHandler);
                if (!String.IsNullOrEmpty(_lastError))
                    return _lastError;
                
                // NOTE actually, it is possible to have multiple in-memory and persistent 
                // storages at the same time.
                registerInMemoryStore(InMemoryStoreKey);
                registerPersistentStore(PersistentStoreKey, mapDataPath);

                _isConfigured = true;
                return null;
            }
        }

        /// <summary>
        ///     Adds map data to in-memory dataStorage to specific level of detail range.
        ///     Supported formats: shapefile, osm xml, osm pbf.
        /// </summary>
        /// <param name="dataStorageType"> Map data dataStorage. </param>
        /// <param name="stylePath"> Stylesheet path. </param>
        /// <param name="path"> Path to file. </param>
        /// <param name="levelOfDetails"> Specifies level of details for which data should be imported. </param>
        public static string AddToStore(MapDataStorageType dataStorageType, string stylePath, string path, Range<int> levelOfDetails)
        {
            lock (__lockObj)
            {
                ResetLastError();
                addToStoreInRange(GetStoreKey(dataStorageType), stylePath, path, levelOfDetails.Minimum,
                    levelOfDetails.Maximum, OnErrorHandler);
                return _lastError;
            }
        }

        /// <summary>
        ///     Adds map data to data storage only to specific quadkey.
        ///     Supported formats: shapefile, osm xml, osm pbf.
        /// </summary>
        /// <param name="dataStorageType"> Map data dataStorage. </param>
        /// <param name="stylePath"> Stylesheet path. </param>
        /// <param name="path"> Path to file. </param>
        /// <param name="quadKey"> QuadKey. </param>
        public static string AddToStore(MapDataStorageType dataStorageType, string stylePath, string path, QuadKey quadKey)
        {
            lock (__lockObj)
            {
                ResetLastError();
                addToStoreInQuadKey(GetStoreKey(dataStorageType), stylePath, path, quadKey.TileX, quadKey.TileY,
                    quadKey.LevelOfDetail, OnErrorHandler);
                return _lastError;
            }
        }

        /// <summary>
        ///     Adda elemet to data storage to specific level of details.
        ///     Supported formats: shapefile, osm xml, osm pbf.
        /// </summary>
        /// <param name="dataStorageType"> Map data dataStorage. </param>
        /// <param name="stylePath"> Stylesheet path. </param>
        /// <param name="element"> Element to add. </param>
        /// <param name="levelOfDetails"> Level of detail range. </param>
        public static string AddElementToStore(MapDataStorageType dataStorageType, string stylePath, Element element, Range<int> levelOfDetails)
        {
            double[] coordinates = new double[element.Geometry.Length*2];
            for (int i = 0; i < element.Geometry.Length; ++i)
            {
                coordinates[i*2] = element.Geometry[i].Latitude;
                coordinates[i*2 + 1] = element.Geometry[i].Longitude;
            }

            string[] tags = new string[element.Tags.Count * 2];
            var tagKeys = element.Tags.Keys.ToArray();
            for (int i = 0; i < tagKeys.Length; ++i)
            {
                tags[i*2] = tagKeys[i];
                tags[i*2 + 1] = element.Tags[tagKeys[i]];
            }

            lock (__lockObj)
            {
                ResetLastError();
                addToStoreElement(GetStoreKey(dataStorageType), stylePath, element.Id,
                    coordinates, coordinates.Length,
                    tags, tags.Length,
                    levelOfDetails.Minimum, levelOfDetails.Maximum, OnErrorHandler);
                return _lastError;
            }
        }

        /// <summary> Checks whether there is data for given quadkey. </summary>
        /// <returns> True if there is data for given quadkey. </returns>
        public static bool HasData(QuadKey quadKey)
        {
            return hasData(quadKey.TileX, quadKey.TileY, quadKey.LevelOfDetail);
        }

        /// <summary> Gets elevation for given coordinate using specific elevation data. </summary>
        /// <returns> Height under sea level. </returns>
        /// <remarks> Elevation data should be present on disk. </remarks>
        public static double GetElevation(QuadKey quadKey, ElevationDataType elevationDataType, GeoCoordinate coordinate)
        {
            return getElevation(quadKey.TileX, quadKey.TileY, quadKey.LevelOfDetail,
                (int) elevationDataType, coordinate.Latitude, coordinate.Longitude);
        }

        /// <summary> Loads quadkey. </summary>
        /// <param name="tag"> A tag which is used to match an object to requested tile in response. </param>
        /// <param name="stylePath"> Stylesheet path. </param>
        /// <param name="quadKey"> QuadKey</param>
        /// <param name="elevationDataType"> Elevation data type.</param>
        /// <param name="onMeshBuilt"> Mesh callback. </param>
        /// <param name="onElementLoaded"> Element callback. </param>
        /// <param name="onError"> Error callback. </param>
        /// <param name="cancelToken"> Cancellation token. </param>
        public static void LoadQuadKey(int tag, string stylePath, QuadKey quadKey, ElevationDataType elevationDataType,
            OnMeshBuilt onMeshBuilt, OnElementLoaded onElementLoaded, OnError onError, CancellationToken cancelToken)
        {
            var cancelTokenHandle = GCHandle.Alloc(cancelToken, GCHandleType.Pinned);
            loadQuadKey(tag, stylePath, quadKey.TileX, quadKey.TileY, quadKey.LevelOfDetail,
                (int) elevationDataType, onMeshBuilt, onElementLoaded, onError, cancelTokenHandle.AddrOfPinnedObject());
            cancelTokenHandle.Free();
        }

        /// <summary> Frees resources. Should be called before application stops. </summary>
        public static void Dispose()
        {
            // NOTE: do not allow to call cleanup as configure method can be called only once (see above)
            // So, let OS release resources once app has been finished
            //cleanup();
        }

        #region Private members

        private static string GetStoreKey(MapDataStorageType dataStorageType)
        {
            return dataStorageType == MapDataStorageType.InMemory ? InMemoryStoreKey : PersistentStoreKey;
        }

        private static void ResetLastError()
        {
            _lastError = null;
        }

        [AOT.MonoPInvokeCallback(typeof(OnError))]
        private static void OnErrorHandler(string message)
        {
            _lastError = message;
        }

        #endregion

        #region PInvoke import

        internal delegate void OnMeshBuilt(int tag, string name,
            IntPtr vertexPtr, int vertexCount,
            IntPtr trianglePtr, int triangleCount,
            IntPtr colorPtr, int colorCount,
            IntPtr uvPtr, int uvCount,
            IntPtr uvMapPtr, int uvMapCount);

        internal delegate void OnElementLoaded(int tag, long id,
            IntPtr tagPtr, int tagCount,
            IntPtr vertexPtr, int vertexCount,
            IntPtr stylePtr, int styleCount);

        internal delegate void OnError(string message);

        [DllImport("UtyMap.Shared")]
        private static extern void configure(string stringPath, OnError errorHandler);

        [DllImport("UtyMap.Shared")]
        private static extern void registerInMemoryStore(string key);

        [DllImport("UtyMap.Shared")]
        private static extern void registerPersistentStore(string key, string path);

        [DllImport("UtyMap.Shared")]
        private static extern void addToStoreInRange(string key, string stylePath, string path, int startLod, int endLod, OnError errorHandler);

        [DllImport("UtyMap.Shared")]
        private static extern void addToStoreInQuadKey(string key, string stylePath, string path, int tileX, int tileY, int lod, OnError errorHandler);

        [DllImport("UtyMap.Shared")]
        private static extern void addToStoreElement(string key, string stylePath, long id, double[] vertices, int vertexLength, 
            string[] tags, int tagLength, int startLod, int endLod, OnError errorHandler);

        [DllImport("UtyMap.Shared")]
        private static extern bool hasData(int tileX, int tileY, int levelOfDetails);

        [DllImport("UtyMap.Shared")]
        private static extern double getElevation(int tileX, int tileY, int levelOfDetails, int eleDataType, double latitude, double longitude);

        [DllImport("UtyMap.Shared")]
        private static extern void loadQuadKey(int tag, string stylePath, int tileX, int tileY, int levelOfDetails, int eleDataType,
            OnMeshBuilt meshBuiltHandler, OnElementLoaded elementLoadedHandler, OnError errorHandler, IntPtr cancelToken);

        [DllImport("UtyMap.Shared")]
        private static extern void cleanup();

        #endregion
    }
}
