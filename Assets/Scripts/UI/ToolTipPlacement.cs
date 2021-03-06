﻿using UnityEngine;

public class ToolTipPlacement : MonoBehaviour
{
    public float verticalOffset = 5f;
    public bool attemptSmartPlacement = true;
    private RectTransform rect;
    private RichText text;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void SetText(string newText)
    {
        text = GetComponentInChildren<RichText>();
        if (text != null)
        {
            text.text = newText;
        }
    }

    private void Update()
    {
        // Counter-rotate
        rect.rotation = Quaternion.Euler(0, 0, 0);//-transform.parent.rotation.eulerAngles.z);
        // Place
        Vector2 offset = Vector2.up * verticalOffset;
        rect.position = (Vector2)transform.parent.position + offset;
        if (attemptSmartPlacement)
        {
            Vector2 upPoint = (Vector2)rect.position + 1 * Vector2.up;
            if (Physics2D.OverlapPoint(upPoint, LayerMask.GetMask("InCourtBackground")) == null)
            {
                rect.position = (Vector2)transform.parent.position - offset;
            }
        }
    }
}
