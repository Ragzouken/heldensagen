using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class FleetMenu : MonoBehaviour 
{
    [SerializeField] private HexIcon iconPrefab;

    private MonoBehaviourPooler<HexItem, HexIcon> icons;

    private Fleet fleet;

    private void Awake()
    {
        icons = new MonoBehaviourPooler<HexItem, HexIcon>(iconPrefab,
                                                          transform,
                                                          (c, i) => { i.Setup(c); i.fleet = fleet; });
    }

    public void Setup(Fleet fleet)
    {
        this.fleet = fleet;

        transform.localPosition = HexGrid.HexToWorld(fleet.nextPosition);

        SetCommand();
    }

    private IEnumerable<HexItem> GetFormations()
    {
        var neighbours = HexGrid.Neighbours(IntVector2.Zero).ToArray();

        var list = new List<HexItem>();

        for (int i = 0; i < Mathf.Min(fleet.formations.Length, 6); ++i)
        {
            Formation formation = fleet.formations[i];

            if (formation == null) continue;

            int j = i;
            float v = UnityEngine.Random.value;

            list.Add(new HexItem
            {
                cell = neighbours[i],
                icon = fleet.flagshipSprite,
                active = true,
                action = () => fleet.formation = formation,
            });
        }

        return list;
    }

    private IEnumerable<HexItem> GetSquadrons()
    {
        var neighbours = HexGrid.Neighbours(IntVector2.Zero).ToArray();

        var list = new List<HexItem>();

        for (int i = 0; i < Mathf.Min(fleet.squadrons.Length, 6); ++i)
        {
            Squadron squadron = fleet.squadrons[i];

            if (squadron == null) continue;

            list.Add(new HexItem
            {
                cell = neighbours[i],
                icon = squadron.sprite,
                active = false,
            });
        }

        return list;
    }

    public void SetCommand()
    {
        var command = new HexItem
        {
            cell = IntVector2.Zero,
            active = true,
            icon = fleet.player.iconSprite,

            action = SetFleet,
        };

        var things = GetFormations().ToArray();

        icons.SetActive(new[] { command }.Concat(things));
    }

    public void SetFleet()
    {
        var fleet = new HexItem
        {
            cell = IntVector2.Zero,
            active = true,
            icon = this.fleet.flagshipSprite,

            action = SetCommand,
        };

        icons.SetActive(new[] { fleet }.Concat(GetSquadrons()));
    }
}
