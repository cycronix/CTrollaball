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
using UnityEngine.EventSystems;

public class GroundTarget : MonoBehaviour {
    public float maxDistance = 1000f;

    // Use this for initialization
    void Start() {}

    //----------------------------------------------------------------------------------------------------------------
    public void OnMouseDown()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
//            Debug.Log("hit: " + hit.point + ", t.pos: " + transform.position);
            if (!EventSystem.current.IsPointerOverGameObject())     // avoid "click through" from UI elements
            {
                GameObject go = GameObject.Find("CTunity");         // nominal gameObject to store transform target
                go.transform.position = hit.point;
                GameObject.Find("Main Camera").GetComponent<maxCamera>().setTarget(go.transform);
            }
        }
    }
}
