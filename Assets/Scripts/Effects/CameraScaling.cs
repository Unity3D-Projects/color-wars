﻿using UnityEngine;

public class CameraScaling : MonoBehaviour
{
    // Credit to:
    // http://www.thegamecontriver.com/2015/06/unity-2d-scale-resize-camera-size-resolution.html
    // for basic idea

    public float default_height = 900;
    public float default_width = 1440;
    private new Camera camera;

    // Use this for initialization
    private void Start()
    {
        camera = GetComponent<Camera>();
        camera.transparencySortMode = TransparencySortMode.Orthographic;
        FixCameraSize();
    }

    public void FixCameraSize()
    {
        float expected_ratio = default_width / default_height;
        float actual_ratio = (float)Screen.width / Screen.height;

        // Potentially not enough width
        if (actual_ratio < expected_ratio)
        {
            // orthographicSize is the vertical worldspace units the camera will
            // show. Multiplying by expected_ratio gives the expected number of
            // horizontal worldspace units.
            float minimum_horizontal_size = camera.orthographicSize * expected_ratio;
            float current_orthographic_width = camera.orthographicSize * (actual_ratio);
            float conversion_factor = minimum_horizontal_size / current_orthographic_width;
            camera.orthographicSize = conversion_factor * camera.orthographicSize;
        }
    }
}
