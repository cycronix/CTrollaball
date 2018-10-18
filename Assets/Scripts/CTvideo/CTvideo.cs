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

// Simple video display (no options)
// Matt Miller, Cycronix, 7-6-2017

public class CTvideo : MonoBehaviour {
	public string url = "http://localhost:8000/CT/CTstream/webcam.jpg";
	public float pollInterval = 0.1f;			// polling interval for new data (sec)
	private Boolean showImage = true;
	private Texture startTexture;
	private CTunity ctunity;
	private CTclient ctclient = null;

	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		ctclient = transform.parent.GetComponent<CTclient>();              // interactive CT updates
		startTexture = GetComponent<Renderer> ().material.GetTexture ("_MainTex");
	}

	private void OnEnable()
    {
		StartCoroutine("DownloadImage");
    }

	String oldCustom = "";
	IEnumerator DownloadImage()
	{
		while (true) {
			yield return new WaitForSeconds (ctunity.pollInterval);

			if (showImage) {

                if (ctclient != null && ctclient.enabled && ctclient.link != null && !ctclient.isLocalControl())
                {       // remote control
					if (ctclient.link.Equals("")) continue;
                    if (ctclient.link.Equals(oldCustom)) continue;
					url = ctclient.link;
                    oldCustom = ctclient.link;
                }
                else    // local control
                {
					url = ctunity.Server + "/CT/"+ctunity.Session+"/Video/" + ctunity.Player + "/webcam.jpg";
                    url = url + "?t=" + ctunity.replayTime;  // live or replay
                    oldCustom = "";
                }

				String urlparams = "";
				//				if (ctunity.isReplayMode()) urlparams = "?t=" + ctunity.replayTime;

				WWW www;
				try
				{
					www = new WWW(url);
				} catch (Exception e) {
					UnityEngine.Debug.Log("CTvideo exception: " + url);
					continue;
				}
				yield return www;

				if (ctclient != null)
				{
					ctclient.link = url;
//					Debug.Log("ctclient: " + ctclient.name + ", url: " + url);
				}

				Texture2D tex = new Texture2D (www.texture.width, www.texture.height, TextureFormat.DXT1, false);
				www.LoadImageIntoTexture (tex);
				GetComponent<Renderer> ().material.mainTexture = tex;

				www.Dispose ();
				www = null;
			} else {
				GetComponent<Renderer> ().material.mainTexture = startTexture;
			}
		}
	}

}
