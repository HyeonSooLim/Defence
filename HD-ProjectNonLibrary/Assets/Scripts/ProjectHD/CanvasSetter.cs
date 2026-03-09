using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasSetter : MonoBehaviour
    {
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;

        [SerializeField] RenderMode renderMode = RenderMode.ScreenSpaceCamera;
        [SerializeField] Vector2 referenceResolution = new(1080, 2280);
        [SerializeField] int sortingOrder = 0;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasScaler = GetComponent<CanvasScaler>();
            _canvas.renderMode = renderMode;
            _canvas.worldCamera = CameraManager.Instance.UICamera;
            _canvas.sortingOrder = sortingOrder;
            _canvasScaler.referenceResolution = referenceResolution;
        }

        private void OnDestroy()
        {
            _canvas = null;
            _canvasScaler = null;
        }
    }
}