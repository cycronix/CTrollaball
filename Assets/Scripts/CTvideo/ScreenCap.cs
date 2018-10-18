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

// Capture screenshot
// MJM 11/20/2017

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//----------------------------------------------------------------------------------------------------------------
public class ScreenCap : MonoBehaviour, IPointerDownHandler
{
	public float pollInterval = 0.1f;			// polling interval for new data (sec)
	public int quality = 70;
	private CTunity ctunity;
//	Boolean saveActive = false;
	int width, height;
	byte[] bytes;
	public Boolean VidCapMode = false;
    
	//----------------------------------------------------------------------------------------------------------------
	// Take a shot immediately
	void Start()
	{
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
		StartCoroutine("SaveJPG");
	}

	//----------------------------------------------------------------------------------------------------------------
	IEnumerator SaveJPG()
	{
		while (true) {
			yield return new WaitForSeconds (ctunity.pollInterval);	
			yield return new WaitForEndOfFrame ();

			if (ctunity.ctvideo == null) continue;	    // not ready to record

//			saveActive = (!vidDisplay.showImage && !chartOptions.showMenu);			// no capture while menu or replay
//			saveActive = (!chartOptions.showMenu);			// notta

			if (VidCapMode) {		// enable based on video capture state
				// Create a texture the size of the screen, RGB24 format
				width = Screen.width;
				height = Screen.height;
				Texture2D tex = new Texture2D (width, height, TextureFormat.RGB24, false);
		
				// Read screen contents into the texture
				tex.ReadPixels (new Rect (0, 0, width, height), 0, 0);
				//		TextureScale.Bilinear(tex, width/2, height/2);		// scale smaller image
				tex.Apply ();

				bytes = tex.EncodeToJPG (quality);
				Destroy (tex);
//				Debug.Log("save image!, vidcapmode:  " + VidCapMode);

				ctunity.ctvideo.setTime (ctunity.ServerTime ());
				ctunity.ctvideo.putData ("screen.jpg", bytes);		// let CTtrackset flush
				ctunity.ctvideo.flush(); 	// todo: flush multiple per block
			}
		}
	}

	//----------------------------------------------------------------------------------------------------------------
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0) /* && !ctunity.observerFlag */)
        {
            VidCapMode = !VidCapMode;
//			Debug.Log("VidCapMode: " + VidCapMode);
			if(VidCapMode)  GetComponent<RawImage>().color = Color.red;
			else            GetComponent<RawImage>().color = Color.white;
        }
    }
}
