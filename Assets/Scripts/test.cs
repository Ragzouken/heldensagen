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

    [SerializeField] private Transform hexParent;
    [SerializeField] private HexView hexPrefab;

    [SerializeField] private SpriteRenderer projectionPrefab;
    [SerializeField] private Color powerColor;
    [SerializeField] private Color weakColor;

    private MonoBehaviourPooler<IntVector2, HexView> hexes;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> projections;

    private void Awake()
    {
        hexes = new MonoBehaviourPooler<IntVector2, HexView>(hexPrefab,
                                                             hexParent,
                                                             (c, v) => v.transform.position = HexGrid.HexToWorld(c));

        projections = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(projectionPrefab,
                                                                          hexParent,
                                                                          (c, v) => { v.transform.position = HexGrid.HexToWorld(c); });
    }

    [JsonArray]
    private class Formation : Dictionary<IntVector2, Type> { };

    private HashSet<IntVector2> points = new HashSet<IntVector2>();
    private Formation formation = new Formation();

    private IntVector2 cursor;

    private void Update()
    {
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float t;

        if (plane.Raycast(ray, out t))
        {
            Vector3 point = ray.GetPoint(t);

            cursor = HexGrid.WorldToHex(point);
        }

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

        var colors = new Dictionary<Type, Color>
        {
            { Type.Power, powerColor },
            { Type.Weakness, weakColor },
        };

        hexes.SetActive(new[] { cursor }, sort: false);
        projections.SetActive(formation.Keys, sort: false);
        projections.MapActive((c, v) => v.color = colors[formation[c]]);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogFormat("{0}", JsonWrapper.Serialise(formation));
        }
    }
}
