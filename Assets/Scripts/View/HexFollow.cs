using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class HexFollow : MonoBehaviour 
{
    public PlaneCamera camera;

    private void Update()
    {
        transform.rotation = Quaternion.AngleAxis(camera.angle, Vector3.up) 
                           * Quaternion.AngleAxis(90,           Vector3.right);
    }
}
