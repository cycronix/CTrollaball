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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickupScore : MonoBehaviour {
	CTunity ctunity;

	public Text countText;
	public Text winText;

	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
		winText.text = "";
		countText.text = "";

		winText.supportRichText = true;
		countText.supportRichText = true;
	}
	
	//-------------------------------------------------------------------------------------------------------
	// get States from objects, keep score
    
	class CTscore
    {
        string player { get; set; }
        public int npickups { get; set; }
        public int nactive { get; set; }
    }
	Boolean GameOver = false;

	void LateUpdate()
	{
		//		if (ctunity.showMenu || ctunity.observerFlag) return; // save effort      
		if (GameOver) return;

		Dictionary<String, CTscore> cts = new Dictionary<String, CTscore>();
		foreach (String player in ctunity.PlayerList) cts.Add(player, new CTscore());   // create

		// pre-filter scores:
		for (int i = 0; i < transform.childCount; i++)
		{
			foreach (String player in ctunity.PlayerList)
			{
				Transform c = transform.GetChild(i);
				if (c.name.StartsWith(player) && c.name.Contains("Pickup"))
				{
					cts[player].npickups++;
					if (c.gameObject.activeSelf) cts[player].nactive++;
				}
			}
		}

		// build scoreboard:
		String scoreboard = "";
		String wintext = "";
		foreach (String player in ctunity.PlayerList)
		{
			int npickups = cts[player].npickups;
			if (npickups == 0) continue;
			int nactive = cts[player].nactive;
			int score = npickups - nactive;
			scoreboard += "<color="+player+">"+player + ": " + score.ToString() + " / " + npickups+"</color>   ";
			if (score > 0 && score >= npickups && !ctunity.isReplayMode())
			{
				wintext = "<color=" + player + ">" + player + " Wins!</color>";
//				GameOver = true;
			}
		}

		countText.text = scoreboard;
		if (ctunity.observerFlag)   winText.text = "Observer";
		else                        winText.text = wintext;
	}

}
