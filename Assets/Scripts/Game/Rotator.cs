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

using UnityEngine;

public class Rotator : MonoBehaviour {
	private CTclient ctplayer;
	private CTunity ctunity;
	private float sizeFactor = 1.01F;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		ctplayer = GetComponent<CTclient>();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();           // reference CTunity script
	}

	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
	void Update () {
		if (ctplayer.isLocalControl())
		{
			Quaternion targetRotation = Quaternion.Euler(new Vector3(15, 30, 45)) * transform.rotation;
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 1F);
			if (transform.localScale.x < 0.1F) sizeFactor = 1.001F;
			else if (transform.localScale.x > 0.5F) sizeFactor = 0.999F;
			transform.localScale *= sizeFactor;
				                                                       
		}
//		    transform.Rotate (new Vector3 (15, 30, 45) * Time.deltaTime);
	}

	//----------------------------------------------------------------------------------------------------------------
	// poof on collide

	void OnTriggerEnter(Collider other) {
//		gameObject.SetActive(false); 
		ctunity.clearObject(gameObject);
	}
}
