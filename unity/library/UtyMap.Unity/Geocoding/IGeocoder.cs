using System;
using UtyRx;

namespace UtyMap.Unity.Geocoding
{
    /// <summary> Defines geocoder API (including reverse). </summary>
    public interface IGeocoder :
        IObservable<GeocoderResult>,
        IObserver<Tuple<string, BoundingBox>>, // place description and bbox restriction
        IObserver<Tuple<GeoCoordinate, float>>, // place coordinate and surrounding bbox size restriction (in meters)
        IDisposable
    {
    }

    /// <summary> Represents geocoding results. </summary>
    public struct GeocoderResult
    {
        /// <summary> Gets or sets element id.</summary>
        public long ElementId;

        /// <summary> Gets or sets geo coordinate of search result center.</summary>
        public GeoCoordinate Coordinate;

        /// <summary> Gets or sets element's bounding box.</summary>
        public BoundingBox BoundingBox;

        /// <summary> Gets or sets formatted name of search result.</summary>
        public string DisplayName;
    }
}
