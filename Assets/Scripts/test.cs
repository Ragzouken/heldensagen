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

    private IntVector2[] points = new IntVector2[0];

    private IEnumerator Start()
    {
        hexes.SetActive(HexGrid.Neighbours(IntVector2.Zero));

        points = Enumerable.Range(0, 4).Select(y => new IntVector2(0, y)).ToArray();

        while (true)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = HexGrid.Rotate(points[i], 1);
            }

            yield return new WaitForSeconds(.25f);
        }
    }

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

        hexes.SetActive(new[] { cursor }, sort: false);
        projections.SetActive(points, sort: false);
    }
}
