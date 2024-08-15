using System;
using Events;
using Extensions.Unity.MonoHelper;
using UnityEngine;
using Zenject;

namespace Components
{
    public class CameraSizeFitter : EventListenerMono
    {
        [Inject] private GridEvents GridEvents{get;set;}
        [Inject] private InputEvents InputEvents{get;set;}
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _transform;

        

        protected override void RegisterEvents()
        {
            GridEvents.GridLoaded += OnGridLoaded;
            InputEvents.ZoomDelta += OnZoomDelta;
        }

        private void OnZoomDelta(float arg0)
        {
            var orthographicSize = _camera.orthographicSize;
            orthographicSize += arg0;
            
            _camera.orthographicSize = Mathf.Clamp(orthographicSize, 2f, 10f);
        }

        private void OnGridLoaded(Bounds gridBounds)
        {
            //width, height lower?
            //width  x height (if height is lower, no need to multiply)
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            _transform.position = gridBounds.center + (Vector3.back * 10f);
            _camera.orthographicSize = gridBounds.extents.x * (screenHeight < screenWidth ? 1 : (1f/ _camera.aspect));
        }

        protected override void UnRegisterEvents()
        {
            GridEvents.GridLoaded -= OnGridLoaded;
            InputEvents.ZoomDelta -= OnZoomDelta;
        }
    }
}