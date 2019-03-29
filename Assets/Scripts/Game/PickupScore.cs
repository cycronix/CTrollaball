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
using CTworldNS;

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

    void LateUpdate()
    {
        PlayerCount();
    }

    // build scoreboard:
    void PlayerCount()
    {
        if (ctunity.WorldList == null) return;  // nothing new

        String scoreboard = "";
        foreach (CTworld ctw in ctunity.WorldList)
        {
            String bold = "", ebold = "";
            if (ctw.active) { bold = "<b><i>"; ebold = "</i></b>"; }
            scoreboard += (bold + "<color=" + ctw.player + ">" + ctw.player + ": " + ctw.objects.Count + "</color>  " + ebold);
        }

//        Debug.Log("scoreboard: " + scoreboard);
        countText.text = scoreboard;
        winText.text = "<color=" + ctunity.Player + ">" + ctunity.Player + "</color>";  // right place to set this?
    }

}

