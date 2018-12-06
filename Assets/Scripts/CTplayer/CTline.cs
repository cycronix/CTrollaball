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
public class CTline : MonoBehaviour {

	private CTunity ctunity;
	private CTclient ctclient;

	private LineRenderer lineR1;
	private Boolean startup = true;
    
	// Use this for initialization
	void Start () {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		ctclient = GetComponent<CTclient>();

		lineR1 = gameObject.AddComponent<LineRenderer>();
		Color myColor = ctunity.objectColor(gameObject);

		setLineProps(lineR1, myColor, myColor);
	}
    
	//----------------------------------------------------------------------------------------------------------------
	void Update() {
        
		if(ctclient.custom != null && ctclient.custom.Length > 6) {     // sanity checks
//			Debug.Log("Lines: " + ctclient.custom);
			string[] spoints = ctclient.custom.Split(';');
			lineR1.positionCount = spoints.Length;

            // build line 
			for (int j = 0; j < spoints.Length; j++) {
				string sv = spoints[j].Substring(1, spoints[j].Length - 2);     // remove the braces
				string[] sa = sv.Split(',');                                    // split x,y,z
				Vector3 newpoint = new Vector3(float.Parse(sa[0]), float.Parse(sa[1]), float.Parse(sa[2]));
                
				if (!startup && ctclient.playSmooth())
				{
					lineR1.SetPosition(j, Vector3.Lerp(lineR1.GetPosition(j), newpoint, Time.deltaTime * ctclient.TrackSpeed));
				}
				else
				{
					lineR1.SetPosition(j, newpoint);
				}
			}
		}
		startup = false;
	}

	//----------------------------------------------------------------------------------------------------------------
	void setLineProps(LineRenderer lineR, Color color1, Color color2)
	{
		lineR.positionCount = 0;
		lineR.loop = false;
		lineR.useWorldSpace = true;
		lineR.widthMultiplier = 0.04f;
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
