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
	private CTclient ctclient;
	private Rigidbody rb;

	public float ForceFactor = 10f;
	public float fuelTime = 5f;                 // seconds of fuel burn
	public float boomTime = 60f;
    public float wobbleFactor = 0.01f;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
		ctclient = GetComponent<CTclient>();
		random = new System.Random(Guid.NewGuid().GetHashCode());      // unique seed
    }

	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
	void FixedUpdate()
	{
		if(rb == null) {    // init async?
			rb = GetComponent<Rigidbody>();

            // start with velocity of grandparent (?)
            if (rb != null)
            {
                Rigidbody pprb = transform.parent.transform.parent.gameObject.GetComponent<Rigidbody>();
                if (pprb != null) rb.velocity = pprb.velocity;
            }
		}
		if (!ctunity.activePlayer(gameObject)) return;

        // save fuel and flightTime with CT
        float fuel = fuelTime;
        Boolean gotfuel = float.TryParse(ctclient.getCustom("Fuel",""+fuel), out fuel);
        float flightTime = 0;
        float.TryParse(ctclient.getCustom("FlightTime", ""+flightTime), out flightTime);
//        Debug.Log(name + ", fuel: " + fuel + ", fueltime: " + fuelTime+", flightTime: "+flightTime);

        fuel -= Time.deltaTime;         // fuel units = RT sec
        if (fuel < 0) fuel = 0;
        flightTime += Time.deltaTime;

        ctclient.putCustom("Fuel", "" + fuel);
        ctclient.putCustom("FlightTime", "" + Math.Round(flightTime*1000f)/1000f);
        if(fuel > 0)
		{
			float noiseX = (float)random.NextDouble() * wobbleFactor;   // bit of uncertainty so rockets don't perfectly "stack"
			float noiseZ = (float)random.NextDouble() * wobbleFactor;
			rb.AddRelativeForce(new Vector3(noiseX, 1f, noiseZ) * ForceFactor);
        }
        else if (flightTime > boomTime) 
		{
//			Debug.Log(name + ": BOOM!");
			ctunity.clearObject(gameObject);
		}
	}
}
