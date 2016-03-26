﻿using UnityEngine;
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

        string data = System.IO.File.ReadAllText(Application.streamingAssetsPath + "/formation.json.txt");

        formation = JsonWrapper.Deserialise<Formation>(data);
    }

    [JsonArray]
    private class Formation : Dictionary<IntVector2, Type> { };

    private HashSet<IntVector2> points = new HashSet<IntVector2>();
    private Formation formation = new Formation();

    private IntVector2 cursor;
    private int rotation;

    private bool edit;

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

        Debug.Log(rotation);

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
