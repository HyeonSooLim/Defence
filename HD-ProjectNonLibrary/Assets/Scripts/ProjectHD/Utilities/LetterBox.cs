using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public class LetterBox : Singleton<LetterBox>
    {
        private const float Width = 1080f;
        private const float Height = 2280f;
        
        private RectTransform _rectTransform;
        
        private RectTransform _boxTop;
        private RectTransform _boxBottom;
        private RectTransform _boxLeft;
        private RectTransform _boxRight;

        private void Awake()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Width, Height);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _rectTransform = gameObject.GetComponent<RectTransform>();
            Utilities.InternalDebug.Log(_rectTransform);

            {
                var imageObject = new GameObject("Imagetop");
                imageObject.transform.SetParent(transform);
                RectTransform rectTransformTop = imageObject.AddComponent<RectTransform>();
                Image imageTop = imageObject.AddComponent<Image>();
                imageTop.color = Color.black;
                imageTop.raycastTarget = false;
                rectTransformTop.anchoredPosition = Vector2.zero;
                rectTransformTop.localRotation = Quaternion.identity;
                rectTransformTop.localScale = Vector3.one;
                rectTransformTop.anchorMax = new Vector2(1, 1);
                rectTransformTop.anchorMin = new Vector2(0f, 0f);
                rectTransformTop.SetLeft(0f);
                rectTransformTop.SetRight(0f);
                rectTransformTop.SetTop(0f);
                rectTransformTop.SetBottom(0f);
                _boxTop = rectTransformTop;   
            }

            {
                var imageObject = new GameObject("ImageBottom");
                imageObject.transform.SetParent(transform);
                RectTransform rectTransformBottom = imageObject.AddComponent<RectTransform>();
                Image imageBottom = imageObject.AddComponent<Image>();
                imageBottom.color = Color.black;
                imageBottom.raycastTarget = false;
                rectTransformBottom.anchoredPosition = Vector2.zero;
                rectTransformBottom.localRotation = Quaternion.identity;
                rectTransformBottom.localScale = Vector3.one;
                rectTransformBottom.anchorMax = new Vector2(1, 1);
                rectTransformBottom.anchorMin = new Vector2(0f, 0f);
                rectTransformBottom.SetLeft(0f);
                rectTransformBottom.SetRight(0f);
                rectTransformBottom.SetTop(0f);
                rectTransformBottom.SetBottom(0f);
                _boxBottom = rectTransformBottom;
            }

            {
                var imageObject = new GameObject("ImageLeft");
                imageObject.transform.SetParent(transform);
                RectTransform rectTransformleft = imageObject.AddComponent<RectTransform>();
                Image imageleft = imageObject.AddComponent<Image>();
                imageleft.color = Color.black;
                imageleft.raycastTarget = false;
                rectTransformleft.anchoredPosition = Vector2.zero;
                rectTransformleft.localRotation = Quaternion.identity;
                rectTransformleft.localScale = Vector3.one;
                rectTransformleft.anchorMax = new Vector2(1, 1);
                rectTransformleft.anchorMin = new Vector2(0f, 0f);
                rectTransformleft.SetLeft(0f);
                rectTransformleft.SetRight(0f);
                rectTransformleft.SetTop(0f);
                rectTransformleft.SetBottom(0f);
                _boxLeft = rectTransformleft;
            }

            {
                var imageObject = new GameObject("ImageRight");
                imageObject.transform.SetParent(transform);
                RectTransform rectTransformRight = imageObject.AddComponent<RectTransform>();
                Image imageRight = imageObject.AddComponent<Image>();
                imageRight.color = Color.black;
                imageRight.raycastTarget = false;
                rectTransformRight.anchoredPosition = Vector2.zero;
                rectTransformRight.localRotation = Quaternion.identity;
                rectTransformRight.localScale = Vector3.one;
                rectTransformRight.anchorMax = new Vector2(1, 1);
                rectTransformRight.anchorMin = new Vector2(0f, 0f);
                rectTransformRight.SetLeft(0f);
                rectTransformRight.SetRight(0f);
                rectTransformRight.SetTop(0f);
                rectTransformRight.SetBottom(0f);
                _boxRight = rectTransformRight;
            }
        }

        private float _tempTime = 0;
        private float _lastWidth;
        private float _lastHeight;

        private void Start()
        {
            _lastWidth = _rectTransform.sizeDelta.x;
            _lastHeight = _rectTransform.sizeDelta.y;
            AdjustBoxes();
        }

        private void Update()
        {
            var rectTransformWidth = _rectTransform.sizeDelta.x;
            var rectTransformHeight = _rectTransform.sizeDelta.y;

            if (Mathf.Abs(rectTransformWidth - _lastWidth) > 0.01f ||
                Mathf.Abs(rectTransformHeight - _lastHeight) > 0.01f)
            {
                _lastWidth = rectTransformWidth;
                _lastHeight = rectTransformHeight;
                AdjustBoxes();
            }
        }

        private void AdjustBoxes()
        {
            var rect = _rectTransform.rect;
            var maxHeight = Height;
            var maxWidth = Width;
            var diffHeightHalf = Mathf.Abs(rect.height - maxHeight) * 0.5f;
            var diffWidthHalf = Mathf.Abs(rect.width - maxWidth) * 0.5f;
            _boxTop.SetBottom(rect.height - diffHeightHalf);
            _boxBottom.SetTop(rect.height - diffHeightHalf);
            _boxLeft.SetRight(rect.width - diffWidthHalf);
            _boxRight.SetLeft(rect.width - diffWidthHalf);
        }
    }   
}
