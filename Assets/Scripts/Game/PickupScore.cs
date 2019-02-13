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
 //   public int updateInterval = 10;         // only count every N updates (save CPU)
 //   private int updateCount = 0;

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
        public int nactive { get; set; }
    }

    void LateUpdate()
    {
//        if((updateCount % updateInterval) == 0) 
        PlayerCount();
//        updateCount++;
    }

 
    void PlayerCount()
    {
        if (ctunity.PlayerList == null) return;  // nothing new

        Dictionary<String, int> cts = new Dictionary<String, int>();
 //       foreach (String player in ctunity.PlayerList) cts.Add(player, 0);   // init

        foreach (String key in ctunity.CTlist.Keys)
        {
            String player = key.Split('/')[0];                  // CTlist uses fullName as key
            if (!cts.ContainsKey(player)) cts.Add(player,0);
            ++cts[player];          // count up associated player objects in CTlist
        }

        // build scoreboard:
        String scoreboard = "";
        foreach(String player in cts.Keys)
 //       foreach (String player in ctunity.PlayerList)
        {
            int nactive = cts[player];
            if (nactive > 0 || (ctunity.Player.Equals(player) && !player.Equals("Observer")))
            {
                String bold = "", ebold = "";
                if (ctunity.Player.Equals(player) || !ctunity.PlayerList.Contains(player) )
                {
//                    Debug.Log("Active Player: " + player);
                    bold = "<b><i>";  ebold = "</i></b>";
                }
  //              else
  //                  Debug.Log("INACTIVE: " + player+", plist: "+ctunity.PlayerList);

                scoreboard += (bold + "<color=" + player + ">" + player + ": " + nactive + "</color>  " + ebold);
            }
        }

//        Debug.Log("scoreboard: " + scoreboard);
        countText.text = scoreboard;
        winText.text = "<color=" + ctunity.Player + ">" + ctunity.Player + "</color>";  // right place to set this?
    }

}

