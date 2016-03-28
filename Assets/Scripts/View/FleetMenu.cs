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
                                                          (c, i) => i.Setup(c));
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

        for (int i = 0; i < neighbours.Length; ++i)
        {
            if (fleet.formations[i] == null) continue;

            yield return new HexItem
            {
                cell = neighbours[i],
                icon = fleet.flagshipSprite,
                active = false,
            };
        }
    }

    public void SetCommand()
    {
        var command = new HexItem
        {
            cell = IntVector2.Zero,
            active = true,
            icon = fleet.commanderSprite,

            action = SetFleet,
        };

        icons.SetActive(new[] { command }.Concat(GetFormations()));
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

        icons.SetActive(fleet);
    }
}
