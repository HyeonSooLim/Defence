using System;
using UnityEngine;

namespace ProjectHD.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaController : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform ??= GetComponent<RectTransform>();
            ApplySafeArea(Screen.safeArea);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_rectTransform == null) return;
#if UNITY_EDITOR
            Debug.Log("화면의 해상도 변경 혹은 회전이 감지되었습니다.");
#endif
            ApplySafeArea(Screen.safeArea);
        }

        private void ApplySafeArea(Rect safeArea)
        {
            // 픽셀 좌표를 0~1 앵커 좌표로 변환하여 설정
            Vector2 min = safeArea.position;
            Vector2 max = min + safeArea.size;
        
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
        }
    }
}