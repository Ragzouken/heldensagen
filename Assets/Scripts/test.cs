using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

public class test : MonoBehaviour 
{
    public enum Type
    {
        Power,
        Weakness,
    }

    [SerializeField] private PlaneCamera camera;
    [SerializeField] private Transform hexParent;
    [SerializeField] private HexView hexPrefab;
    [SerializeField] private SpriteRenderer visionPrefab;

    [SerializeField] private SpriteRenderer projectionPrefab;
    [SerializeField] private Color powerColor;
    [SerializeField] private Color weakColor;
    [SerializeField] private Color visionColor;

    [SerializeField] private Transform flagship;
    [SerializeField] private float period;
    [SerializeField] private AnimationCurve curve;

    private MonoBehaviourPooler<IntVector2, HexView> hexes;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> projections;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> vision;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> visionRange;

    private void Awake()
    {
        hexes = new MonoBehaviourPooler<IntVector2, HexView>(hexPrefab,
                                                             hexParent,
                                                             (c, v) => v.transform.position = HexGrid.HexToWorld(c));

        projections = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(projectionPrefab,
                                                                          hexParent,
                                                                          (c, v) => { v.transform.position = HexGrid.HexToWorld(c); });

        vision = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(visionPrefab,
                                                              hexParent,
                                                              (c, v) => v.transform.position = HexGrid.HexToWorld(c));

        visionRange = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(projectionPrefab,
                                                              hexParent,
                                                              (c, v) => { v.transform.position = HexGrid.HexToWorld(c); });

        string data = System.IO.File.ReadAllText(Application.streamingAssetsPath + "/formation.json.txt");

        formation = JsonWrapper.Deserialise<Formation>(data);
    }

    [JsonArray]
    private class Formation : Dictionary<IntVector2, Type> { };

    private HashSet<IntVector2> points = new HashSet<IntVector2>();
    private Formation formation = new Formation();

    private IntVector2 cursor;
    private int rotation;
    private Vector3 cursorv;

    private bool edit;

    private Dictionary<IntVector2, float> visions = new Dictionary<IntVector2, float>();

    private IEnumerator Start()
    {
        while (true)
        {
            float t = (Time.timeSinceLevelLoad % period) / period;
            //float t = Mathf.PingPong(Time.timeSinceLevelLoad, period) / period;

            Vector3 start  = HexGrid.HexToWorld(IntVector2.Zero);
            Vector3 finish = HexGrid.HexToWorld(IntVector2.Down);
            var startq = Quaternion.AngleAxis(120, Vector3.up);
            var finishq = Quaternion.AngleAxis(0, Vector3.up);

            t = curve.Evaluate(t);

            flagship.localPosition = Vector3.Lerp(start, finish, t);
            flagship.rotation = Quaternion.Slerp(startq, finishq, t * 1.25f);

            yield return null;
        }
    }

    private void Update()
    {
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float t;

        var points = new[] { flagship.position, HexGrid.HexToWorld(new IntVector2(5, 5)), cursorv };

        camera.worldCenter = points.Aggregate((a, b) => a + b) * (1f / points.Length);
        camera.worldRadius = points.SelectMany(x => points, (x, y) => new { a = x, b = y }).Max(g => (g.a - g.b).magnitude);

        visions.Clear();

        foreach (Vector3 point in points)
        {
            foreach (IntVector2 cell in HexGrid.InRange(HexGrid.WorldToHex(point), 6))
            {
                float alpha;

                if (!visions.TryGetValue(cell, out alpha))
                {
                    alpha = 0f;
                }

                float current = (1 - (point - HexGrid.HexToWorld(cell)).magnitude / 4f) * 0.25f;

                visions[cell] = Mathf.Max(alpha, current);
            }
        }

        vision.SetActive(visions.Keys);
        vision.MapActive((c, v) => { var co = v.color; co.a = visions[c]; v.color = co; } );

        int vrange = 2;

        visionRange.SetActive(HexGrid.InRange(Vector2.zero, vrange));
        visionRange.MapActive((c, v) => v.color = visionColor);

        if (plane.Raycast(ray, out t))
        {
            cursorv = ray.GetPoint(t);

            cursor = HexGrid.WorldToHex(cursorv);
        }

        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            edit = !edit;
        }

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            rotation = (rotation - 1 + 6) % 6;
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            rotation = (rotation + 1 + 6) % 6;
        }

        if (edit)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Type type;

                if (formation.TryGetValue(cursor, out type))
                {
                    formation[cursor] = type == Type.Power ? Type.Weakness : Type.Power;
                }
                else
                {
                    formation[cursor] = Type.Power;
                }
            }
            else if (Input.GetMouseButton(1))
            {
                formation.Remove(cursor);
            }
        }

        var colors = new Dictionary<Type, Color>
        {
            { Type.Power, powerColor },
            { Type.Weakness, weakColor },
        };

        var form = Translated(Rotated(formation, rotation), cursor);
        if (edit) form = formation;

        hexes.SetActive(new[] { cursor }, sort: false);
        projections.SetActive(form.Keys, sort: false);
        projections.MapActive((c, v) => v.color = colors[form[c]]);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogFormat("{0}", JsonWrapper.Serialise(formation));

            System.IO.File.WriteAllText(Application.streamingAssetsPath + "/formation.json.txt", 
                                        JsonWrapper.Serialise(formation));
        }
    }

    private Formation Rotated(Formation formation, int rotation)
    {
        var rotated = new Formation();

        foreach (var pair in formation)
        {
            rotated[HexGrid.Rotate(pair.Key, rotation)] = pair.Value;
        }

        return rotated;
    }

    private Formation Translated(Formation formation, IntVector2 translation)
    {
        var translated = new Formation();

        foreach (var pair in formation)
        {
            translated[pair.Key + translation] = pair.Value;
        }

        return translated;
    }
}
