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

    private MonoBehaviourPooler<IntVector2, HexView> hexes;

    private void Awake()
    {
        hexes = new MonoBehaviourPooler<IntVector2, HexView>(hexPrefab,
                                                             hexParent,
                                                             (c, v) => v.transform.position = HexGrid.HexToWorld(c));
    }

    private IEnumerator Start()
    {
        hexes.SetActive(HexGrid.Neighbours(IntVector2.Zero));

        var points = Enumerable.Range(0, 4).Select(y => new IntVector2(0, y)).ToArray();

        while (true)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                //points[i] = HexGrid.Rotate(points[i], 1);
            }

            hexes.SetActive(points.Concat(new[] { cursor }), sort: false);

            yield return new WaitForSeconds(.125f);
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

            Debug.LogFormat("y = {0}", cursor.y);
        }
    }
}
