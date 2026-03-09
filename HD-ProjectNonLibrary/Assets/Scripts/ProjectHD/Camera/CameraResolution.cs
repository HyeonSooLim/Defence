using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraResolution : MonoBehaviour
{
    [SerializeField]
    private float width = 2280;
    [SerializeField]
    private float height = 1080;
    [SerializeField]
    private bool clearBlack;
    [SerializeField]
    private Camera _camera;

    [SerializeField] private Rect _customRect;

    public Vector2 ScreenSize
    {
        get
        {
            return new(width, height);
        }
    }

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _customRect = _camera.rect;
        AdjustCamera();
    }

    public void AdjustCamera()
    {
        if (!_camera) return;

        Rect rect = _camera.rect;
        float scaleheight = ((float)Screen.width / Screen.height) / (width / height); // (가로 / 세로)
        float scalewidth = 1f / scaleheight;
        // Debug.Log($"{Screen.width},{Screen.height} {width},{height} ({scalewidth} : {scaleheight})");
        if (scaleheight <= 1)
        {
            rect.height = scaleheight;
            rect.y = (1f - scaleheight) / 2f;
        }
        /*else
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }*/

        if (scalewidth <= 1)
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }
        _camera.rect = rect;
    }

    private void OnPreCull()
    {
        if (!Application.isPlaying)
            return;

        if (!_camera) return;

        Rect rect = _customRect;
        float scaleheight = ((float)Screen.width / Screen.height) / (width / height); // (가로 / 세로)
        float scalewidth = 1f / scaleheight;
        // Debug.Log($"{Screen.width},{Screen.height} {width},{height} ({scalewidth} : {scaleheight})");
        if (scaleheight <= 1)
        {
            rect.height = scaleheight;
            rect.y = (1f - scaleheight) / 2f;
        }
        /*else
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }*/

        if (scalewidth <= 1)
        {
            rect.width = scalewidth;
            rect.x = (1f - scalewidth) / 2f;
        }
        _camera.rect = rect;
    }

#if UNITY_EDITOR

    [Button]
    public void DrawBlack()
    {
        GL.Clear(true, true, Color.black);
    }

    [Button]
    public void ResetCameraSize()
    {
        // AdjustCamera();
    }
#endif
}
