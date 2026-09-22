using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIClickDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            //Debug.Log($"UI elements hit: {results.Count}");
            foreach (var result in results)
            {
                var graphic = result.gameObject.GetComponent<Graphic>();
                Debug.Log($"- {result.gameObject.name} (Raycast Target: {graphic != null && graphic.raycastTarget})");
            }
        }
    }
}