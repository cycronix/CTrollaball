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

public class ToggleTracks : MonoBehaviour, IPointerDownHandler
{
	private CTunity ctunity;

    // Use this for initialization
    void Start()
    {
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		setColor();
    }

    //----------------------------------------------------------------------------------------------------------------
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
        {
			ctunity.trackEnabled = !ctunity.trackEnabled;
			setColor();
        }
    }

	private void setColor() {
		if (ctunity.trackEnabled) GetComponent<RawImage>().color = Color.red;
        else GetComponent<RawImage>().color = Color.white;
	}
}
