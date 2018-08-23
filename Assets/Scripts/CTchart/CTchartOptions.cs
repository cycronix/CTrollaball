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

// Config Menu for CloudTurbine chart displays
// Matt Miller, Cycronix, 6-16-2017

public class CTchartOptions : MonoBehaviour {
	public Boolean showMenu = true;
	public string Server = "http://localhost:8000";
	public string Source = "CTmousetrack";
	public string Chan1 = "x";
	public string Chan2 = "y";
	public string Mode = "StripChart";
	public float Duration = 1F;
//	public int MaxPts = 500;
	private CTclient ctclient;
	private Rigidbody rbc;

	// Use this for initialization
	void Start () {
		ctclient = transform.parent.GetComponent<CTclient>();
		rbc = transform.parent.parent.GetComponent<Rigidbody>();  // presumes chart is grandchild of vehicle
			
		InputField[] fields = gameObject.GetComponentsInChildren<InputField>();
		foreach (InputField c in fields) {
			switch (c.name) {
			case "Server":
				c.text = Server;
				break;
			case "Source":
				c.text = Source;
				break;
			case "Chan1":
				c.text = Chan1;
				break;
			case "Chan2":
				c.text = Chan2;
				break;
			case "Duration":
                c.text = Duration + "";
                break;
//			case "MaxPts":
//				c.text = MaxPts+"";
//				break;
			}
		}
			
		Dropdown mode = gameObject.GetComponentInChildren<Dropdown> ();
		List<Dropdown.OptionData> modes = mode.GetComponent<Dropdown> ().options;
		if (modes[0].text == "StripChart") 	mode.value = 0;		// crude, hard-wired
		else 								mode.value = 1;

		Button[] buttons = gameObject.GetComponentsInChildren<Button> ();
		foreach (Button b in buttons) {
			switch (b.name) {
			case "Submit":
				b.onClick.AddListener (submitButton);
				break;
			case "Cancel":
				b.onClick.AddListener (cancelButton);
				break;
			
			}
		}
	}

	void submitButton() {	
		InputField[] fields = gameObject.GetComponentsInChildren<InputField>();
		foreach (InputField c in fields) {
			switch (c.name) {
			case "Server":	Server = c.text;	break;
			case "Source":	Source = c.text;	break;
			case "Chan1":	Chan1 = c.text;		break;
			case "Chan2":	Chan2 = c.text;		break;
			case "Duration":
                Duration = float.Parse(c.text);
                break;
//			case "MaxPts":
//				MaxPts = Int32.Parse (c.text);
//				break;
			}
		}
		Dropdown mode = gameObject.GetComponentInChildren<Dropdown> ();
		Mode = mode.GetComponent<Dropdown>().options[mode.value].text;

		showMenu = false;	

	}

	void cancelButton() {
		showMenu = false;
	}

	// Update is called once per frame
	void Update () {
//		Debug.Log("CTchartOptions showMenu: " + showMenu + ", localControl: "+ctclient.isLocalControl());
		if (ctclient!=null && ctclient.enabled && !ctclient.isLocalControl())
		{
			showMenu = false;  // only local-control objects set menu
		}

		foreach (Transform child in transform) {
			if (child.name == "ChartCanvas") {
				child.gameObject.SetActive (showMenu);
			}
		}

		// keep from moving when typing
		if (rbc != null)
		{
			if (showMenu) rbc.constraints = RigidbodyConstraints.FreezeAll;
			else rbc.constraints = RigidbodyConstraints.None;
		}
	}


	public void OnMouseEnter() {
//		Debug.Log ("MouseEnter");
//		showMenu = true;
	}

	public void OnMouseExit() {
//		Debug.Log ("MouseExit");
//		showMenu = false;
	}
    
	public void OnMouseDown() {
//		Debug.Log (gameObject.name+": MouseDown");
		showMenu = true;
	}

	public void OnMouseUp() {
//		Debug.Log ("MouseUp");
	}

}
