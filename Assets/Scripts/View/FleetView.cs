﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class FleetView : MonoBehaviour 
{
    [SerializeField] private AnimationCurve moveCurve;
    [SerializeField] private AnimationCurve turnCurve;

    [SerializeField] private Transform cloudPrefab;
    [SerializeField] private int clouds;

    private Fleet fleet;

    private GameObject[] cloudInstances;

    private void Awake()
    {
        cloudInstances = new GameObject[clouds * 6];

        int i = 0;

        foreach (Vector3 point in GetPointsOnSphere(cloudInstances.Length))
        {
            var cloud = Instantiate(cloudPrefab, point * 0.5f, Quaternion.identity) as Transform;

            cloud.SetParent(transform, true);
            cloud.gameObject.SetActive(true);

            cloudInstances[i] = cloud.gameObject;

            i += 1;
        }
    }

    public IEnumerator Start()
    {
        yield return new WaitForSeconds(1);

        foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
        {
            system.Pause();
        }

    }

    public void Setup(Fleet fleet)
    {
        this.fleet = fleet;
    }

    public void Refresh()
    {
        Vector3 currPos = HexGrid.HexToWorld(fleet.position);
        Vector3 nextPos = HexGrid.HexToWorld(fleet.nextPosition);

        var currAngle = Quaternion.AngleAxis(fleet.orientation     * 60, -Vector3.up);
        var nextAngle = Quaternion.AngleAxis(fleet.nextOrientation * 60, -Vector3.up);

        transform.position =     Vector3.Lerp(currPos,   nextPos,   moveCurve.Evaluate(fleet.progress));
        transform.rotation = Quaternion.Slerp(currAngle, nextAngle, turnCurve.Evaluate(fleet.progress));

        for (int i = 0; i < 6; ++i)
        {
            bool active = (i < fleet.squadrons.Length && fleet.squadrons[i] != null);

            for (int c = 0; c < clouds; ++c)
            {
                cloudInstances[i * clouds + c].SetActive(active);
            }
        }
    }

    private IEnumerable<Vector3> GetPointsOnSphere(int nPoints)
    {
        float fPoints = (float)nPoints;
 
        float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
        float off = 2 / fPoints;
 
        for (int k = 0; k < nPoints; k++)
        {
            float y = k * off - 1 + (off / 2);
            float r = Mathf.Sqrt(1 - y * y);
            float phi = k * inc;
 
            yield return new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
        }
    }
}

public class Squadron
{
    public Sprite sprite;
}

public class Fleet
{
    public IntVector2 position;
    public int orientation;

    public IntVector2 nextPosition;
    public int nextOrientation;

    public float progress;
    public int visionRange = 2;

    public Player player;
    public Sprite flagshipSprite;

    public Squadron[] squadrons = new Squadron[6];

    public Formation[] formations = new Formation[6];
    public Formation formation;

    public void ChooseFormation(Formation formation, int orientation)
    {
        this.formation = formation;
        nextOrientation = orientation; //(orientation + formation.orientationOffset) % 6;

        nextPosition = HexGrid.Rotate(IntVector2.Up, orientation) + position;
    }

    public Formation GetFormation()
    {
        return test.Translated(test.Rotated(formation, nextOrientation), position);
    }
}
