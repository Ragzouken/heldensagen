using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

public class HexIcon : MonoBehaviour, IDragHandler
{
    public enum Follow
    {
        None,
        Camera,
        Center,
    }

    public PlaneCamera camera;

    [SerializeField] private Image iconImage;
    [SerializeField] private Image backImage;
    [SerializeField] private Image frameImage;

    [SerializeField] private Button button;

    public HexIcon parent;
    public Follow follow;

    public test t;

    private HexItem item;

    private void Awake()
    {
        button.onClick.AddListener(() => item.action());
    }

    private void Update()
    {
        if (follow == Follow.Camera)
        {
            iconImage.transform.rotation = Quaternion.AngleAxis(camera.angle, Vector3.up) 
                                         * Quaternion.AngleAxis(90,           Vector3.right);
        }
        else if (follow == Follow.Center)
        {
            
        }
    }

    public void Setup(HexItem item)
    {
        this.item = item;

        iconImage.sprite = item.icon;
        button.interactable = item.active;

        transform.localPosition = HexGrid.HexToWorld(item.cell);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        var plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray1 = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
        Ray ray2 = eventData.pressEventCamera.ScreenPointToRay(eventData.pressPosition);
        float t1, t2;

        plane.Raycast(ray1, out t1);
        plane.Raycast(ray2, out t2);

        Vector3 d = ray1.GetPoint(t1) - transform.parent.position; //ray2.GetPoint(t2);
        Vector2 vector = new Vector2(d.x, d.z);

        float angle = Mathf.Atan2(vector.y, vector.x);
        int rotation = Mathf.RoundToInt(angle / (Mathf.PI * 2) * 6);

        t.rotation = rotation;
    }
}

public class HexItem
{
    public IntVector2 cell;
    public Sprite icon;
    public Action action;
    public bool active;
}
