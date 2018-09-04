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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class pickupDispenser : MonoBehaviour, IPointerDownHandler  {
	private CTunity ctunity;
	private static int nobject = 0;
	public int pickupsPerClick = 5;
	public int maxPickups = 100;

	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
	}

	//----------------------------------------------------------------------------------------------------------------
	public void OnPointerDown(PointerEventData eventData)
	{
		if (Input.GetMouseButton(0))
		{
			if (ctunity.showMenu) return;          // notta if changing settings...
			dispensePickups(ctunity.Player);
		}
	}

	public void dispensePickups(string player)
	{
//		Debug.Log("dispensePickups!");

		if (nobject >= maxPickups)
		{
			Debug.Log("Max Pickups!");
			return;
		}

		if (ctunity.observerFlag) return;

		// dynamic game object creation:
		System.Random random = new System.Random();
		for (int i = 0; i < pickupsPerClick; i++)
		{
			float xrand = (float)(random.Next(-95, 95)) / 10F;
			float yrand = (float)(random.Next(-95, 95)) / 10F;
			float zrand = (float)(random.Next(10, 50)) / 10F;
			if (ctunity.Model.Equals("Ball")) zrand = 0.4F;                 // fixed elevation if ball
			ctunity.newGameObject(player + ".Pickup" + nobject++, "Pickup", new Vector3(xrand, zrand, yrand), Quaternion.identity, false, true);
		}

	}
}
