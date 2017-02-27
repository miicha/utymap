using System;
using UnityEngine;

namespace Assets.Scripts.Scene.Controllers
{
    internal abstract class TileGridBehaviour : MonoBehaviour
    {
        public GameObject Pivot;
        public GameObject Planet;

        private Camera _camera;
        private Vector3 _lastPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        /// <summary> Called with updated position. </summary>
        /// <returns> False if position is not in scene. </returns>
        protected abstract bool OnPositionUpdated(Vector3 position);

        /// <summary> Maximum distance from world origin. </summary>
        protected abstract float MaxOriginDistance();

        /// <summary> Gets tile controller. </summary>
        protected abstract TileGridController GetTileController();

        /// <summary> Performs framework initialization once, before any Start() is called. </summary>
        void Awake()
        {
            _camera = GetComponent<Camera>();
            GetTileController().UpdateCamera(_camera, transform.position);
            GetTileController().MoveOrigin(Vector3.zero);
        }

        void Update()
        {
            // no movements
            if (_lastPosition == transform.position)
                return;
            
            _lastPosition = transform.position;

            if (!OnPositionUpdated(_lastPosition))
                return;

            GetTileController().UpdateCamera(_camera, _lastPosition);
            GetTileController().Build(Planet, _lastPosition);

            KeepOrigin();
        }

        void OnGUI()
        {
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                String.Format("Position:{0}\nGeo:{1}\nQuadKey: {2}\nLOD:{3}\nScreen: {4}:{5}\nFoV: {6}",
                    transform.position,
                     GetTileController().Projection.Project(transform.position),
                     GetTileController().CurrentQuadKey,
                     GetTileController().CurrentLevelOfDetail,
                    Screen.width, Screen.height,
                    _camera.fieldOfView));
        }

        private void KeepOrigin()
        {
            var position = transform.position;
            if (!IsFar(position))
                return;

            Pivot.transform.position = GetTileController().WorldOrigin;
            Planet.transform.position += new Vector3(position.x, 0, position.z) * -1;
            _lastPosition = transform.position;

            GetTileController().MoveOrigin(position);
        }

        private bool IsFar(Vector3 position)
        {
            return Vector2.Distance(new Vector2(position.x, position.z), GetTileController().WorldOrigin) > MaxOriginDistance();
        }
    }
}
