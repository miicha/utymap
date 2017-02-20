using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Utils;

namespace Assets.Scenes.Surface.Scripts
{
    /// <summary> Provides API for calculating surface scene specific values. </summary>
    internal static class SurfaceCalculator
    {
        /// <summary> Max distance from origin before moving back. </summary>
        private const float MaxOriginDistance = 2000;

        /// <summary> Minimal LOD. </summary>
        private const int MinLod = 9;

        /// <summary> Maximal LOD. </summary>
        private const int MaxLod = 16;

        /// <summary> World scale. </summary>
        public const float Scale = 0.01f;

        /// <summary> Origin in world coordinates. </summary>
        public static readonly Vector3 Origin = Vector3.zero;

        /// <summary> Origin as GeoCoordinate. </summary>
        public static GeoCoordinate GeoOrigin = GeoUtils.QuadKeyToBoundingBox(
            GeoUtils.CreateQuadKey(new GeoCoordinate(52.53180, 13.38744), MinLod)).Center();

        /// <summary> Returns current LOD. </summary>
        public static int CurrentLevelOfDetails = MinLod;

        /// <summary> Checks whether position is far from origin. </summary>
        public static bool IsFar(Vector3 position)
        {
            return Vector2.Distance(new Vector2(position.x, position.z), Origin) > MaxOriginDistance;
        }

        /// <summary> Gets zoom speed ratio. </summary>
        public static float GetZoomSpeedRatio(int lod)
        {
            return Mathf.Pow(2, -(lod - MinLod)) * Scale;
        }

        /// <summary> Gets pan speed ratio. </summary>
        public static float GetPanSpeedRatio(int lod)
        {
            return Mathf.Pow(2, -(lod - MinLod)) * Scale;
        }

        /// <summary> Gets quadkey for position. </summary>
        public static QuadKey GetQuadKey(Vector3 position)
        {
            var currentPosition = GeoUtils.ToGeoCoordinate(GeoOrigin, new Vector2(position.x, position.z) / Scale);
            return GeoUtils.CreateQuadKey(currentPosition, CurrentLevelOfDetails);
        }

        /// <summary> Gets range (interval) tree with LODs </summary>
        /// <remarks> Modifies camera's field of view. </remarks>
        public static RangeTree<float, int> GetLodTree(Camera camera, Vector3 position)
        {
            const float sizeRatio = 0.75f;
            var tree = new RangeTree<float, int>();

            var maxDistance = position.y - 1;

            var aspectRatio = sizeRatio * (Screen.height < Screen.width ? 1 / camera.aspect : 1);

            var fov = GetFieldOfView(GeoUtils.CreateQuadKey(GeoOrigin, MinLod), maxDistance, aspectRatio);

            tree.Add(maxDistance, float.MaxValue, MinLod);
            for (int lod = MinLod + 1; lod <= MaxLod; ++lod)
            {
                var frustumHeight = GetFrustumHeight(GeoUtils.CreateQuadKey(GeoOrigin, lod), aspectRatio);
                var distance = frustumHeight * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                tree.Add(distance, maxDistance, lod - 1);
                maxDistance = distance;
            }
            tree.Add(float.MinValue, maxDistance, MaxLod);

            camera.fieldOfView = fov;

            return tree;
        }

        /// <summary> Gets projection. </summary>
        public static IProjection GetProjection()
        {
            return new ScaledProjection(new CartesianProjection(GeoOrigin), Scale);
        }

        /// <summary> Gets height of camera's frustum. </summary>
        private static float GetFrustumHeight(QuadKey quadKey, float aspectRatio)
        {
            return GetGridSize(quadKey) * aspectRatio;
        }

        /// <summary> Gets field of view for given quadkey and distance. </summary>
        private static float GetFieldOfView(QuadKey quadKey, float distance, float aspectRatio)
        {
            return 2.0f * Mathf.Rad2Deg * Mathf.Atan(GetFrustumHeight(quadKey, aspectRatio) * 0.5f / distance);
        }

        /// <summary> Get side size of grid consists of 9 quadkeys. </summary>
        private static float GetGridSize(QuadKey quadKey)
        {
            var bbox = GeoUtils.QuadKeyToBoundingBox(quadKey);
            var bboxWidth = bbox.MaxPoint.Longitude - bbox.MinPoint.Longitude;
            var minPoint = new GeoCoordinate(bbox.MinPoint.Latitude, bbox.MinPoint.Longitude - bboxWidth);
            var maxPoint = new GeoCoordinate(bbox.MinPoint.Latitude, bbox.MaxPoint.Longitude + bboxWidth);
            return (float) GeoUtils.Distance(minPoint, maxPoint) * Scale;
        }
    }
}
