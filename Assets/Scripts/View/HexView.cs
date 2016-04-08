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
                      Color color,
                      float scale=1f)
    {
        transform.localPosition = HexGrid.HexToWorld(position);
        transform.localRotation = Quaternion.AngleAxis(60 * orientation, Vector3.up);
        transform.localScale = Vector3.one * scale;

        renderer.sprite = icon;
        renderer.color = color;
    }
}
