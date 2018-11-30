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

// Build XY Queue from Player positions to feed CTchart/LineRenderer trail 

using System;
using System.Collections.Generic;
using UnityEngine;

//----------------------------------------------------------------------------------------------------------------
public class CTtrackset : MonoBehaviour {
	public Boolean trackEnabled = true;
	public int MaxPts = 500;
    
	private CTunity ctunity;
	private CTclient ctclient;

	private Queue<Vector3> XYplayer = new Queue<Vector3>();     // player ball track
	private LineRenderer lineR1;

	private Color myColor = Color.clear;  // clear means use default color

	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		ctclient = GetComponent<CTclient>();

		lineR1 = gameObject.AddComponent<LineRenderer>();
		//		Color myColor = ctunity.Text2Color(name, 1F);
		setLineProps(lineR1, Color.black, Color.clear);             // KISS
	}

	//----------------------------------------------------------------------------------------------------------------
    private void OnEnable()
    {
		XYplayer.Clear();
        if(lineR1 != null) lineR1.positionCount = 0;  // clears old 3D line
    }
    
	//----------------------------------------------------------------------------------------------------------------
	void Update() {
		if (!trackEnabled) return;
        
//		if (ctunity.isReplayMode())
		if (!ctunity.trackEnabled || ctunity.isPaused())
        {          
            XYplayer.Clear();
			lineR1.positionCount = 0;  // clears old 3D line
            return;
        }

		XYplayer.Enqueue(transform.position);
		while (XYplayer.Count > MaxPts) XYplayer.Dequeue(); // limit size of queue

//		setLineProps(lineR1, ctclient.myColor, ctclient.myColor);
		lineR1.positionCount = XYplayer.Count;
		lineR1.SetPositions(XYplayer.ToArray());

        // Debug.Log ("lineR1 x: " + xv [xv.Length-1].x + ", y: " + xv [xv.Length-1].y + ", z: " + xv [xv.Length-1].z);
	}

	//----------------------------------------------------------------------------------------------------------------
	void setLineProps(LineRenderer lineR, Color color1, Color color2)
	{
		lineR.positionCount = 0;
		lineR.loop = false;
		lineR.useWorldSpace = true;
		lineR.widthMultiplier = 0.03f;
		lineR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		lineR.receiveShadows = false;
		lineR.material = new Material(Shader.Find("Sprites/Default"));
		lineR.numCornerVertices = 0;

		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(color1, 0.0f), new GradientColorKey(color2, 1.0f) },
			new GradientAlphaKey[] {
					new GradientAlphaKey (0.0f, 0.01f),
					new GradientAlphaKey (0.8f, 0.2f),
					new GradientAlphaKey (1.0f, 1.0f)
			}
		);
		lineR.colorGradient = gradient;
	}
}
