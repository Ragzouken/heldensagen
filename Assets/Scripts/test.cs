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

        var points = HexGrid.Neighbours(IntVector2.One).Concat(new[] { IntVector2.One }).ToArray();

        yield return new WaitForSeconds(2);

        while (true)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = HexGrid.Rotate(points[i], 1);
            }

            hexes.SetActive(points, sort: false);

            yield return new WaitForSeconds(.125f);
        }
    }
}
