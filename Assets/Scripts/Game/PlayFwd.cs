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
using UnityEngine.EventSystems;
using UnityEngine.UI;

//----------------------------------------------------------------------------------------------------------------
// toggle replay mode

public class PlayFwd : MonoBehaviour, IPointerDownHandler
{       // required interface when using the OnPointerDown method.

	private CTtimecontrol cttc;
	private GameObject slider;
	public Boolean reversePlay = false;
	public Boolean pausePlay = false;

    //----------------------------------------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
        cttc = GameObject.Find("CTunity").GetComponent<CTtimecontrol>();       // reference CTunity script
		slider = GameObject.Find("Slider");
    }

	private void LateUpdate()
	{
		setButtonColors();
	}

	//----------------------------------------------------------------------------------------------------------------
	// onPointerDown for UI objects
	public void OnPointerDown(PointerEventData eventData)
    {
		if (pausePlay)         cttc.playPause();
		else if (reversePlay)  cttc.playRvs();
		else                   cttc.playFwd();
	}

	//----------------------------------------------------------------------------------------------------------------
	private void setButtonColors() {
		if (cttc == null) return;   // delayed init?

		if (!slider.activeSelf)
		{
			GetComponent<RawImage>().enabled = false;  // hide if slider not enabled
			return;
		}
		else GetComponent<RawImage>().enabled = true;

        // paint UI buttons red/white
		foreach (Transform child in transform.parent)
		{
			PlayFwd pf = child.GetComponent<PlayFwd>();

			// convoluted logic to achieve radio button function:
			if (pf != null)
			{
				RawImage buttonImage = child.GetComponent<RawImage>();
				if (cttc.playFactor == 0F)
				{
					if (pf.pausePlay)                       buttonImage.color = Color.red;
					else                                    buttonImage.color = Color.white;
				}
				else if (cttc.playFactor < 0F)
				{
					if (pf.reversePlay && !pf.pausePlay)    buttonImage.color = Color.red;
                    else                                    buttonImage.color = Color.white;
				}
				else if (cttc.playFactor > 0F)
				{
					if (!pf.reversePlay && !pf.pausePlay)   buttonImage.color = Color.red;
					else                                    buttonImage.color = Color.white;                  
				}
			}
		}
	}
}
