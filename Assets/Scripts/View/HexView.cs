using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class HexView : MonoBehaviour 
{
    public new SpriteRenderer renderer;

    public void Setup(IntVector2 position,
                      int orientation,
                      Sprite icon,
                      Color color)
    {
        transform.localPosition = HexGrid.HexToWorld(position);
        transform.localRotation = Quaternion.AngleAxis(60 * orientation, Vector3.up);

        renderer.sprite = icon;
        renderer.color = color;
    }
}
