using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[SelectionBase]
public class PlaneCamera : MonoBehaviour
{
    [Header("Reorientation Speed")]
    public float focusTime = .25f;
    public float pivotTime = .25f;
    public float depthTime = .25f;
    public float angleTime = .25f;

    [Header("Orientation Limits")]
    public float minPivot =  0;
    public float maxPivot = 90;
    public float minDepth =  5;
    public float maxDepth = 15;
    public float angleCenter = 0;
    public float angleArc = 720;

    public Rect worldBounds;

    [Header("Link Pivot & Zoom")]
    public bool linkPivotToDepth;
    public AnimationCurve pivotZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Target Orientation")]
    public Vector2 focusTarget;
    public float pivotTarget;
    public float depthTarget;
    public float angleTarget;

    [Header("Internal Transforms")]
    [SerializeField]
    protected Transform depthTransform;
    [SerializeField]
    protected Transform pivotTransform;
    [SerializeField]
    protected Transform focusTransform;

    public Vector2 forward
    {
        get
        {
            return new Vector2(focusTransform.forward.x,
                               focusTransform.forward.z);
        }
    }

    public Vector2 right
    {
        get
        {
            return new Vector2(focusTransform.right.x,
                               focusTransform.right.z);
        }
    }

    public Vector2 focus
    {
        get
        {
            return new Vector2(focusTransform.position.x,
                                focusTransform.position.z);
        }

        protected set
        {
            focusTransform.position = new Vector3(value.x, 0, value.y);
        }
    }

    public float pivot
    {
        get
        {
            return pivotTransform.localEulerAngles.x;
        }

        protected set
        {
            Vector3 angles = pivotTransform.localEulerAngles;

            angles.x = value;

            pivotTransform.localEulerAngles = angles;
        }
    }

    public float depth
    {
        get
        {
            return -depthTransform.localPosition.z;
        }

        protected set
        {
            depthTransform.localPosition = Vector3.back * value;
        }
    }

    public float angle
    {
        get
        {
            return focusTransform.localRotation.eulerAngles.y;
        }

        protected set
        {
            Vector3 angles = focusTransform.localEulerAngles;

            angles.y = value;

            focusTransform.localEulerAngles = angles;
        }
    }

    private Vector2 focusVelocity;
    private float pivotVelocity;
    private float depthVelocity;
    private float angleVelocity;

    protected virtual void Awake()
    {
        focusTarget = focus;
        pivotTarget = pivot;
        depthTarget = depth;
        angleTarget = angle;

        ClampTargets();
        SnapToTargets();
    }

    protected virtual void Update()
    {
        ClampTargets();

        focus = Vector2.SmoothDamp(focus, focusTarget, ref focusVelocity, focusTime);
        pivot = Mathf.SmoothDamp(pivot, pivotTarget, ref pivotVelocity, pivotTime);
        depth = Mathf.SmoothDamp(depth, depthTarget, ref depthVelocity, depthTime);
        angle = Mathf.SmoothDampAngle(angle, angleTarget, ref angleVelocity, angleTime);

        if (linkPivotToDepth)
        {
            pivotTarget = PivotFromDepth(depth);
            pivot = pivotTarget;
        }
    }

    protected virtual void ClampTargets()
    {
        if (angleTarget >  180) angleTarget -= 360;
        if (angleTarget < -180) angleTarget += 360;

        pivotTarget = Mathf.Clamp(pivotTarget, minPivot, maxPivot);
        angleTarget = Mathf.Clamp(angleTarget, angleCenter - angleArc * 0.5f, angleCenter + angleArc * 0.5f);
        depthTarget = Mathf.Clamp(depthTarget, minDepth, maxDepth);
        focusTarget = new Vector2(Mathf.Clamp(focusTarget.x, worldBounds.xMin, worldBounds.xMax),
                                  Mathf.Clamp(focusTarget.y, worldBounds.yMin, worldBounds.yMax));
    }

    public virtual void SnapToTargets()
    {
        focus = focusTarget;
        pivot = pivotTarget;
        depth = depthTarget;
        angle = angleTarget;
    }

    public virtual float PivotFromDepth(float depth)
    {
        float u = Mathf.InverseLerp(minDepth, maxDepth, depth);
        float v = pivotZoomCurve.Evaluate(u);
        float pivot = Mathf.Lerp(minPivot, maxPivot, v);

        return pivot;
    }
}
