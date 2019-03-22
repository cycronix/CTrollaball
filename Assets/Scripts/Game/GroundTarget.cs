/*
Copyright 2019 Cycronix
 
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
 
    http://www.apache.org/licenses/LICENSE-2.0
 
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GroundTarget : MonoBehaviour
{
    public float maxDistance = 1000f;
    private CTunity ctunity;
    private Vector3 targetPos = Vector3.one;
    internal GameObject targetObj = null;
    private Camera mainCamera = null;

    // Use this for initialization
    void Start()
    {
//       ctunity = GameObject.Find("CTunity");
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        targetPos = transform.position;
        mainCamera = Camera.main;                   // up front for efficiency
    }

    void OnGUI()
    {
        Event m_Event = Event.current;
        if (m_Event.button != 0) return;                    // only check left-mouse button?
        if (!ctunity.activePlayer(gameObject)) return;

        if (EventSystem.current.IsPointerOverGameObject()) return;         // no deal if clicking on UI element

        if (    (m_Event.type == EventType.MouseDown  && m_Event.clickCount == 2) 
            /* ||  (m_Event.button == 1 && m_Event.type == EventType.MouseDown )
            /* || (m_Event.type == EventType.MouseDrag) */ )
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                if(hit.collider.gameObject == gameObject)  // double click delete target
                {
                    Debug.Log(name + " Buh Bye");
                    ctunity.clearObject(gameObject);
                    return;
                }

                if (hit.collider.gameObject.GetComponent<CTclient>() == null)    // no target CTclient player objects
                    targetPos = hit.point;
                // Debug.Log("SET targetPos: " + targetPos + ", targetObj: " + CTunity.fullName(targetObj));
                // GameObject.Find("Main Camera").GetComponent<maxCamera>().setTarget(hit.transform);
            }
            m_Event.Use();
        }
    }

    private void Update()
    {
        if (!ctunity.activePlayer(gameObject)) return;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime);
    }
}
