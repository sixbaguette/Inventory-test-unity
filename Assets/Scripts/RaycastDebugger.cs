using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            Debug.Log("=== RAYCAST DEBUG ===");
            foreach (var r in results)
            {
                Debug.Log($"Hit: {r.gameObject.name} (Layer: {r.gameObject.layer})");
            }
        }
    }
}
