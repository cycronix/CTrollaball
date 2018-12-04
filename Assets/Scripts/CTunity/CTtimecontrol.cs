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

//----------------------------------------------------------------------------------------------------------------
public class CTtimecontrol : MonoBehaviour {
	public double masterTime = 0;
//	private double now = 0;
	public Boolean replayActive = false;
	public Text replayText;                     // set via GUI component
	public Text timeText;
    
	public double shortReplayDuration = 10F;
	public double longReplayDuration = 300F;

	private Slider slider;
	private CTunity ctunity;

	private double startReplayTime = 0;
	private double endReplayTime = 0;
	private double durationReplayTime = 0;
	private double playTimeRef = 0;                  // auto-play fwd/rvs from slider

	private static string replayLabel = "Replay";
	private static string pausedLabel = "Paused";
	private static string liveLabel = "Live";
//	private static string remoteLabel = "Remote";
	private static string stateString = pausedLabel;

	public double playFactor = 0F;          // factor to auto-play masterTime back, pause, forward
	private double clickTime = 0F;          // double-click timer

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		slider = GameObject.Find ("Slider").GetComponent<Slider> ();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity> ();		// reference CTunity script
		replayText.supportRichText = true;
		setStateText(pausedLabel);
	}

	//----------------------------------------------------------------------------------------------------------------
	// Update is called once per frame
	float oldSliderValue = 1F;

	void Update () {

        Boolean ractive = ctunity.setTime(masterTime, stateString, playFactor==0);     // interface to CTunity
		if (ractive != replayActive)
		{
			replayActive = ractive;
			if (replayActive)   startReplay();
			else                stopReplay();

			oldSliderValue = 1F;    // init
		}

		if (ctunity.gamePaused)
		{
			setStateText(pausedLabel);
			slider.gameObject.SetActive(false);
			timeText.text = "";
		}
		else
		if (replayActive)
		{
//			Debug.Log("replayactive: " + replayActive+", keyleft: "+Input.GetKeyDown(KeyCode.LeftArrow)+", pf: "+playFactor);

			slider.gameObject.SetActive(true);
			if (slider.value != oldSliderValue) playPause();   // no tug-o-war
            
            // check for replay-mode arrow key press
			if (Input.GetKeyDown(KeyCode.LeftArrow)) playRvs();
			if (Input.GetKeyDown(KeyCode.RightArrow)) playFwd();
			if (Input.GetKeyDown(KeyCode.DownArrow)) playPause();         
			if (Input.GetKeyDown(KeyCode.UpArrow)) ctunity.toggleReplay();
            
            // adjust slider value if playFwd or playRvs
			if (playFactor != 0)
            {
				double dt = playFactor * (nowTime() - playTimeRef);
				slider.value = Mathf.Clamp(slider.value + (float)(dt / durationReplayTime), 0F, 1F);
				playTimeRef = nowTime();
            }
			else {      // manual slider control:
				
			}

            // limit checks
			if (slider.value >= 1F && playFactor > 0F)    // was >= 1, but that insta-pauses on startup when value==1 ??
            {
                playPause();
                slider.value = 1F;
            }
            else if (slider.value <= 0F)
            {
                playPause();
                slider.value = 0F;
            }

			oldSliderValue = slider.value;

			masterTime = startReplayTime + slider.value * durationReplayTime;
			setStateText(replayLabel);
			timeText.text = "T-"+(endReplayTime - masterTime).ToString("F2");
		}
		else
		{
			slider.gameObject.SetActive(false);

			// check for downArrow double-click enter Replay mode
			if (/* ctunity.observerFlag &&  */ Input.GetKeyDown(KeyCode.DownArrow)) {
				if((nowTime()-clickTime) < 0.5F) ctunity.toggleReplay();
				clickTime = nowTime();
			}        

			masterTime = ctunity.ServerTime();
			playFactor = 0F;

            // update info labels
            setStateText(liveLabel);
			timeText.text = "";
		}
        
	}

	//----------------------------------------------------------------------------------------------------------------
	private void startReplay() {
		// to do:  change replayDuration to oldest active CTstates time...
		double timeDelay = shortReplayDuration;    
		if (ctunity.observerFlag) timeDelay = longReplayDuration;           

		endReplayTime = ctunity.latestTime;

		startReplayTime = endReplayTime - timeDelay;
		durationReplayTime = endReplayTime - startReplayTime;

		setStateText(pausedLabel);

		playTimeRef = ctunity.ServerTime();
	}

	//----------------------------------------------------------------------------------------------------------------
    // UI button control functions

	public void playFwd() {
		playTimeRef = nowTime();        // for relative time-lapse
		playFactor = 1F;

	}
	public void playRvs() {
		playTimeRef = nowTime();
		playFactor = -1F;

	}
	public void playPause() {
		playTimeRef = nowTime();
		playFactor = 0F;
//		Debug.Log("playPause!!!");
	}

	//----------------------------------------------------------------------------------------------------------------
	private void stopReplay() {
		replayActive = false;
		ctunity.lastSubmitTime = ctunity.ServerTime();			// limit future replays to stopReplay time
		setStateText(liveLabel);
		slider.value = 1f;
	}
 
	//----------------------------------------------------------------------------------------------------------------
	public void setStateText(String statetext) {
		stateString = statetext;
		replayText.text = "<color=" + ctunity.Player + ">" + statetext + "</color>";
	}

	//----------------------------------------------------------------------------------------------------------------
	private double nowTime() {
		return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}
}
