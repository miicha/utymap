using System;
using UnityEngine;
using UnityEngine.UI;
using UtyMap.Unity;
using UtyMap.Unity.Geocoding;
using UtyMap.Unity.Utils;

using UtyRx;

namespace Assets.Scripts.Scenes.ThirdPerson.Controllers
{
    /// <summary> Searches for address using cached locally data. </summary>
    internal sealed class AddressController: IDisposable
    {
        private const float SearchSize = 100;

        private readonly IGeocoder _geocoder;
        private readonly Text _text;
        private readonly IDisposable _subscription;

        private GeoCoordinate _coordinate;
        private Vector3 _position;

        private GeoCoordinate? _currentLocation;

        public AddressController(IGeocoder geocoder, Text text)
        {
            _geocoder = geocoder;
            _text = text;

            _subscription = _geocoder
                .ObserveOn(Scheduler.MainThread)
                .Subscribe(ProcessResult);
        }

        public void Update(Vector3 position, GeoCoordinate relativeNullPoint)
        {
            if (Vector3.Distance(position, _position) < 10)
                return;

            _position = position;
            _coordinate = GeoUtils.ToGeoCoordinate(relativeNullPoint, position.x, position.z);

            _geocoder.OnNext(new Tuple<GeoCoordinate, float>(_coordinate, SearchSize));
        }

        private void ProcessResult(GeocoderResult result)
        {
            var distanceToNewPlace = IsValidCoordinate(result.Coordinate)
                ? GeoUtils.Distance(result.Coordinate, _coordinate)
                : 50; // NOTE give element some weight as we don't know its geometry

            var distanceToOldPlace = _currentLocation.HasValue
                ? GeoUtils.Distance(_currentLocation.Value, _coordinate)
                : float.MaxValue;

            if (distanceToNewPlace <= distanceToOldPlace)
            {
                _text.text = result.DisplayName;
                _currentLocation = result.Coordinate;
            }
        }

        private static bool IsValidCoordinate(GeoCoordinate coordinate)
        {
            return Math.Abs(coordinate.Latitude) > double.Epsilon &&
                   Math.Abs(coordinate.Longitude) > Double.Epsilon;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
