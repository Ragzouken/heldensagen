using UnityEngine;
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

    public void Refresh(float progress=0f)
    {
        Vector3 currPos = HexGrid.HexToWorld(fleet.prev.position);
        Vector3 nextPos = HexGrid.HexToWorld(fleet.next.position);

        var currAngle = Quaternion.AngleAxis(fleet.prev.orientation * 60, -Vector3.up);
        var nextAngle = Quaternion.AngleAxis(fleet.next.orientation * 60, -Vector3.up);

        transform.position =     Vector3.Lerp(currPos,   nextPos,   moveCurve.Evaluate(progress));
        transform.rotation = Quaternion.Slerp(currAngle, nextAngle, turnCurve.Evaluate(progress));

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
    public struct State
    {
        public Formation formation;
        public IntVector2 position;
        public int orientation;
        public bool flip;

        public Formation oriented
        {
            get
            {
                return formation.Reoriented(position, orientation, flip);
            }
        }

        public State(Formation formation, 
                     IntVector2 position,
                     int orientation,
                     bool flip)
        {
            this.formation = formation;
            this.position = position;
            this.orientation = orientation;
            this.flip = flip;
        }
    }


    public State prev, next;

    public int visionRange = 2;
    public int ships = 8;

    public Player player;
    public Sprite flagshipSprite;

    public Squadron[] squadrons = new Squadron[6];

    public Formation[] formations = new Formation[6];

    public IEnumerable<State> possibilities
    {
        get
        {
            foreach (var pair in prev.oriented.Where(p => p.Value.move))
            {
                IntVector2 position = pair.Key;
                Formation.Cell cell = pair.Value;

                yield return new State(prev.formation, 
                                       position, 
                                       (6 - cell.orientation) % 6, 
                                       prev.flip);
            }

            foreach (var formation in formations)
            {
                for (int r = 0; r < 6; ++r)
                {
                    for (int f = 0; f < 2; ++f)
                    {
                        yield return new State(formation,
                                               prev.position,
                                               r,
                                               f == 1);
                    }
                }
            }
        }
    }
}
