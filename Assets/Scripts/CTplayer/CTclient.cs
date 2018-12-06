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
using CTworldNS;

//----------------------------------------------------------------------------------------------------------------

public class CTclient : MonoBehaviour
{
	public Boolean smoothTrack = false;
	public Boolean smoothReplay = false;

	internal float TrackSpeed = 5F;             // multiplier on how fast to Lerp to position/rotation
//	internal float RotateSpeed = 1F;            // rotation speed multiplier
    public float DeadReckon = 1.5F;             // how much to shoot past known position for dead reckoning
	public float SmoothTime = 0.4F;             // SmoothDamp time (sec)

	public Boolean autoColor = true;            // set object color based on naming convention
	public Boolean isGhost = false;             // set for "ghost" player (affects color-alpha)
	public Boolean isRogue = false;             // "rogue" clients flag ignore local controls
    
	public CTobject ctobject = null;            // for public ref

	internal String prefab="";                  // programmatically set; reference value
	internal String link = "";                  // for sending custom info via CTstates.txt
	internal String custom = "";                // catch-all custom string
    
	internal Color myColor = Color.clear;       // keep track of own color setting

	private Boolean noTrack = false;      // global or child of (connected to) player object
	private Vector3 myPos = Vector3.zero;
	private Vector3 myScale = Vector3.one;
	private Quaternion myRot = new Quaternion(0, 0, 0, 0);
//	private Boolean myState = true;

	private Boolean startup = true;
	internal Boolean replayMode = false;
	private Boolean playPaused = true;

	private Rigidbody rb;
	private CTunity ctunity;

	// Lerp helper params:
	private Vector3 targetPos = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private float stopWatch = 0F;

	private Vector3 oldPos = Vector3.zero;
    private Vector3 oldScale = Vector3.one;
    private Quaternion oldRot = Quaternion.identity;

	private String fullName = "";
	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start()
	{
		rb = GetComponent<Rigidbody>();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
        
//		if (gameObject.name.Contains("/")) noTrack = true;      // see if this client object is child-of-player (set in ToggleGameObject)

		if (autoColor) setColor();          // set default color based on object name

		if (isGhost)
        {
            Physics.IgnoreCollision(                    // no self-bump
                 GetComponent<Collider>(),
                 GameObject.Find(ctunity.Player).GetComponent<Collider>(),
                 true
            );
        }
        
		fullName = CTunity.fullName(gameObject);        // get once in advance

		ctunity.CTregister(gameObject);                 // register with CTunity...
	}
    
	//----------------------------------------------------------------------------------------------------------------
	void Update()
//    void FixedUpdate()
	{
//		Debug.Log("dt: " + Time.deltaTime);
		doTrack();
	}

	//----------------------------------------------------------------------------------------------------------------
	// setState called from CTunity, works on active/inactive

	public void setState(CTobject cto, Boolean ireplay, Boolean iplayPaused)
	{
//		if (name.Contains("Layer")) Debug.Log("setState, replayMode: " + ireplay);

		ctobject = cto;                             // for public ref
        
		// set baseline for Lerp going forward to next step
		oldPos = transform.position;
		oldRot = transform.rotation;
		oldScale = transform.localScale;

		if (stopWatch > 0F) TrackSpeed = (3F * TrackSpeed + (1F / stopWatch)) / 4F;  // weighted moving average

		// update targets
		stopWatch = 0F;
		myPos = cto.pos;
		myRot = cto.rot;
		myScale = cto.scale;
		myColor = cto.color;

		replayMode = ireplay;
		playPaused = iplayPaused;
		custom = cto.custom;

		// locals for immediate action:
		if (replayMode || !isLocalObject())
		{
			gameObject.SetActive(cto.state);            // need to activate here (vs Update callback)
			setColor(cto.color);                        // set color for non-local objects
		}

		if(rb==null) rb = GetComponent<Rigidbody>();    // try again; async issue?
		if (rb != null)
		{
			if (replayMode || !isLocalControl()) { 
				rb.isKinematic = true; 
				rb.useGravity = false; 
			}
			else { 
				rb.isKinematic = false; 
				rb.useGravity = true; 
			}
		}

		startup = false;
	}
    
	//----------------------------------------------------------------------------------------------------------------
	// doTrack runs under Update(), enables smooth Lerp's
	// note:  doTrack (called from Update) doesn't spin for inactive objects!
	private void doTrack()
	{
//		if (name.Contains("Layer")) Debug.Log("doTrack: " + name+", noTrack: "+noTrack+", localC: "+isLocalControl()+", startup: "+startup);
		if (noTrack) return;                 // relative "attached" child object go for ride with parent
		    
		if (isLocalControl() || startup)
		{
			if(rb != null) rb.useGravity = true;
			return;
		}
//		Debug.Log(name+": doTrack!!!");

		stopWatch += Time.deltaTime;
		if(rb != null) rb.useGravity = false;                  // no gravity if track-following

		if (playSmooth())
		{
			// SmoothDamp is smoother than linear motion between updates...
			// SmoothDamp with t=0.4F is ~smooth, but ~laggy

			float Tclamp = Mathf.Clamp(stopWatch * TrackSpeed, 0f, DeadReckon);  // custom clamp extrapolated interval

			if (SmoothTime > 0F)
			{
				targetPos = transform.position + DeadReckon * (myPos - transform.position);    // dead reckoning
				transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, SmoothTime);
			}
			else {
				transform.position = Vector3.LerpUnclamped(oldPos, myPos, Tclamp);
			}

			// LerpUnclamped:  effectively extrapolates (dead reckoning)
			transform.rotation = Quaternion.LerpUnclamped(oldRot, myRot, Tclamp);
			if(myScale != Vector3.zero)
				transform.localScale = Vector3.LerpUnclamped(oldScale, myScale, Tclamp);
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
    // apply smoothing to position updates?
	public Boolean playSmooth() {
		return (smoothTrack && !replayMode) || (smoothReplay && replayMode) || (smoothTrack && replayMode && !playPaused);
	}

	//----------------------------------------------------------------------------------------------------------------
	public void stopMoving() {
		if(rb != null) rb.velocity = rb.angularVelocity = Vector3.zero;
	}
    
	//----------------------------------------------------------------------------------------------------------------
	public Boolean isRemoteControl() {
		return ( !startup && (replayMode || !isLocalObject() || ctunity.gamePaused) );
	}
    
	//----------------------------------------------------------------------------------------------------------------
	public Boolean isLocalControl() {
		Boolean ilc = isRogue || (isLocalObject() && !replayMode && !ctunity.gamePaused);
//		if(gameObject.name.Equals("World.JB2")) Debug.Log(name+", isLocalControl: "+ilc+", isRogue: "+isRogue+", replayMode: "+replayMode+", showMenu: "+ctunity.showMenu);
		return ilc;
	}
    
	//----------------------------------------------------------------------------------------------------------------
	public Boolean isLocalObject()
    {
//		if (gameObject.name.Equals("World.JB2")) Debug.Log("ilo name: " + gameObject.name);
		Boolean localObject = false;
		if (ctunity == null) return false;
//		if (CTunity.fullName(gameObject).StartsWith(ctunity.Player) && !prefab.Equals("Ghost") && !ctunity.observerFlag)
		if (fullName.StartsWith(ctunity.Player) && !prefab.Equals("Ghost") && !ctunity.observerFlag)
			    localObject = true;

//		if (gameObject.name.Equals("World.JB2")) Debug.Log("isLocalObject: "+localObject+", name: " + gameObject.name + ", Player: " + ctunity.Player+", observer: "+ctunity.observerFlag);
		return localObject;
    }

	//----------------------------------------------------------------------------------------------------------------
	public Boolean isReplayMode()
    {
        return replayMode;
    }

	//----------------------------------------------------------------------------------------------------------------
	// set object color

	void setColor(Color color) {
		if (color == Color.clear) return;          // use default color
		myColor = color;

		if (isGhost) color.a = 0.4F;                                // force ghost to be translucent
		Renderer rend = gameObject.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;

        // apply color to any model component labelled "Trim":
        Component[] renderers = GetComponentsInChildren(typeof(Renderer));
        foreach (Renderer childRenderer in renderers)
        {
            if (childRenderer.material.name.StartsWith("Trim"))    // clugy
                childRenderer.material.color = color;
        }
	}

	// set color based on object name
	void setColor() {
        Color color = ctunity.objectColor(gameObject);
		if(!color.Equals(Color.gray)) setColor(color);      // don't set if default (no name match)
	}
}

