using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Utils;

using UtyRx;

namespace Assets.Scripts.Scenes.ThirdPerson.Controllers
{
    /// <summary> Searches for address using cached locally data. </summary>
    internal sealed class AddressController
    {
        private const int SearchSize = 100;

        /// <summary> Expected address tags. </summary>
        /// <see cref="http://wiki.openstreetmap.org/wiki/Key:addr"/>
        private readonly List<string> _addressTags = new List<string>()
        {
            "addr:country", "addr:city", "addr:suburb", "addr:state", "addr:province",
            "addr:district","addr:postcode", "addr:place", "addr:street", "addr:housenumber", "addr:name"
        };

        private readonly IMapDataStore _dataStore;
        private readonly Text _text;
        private readonly int _levelOfDetail;
        private readonly ITrace _trace;

        private GeoCoordinate _coordinate;
        private Vector3 _position;

        private GeoCoordinate? _currentLocation;

        public AddressController(IMapDataStore dataStore, Text text, int levelOfDetail, ITrace trace)
        {
            _dataStore = dataStore;
            _text = text;
            _levelOfDetail = levelOfDetail;
            _trace = trace;
            _dataStore
                .ObserveOn<Element>(Scheduler.MainThread)
                .Subscribe(ProcessResult);
        }

        public void Update(Vector3 position, GeoCoordinate relativeNullPoint)
        {
            if (Vector3.Distance(position, _position) < 10)
                return;

            _position = position;

            _coordinate = GeoUtils.ToGeoCoordinate(relativeNullPoint, position.x, position.z);
            _dataStore.OnNext(CreateQuery(_coordinate));
        }

        private void ProcessResult(Element element)
        {
            var location = GetLocation(element);

            var distanceToNewPlace = location.HasValue
                ? GeoUtils.Distance(location.Value, _coordinate)
                : 50; // NOTE give element some weight as we don't know its geometry

            var distanceToOldPlace = _currentLocation.HasValue
                ? GeoUtils.Distance(_currentLocation.Value, _coordinate)
                : float.MaxValue;

            if (distanceToNewPlace <= distanceToOldPlace)
            {
                _text.text = GetAddress(element);
                _currentLocation = location;
            }
        }

        /// <summary> Creates search query. </summary>
        /// <param name="coordinate"> Current geo position. </param>
        private MapQuery CreateQuery(GeoCoordinate coordinate)
        {
            if (_levelOfDetail != 16)
                _trace.Warn("address", "Search query is designed for raw OSM data schema" +
                                       " which is used only for LOD 16 with default mapcss.");
            return new MapQuery(
                // NOT terms
                "",
                // AND terms
                "addr street housenumber",
                // OR terms
                "",
                // limit search results by bounding box
                BoundingBox.Create(coordinate, SearchSize),
                // search only for given LOD
                new Range<int>(_levelOfDetail, _levelOfDetail));
        }

        /// <summary> Gets element location. </summary>
        private GeoCoordinate? GetLocation(Element element)
        {
            if (element.Geometry.Length == 1)
            {
                var location = element.Geometry[0];
                // NOTE Relations are not processed yet fully.
                if (Math.Abs(location.Latitude) < double.Epsilon &&
                    Math.Abs(location.Longitude) < double.Epsilon)
                    return null;
                return location;
            }

            double lat = 0, lon = 0;
            foreach (var coordinate in element.Geometry)
            {
                lat += coordinate.Latitude;
                lon += coordinate.Longitude;
            }

            return new GeoCoordinate(lat / element.Geometry.Length, lon / element.Geometry.Length);
        }

        /// <summary> Gets address string from element tags. </summary>
        private string GetAddress(Element element)
        {
            // TODO string manipulations can be optimized
            var tags = new List<string>();
            foreach (var tag in _addressTags)
            {
                if (element.Tags.ContainsKey(tag))
                    tags.Add(element.Tags[tag]);
            }

            return String.Join(", ", tags.ToArray());
        }
    }
}
