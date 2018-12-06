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
// Follow TrackTarget and record its travels 

using System;
using System.Collections.Generic;
using UnityEngine;

//----------------------------------------------------------------------------------------------------------------
public class CTdoppelganger : MonoBehaviour {

	private CTunity ctunity;
	private CTclient ctclient;
	private Vector3 myScale = Vector3.one;
	private GameObject trackobject = null;

	public String TrackTarget = null;               // child target (player.tracktarget)

	// Use this for initialization
	void Start()
	{      
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		ctclient = GetComponent<CTclient>();

		//		myScale = transform.localScale;                             // scale not set until enabled?
		transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);    // hide at startup      

		//		ctclient.custom = TrackTarget;
		if (TrackTarget == null || TrackTarget.Equals("")) TrackTarget = ctunity.Player + "/Ball";
	}

	private void OnEnable()
	{
		myScale = transform.localScale;
	}

	//----------------------------------------------------------------------------------------------------------------
	void Update()
	{
		//		if (!ctclient.isLocalObject()) return;   // notta unless locally recorded object
//		TrackTarget = ctclient.custom;
//		if (TrackTarget.Equals("")) TrackTarget = ctunity.Player;                   // default to track parent
		if (trackobject == null || !TrackTarget.Equals(trackobject.name)) 
			trackobject = GameObject.Find(TrackTarget);     // child-init

//		Debug.Log("Doppel me: "+transform.name+", TrackTarget: " + TrackTarget + ", ctclient.custom: " + ctclient.custom+", trackobject: "+trackobject);

		if (ctunity.isReplayMode())
		{
			transform.localScale = myScale;                             // appear, let ctclient drive
		}
		else 
		{
			transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);    // hide
			if (trackobject != null /* && ctclient.isLocalObject() */)        // follow if local target
			{
				transform.position = trackobject.transform.position;   
				transform.rotation = trackobject.transform.rotation;
			}
		}
	}
}
