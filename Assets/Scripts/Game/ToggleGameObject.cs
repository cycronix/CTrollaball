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

public class ToggleGameObject : MonoBehaviour, IPointerDownHandler
{      
	public String Prefab = "";
	public Vector3 Position = new Vector3(0, 0, 0);
	public Vector3 Rotation = new Vector3(0, 0, 0);
	public float Scale = 1F;

	public Boolean ChildOfPlayer = false;
	public Boolean UiLayer = false;

	private Boolean Active = false;
	private CTunity ctunity;
	private GameObject thisObject=null;

    //----------------------------------------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
	}

    //----------------------------------------------------------------------------------------------------------------
    // onPointerDown for UI objects
    public void OnPointerDown(PointerEventData eventData)
    {
//		if (Prefab.Equals("") || ctunity.showMenu || ctunity.isReplayMode()) return;                // not initialized
		if (Prefab.Equals("")) return;                // not initialized
		if (ChildOfPlayer && (ctunity.showMenu || ctunity.isReplayMode() || ctunity.observerFlag)) return;
              
		toggleGameObject();

		if (thisObject.activeSelf) GetComponent<RawImage>().color = Color.red;
        else GetComponent<RawImage>().color = Color.white;
    }
    
	//----------------------------------------------------------------------------------------------------------------
	// create new game object
	private void toggleGameObject()
    {
		GameObject thisobj = GameObject.Find(ctunity.Player + "/" + Prefab);
		if (thisobj != null) thisObject = thisobj;

//		if (thisObject != null)                         // exists; toggle active state on/off
        if (thisObject != null && thisObject.activeSelf)
		{
			//			thisObject.SetActive(!thisObject.activeSelf);       // toggle active/inactive
			if (UiLayer) thisObject.SetActive(false);
			else         ctunity.clearObject(thisObject.name);
		}
		else                                    // load prefab
		{
			String objectName = ctunity.Player + "." + Prefab;
			if (ChildOfPlayer) objectName = ctunity.Player + "/" + Prefab;

			if (UiLayer)
			{
				GameObject tgo = GameObject.Find(objectName);

                GameObject go = ((GameObject)ctunity.getPrefab(Prefab));
                go.SetActive(true);
                            
                Transform pf = go.transform;
				Transform newp = Instantiate(pf, Position, Quaternion.Euler(Rotation) * pf.rotation);    // parent
				thisObject = newp.gameObject;

                // CTchartCanvas is empty holder for UI-layer charts
				thisObject.transform.SetParent(GameObject.Find("CTchartCanvas").transform, false);
//				thisObject.transform.SetParent(GameObject.Find("Players").transform, false);

				thisObject.transform.localScale = Scale * thisObject.transform.localScale;

                // following seems to disable entire CTchart object
				CTclient ctc = thisObject.transform.GetComponent<CTclient>();
				if (ctc != null) ctc.enabled = false;

				newp.name = objectName;
			}
			else
			{
				// use ctunity method to create object: gets it on CTlist and multi-player CTstates.txt
				thisObject = ctunity.newGameObject(objectName, Prefab, Position, Quaternion.Euler(Rotation), Vector3.one, false, true);
				thisObject.transform.localScale = Scale * thisObject.transform.localScale;
			}
            
		}
    }

}
