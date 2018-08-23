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

// Stripchart -OR- Cross-plot X-Y chart from CloudTurbine data
// Matt Miller, Cycronix, 6-16-2017

public class CTchart : MonoBehaviour {
	private string Server = "http://localhost:8000";
	private string Source = "CTmousetrack";
	private string Chan1 = "x";
	private string Chan2 = "y";
	private string Mode = "StripChart";
	private int numChan=0;						// NumDims: 1=stripchart, 2=xyplot

	public int MaxPts = 500;					// max points in data "trail"
	public float pollInterval = 0.05f;          // polling interval for new data (sec)
//	public Boolean observerMode = false;        // observer mode (CT read-only)

	private float Duration = 1F;
    
	private CTchartOptions chartOptions = null;

	private GameObject Chart1;
	private GameObject Chart2;
    
	private LineRenderer lineR1;
	private LineRenderer lineR2;
	private CTunity ctunity=null;
	private CTclient ctclient=null;

	Vector3[] p1=null, p2=null;
	int Ngot = 0;

//	string[] xvals = null, yvals = null;

	void Start()
	{
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		ctclient = GetComponent<CTclient>();              // interactive CT updates

		foreach (Transform child in transform) {
			if (child.name == "Chart1") {
				Chart1 = child.gameObject;
				Chart1.AddComponent<LineRenderer> ();
				lineR1 = Chart1.GetComponent<LineRenderer>();
			}
			if (child.name == "Chart2"){
				Chart2 = child.gameObject;
				Chart2.AddComponent<LineRenderer> ();
				lineR2 = Chart2.GetComponent<LineRenderer>();
			}
		}

		setLineProps (lineR1, Color.blue);
		setLineProps (lineR2, Color.red);
	}

	private void OnEnable()
	{
		StartCoroutine("getData");
	}

	void Update() {

		if (chartOptions == null) {
			foreach (Transform child in transform) {
				if (child.name == "ChartOptions") {
					chartOptions = child.gameObject.GetComponent<CTchartOptions>();
				}
			}
		} else {
			Server = chartOptions.Server;
			Source = chartOptions.Source;
			Chan1 = chartOptions.Chan1;
			Chan2 = chartOptions.Chan2;
			Mode = chartOptions.Mode;
			Duration = chartOptions.Duration; 
//			MaxPts = chartOptions.MaxPts;
		}

//		updateLines();
	}

	void setLineProps (LineRenderer lineR, Color linecolor) {
		lineR.positionCount = 0;
		lineR.loop = false;
		lineR.useWorldSpace = false;
		lineR.widthMultiplier = 0.002f;
		lineR.material = new Material(Shader.Find("Sprites/Default"));  // needed
		lineR.material.color = linecolor;
		lineR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		lineR.receiveShadows = false;
	}

	// fetch XY values from CloudTurbine, store in FIFO (queue)
	String oldCustom = "";
	IEnumerator getData()
	{
		while (true) {
			yield return new WaitForSeconds (pollInterval);
			if (chartOptions==null || chartOptions.showMenu) continue;
//			Debug.Log("CTchart chartOptions.showMenu: " + chartOptions.showMenu);

			string urlparams = "?f=d";                  // drop this to get time,data pairs...

			// two channels = two HTTP GETs
			WWW www1=null;
			WWW www2=null;

			// notta 
            if (Chan1.Length == 0)
            {
                lineR1.positionCount = 0;
                yield return null;
            }
            if (Chan2.Length == 0 || Mode == "CrossPlot")
            {
                lineR2.positionCount = 0;
            }

			string url1="", url2="";
			if (ctclient!=null && ctclient.enabled && ctclient.custom!=null && !ctclient.isLocalControl())
//			if (ctunity.isReplayMode())
			{
				if (ctclient.custom.Equals(oldCustom)) continue;
				string[] customparts = ctclient.custom.Split(',');
				url1 = customparts[0];
				if (customparts.Length > 1)
				{
					url2 = customparts[1];
					numChan = 2;
				}
				else numChan = 1;
				oldCustom = ctclient.custom;
			}
			else 
            {
				// figure out xplot and chart1/2 situation
                if (Chan2.Length > 0)   numChan = 2;
                else                    numChan = 1;

//				urlparams += "&t=" + (ctunity.ServerTime() - Duration) + "&d=" + Duration;
				urlparams += "&t=" + (ctunity.replayTime - Duration) + "&d=" + Duration;  // live or replay

				url1 = Server + "/CT/" + Source + "/" + Chan1 + urlparams;
				url2 = Server + "/CT/" + Source + "/" + Chan2 + urlparams;
				oldCustom = "";
            }

            // fetch data
			try
			{
				www1 = new WWW(url1);

				if (numChan > 1)
				{
					www2 = new WWW(url2);
					if(ctclient!=null) ctclient.custom = url1 + "," + url2;
				}
				else
				{
					if(ctclient!=null) ctclient.custom = url1;
				}
			} catch(Exception e) {
				Debug.Log("CTchart Exception on WWW fetch: "+e);
				continue;
			}

			yield return www1;
			if(numChan > 1) yield return www2;
//			Debug.Log("CTchart url1: " + url1);

			if (!string.IsNullOrEmpty (www1.error)) {
				Debug.Log("www1.error: " + www1.error+", url1: "+url1);
			}
			else {

				try {
					// fetch time-interval info from header (vs timestamps)
					Dictionary<string,string> whead = www1.responseHeaders;
					double htime = 0, hdur = 0;
					try {
						if (whead.ContainsKey ("time")) 	htime = double.Parse (whead ["time"]);
						if (whead.ContainsKey ("duration"))	hdur = double.Parse (whead ["duration"]);
					} catch (Exception) {
						Debug.Log ("Exception on htime parse!");
					}

					// parse data into value queues
				    string[] xvals = www1.text.Split ('\n');
					string[] yvals = null;
					if (numChan > 1) yvals = www2.text.Split('\n');

					double ptsPerSec = xvals.Length / hdur;              // deduce queue size from apparent sample rate
					MaxPts = (int)(Duration * ptsPerSec);

					if (Mode == "CrossPlot")
					{
						yvals = www2.text.Split('\n');
						int maxCount = Math.Min(xvals.Length, yvals.Length);
						p1 = new Vector3[maxCount];
						p2 = null;

						Ngot = 0;
						for (int i = 0; i < maxCount; i++)
						{
							try
							{
								float xv = float.Parse(xvals[i]) - 0.5f;
								float yv = float.Parse(yvals[i]) - 0.5f;
								p1[Ngot] = new Vector3(xv, yv, -0.6f);
							} 
							catch(Exception) {};

							Ngot++;
						}
						lineR1.positionCount = Ngot-2;   // why ratty end???
                        lineR1.SetPositions(p1);
					}
					else                // stripchart
					{
						int maxCount = xvals.Length;
						p1 = new Vector3[maxCount];

						if (numChan > 1)
						{
							yvals = www2.text.Split('\n');
							maxCount = Math.Min(xvals.Length, yvals.Length);
							p2 = new Vector3[maxCount];
						}
						else p2 = null;

						float x1 = -0.5f;
						float dx = 1.0f / (maxCount - 1);

						Ngot = 0;
						for (int i = 0; i < maxCount; i++)
						{
							try
							{
								float xv = float.Parse(xvals[i]) - 0.5f;
								if (numChan == 1) xv = float.Parse(xvals[i]) / 65536.0f;  // cluge: audio scaling
								p1[Ngot] = new Vector3(x1, xv, -0.6f);

								if (numChan > 1)
								{
									float yv = float.Parse(yvals[i]) - 0.5f;
									p2[Ngot] = new Vector3(x1, yv, -0.6f);
								}

							}
							catch (Exception) {
								x1 += dx;
							}

							Ngot++;
							x1 += dx;
						}
						lineR1.positionCount = lineR2.positionCount = Ngot-2;  // why ratty end???
						lineR1.SetPositions(p1);
						if (numChan > 1) lineR2.SetPositions(p2);
					}

				} catch (FormatException) {
					Debug.Log ("Error parsing values! "+www1.text);
				}
			} 

			www1.Dispose ();  	
			www1 = null;
			if (numChan > 1) {
				www2.Dispose ();
				www2 = null;
			}
		}
	}
/*		
	void updateLines() {
		Debug.Log("updateLines p1: " + p1);
		lineR1.positionCount = lineR2.positionCount = Ngot;
        if (p1 != null) lineR1.SetPositions(p1);
        if (p2 != null) lineR2.SetPositions(p2);
	}
*/	
}
