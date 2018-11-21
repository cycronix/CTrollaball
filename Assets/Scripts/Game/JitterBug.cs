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
	private Vector3 myPos = Vector3.zero;
	private System.Random random;
	public float UpdateInterval = 1f;
	private float stopWatch = 0f;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		random = new System.Random();
	}
   
	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
	Vector3 velocity = Vector3.zero;
	Vector3 newPos = Vector3.zero;

	void Update()
	{
		stopWatch += Time.deltaTime;
		if (stopWatch >= UpdateInterval)
		{
			stopWatch = 0f;

			float xrand = (float)(random.Next(-95, 95)) / 10F;
			float yrand = (float)(random.Next(-95, 95)) / 10F;
			float zrand = (float)(random.Next(-50, 20)) / 10F;
			newPos = transform.position + new Vector3(xrand, zrand, yrand);

			newPos.x = Mathf.Clamp(newPos.x, -10f, 10f);
			newPos.y = Mathf.Clamp(newPos.y, 1f, 10f);
			newPos.z = Mathf.Clamp(newPos.z, -10f, 10f);
		}

//		transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime);
		transform.position = Vector3.SmoothDamp(transform.position, newPos, ref velocity, UpdateInterval);
	}
    
}
