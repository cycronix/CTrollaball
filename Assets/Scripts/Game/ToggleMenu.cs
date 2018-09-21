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

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToggleMenu : MonoBehaviour, IPointerDownHandler {
	private GameObject gameOptions;
	static Boolean showMenu = true;
	private CTunity ctunity;

	// Use this for initialization
	void Start () {
		gameOptions = GameObject.Find("Setup").gameObject;
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
	}

	//----------------------------------------------------------------------------------------------------------------
    // OnPointerDown works for overlay canvas
	public void OnPointerDown (PointerEventData eventData)      
    {
        if (Input.GetMouseButton(0))
        {
			//			Debug.Log("PointerDown ToggleMenu!");
			doit();
        }
    }

    // OnMouseDown works on in-world canvas
	public void OnMouseDown()
	{
		//		Debug.Log("MouseDown ToggleMenu!");
		doit();
	}

	private void doit() {
		GameObject.Find("Main Camera").GetComponent<maxCamera>().setTarget(GameObject.Find("Ground").transform);
        showMenu = !gameOptions.activeSelf;
        ctunity.setReplay(false);
//        ctunity.observerFlag = true;  // ??
//		ctunity.showMenu = false;   // observer mode while in menu...
		gameOptions.SetActive(showMenu);
	}
}
