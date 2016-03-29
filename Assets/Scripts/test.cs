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
        Conflict,
    }

    [SerializeField] private PlaneCamera camera;
    [SerializeField] private Transform hexParent;
    [SerializeField] private HexView hexPrefab;
    [SerializeField] private SpriteRenderer visionPrefab;

    [SerializeField] private SpriteRenderer projectionPrefab;
    [SerializeField] private Color powerColor;
    [SerializeField] private Color weakColor;
    [SerializeField] private Color conflictColor;
    [SerializeField] private Color visionColor;

    [SerializeField] private float period;
    [SerializeField] private AnimationCurve curve;

    [SerializeField] private FleetView fleetPrefab;
    [SerializeField] private Transform fleetParent;

    [SerializeField] private FleetMenu menu;
    [SerializeField] private Sprite commanderSprite;
    [SerializeField] private Sprite flagshipSprite;
    [SerializeField] private Sprite commanderSprite2;

    [SerializeField] private AudioSource selectSound;

    private MonoBehaviourPooler<IntVector2, HexView> hexes;
    private MonoBehaviourPooler<Projection, SpriteRenderer> projections;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> vision;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> visionRange;
    private MonoBehaviourPooler<Fleet, FleetView> fleets;

    private Dictionary<IntVector2, Color> borderColors 
        = new Dictionary<IntVector2, Color>();

    private class Projection
    {
        public IntVector2 cell;
        public Type type;
        public float mult;
    }

    private List<Formation> formations = new List<Formation>();

    public Sprite[] formationIcons;

    private void Awake()
    {
        hexes = new MonoBehaviourPooler<IntVector2, HexView>(hexPrefab,
                                                             hexParent,
                                                             (c, v) => v.transform.localPosition = HexGrid.HexToWorld(c));

        projections = new MonoBehaviourPooler<Projection, SpriteRenderer>(projectionPrefab,
                                                                          hexParent,
                                                                          (p, v) => { v.transform.localPosition = HexGrid.HexToWorld(p.cell); });

        vision = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(visionPrefab,
                                                              hexParent,
                                                              (c, v) => v.transform.localPosition = HexGrid.HexToWorld(c));

        visionRange = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(projectionPrefab,
                                                              hexParent,
                                                              (c, v) => { v.transform.localPosition = HexGrid.HexToWorld(c); });

        fleets = new MonoBehaviourPooler<Fleet, FleetView>(fleetPrefab, fleetParent, (f, v) => v.Setup(f));

        string path = Application.streamingAssetsPath;

        foreach (var file in System.IO.Directory.GetFiles(path))
        {
            if (!file.EndsWith(".txt")) continue;

            string data = System.IO.File.ReadAllText(file);

            formations.Add(JsonWrapper.Deserialise<Formation>(data));
        }

        formation = new Formation();
        Formation.icons = formationIcons;
    }

    private HashSet<IntVector2> points = new HashSet<IntVector2>();
    private Formation formation = new Formation();

    private IntVector2 cursor;
    public int rotation;
    private Vector3 cursorv;

    private bool edit;

    private Dictionary<IntVector2, float> visions = new Dictionary<IntVector2, float>();

    private Fleet[] fleets_;

    private Fleet selected;

    private Squadron TestSquad()
    {
        return new Squadron
        {
            sprite = flagshipSprite,
        };
    }

    public Player human;
    public Player cpu;

    private Fleet MakeFleet(IntVector2 position, Player owner)
    {
        var fleet = new Fleet
        {
            position = position,
            orientation = 0,

            nextPosition = position,
            nextOrientation = 0,

            player = owner,
            flagshipSprite = flagshipSprite,

            squadrons = Enumerable.Range(0, 3).Select(i => TestSquad()).ToArray(),

            formation = formations[0],
            formations = formations.ToArray(),
        };

        fleet.ChooseFormation(fleet.formation, fleet.orientation);

        return fleet;
    }

    private IEnumerator Start()
    {
        fleets_ = new Fleet[]
        {
            MakeFleet(IntVector2.Zero, human),
            MakeFleet(new IntVector2(0, 2), human),

            MakeFleet(new IntVector2(5, 3), cpu),
            MakeFleet(new IntVector2(7, 3), cpu),
            MakeFleet(new IntVector2(9, 3), cpu),
        };

        fleets.SetActive(fleets_, sort: false);

        while (true)
        {
            Color blank = visionColor;
            blank.a = 0;

            var currVision = new HashSet<IntVector2>(fleets_.SelectMany(f => HexGrid.InRange(f.position, f.visionRange)));
            var nextVision = new HashSet<IntVector2>(fleets_.SelectMany(f => HexGrid.InRange(f.nextPosition, f.visionRange)));

            visionRange.SetActive(currVision.Concat(nextVision), sort: false);
            visionRange.MapActive((c, v) => v.color = Color.Lerp(currVision.Contains(c) ? visionColor : blank, 
                                                                 nextVision.Contains(c) ? visionColor : blank, 
                                                                 time));

            fleets.MapActive((f, v) => v.Refresh());

            yield return null;
        }
    }

    private float time;
    private int run;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            selected = null;

            run += 1;
        }

        if (run > 0)
        {
            time += Time.deltaTime / period;

            if (time > 1)
            {
                float u = time % 1;

                foreach (Fleet fleet in fleets_)
                {
                    fleet.position = fleet.nextPosition;
                    fleet.orientation = fleet.nextOrientation;

                    var n = HexGrid.Neighbours(fleet.position).ToArray();

                    fleet.ChooseFormation(fleet.formation, fleet.orientation);
                }

                time -= 1;

                run -= 1;
            }

            fleets.MapActive((f, v) =>
            {
                f.progress = time;
                v.Refresh();
            });
        }

        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float t;

        var points = new [] { cursorv }.Concat(fleets.Instances.Select(fleet => fleet.transform.localPosition)).ToArray();

        camera.worldCenter = points.Skip(1).Aggregate((a, b) => a + b) * (1f / (points.Length - 1));
        camera.worldRadius = points.Skip(1).SelectMany(x => points.Skip(1), (x, y) => new { a = x, b = y }).Max(g => (g.a - g.b).magnitude);

        camera.worldRadius = Mathf.Max(camera.worldRadius, 3);

        var highlight = fleets_.FirstOrDefault(fleet => fleet.position == cursor) ?? selected;

        var colors = new Dictionary<Type, Color>
        {
            { Type.Power,    powerColor    },
            { Type.Weakness, weakColor     },
            { Type.Conflict, conflictColor },
        };


        borderColors.Clear();

        foreach (Vector3 point in points)
        {
            //Vector3 point = cursorv;

            foreach (IntVector2 cell in HexGrid.InRange(HexGrid.WorldToHex(point), 5))
            {
                Color color;

                if (!borderColors.TryGetValue(cell, out color))
                {
                    color = weakColor;
                    color.a = 0f;
                }

                float current = (1 - (point - HexGrid.HexToWorld(cell)).magnitude / 4f) * 0.25f;

                color.a = Mathf.Max(current, color.a);

                borderColors[cell] = color;
            }
        }

        if (highlight != null)
        {
            foreach (var pair in highlight.GetFormation())
            {
                Color color = colors[pair.Value];
                color.a = 1f;

                borderColors[pair.Key] = color;
            }
        }

        vision.SetActive(borderColors.Keys, sort: false);
        vision.MapActive((c, v) => v.color = borderColors[c] );

        if (plane.Raycast(ray, out t))
        {
            cursorv = ray.GetPoint(t);

            cursor = HexGrid.WorldToHex(cursorv);

            bool previous = selected != null;

            if (Input.GetMouseButtonDown(0) 
             && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
             && run == 0)
            {
                selected = fleets_.FirstOrDefault(fleet => fleet.position == cursor);

                if (selected != null)
                {
                    menu.Setup(selected);

                    selectSound.Play();
                }
                else if (previous)
                {
                    selectSound.Play();
                }
            }
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

        menu.gameObject.SetActive(selected != null);

        if (selected != null)
        {
            menu.transform.localPosition = HexGrid.HexToWorld(selected.position);
        }

        var allforms = fleets_.Where(fleet => fleet.formation != null && fleet.player == human)
                              .SelectMany(fleet => fleet.GetFormation().Select(p => new Projection { cell = p.Key, type = p.Value, mult = fleet == highlight ? 1 : 0.75f } ))
                              .ToArray();

        {
            hexes.SetActive(new[] { cursor }, sort: false);

            if (!edit)
            {
                projections.SetActive(allforms, sort: false);
            }
            else
            {
                var p = formation.Select(p_ => new Projection { cell = p_.Key, type = p_.Value, mult = 1 });

                projections.SetActive(p, sort: false);
            }

            projections.MapActive((p, v) => v.color = colors[p.type] * p.mult);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogFormat("{0}", JsonWrapper.Serialise(formation));

            System.IO.File.WriteAllText(Application.streamingAssetsPath + "/formation.json.txt", 
                                        JsonWrapper.Serialise(formation));
        }
    }

    public static Formation Rotated(Formation formation, int rotation)
    {
        var rotated = new Formation();

        foreach (var pair in formation)
        {
            rotated[HexGrid.Rotate(pair.Key, rotation)] = pair.Value;
        }

        return rotated;
    }

    public static Formation Translated(Formation formation, IntVector2 translation)
    {
        var translated = new Formation();

        foreach (var pair in formation)
        {
            translated[pair.Key + translation] = pair.Value;
        }

        return translated;
    }
}

[JsonArray]
public class Formation : Dictionary<IntVector2, test.Type>
{
    public static Sprite[] icons;
};

[System.Serializable]
public class Player
{
    public Sprite portraitSprite;
    public Sprite iconSprite;
}
