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

//----------------------------------------------------------------------------------------------------------------
// Rocket:  CTclient with vertical thrust (and maybe explodes) 

using UnityEngine;
using System;

public class Launcher : MonoBehaviour {
	private CTunity ctunity;
    
	private float stopWatch = 0f;
	public float launchInterval = 5;                 // seconds of fuel burn
	public float lingerTime = 10;

	public int Nlaunch = 10;
	private int Ilaunch = 0;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
		stopWatch = launchInterval;  // immediate launch one
	}
   
	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
    
	void FixedUpdate()
	{
//		if (!CTunity.activeWrite) return;
		if (!ctunity.activePlayer(gameObject) /* || !CTunity.activeWrite */) return;

		stopWatch += Time.deltaTime;
		if (stopWatch >= launchInterval)
		{
			if (Ilaunch >= Nlaunch)
			{
				if (stopWatch > lingerTime)
				{
					Debug.Log(name+": Buh Bye!");
					ctunity.clearObject(gameObject);  // poof
				}
			}
			else
			{
//				Debug.Log("Launch: " + Ilaunch);
				ctunity.newPlayer("Launcher:Rocket" + Ilaunch, "Rocket");
				Ilaunch++;
				stopWatch = 0;
			}
		}
        
	}
    
}
