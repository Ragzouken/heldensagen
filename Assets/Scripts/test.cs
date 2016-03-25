using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class test : MonoBehaviour 
{
    [SerializeField] private Transform hexParent;
    [SerializeField] private HexView hexPrefab;

    [SerializeField] private SpriteRenderer projectionPrefab;
    [SerializeField] private Color projectionColor;

    private MonoBehaviourPooler<IntVector2, HexView> hexes;
    private MonoBehaviourPooler<IntVector2, SpriteRenderer> projections;

    private void Awake()
    {
        hexes = new MonoBehaviourPooler<IntVector2, HexView>(hexPrefab,
                                                             hexParent,
                                                             (c, v) => v.transform.position = HexGrid.HexToWorld(c));

        projections = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(projectionPrefab,
                                                                          hexParent,
                                                                          (c, v) => { v.color = projectionColor; v.transform.position = HexGrid.HexToWorld(c); });
    }

    private HashSet<IntVector2> points = new HashSet<IntVector2>();

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

        if (Input.GetMouseButton(0))
        {
            points.Add(cursor);
        }
        else if (Input.GetMouseButton(1))
        {
            points.Remove(cursor);
        }

        hexes.SetActive(new[] { cursor }, sort: false);
        projections.SetActive(points, sort: false);
    }
}
