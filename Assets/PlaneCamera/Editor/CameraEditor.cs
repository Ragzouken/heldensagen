using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(PlaneCamera))]
public class CameraEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        var camera = target as PlaneCamera;

        float pivot = Mathf.LerpAngle(camera.minPivot, camera.maxPivot, 0.5f);
        float depth = Mathf.LerpAngle(camera.minDepth, camera.maxDepth, 0.5f);

        camera.minDepth = Handles.ScaleValueHandle(
            camera.minDepth,
            camera.transform.TransformPoint(Quaternion.Euler(pivot, 0, 0) * Vector3.back * camera.minDepth),
            camera.transform.rotation * Quaternion.Euler(pivot, 0, 0),
            1,
            Handles.CubeCap,
            1);

        camera.maxDepth = Handles.ScaleValueHandle(camera.maxDepth,
                                                    camera.transform.position - camera.transform.rotation * Quaternion.Euler(pivot, 0, 0) * Vector3.forward * camera.maxDepth,
                                                    camera.transform.rotation * Quaternion.Euler(pivot+180, 0, 0),
                                                    1,
                                                    Handles.CubeCap,
                                                    1);

        camera.minPivot = Handles.ScaleValueHandle(camera.minPivot,
                                    camera.transform.position - camera.transform.rotation * Quaternion.Euler(camera.minPivot, 0, 0) * Vector3.forward * depth,
                                    camera.transform.rotation * Quaternion.Euler(camera.minPivot+90, 0, 0),
                                    .5f,
                                    Handles.CubeCap,
                                    1);

        camera.maxPivot = Handles.ScaleValueHandle(camera.maxPivot,
                                            camera.transform.position - camera.transform.rotation * Quaternion.Euler(camera.maxPivot, 0, 0) * Vector3.forward * depth,
                                            camera.transform.rotation * Quaternion.Euler(camera.maxPivot-90, 0, 0),
                                            .5f,
                                            Handles.CubeCap,
                                            1);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
