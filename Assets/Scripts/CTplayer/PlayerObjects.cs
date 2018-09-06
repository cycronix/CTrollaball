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

//----------------------------------------------------------------------------------------------------------------
public class PlayerObjects : MonoBehaviour {

	private CTunity ctunity;
	private Transform ctplayer;

	private string Player;
	public int pickupsPerClick = 5;
	public int maxPickups = 100;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {

		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();   // reference CTunity script
		if (!gameObject.name.Equals(ctunity.Player)) return;            // external or remote player no-spawn local gameObjects
      
		ctplayer = GameObject.Find("Players").transform;                // reference Players container
		Player = gameObject.name;
		Vector3 gpos = ctunity.groundPos(Player);

		// launch player-owned game objects:
		// TO DO:  player-launched objects need thought and design

		GameObject ground = ctunity.newGameObject(Player + ".Ground", "Ground", gpos, Quaternion.identity, false, true);

		// adjust ground object color (needs to be centralized)
        Renderer renderer = ground.GetComponent<Renderer>();
		if (renderer != null)
		{
			Color c = renderer.material.color;
			float H, S, V;
			Color.RGBToHSV(c, out H, out S, out V);
			renderer.material.color = Color.HSVToRGB(H, (float)(S * 0.8F), (float)(V * 0.8F), false);   // toned-down color
		}

		if(npickups(Player) == 0) dispensePickups();                    // init
	}

	//----------------------------------------------------------------------------------------------------------------
	public void dispensePickups()
    {
        Vector3 groundPos = ctunity.groundPos(Player);
        int nobject = npickups(Player);
//        Debug.Log("DispensePickups! player: " + Player + ", groundPos: " + groundPos + ", npickups: " + nobject);

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
            ctunity.newGameObject(Player + ".Pickup" + nobject++, "Pickup", groundPos + new Vector3(xrand, zrand, yrand), Quaternion.identity, false, true);
        }
    }

	//----------------------------------------------------------------------------------------------------------------
    public int npickups(string player)
    {
		int np = 0;
        for (int i = 0; i < ctplayer.childCount; i++)
        {
            Transform c = ctplayer.GetChild(i);

            if (c.name.StartsWith(player) && c.name.Contains("Pickup") && c.gameObject.activeSelf)
            {
                np++;
            }
        }
        return np;
    }
}
