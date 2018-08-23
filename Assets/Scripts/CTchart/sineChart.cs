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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sineChart : MonoBehaviour {
	// Creates a line renderer that follows a Sin() function
	// and animates it.

	public Color c1 = Color.black;
	public Color c2 = Color.black;
	public int npts = 100;

	private Queue ybuf = new Queue();
	private float t = 0;

	void Start()
	{
		LineRenderer lineR = gameObject.AddComponent<LineRenderer>();
		lineR.widthMultiplier = 0.2f;
		lineR.positionCount = npts;
		lineR.loop = false;
		lineR.useWorldSpace = false;
		lineR.widthMultiplier = 0.01f;

		lineR.material.color = Color.blue;
		lineR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		lineR.receiveShadows = false;
	}

	void Update() {
		UpdateLine(Mathf.Sin(t), 2.0f);
		t += 2.0f * Mathf.PI / 100;
	}

	void UpdateLine(float yval, float yscale)
	{
		ybuf.Enqueue (yval);
		if (ybuf.Count > npts) ybuf.Dequeue ();

		LineRenderer lineR = GetComponent<LineRenderer>();
		if (lineR == null) return;

		float x1 = -0.5f;
		float dx = 1.0f / (npts - 1);
		int i = 0;
		foreach (float y in ybuf)
		{
			if (i >= npts) break;
			lineR.SetPosition(i, new Vector3(x1, y/yscale, -0.6f));
			x1 += dx;
			i += 1;
		}
	}
}
