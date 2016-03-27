using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Graphic))]
public class CircularRaycastFillter :  MonoBehaviour, ICanvasRaycastFilter
{
    public float radius;

    bool ICanvasRaycastFilter.IsRaycastLocationValid(Vector2 sp, 
                                                     Camera eventCamera)
    {
        Ray ray = eventCamera.ScreenPointToRay(sp);
        var plane = new Plane(Vector3.up, Vector3.zero);
        float t;

        plane.Raycast(ray, out t);

        Vector3 point = ray.GetPoint(t);

        return (point - transform.position).magnitude <= radius;
    }
}
