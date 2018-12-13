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

public class Rocket : MonoBehaviour {
	private System.Random random;
	private CTunity ctunity;
	private Rigidbody rb;
	private float stopWatch = 0f;

	public float ForceFactor = 10f;
	public float fuelTime = 5f;                 // seconds of fuel burn
	public float boomTime = 60f;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
		rb = GetComponent<Rigidbody>();
		stopWatch = 0f;
		random = new System.Random(Guid.NewGuid().GetHashCode());      // unique seed
	}
   
	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
	void FixedUpdate()
	{
//		if (!CTunity.activeWrite) return;
		if (!ctunity.activePlayer(gameObject)) return;

		stopWatch += Time.deltaTime;
		if (stopWatch < fuelTime)
		{
			float noiseX = (float)random.NextDouble() / 100f;   // bit of uncertainty so rockets don't perfectly "stack"
			float noiseZ = (float)random.NextDouble() / 100f;
			rb.AddRelativeForce(new Vector3(noiseX, 1f, noiseZ) * ForceFactor);
		}
		else if (stopWatch > boomTime) 
		{
//			Debug.Log(name + ": BOOM!");
			ctunity.clearObject(gameObject);
		}
			
	}
    
}
