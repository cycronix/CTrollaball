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
using UnityEngine.EventSystems;

//----------------------------------------------------------------------------------------------------------------
// Simple video display (no options)
// Matt Miller, Cycronix, 7-6-2017
using System.Diagnostics;		// for stopWatch

public class CTreplay : MonoBehaviour, IPointerDownHandler {		// required interface when using the OnPointerDown method.
	public string VidChan = "screen.jpg";
	private float pollInterval = 0.1f;			// polling interval for new data (sec)
	private double replayInterval = 5.0f;
	public Boolean showImage = false;
	public Text replayText;						// set via GUI component

	private Texture startTexture;
	private CTunity ctunity;
	private double replayTime = 0;
	private Stopwatch stopWatch = new Stopwatch ();

	private RectTransform rectTransform;
//	private RectTransform originalRectTransform;
	private Vector2 anchorMin1, anchorMax1, anchorPos1, sizeDelta1;

	private double elapsedTime = 0;
	private double oldElapsedTime = 0;
    
//	private CTtimecontrol timeControl;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
//		pollInterval = transform.GetComponent<ScreenCap>().pollInterval;		// screenCap poll rate

//		timeControl = GameObject.Find ("CTunity").GetComponent<CTtimecontrol>();

		//  preserve initial vidimage to toggle full-screen and back
		rectTransform = GetComponent<RectTransform>();
		anchorMin1 = rectTransform.anchorMin;
		anchorMax1 = rectTransform.anchorMax;
		anchorPos1 = rectTransform.anchoredPosition;
		sizeDelta1 = rectTransform.sizeDelta;
		startTexture = GetComponent<RawImage> ().texture;

		StartCoroutine("DownloadImage");
	}

	//----------------------------------------------------------------------------------------------------------------
	IEnumerator DownloadImage()
	{
		while (true) {
			if (!showImage || ctunity.showMenu) {
				yield return new WaitForSeconds (pollInterval);
//				GetComponent<Renderer> ().material.mainTexture = startTexture;
			} 
			else {
				elapsedTime = stopWatch.ElapsedMilliseconds / 1000.0f;
				float deltaTime = (float)(elapsedTime - oldElapsedTime);
				float thisWait = pollInterval - deltaTime;		// wait balance of vidrate
				if 		(thisWait < 0.01f) 		  thisWait = 0.01f;				// minimum 10ms wait
				else if (thisWait > pollInterval) thisWait = pollInterval;		// cap
//				UnityEngine.Debug.Log ("thisWait: " + thisWait + ", elapsedTime: " + elapsedTime+", deltaTime: "+deltaTime+", replayInterval: "+replayInterval);
				yield return new WaitForSeconds (thisWait);		// replay poll interval 

				elapsedTime = stopWatch.ElapsedMilliseconds / 1000.0f;		// update after possible wait
				oldElapsedTime = elapsedTime;
				string imageurl = ctunity.Server + "/CT/" + ctunity.Player + "/" + VidChan + "?t=" + (replayTime + elapsedTime);		
//				UnityEngine.Debug.Log ("imgurl: " + imageurl+", elapsed: "+elapsedTime+", replayTime: "+replayTime);
				WWW www = new WWW (imageurl);
				yield return www;

				if (string.IsNullOrEmpty (www.error)) {		// no error if www.error not set
					vidWindowSize (showImage);									// overkill?
					transform.GetComponent<RawImage> ().texture = www.texture;	// CT image to screen				 
					www.Dispose ();
					www = null;
				} else {
					UnityEngine.Debug.Log ("CTvideo failed to fetch url: " + imageurl + ", www.error: " + www.error);
				}

				if (elapsedTime > replayInterval) {			// end of replay
					replayOff ();
					stopWatch.Reset ();
					oldElapsedTime = 0.0f;
				}
//				System.GC.Collect();
				Resources.UnloadUnusedAssets ();
			}
		}
	}

	//----------------------------------------------------------------------------------------------------------------
	void replayOff() {
		showImage = false;
		replayText.text = "";
		vidWindowSize (showImage);		// overkill?
		transform.GetComponent<RawImage> ().texture = startTexture;
	}

	// onMouseDown for scene objects
//	public void OnMouseDown() {
//		Debug.Log ("MouseDown");
//		showImage = !showImage;		// toggle
//	}
		
	//----------------------------------------------------------------------------------------------------------------
	// onPointerDown for UI objects
	public void OnPointerDown (PointerEventData eventData) 		
	{
		replayInterval = ctunity.MaxPts / 50.0f;			// presumes 50Hz updates

		// TO DO:  integrate action and screencap replay logic with CTtimecontrol...

//		if(ctunity.ReplayMode.Equals("Action")) {			// Action replay moves objects
//			ctunity.toggleReplay();
//		} 
//		else {													// screencap replay 
			showImage = !showImage;
			if (showImage) {
//				double now = (DateTime.UtcNow - new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
				double now = ctunity.ServerTime();

				double maxDelay = now - ctunity.lastSubmitTime;
				if (replayInterval > maxDelay) replayInterval = maxDelay;		// no going past BOF

				replayTime = now - replayInterval;		// 10s replay
				oldElapsedTime = elapsedTime = 0.0f;
				stopWatch.Start ();
			} else {
				replayOff ();
			}
//		}
	}

	//----------------------------------------------------------------------------------------------------------------
	Boolean islarge = false;		// avoid flicker
	private void vidWindowSize(Boolean fullSize) {

		if (fullSize && !islarge) {
			rectTransform.anchorMin = new Vector2(0, 0);
			rectTransform.anchorMax = new Vector2(1, 1);
			rectTransform.anchoredPosition = Vector2.zero;	
			//			rectTransform.localPosition = Vector3.zero;
			rectTransform.sizeDelta = Vector2.zero;
			replayText.text = "Screencap Replay";
			islarge = true;
		} 

		if(!fullSize && islarge) {
			rectTransform.anchorMin = anchorMin1;
			rectTransform.anchorMax = anchorMax1;
			rectTransform.anchoredPosition = anchorPos1;
			rectTransform.sizeDelta = sizeDelta1;
			replayText.text = "";
			islarge = false;
		}
//		Debug.Log ("replayText: " + replayText.text);
	}
		
	//----------------------------------------------------------------------------------------------------------------
	// following doesn't fire?
	public void OnPointerUp (PointerEventData eventData) 		
	{
		UnityEngine.Debug.Log (this.gameObject.name + " PointerUp");
//		showImage = !showImage;		// toggle
	}
		
}
