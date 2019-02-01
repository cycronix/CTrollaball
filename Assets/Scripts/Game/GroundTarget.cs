/*
Copyright 2018 Cycronix
 
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
    private GameObject ctunity;

    // Use this for initialization
    void Start()
    {
        ctunity = GameObject.Find("CTunity");
    }

    void OnGUI()
    {
        Event m_Event = Event.current;

        if (m_Event.button != 0) return;                    // only check left-mouse button?

        if (EventSystem.current.IsPointerOverGameObject())          // no orbit if clicking on UI element
        {
            return;
        }

        // TO DO:  follow mouse drag...

        if (m_Event.type == EventType.MouseDown && m_Event.clickCount == 2)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                //            Debug.Log("hit: " + hit.point + ", t.pos: " + transform.position);
                ctunity.transform.position = hit.point;
       //         GameObject.Find("Main Camera").GetComponent<maxCamera>().setTarget(ctunity.transform);
            }
        }
    }
}
