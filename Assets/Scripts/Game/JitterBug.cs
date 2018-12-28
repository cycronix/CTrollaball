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
// JitterBug:  a non-CTclient object that moves around randomly. 
// Makes a good Doppelganger TrackTarget.

using UnityEngine;
using System;

public class JitterBug : MonoBehaviour {
	private System.Random random;
	public float UpdateInterval = 0.1f;
	public float rangeLimit = 20f;
	public float speedFactor = 1f;

	private float stopWatch = 0f;

//	private CTclient ctclient;
	private CTunity ctunity;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
//		ctclient = GetComponent<CTclient>();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script

//		random = new System.Random();
		random = new System.Random(Guid.NewGuid().GetHashCode());      // unique seed
	}
   
	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
	Vector3 velocity = Vector3.zero;
	Vector3 newPos = Vector3.zero;
    
	void Update()
	{
//		if (!ctclient.isLocalControl()) return;                 // notta unless under local-control
//		if (!CTunity.activeWrite) return;
		if (!ctunity.activePlayer(gameObject) /* || !CTunity.activeWrite */) return;   // consolidate to single check...

		stopWatch += Time.deltaTime;
		if (stopWatch >= UpdateInterval)
		{
			stopWatch = 0f;

			float xrand = speedFactor * (float)(random.Next(-95, 95)) / 10F;
			float yrand = speedFactor * (float)(random.Next(-95, 95)) / 10F;
			float zrand = speedFactor * (float)(random.Next(-95, 95)) / 10F;
			newPos = transform.localPosition + new Vector3(xrand, zrand, yrand);

			newPos.x = Mathf.Clamp(newPos.x, -rangeLimit, rangeLimit);
			newPos.y = Mathf.Clamp(newPos.y, 0f, rangeLimit/2f);
			newPos.z = Mathf.Clamp(newPos.z, -rangeLimit, rangeLimit);
		}

//		transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime);
		transform.localPosition = Vector3.SmoothDamp(transform.localPosition, newPos, ref velocity, UpdateInterval);
	}
    
}
