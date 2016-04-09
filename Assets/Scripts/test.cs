using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

public class test : MonoBehaviour 
{
    [SerializeField] private PlaneCamera camera;
    [SerializeField] private Transform hexParent;

    [SerializeField] private HexView projectionPrefab;
    [SerializeField] private Color powerColor;
    [SerializeField] private Color weakColor;
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

    [SerializeField] private ConflictView conflictPrefab;

    [Header("Hex Sprites")]
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite moveSprite;
    [SerializeField] private Sprite moveSpriteLine;

    private MonoBehaviourPooler<Projection, HexView> hover;
    private MonoBehaviourPooler<Fleet, FleetView> fleets;
    
    private MonoBehaviourPooler<Projection, HexView> prev;
    private MonoBehaviourPooler<Projection, HexView> next;

    private MonoBehaviourPooler<Conflict, ConflictView> conflicts;

    //private MonoBehaviourPooler<, >

    private Dictionary<IntVector2, Color> borderColors 
      = new Dictionary<IntVector2, Color>();

    private class Projection
    {
        public IntVector2 cell;
        public Formation.Cell.Type type;
        public float mult;
        public bool move;
        public int orientation;
    }

    private List<Formation> formations = new List<Formation>();

    public Sprite[] formationIcons;

    private void Awake()
    {
        hover = new MonoBehaviourPooler<Projection, HexView>(projectionPrefab, hexParent);
        prev = new MonoBehaviourPooler<Projection, HexView>(projectionPrefab, hexParent);
        next = new MonoBehaviourPooler<Projection, HexView>(projectionPrefab, hexParent);

        fleets = new MonoBehaviourPooler<Fleet, FleetView>(fleetPrefab, fleetParent, (f, v) => v.Setup(f));

        conflicts = new MonoBehaviourPooler<Conflict, ConflictView>(conflictPrefab, hexParent);

        string path = Application.streamingAssetsPath;

        foreach (var file in System.IO.Directory.GetFiles(path))
        {
            if (!file.EndsWith(".txt")) continue;

            string data = System.IO.File.ReadAllText(file);

            formations.Add(JsonWrapper.Deserialise<Formation>(data));
        }

        Formation.icons = formationIcons;
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
            prev = new Fleet.State(formations[0], position, 0, false),
            next = new Fleet.State(formations[0], position, 0, false),

            player = owner,
            flagshipSprite = flagshipSprite,

            squadrons = Enumerable.Range(0, 3).Select(i => TestSquad()).ToArray(),
            
            formations = formations.ToArray(),
        };
        
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
            MakeFleet(new IntVector2(7, 3), cpu),
            //MakeFleet(new IntVector2(9, 3), cpu),
        };

        fleets.SetActive(fleets_, sort: false);

        ComputeThreat(human);
        ComputeThreat(cpu);


        while (true)
        {
            Color blank = visionColor;
            blank.a = 0;

            var currVision = new HashSet<IntVector2>(fleets_.SelectMany(f => HexGrid.InRange(f.prev.position, f.visionRange)));
            var nextVision = new HashSet<IntVector2>(fleets_.SelectMany(f => HexGrid.InRange(f.next.position, f.visionRange)));

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

    private void ComputeThreat(Player player)
    {
        foreach (Fleet fleet in fleets_.Where(f => f.player != player))
        {
            foreach (Fleet.State state in fleet.possibilities)
            {
                foreach (var pair in state.oriented)
                {
                    IntVector2 position = pair.Key;
                    Formation.Cell cell = pair.Value;

                    if (cell.type == Formation.Cell.Type.Power)
                    {
                        float power;
                        this.power.TryGetValue(position, out power);
                        this.power[position] = power + 1;
                    }
                    else if (cell.type == Formation.Cell.Type.Weakness)
                    {
                        float weak;
                        this.weak.TryGetValue(position, out weak);
                        this.weak[position] = weak + 1;
                    }
                }
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

        var enemies = fleets_.Where(fleet => fleet.player != player);

        IntVector2 center = enemies.Aggregate(IntVector2.Zero, (p, f) => p + f.prev.position)
                          * (1f / enemies.Count());

        foreach (Fleet fleet in fleets_.Where(f => f.player == player))
        {
            float opportunism = Random.value;

            fleet.next = fleet.possibilities.MaxBy(s => Value(s, opportunism) 
                                                      - 0.5f * HexGrid.Distance(s.position, center)
                                                      - 0.1f * Mathf.Abs((Orient(s.position, center) - s.orientation + 6) % 6));
        }
    }

    private int Orient(IntVector2 from, IntVector2 to)
    {
        IntVector2 d = to - from;

        float angle = Mathf.Atan2(d.y, d.x);
        int rotation = Mathf.RoundToInt(angle / (Mathf.PI * 2) * 6);

        return rotation;
    }

    private float Value(Fleet.State state, float opportunism)
    {
        float threat = 0;
        float opportunity = 0;

        var formation = state.oriented;

        foreach (var pair in formation)
        {
            if (pair.Value.type == Formation.Cell.Type.Power)
            {
                opportunity += Get(weak, pair.Key);
            }
            else if (pair.Value.type == Formation.Cell.Type.Weakness)
            {
                threat += Get(power, pair.Key);
            }
        }

        return Mathf.Lerp(threat, opportunity, opportunism);
    }

    private class Conflict
    {
        public IntVector2 cell;
        public HashSet<Fleet> attackers = new HashSet<Fleet>();
        public HashSet<Fleet> defenders = new HashSet<Fleet>();

        public bool valid
        {
            get
            {
                var p1 = attackers.Select(f => f.player).FirstOrDefault();

                return p1 != null 
                    && attackers.Concat(defenders).Any(f => f.player != p1);
            }
        }

        public void Test()
        {
            var players = attackers.Concat(defenders)
                         .Select(f => f.player)
                         .Distinct()
                         .ToArray();


        }
    }

    private IEnumerator FlashFormations()
    {
        Color alpha = new Color(1, 1, 1, 0.75f);

        foreach (Fleet fleet in fleets_)
        {
            var colors = fleet.prev.oriented
                              .ToDictionary(p => p.Key, 
                                            p => p.Value.type == Formation.Cell.Type.Power ? powerColor * alpha 
                                                                                           : weakColor  * alpha);

            yield return StartCoroutine(formationAnim.FadeInColors(.5f, colors));
        }

        yield return new WaitForSeconds(2);

        yield return StartCoroutine(formationAnim.FadeAndClear(.5f));

        var cells = fleets_.SelectMany(fleet => fleet.prev.oriented.Keys).ToArray();
        var conflicts = cells.Distinct().ToDictionary(c => c, c => new Conflict { cell = c });
        
        foreach (Fleet fleet in fleets_)
        {
            foreach (var pair in fleet.prev.oriented)
            {
                Conflict conflict = conflicts[pair.Key];
                Formation.Cell cell = pair.Value;

                if (cell.type == Formation.Cell.Type.Power)
                {
                    conflict.attackers.Add(fleet);
                }
                else if (cell.type == Formation.Cell.Type.Weakness)
                {
                    conflict.defenders.Add(fleet);
                }
            }
        }

        this.conflicts.SetActive(conflicts.Values.Where(c => c.valid));
        this.conflicts.MapActive((c, h) => h.transform.localPosition = HexGrid.HexToWorld(c.cell));

        foreach (Conflict conflict in conflicts.Values.Where(c => c.valid))
        {
            yield return StartCoroutine(formationAnim.FadeInColors(.5f, new Dictionary<IntVector2, Color>
            {
                { conflict.cell, Color.red * alpha * alpha },
            }));

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(2);

        //yield return StartCoroutine(formationAnim.FadeAndClear(.5f));
    }

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
                    fleet.prev = fleet.next;
                }

                time -= 1;

                run -= 1;

                StartCoroutine(FlashFormations());

                ComputeThreat(human);
                ComputeThreat(cpu);
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

        var highlight = fleets_.FirstOrDefault(fleet => fleet.prev.position == cursor) ?? selected;

        var colors = new Dictionary<Formation.Cell.Type, Color>
        {
            { Formation.Cell.Type.Power,    powerColor },
            { Formation.Cell.Type.Weakness, weakColor  },
        };


        borderColors.Clear();

        foreach (Vector3 point in points.Take(1))
        {
            //Vector3 point = cursorv;

            int range = 3;

            foreach (IntVector2 cell in HexGrid.InRange(HexGrid.WorldToHex(point), range))
            {
                Color color;

                if (!borderColors.TryGetValue(cell, out color))
                {
                    color = weakColor;
                    color.a = 0f;
                }

                float limit = range;
                float distance = (point - HexGrid.HexToWorld(cell)).magnitude;

                float current = (1 - distance / range) * 0.125f;

                color.a = Mathf.Max(current, color.a);

                borderColors[cell] = color;
            }
        }

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
                selected = fleets_.FirstOrDefault(fleet => fleet.prev.position == cursor);

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
                    cell.type = cell.type == Formation.Cell.Type.Power ? Formation.Cell.Type.Weakness : Formation.Cell.Type.Power;

                    formation2[cursor] = cell;
                }
                else
                {
                    formation2[cursor] = new Formation.Cell { type = Formation.Cell.Type.Power };
                }
            }
            else if (Input.GetMouseButton(1))
            {
                formation2.Remove(cursor);
            }
            else if (Input.GetKeyDown(KeyCode.M) && existing)
            {
                cell.move = !cell.move;
                formation2[cursor] = cell;
            }
            else if (Input.GetKeyDown(KeyCode.Comma) && existing)
            {
                cell.orientation = (cell.orientation - 1) % 6;
                formation2[cursor] = cell;
            }
            else if (Input.GetKeyDown(KeyCode.Period) && existing)
            {
                cell.orientation = (cell.orientation + 1) % 6;
                formation2[cursor] = cell;
            }

            var prevs = formation2.Select(pair => new Projection
            {
                cell = pair.Key,
                type = pair.Value.type,
                mult = 1,
                orientation = pair.Value.orientation,
                move = pair.Value.move,
            });

            next.SetActive();
            prev.SetActive(prevs, sort: false);
            prev.MapActive((p, v) =>
            {
                v.Setup(p.cell,
                        p.orientation,
                        p.move ? moveSprite : fillSprite,
                        colors[p.type] * 0.75f,
                        scale: .9f);
            });
        }
        else
        {
            var hexes = new List<Projection>();

            foreach (Fleet fleet in fleets_)
            {
                Formation.Cell cell;

                if (fleet.prev.oriented.TryGetValue(cursor, out cell) 
                 && cell.move
                 && selected == null)
                {
                    int orientation = (6 - cell.orientation) % 6;

                    Formation formhover = fleet.prev.formation.Reoriented(cursor, orientation, fleet.prev.flip);

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
                        fleet.next = new Fleet.State(fleet.prev.formation,
                                                     cursor,
                                                     orientation,
                                                     fleet.prev.flip);
                    }
                }
            }

            hover.SetActive(hexes, sort: false);
            hover.MapActive((p, v) =>
            {
                v.Setup(p.cell,
                        p.orientation,
                        p.move ? moveSprite : fillSprite,
                        colors[p.type] * p.mult * 1,
                        scale: 0.95f);
            });

            var prevs = new List<Projection>();
            var nexts = new List<Projection>();

            foreach (Fleet fleet in fleets_)
            {
                if (highlight == null || highlight == fleet)
                {
                    prevs.AddRange(fleet.prev.oriented.Select(p => new Projection
                    {
                        cell = p.Key,
                        type = p.Value.type,
                        mult = 1,
                        move = p.Value.move,
                        orientation = p.Value.orientation,
                    }));
                }

                if (fleet != selected) continue;

                nexts.AddRange(fleet.next.oriented.Select(p => new Projection
                {
                    cell = p.Key,
                    type = p.Value.type,
                    mult = 1,
                    move = p.Value.move,
                    orientation = p.Value.orientation,
                }));
            }

            prev.SetActive(prevs, sort: false);
            prev.MapActive((p, v) =>
            {
                v.Setup(p.cell,
                        p.orientation,
                        p.move ? moveSpriteLine : null,
                        Color.white * 0.25f,
                        scale: .9f);
            });

            next.SetActive(nexts, sort: false);
            next.MapActive((p, v) =>
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
            menu.transform.localPosition = HexGrid.HexToWorld(selected.prev.position);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogFormat("{0}", JsonWrapper.Serialise(formation2));

            System.IO.File.WriteAllText(Application.streamingAssetsPath + "/formation.json.txt", 
                                        JsonWrapper.Serialise(formation2));
        }
    }
}

[JsonArray]
public class Formation : Dictionary<IntVector2, Formation.Cell>
{
    public static Sprite[] icons;

    [JsonObject(IsReference = false)]
    public struct Cell
    {
        public enum Type
        {
            None,
            Power,
            Weakness,
        }

        public Type type;
        public int orientation;
        public bool move;
    }

    public Formation Reoriented(IntVector2 position, 
                                int orientation,
                                bool flip)
    {
        var reoriented = new Formation();

        foreach (var pair in this)
        {
            var hex  = pair.Key;
            var cell = pair.Value;

            hex = flip 
                ? HexGrid.FlipX(hex) 
                : hex;
            hex = HexGrid.Rotate(hex, orientation)
                + position;

            if (flip) cell.orientation = 6 - cell.orientation;

            cell.orientation = (cell.orientation + 6 - orientation) % 6;

            reoriented[hex] = cell;
        }
        
        return reoriented;
    }
}

[System.Serializable]
public class Player
{
    public Sprite portraitSprite;
    public Sprite iconSprite;
}
