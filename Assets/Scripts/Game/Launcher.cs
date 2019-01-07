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
	private int Ilaunch = 0;
//	private int myHash = 0;

	public float launchInterval = 5;                 // seconds of fuel burn
	public int Nlaunch = 1;
	public String Missile = "Rocket";

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
		stopWatch = 0;  
//		myHash = Math.Abs(Guid.NewGuid().GetHashCode());      
	}
   
	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
    
	void FixedUpdate()
	{
		if (!ctunity.activePlayer(gameObject)) return;

		stopWatch += Time.deltaTime;
		if (stopWatch >= launchInterval && (Nlaunch==0 || Ilaunch<Nlaunch))
		{
			ctunity.newPlayer(CTunity.fullName(gameObject)+"/R-" /* + myHash + "-" */ + Ilaunch, Missile);   // unique names
//            ctunity.newPlayer(ctunity.Player + "/R-" + myHash + "-" + Ilaunch, Missile);   // unique names         
            Ilaunch++;
			stopWatch = 0;
		}
	}
}
