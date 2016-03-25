using UnityEngine;
using UnityEditor;


public class PlaneCameraGizmo
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
    static void DrawGizmo(PlaneCamera camera, GizmoType type)
    {
        Gizmos.color = Color.magenta;

        Gizmos.DrawLine(camera.transform.TransformPoint(Quaternion.Euler(camera.minPivot, 0, 0) * Vector3.back * camera.minDepth),
                        camera.transform.TransformPoint(Quaternion.Euler(camera.minPivot, 0, 0) * Vector3.back * camera.maxDepth));
        Gizmos.DrawLine(camera.transform.TransformPoint(Quaternion.Euler(camera.maxPivot, 0, 0) * Vector3.back * camera.minDepth),
                        camera.transform.TransformPoint(Quaternion.Euler(camera.maxPivot, 0, 0) * Vector3.back * camera.maxDepth));

        {
            int divisions = Mathf.FloorToInt(camera.maxPivot - camera.minPivot);

            for (int i = 1; i <= divisions; ++i)
            {
                float prev = Mathf.LerpAngle(camera.minPivot, camera.maxPivot, (i - 1) / (float) divisions);
                float next = Mathf.LerpAngle(camera.minPivot, camera.maxPivot, (i - 0) / (float) divisions);

                Gizmos.DrawLine(camera.transform.TransformPoint(Quaternion.Euler(prev, 0, 0) * Vector3.back * camera.minDepth),
                                camera.transform.TransformPoint(Quaternion.Euler(next, 0, 0) * Vector3.back * camera.minDepth));
            }

            for (int i = 1; i <= divisions; ++i)
            {
                float prev = Mathf.LerpAngle(camera.minPivot, camera.maxPivot, (i - 1) / (float) divisions);
                float next = Mathf.LerpAngle(camera.minPivot, camera.maxPivot, (i - 0) / (float) divisions);

                Gizmos.DrawLine(camera.transform.TransformPoint(Quaternion.Euler(prev, 0, 0) * Vector3.back * camera.maxDepth),
                                camera.transform.TransformPoint(Quaternion.Euler(next, 0, 0) * Vector3.back * camera.maxDepth));
            }
        }

        Vector3 tl = new Vector3(camera.worldBounds.xMin, 0, camera.worldBounds.yMin);
        Vector3 bl = new Vector3(camera.worldBounds.xMin, 0, camera.worldBounds.yMax);
        Vector3 tr = new Vector3(camera.worldBounds.xMax, 0, camera.worldBounds.yMin);
        Vector3 br = new Vector3(camera.worldBounds.xMax, 0, camera.worldBounds.yMax);

        if (camera.transform.parent != null)
        {
            tl = camera.transform.parent.TransformPoint(tl);
            bl = camera.transform.parent.TransformPoint(bl);
            tr = camera.transform.parent.TransformPoint(tr);
            br = camera.transform.parent.TransformPoint(br);
        }

        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);

        if (camera.linkPivotToDepth)
        {
            int divisions = Mathf.FloorToInt(camera.maxPivot - camera.minPivot);

            for (int i = 1; i <= divisions; ++i)
            {
                float prevDepth = Mathf.LerpAngle(camera.minDepth, camera.maxDepth, (i - 1) / (float) divisions);
                float nextDepth = Mathf.LerpAngle(camera.minDepth, camera.maxDepth, (i - 0) / (float) divisions);

                float prevPivot = camera.PivotFromDepth(prevDepth);
                float nextPivot = camera.PivotFromDepth(nextDepth);

                Gizmos.DrawLine(camera.transform.TransformPoint(Quaternion.Euler(prevPivot, 0, 0) * Vector3.back * prevDepth),
                                camera.transform.TransformPoint(Quaternion.Euler(nextPivot, 0, 0) * Vector3.back * nextDepth));
            }
        }
    }
}
