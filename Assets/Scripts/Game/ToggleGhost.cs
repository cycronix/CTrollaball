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
using UnityEngine.UI;

public class ToggleGhost : MonoBehaviour
{
	private CTunity ctunity;
	public double doubleClickTime = 0.5F;
	private double clickTime = 0F;
	private DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Use this for initialization
    void Start()
    {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
    }

	private void Update()
	{
		// unset double-click timer
		if ( (clickTime > 0) && ((nowTime() - clickTime) > doubleClickTime) ) clickTime = 0;
	}

	//----------------------------------------------------------------------------------------------------------------
	public void OnMouseDown()
	{
		if (ctunity == null) return;   // not launched yet
		if (ctunity.observerFlag || ctunity.showMenu) return;     // observers don't get ghost-followers
      
		if (clickTime > 0F)
		{
			ctunity.Ghost = !ctunity.Ghost;
            if (ctunity.Ghost) ctunity.newPlayer(ctunity.Player, "Ghost", true);
            else ctunity.clearPlayer(ctunity.Player + "g");
		}

		clickTime = nowTime();  // start double-click timer
	}

	//----------------------------------------------------------------------------------------------------------------
    private double nowTime()
    {
//        return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
		return (DateTime.UtcNow - refTime).TotalSeconds;
    }
}
