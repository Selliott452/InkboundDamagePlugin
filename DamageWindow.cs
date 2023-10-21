using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InkboundDamage;

public class DamageWindow : MonoBehaviour
{
    private void Awake()
    {

    }
}


 
public class DraggableWindowScript : MonoBehaviour, IDragHandler
{
    public Canvas canvas;
 
    private RectTransform rectTransform;
 
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }
 
    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}