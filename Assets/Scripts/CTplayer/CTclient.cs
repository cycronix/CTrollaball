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
using CTworldNS;

//----------------------------------------------------------------------------------------------------------------

public class CTclient : MonoBehaviour
{
	public Boolean smoothTrack = false;
	public Boolean smoothReplay = false;

	public float TrackSpeed = 2F;               // multiplier on how fast to Lerp to position/rotation
	public float RotateSpeed = 1F;              // rotation speed multiplier
    public float OverShoot = 0.5F;              // how much to shoot past known position for dead reckoning

	internal String prefab="Player";            // programmatically set; reference value
	internal String link = "";                  // for sending custom info via CTstates.txt
	internal String custom = "";                // catch-all custom string

	private Boolean ChildOfPlayer = false;      // global or child of (connected to) player object
	private Vector3 myPos = Vector3.zero;
	private Vector3 myScale = Vector3.one;

	private Quaternion myRot = new Quaternion(0, 0, 0, 0);
	private Boolean myState = true;

	private Boolean startup = true;
	private Boolean replayMode = false;
    
	private Rigidbody rb;
	private CTunity ctunity;

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start()
	{
		rb = GetComponent<Rigidbody>();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script

		// see if this client object is child-of-player (set in ToggleGameObject)
		if (gameObject.name.Contains("/")) ChildOfPlayer = true;

		ctunity.CTregister(gameObject);     // register with CTunity...
	}
    
	//----------------------------------------------------------------------------------------------------------------
	void FixedUpdate()
	{
		doTrack();
	}

	//----------------------------------------------------------------------------------------------------------------
    // setState called from CTunity, works on active/inactive
    
	public void setState(CTobject cto, Boolean ireplay) {
		
		// globals to share with Update() loop:
		myPos = cto.pos;
		myRot = cto.rot;
		myScale = cto.scale;
		replayMode = ireplay;
		custom = cto.custom;

		// locals for immediate action:
		Color icolor = cto.color;

		if(replayMode || !isLocalObject()) gameObject.SetActive(cto.state);         // need to activate here (vs Update callback)
        
		if (rb != null)
		{
			if (replayMode) { rb.isKinematic = true; rb.useGravity = false; }
			else            { rb.isKinematic = false; rb.useGravity = true; }
		}

		if(icolor != null && icolor != Color.clear) {
			Renderer renderer = transform.gameObject.GetComponent<Renderer>();
			if (renderer != null)
			{
				renderer.material.color = icolor;
                // difficult to change rendering mode in script...  following NG
//				if (icolor.a >= 1.0F)   renderer.material.SetFloat("_Mode", 0f);    // opaque
//				else                    renderer.material.SetFloat("_Mode", 2f);    // fade
			}
		}
		startup = false;
	}
    
	//----------------------------------------------------------------------------------------------------------------
	private Vector3 targetPos = Vector3.zero;
	private Vector3 oldPos = Vector3.zero;
	private Vector3 velocity = Vector3.zero;

	// doTrack runs under Update(), enables smooth Lerp's
	// note:  doTrack (called from Update) doesn't spin for inactive objects!
	private void doTrack()
	{
		if (ChildOfPlayer) return;                      // relative "attached" child object go for ride with parent
        
		if (isLocalControl() || startup)
		{
			if(rb != null) rb.useGravity = true;
			return;
		}
        
		if(rb != null) rb.useGravity = false;                  // no gravity if track-following

		if ((smoothTrack && !replayMode) || (smoothReplay && replayMode))
		{
			targetPos = myPos + OverShoot * (myPos - transform.position);    // dead reckoning
			oldPos = myPos;

			// SmoothDamp with t=0.4F is ~smooth, but ~laggy
			transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 1F/TrackSpeed);
//			transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * TrackSpeed);
			transform.rotation = Quaternion.Lerp(transform.rotation, myRot, Time.deltaTime * RotateSpeed);
			if(myScale != Vector3.zero)
				transform.localScale = Vector3.Lerp(transform.localScale, myScale, Time.deltaTime * RotateSpeed);
		}
		else
		{
			stopMoving();     // stop moving!

            transform.position = myPos;                     // hard-set without smooth
            transform.rotation = myRot;
			if (myScale != Vector3.zero) transform.localScale = myScale;
		}
	}

	//----------------------------------------------------------------------------------------------------------------
	public void stopMoving() {
		if(rb != null) rb.velocity = rb.angularVelocity = Vector3.zero;
	}
    
	//----------------------------------------------------------------------------------------------------------------
	public Boolean isRemoteControl() {
		return ( !startup && (replayMode || !isLocalObject() || ctunity.showMenu) );
	}
    
	//----------------------------------------------------------------------------------------------------------------
	public Boolean isLocalControl() {
		return (isLocalObject() && !replayMode && !ctunity.showMenu);
	}

	//----------------------------------------------------------------------------------------------------------------
	private Boolean isLocalObject()
    {
		Boolean localObject = false;
		if (ctunity == null) return false;
		if (gameObject.name.StartsWith(ctunity.Player) && !prefab.Equals("Ghost") && !ctunity.observerFlag)
			    localObject = true;

		return localObject;
    }

	//----------------------------------------------------------------------------------------------------------------
	public Boolean isReplayMode()
    {
        return replayMode;
    }
    
}

