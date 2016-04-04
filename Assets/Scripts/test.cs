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
        None,
        Power,
        Weakness,
        Conflict,
    }

    [SerializeField] private PlaneCamera camera;
    [SerializeField] private Transform hexParent;
    [SerializeField] private HexView hexPrefab;
    [SerializeField] private SpriteRenderer visionPrefab;

    [SerializeField] private HexView projectionPrefab;
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

    [SerializeField] private Sprite fighterSprite;
    [SerializeField] private Sprite destroyerSprite;

    [SerializeField] private AudioSource selectSound;

    [SerializeField] private HexGridAnimator formationAnim;

    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite moveSprite;

    private MonoBehaviourPooler<IntVector2, HexView> hexes;
    private MonoBehaviourPooler<Projection, HexView> projections;
    private MonoBehaviourPooler<Projection, HexView> hover;
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
        public bool move;
        public int orientation;
    }

    private List<Formation> formations = new List<Formation>();

    public Sprite[] formationIcons;

    private void Awake()
    {
        hexes = new MonoBehaviourPooler<IntVector2, HexView>(hexPrefab,
                                                             hexParent,
                                                             (c, v) => v.transform.localPosition = HexGrid.HexToWorld(c));

        projections = new MonoBehaviourPooler<Projection, HexView>(projectionPrefab,
                                                                   hexParent,
                                                                   (p, v) => v.Setup(p.cell, 
                                                                                     p.orientation, 
                                                                                     null, 
                                                                                     Color.magenta));

        hover = new MonoBehaviourPooler<Projection, HexView>(projectionPrefab, hexParent);

        vision = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(visionPrefab,
                                                              hexParent,
                                                              (c, v) => v.transform.localPosition = HexGrid.HexToWorld(c));

        visionRange = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(visionPrefab,
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

        Formation__.icons = formationIcons;
    }

    private HashSet<IntVector2> points = new HashSet<IntVector2>();
    private Formation formation2 = new Formation();

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
            sprite = Random.value > 0.5f ? destroyerSprite : fighterSprite,
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

    private Dictionary<IntVector2, float> power = new Dictionary<IntVector2, float>();
    private Dictionary<IntVector2, float> weak = new Dictionary<IntVector2, float>();

    private IEnumerator Start()
    {
        fleets_ = new Fleet[]
        {
            MakeFleet(IntVector2.Zero, human),
            //MakeFleet(new IntVector2(0, 2), human),

            MakeFleet(new IntVector2(3, 3), cpu),
            //MakeFleet(new IntVector2(7, 3), cpu),
            //MakeFleet(new IntVector2(9, 3), cpu),
        };

        fleets.SetActive(fleets_, sort: false);

        ComputeThreat();

        while (true)
        {
            Color blank = visionColor;
            blank.a = 0;

            var currVision = new HashSet<IntVector2>(fleets_.SelectMany(f => HexGrid.InRange(f.position, f.visionRange)));
            var nextVision = new HashSet<IntVector2>(fleets_.SelectMany(f => HexGrid.InRange(f.nextPosition, f.visionRange)));

            /*
            visionRange.SetActive(currVision.Concat(nextVision), sort: false);
            visionRange.MapActive((c, v) => v.color = Color.Lerp(currVision.Contains(c) ? visionColor : blank, 
                                                                 nextVision.Contains(c) ? visionColor : blank, 
                                                                 time));
            */

            fleets.MapActive((f, v) => v.Refresh());

            yield return null;
        }
    }

    private float time;
    private int run;

    private float Get(Dictionary<IntVector2, float> dict, IntVector2 cell)
    {
        if (dict.ContainsKey(cell))
            return dict[cell];

        return 0f;
    }

    private void ComputeThreat()
    {
        return;

        int count = 0;

        foreach (Fleet fleet in fleets_.Where(f => f.player == human))
        {
            foreach (IntVector2 position in Variations(fleet, Type.Power))
            {
                float power;

                this.power.TryGetValue(position, out power);

                this.power[position] = power + 1;

                count += 1;
            }

            foreach (IntVector2 position in Variations(fleet, Type.Weakness))
            {
                float weak;

                this.weak.TryGetValue(position, out weak);

                this.weak[position] = weak + 1;

                count -= 1;
            }
        }

        {
            float max = power.Max(p => p.Value);

            foreach (IntVector2 cell in power.Keys.ToArray())
            {
                power[cell] /= max;
            }

            max = weak.Max(p => p.Value);

            foreach (IntVector2 cell in weak.Keys.ToArray())
            {
                weak[cell] /= max;
            }
        }

        foreach (Fleet fleet in fleets_.Where(f => f.player == cpu))
        {
            float max = -100;

            foreach (Formation formation in fleet.formations)
            {
                for (int r = 0; r < 6; ++r)
                {
                    for (int f = 0; f < 2; ++f)
                    {
                        float value = Value(fleet, formation, r, f == 1);

                        if (value > max)
                        {
                            fleet.flip = f == 1;
                            fleet.ChooseFormation(formation, r);
                            max = value;
                        }
                    }
                }
            }

            if (max <= 0)
            {
                int d = HexGrid.Distance(fleet.position, fleets_[0].position);

                for (int r = 0; r < 6; ++r)
                {
                    IntVector2 position = HexGrid.Rotate(IntVector2.Right, r) + fleet.position;

                    if (HexGrid.Distance(position, fleets_[0].position) < d)
                    {
                        fleet.ChooseFormation(fleet.formations[0], r);
                    }
                }
            }
        }
    }

    private float Value(Fleet fleet, Formation formation, int r, bool f)
    {
        float threat = 0;
        float opportunity = 0;

        if (f) formation = Flipped(formation);
                    
        foreach (var pair in formation)
        {
            IntVector2 cell = HexGrid.Rotate(pair.Key, r) + fleet.position;

            if (pair.Value.type == Type.Power)
            {
                opportunity += Get(weak, cell);
            }
            else if (pair.Value.type == Type.Weakness)
            {
                threat += Get(power, cell);
            }
        }

        return opportunity - 0.5f * threat;
    }

    private IEnumerator FlashFormations()
    {
        Color alpha = new Color(1, 1, 1, 0.75f);

        foreach (Fleet fleet in fleets_)
        {
            var colors = fleet.GetFormation()
                              .ToDictionary(p => p.Key, 
                                            p => p.Value.type == Type.Power ? powerColor * alpha 
                                                                            : weakColor  * alpha);

            yield return StartCoroutine(formationAnim.FadeInColors(.5f, colors));
        }

        yield return new WaitForSeconds(2);

        yield return StartCoroutine(formationAnim.FadeAndClear(.5f));
    }

    private void Update()
    {        
        //visionRange.SetActive(power.Keys.Concat(weak.Keys), sort: true);
        visionRange.MapActive((c, v) => v.color = (Color.red * Get(power, c) + Color.green * Get(weak, c)) * 0.5f);

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

                ComputeThreat();

                StartCoroutine(FlashFormations());
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

        if (highlight != null && !edit)
        {
            foreach (var pair in highlight.GetFormation())
            {
                Color color = colors[pair.Value.type];
                color.a = 1f;

                borderColors[pair.Key] = color;
            }
        }

        //vision.SetActive(borderColors.Keys, sort: false);
        //vision.MapActive((c, v) => v.color = borderColors[c] );

        if (plane.Raycast(ray, out t))
        {
            cursorv = ray.GetPoint(t);

            cursor = HexGrid.WorldToHex(cursorv);

            bool previous = selected != null;

            if (Input.GetMouseButtonDown(0) 
             && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
             && run == 0
             && !edit)
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
            Formation.Cell cell;

            bool existing = formation2.TryGetValue(cursor, out cell);

            if (Input.GetMouseButtonDown(0))
            {
                if (existing)
                {
                    cell.type = cell.type == Type.Power ? Type.Weakness : Type.Power;
                }
                else
                {
                    formation2[cursor] = new Formation.Cell { type = Type.Power };
                }
            }
            else if (Input.GetMouseButton(1))
            {
                formation2.Remove(cursor);
            }
            else if (Input.GetKeyDown(KeyCode.M) && existing)
            {
                cell.move = !cell.move;
            }
            else if (Input.GetKeyDown(KeyCode.Comma) && existing)
            {
                cell.orientation = (cell.orientation - 1) % 6;
            }
            else if (Input.GetKeyDown(KeyCode.Period) && existing)
            {
                cell.orientation = (cell.orientation + 1) % 6;
            }
        }
        else
        {
            var hexes = new List<Projection>();

            foreach (Fleet fleet in fleets_)
            {
                Formation.Cell cell;
                Formation formation = fleet.GetFormation();
                Formation formreal = fleet.formation;

                if (formation.TryGetValue(cursor, out cell) && cell.move)
                {
                    int orientation = (6 - cell.orientation) % 6;

                    Formation formhover = formreal.Reoriented(cursor, orientation, fleet.flip);

                    hexes.AddRange(formhover.Select(p => new Projection
                    {
                        cell = p.Key,
                        type = p.Value.type,
                        mult = 1,
                        move = p.Value.move,
                        orientation = p.Value.orientation,
                    }));

                    if (Input.GetMouseButtonDown(0))// && fleet.player == human)
                    {
                        fleet.nextPosition = cursor;
                        fleet.nextOrientation = orientation;
                    }
                }
            }

            hover.SetActive(hexes, sort: false);
            hover.MapActive((p, v) =>
            {
                v.Setup(p.cell,
                        p.orientation,
                        p.move ? moveSprite : fillSprite,
                        colors[p.type] * p.mult * 1);
            });
        }

        menu.gameObject.SetActive(selected != null);

        if (selected != null)
        {
            menu.transform.localPosition = HexGrid.HexToWorld(selected.position);
        }

        var allforms = fleets_.Where(fleet => fleet.formation != null)
                              .SelectMany(fleet => fleet.GetFormation().Select(p => new Projection
                              {
                                  cell = p.Key,
                                  type = p.Value.type,
                                  mult = fleet == highlight ? 1 : 0.75f,
                                  move = p.Value.move,
                                  orientation = p.Value.orientation,
                              } ))
                              .ToArray();

        {
            hexes.SetActive(new[] { cursor }, sort: false);

            if (!edit)
            {
                projections.SetActive(allforms, sort: false);
            }
            else
            {
                var p = formation2.Select(pair => new Projection
                {
                    cell = pair.Key,
                    type = pair.Value.type,
                    mult = 1,
                    orientation = pair.Value.orientation,
                    move = pair.Value.move,
                });

                projections.SetActive(p, sort: false);
            }

            projections.MapActive((p, v) =>
            {
                v.Setup(p.cell, 
                        p.orientation, 
                        p.move ? moveSprite : fillSprite, 
                        colors[p.type] * p.mult);
            });
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogFormat("{0}", JsonWrapper.Serialise(formation2));

            System.IO.File.WriteAllText(Application.streamingAssetsPath + "/formation.json.txt", 
                                        JsonWrapper.Serialise(formation2));
        }
    }

    public IEnumerable<IntVector2> Variations(Fleet fleet,
                                              Type type)
    {
        foreach (Formation formation in fleet.formations)
        {
            for (int r = 0; r < 6; ++r)
            {
                for (int f = 0; f < 2; ++f)
                {
                    Formation variant = formation;

                    if (f == 1) variant = Flipped(variant);
                    
                    foreach (var pair in variant)
                    {
                        if (pair.Value.type == type) yield return HexGrid.Rotate(pair.Key, r) + fleet.position;
                    }
                }
            }
        }
    }

    public static Formation Flipped(Formation formation)
    {
        var flipped = new Formation();

        foreach (var pair in formation)
        {
            IntVector2 prev = pair.Key;
            IntVector2 next = prev;
            
            if (prev.x != 0)
            {
                next.x = -prev.x;
                next.y = -prev.z;
            }

            var cell = pair.Value;

            flipped[next] = new Formation.Cell
            {
                move = cell.move,
                type = cell.type,
                orientation = 6 - cell.orientation,
            };
        }

        return flipped;
    }

    public static Formation Rotated(Formation formation, int rotation)
    {
        var rotated = new Formation();

        foreach (var pair in formation)
        {
            rotated[HexGrid.Rotate(pair.Key, rotation)] = new Formation.Cell
            {
                move = pair.Value.move,
                orientation = (pair.Value.orientation + 6 - rotation) % 6,
                type = pair.Value.type,
            };
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
public class Formation__ : Dictionary<IntVector2, test.Type>
{
    public int orientationOffset;
    public static Sprite[] icons;
};

[JsonArray]
public class Formation : Dictionary<IntVector2, Formation.Cell>
{
    [JsonObject(IsReference = false)]
    public class Cell
    {
        public test.Type type;
        public int orientation;
        public bool move;
    }

    public Formation Reoriented(IntVector2 position, 
                                int orientation,
                                bool flip=false)
    {
        var form = flip ? test.Flipped(this) : this;
        
        return test.Translated(test.Rotated(form, orientation), position);
    }
}

[System.Serializable]
public class Player
{
    public Sprite portraitSprite;
    public Sprite iconSprite;
}
