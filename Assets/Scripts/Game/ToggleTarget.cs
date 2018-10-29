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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToggleTarget : MonoBehaviour {
	private CTunity ctunity;

    // Use this for initialization
    void Start()
    {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
    }

    //----------------------------------------------------------------------------------------------------------------
    public void OnMouseDown()
    {
		//        Debug.Log("toggle target: " + transform.name);
		if (!EventSystem.current.IsPointerOverGameObject())     // avoid "click through" from UI elements
		{
			GameObject.Find("Main Camera").GetComponent<maxCamera>().setTarget(transform);
//			Cursor.lockState = CursorLockMode.Locked;       // center mouse cursor (good for subsequent orbit-drag)
//			Cursor.lockState = CursorLockMode.None;
		}
	}
}
