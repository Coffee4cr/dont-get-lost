﻿
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class axes : MonoBehaviour
{
    // #pragma is to stop it complaining about these 
    // not being initialized, because they are initialized
    // through reflection in the Editor.
#pragma warning disable 0649
    [SerializeField] GameObject x_axis;
    [SerializeField] GameObject y_axis;
    [SerializeField] GameObject z_axis;
#pragma warning restore 0649

    public enum AXIS
    {
        NONE, X, Y, Z
    }

    public GameObject get_axis(AXIS a)
    {
        switch (a)
        {
            case AXIS.X: return x_axis;
            case AXIS.Y: return y_axis;
            case AXIS.Z: return z_axis;
            case AXIS.NONE: return null;
            default: throw new System.Exception("Unkown axis!");
        }
    }

    Dictionary<Renderer, Color> initial_colors = new Dictionary<Renderer, Color>();

    private void Start()
    {
        foreach (var a in new GameObject[] { x_axis, y_axis, z_axis })
            foreach (var r in GetComponentsInChildren<Renderer>())
                initial_colors[r] = r.material.color;
    }

    void highlight(GameObject a, bool highlight)
    {
        float b = highlight ? 0.5f : 1f;
        foreach (var r in a.GetComponentsInChildren<Renderer>())
        {
            var init_color = initial_colors[r];
            r.material.color = new Color(
                init_color.r * b + (1 - b),
                init_color.g * b + (1 - b),
                init_color.b * b + (1 - b)
            );
        }
    }

    public void highlight_axis(AXIS a)
    {
        highlight(x_axis, false);
        highlight(y_axis, false);
        highlight(z_axis, false);
        if (a != AXIS.NONE)
            highlight(get_axis(a), true);
    }
}