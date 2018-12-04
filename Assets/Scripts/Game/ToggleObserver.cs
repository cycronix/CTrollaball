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

public class ToggleObserver : MonoBehaviour, IPointerDownHandler {
	private CTsetup ctsetup;
//	static Boolean showMenu = true;
	private CTunity ctunity;
	private GameObject replayControl;
	private GameObject gameOptions;

	// Use this for initialization
	void Start () {
		ctsetup = GameObject.Find("Setup").GetComponent<CTsetup>();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
		replayControl = GameObject.Find("replayControl");
		gameOptions = GameObject.Find("Setup").gameObject;
	}

	//----------------------------------------------------------------------------------------------------------------
	public void OnPointerDown (PointerEventData eventData)      
    {
        if (Input.GetMouseButton(0))
        {
			Debug.Log("ToggleObserver!");
			if (ctunity.observerFlag)
			{
				GameObject.Find("Main Camera").GetComponent<maxCamera>().setTarget(GameObject.Find("Ground").transform);
                gameOptions.SetActive(true);
                ctunity.setReplay(false);
			}
			else
			{
				ctunity.observerFlag = true;
				ctsetup.serverConnect();
				ctunity.Player = "Observer";
				ctunity.lastSubmitTime = ctunity.ServerTime();
				ctunity.gamePaused = false;
				replayControl.SetActive(true);
			}

			ctunity.CTdebug(null);                // clear warnings/debug text
        }
    }
    
}
