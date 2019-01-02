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
	public Boolean smoothTrack = true;
	public Boolean smoothReplay = false;

	internal float TrackSpeed = 5F;             // multiplier on how fast to Lerp to position/rotation
												//	internal float RotateSpeed = 1F;            // rotation speed multiplier
	public float DeadReckon = 1.5F;             // how much to shoot past known position for dead reckoning
	public float SmoothTime = 0.4F;             // SmoothDamp time (sec)

	public Boolean autoColor = false;            // set object color based on naming convention
	public Boolean isRogue = false;             // "rogue" clients flag ignore local controls

	public CTobject ctobject = null;            // for public ref

	internal String prefab = "";                  // programmatically set; reference value
	internal String link = "";                  // for sending custom info via CTstates.txt
	internal String custom = "";                // catch-all custom string

	internal Color myColor = Color.clear;       // keep track of own color setting

	private Boolean noTrack = false;      // global or child of (connected to) player object
	private Vector3 myPos = Vector3.zero;
	private Vector3 myScale = Vector3.one;
	private Quaternion myRot = new Quaternion(0, 0, 0, 0);

	private Boolean startup = true;
	internal Boolean replayMode = false;
	private Boolean playPaused = true;

	private Rigidbody rb;
	private CTunity ctunity;

	// Lerp helper params:
	private Vector3 targetPos = Vector3.zero;
	private Vector3 velocity = Vector3.zero;
    //	private Vector3 rotvel = Vector3.zero;
	private float stopWatch = 0F;

	private Vector3 oldPos = Vector3.zero;
	private Vector3 oldScale = Vector3.one;
	private Quaternion oldRot = Quaternion.identity;

	private String fullName = "";

	internal int Generation = 0;                  // keep track of clone-generation
	internal float runTime = 0f;                    //  keep track of live running time

	//----------------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start()
	{
		rb = GetComponent<Rigidbody>();
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
		if (autoColor) setColor();          // set default color based on object name
		fullName = CTunity.fullName(gameObject);        // get once in advance
		ctunity.CTregister(gameObject);                 // register with CTunity...

	}

	//----------------------------------------------------------------------------------------------------------------
	void Update()
	//    void FixedUpdate()
	{
//		Debug.Log(name + ", position: " + transform.localPosition);
		doTrack();
	}

	//----------------------------------------------------------------------------------------------------------------
	// setState called from CTunity, works on active/inactive

	public void setState(CTobject cto, Boolean ireplay, Boolean iplayPaused)
	{
		if (ctunity == null) return;        // async
											//		Debug.Log(cto.id+": setState, replayMode: " + ireplay);
		ctobject = cto;                             // for public ref
        
		// set baseline for Lerp going forward to next step
		oldPos = transform.localPosition;
		oldRot = transform.localRotation;
		oldScale = transform.localScale;

		if (stopWatch > 0F) TrackSpeed = (3F * TrackSpeed + (1F / stopWatch)) / 4F;  // weighted moving average

		// update targets
		stopWatch = 0F;
		myPos = cto.pos;
		myRot = cto.rot;
		myScale = cto.scale;
		//		Debug.Log("myScale: " + myScale);
		myColor = cto.color;

		replayMode = ireplay;
		playPaused = iplayPaused;           // playPaused used by smooth replay logic
		custom = cto.custom;

		// locals for immediate action:
		if (replayMode || !isLocalObject())
		{
			gameObject.SetActive(cto.state);            // need to activate here (vs Update callback)
			setColor(cto.color);                        // set color for non-local objects
		}

		if (rb == null) rb = GetComponent<Rigidbody>();    // try again; async issue?
		setGravity();

		//		Debug.Log(name+", ilc: "+isLocalControl());
		startup = false;
	}

	private void setGravity() { 
		if (rb != null)
        {
            if (replayMode || !isLocalControl())
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
	}


	//----------------------------------------------------------------------------------------------------------------
    // jump to current position
	public void jumpState() {
		if (startup) return; 

        stopMoving();     // stop moving!
        transform.localPosition = myPos;                   
        transform.localRotation = myRot;
        if (myScale != Vector3.zero) transform.localScale = myScale;

		oldPos = transform.localPosition;        // reset prior-state
        oldRot = transform.localRotation;
        oldScale = transform.localScale;

		velocity = Vector3.zero;
//		Debug.Log(name+": jumpState to pos: " + myPos);
	}

	//----------------------------------------------------------------------------------------------------------------
	// doTrack runs under Update(), enables smooth Lerp's
	// note:  doTrack (called from Update) doesn't spin for inactive objects!
	private void doTrack()
	{
		if (noTrack) return;                 // relative "attached" child object go for ride with parent

        if (isLocalControl() || startup)
		{
//			Debug.Log(name + ": localControl, no track!");
//			if(rb != null) rb.useGravity = true;
			setGravity();
			return;
		}
//		Debug.Log("client setTrack myPos: " + myPos);
		float dt = Time.deltaTime;
		stopWatch += dt;
		if(rb != null) rb.useGravity = false;                  // no gravity if track-following

		if (playSmooth())
		{
			// SmoothDamp is smoother than linear motion between updates...
			// SmoothDamp with t=0.4F is ~smooth, but ~laggy

			float Tclamp = Mathf.Clamp(stopWatch * TrackSpeed, 0f, DeadReckon);  // custom clamp extrapolated interval
//			Debug.Log(name + ": playSmooth, myPos: " + myPos);

			if (SmoothTime > 0F)
			{
				targetPos = transform.localPosition + DeadReckon * (myPos - transform.localPosition);    // dead reckoning
				transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref velocity, SmoothTime);
				//                Vector3 rot = Vector3.SmoothDamp(transform.rotation.ToEuler(), myRot.ToEuler(), ref rotvel, SmoothTime);
				//				transform.rotation = Quaternion.Euler(rot);
//				Debug.Log(name+": smoothTrack from: " + transform.localPosition + ", to: " + targetPos);
			}
			else {
//				Debug.Log(name + ": LerpTrack from: " + oldPos + ", to: " + myPos);
				transform.localPosition = Vector3.LerpUnclamped(oldPos, myPos, Tclamp);
			}

			// LerpUnclamped:  effectively extrapolates (dead reckoning)
			transform.localRotation = Quaternion.LerpUnclamped(oldRot, myRot, Tclamp);
			if(myScale != Vector3.zero)
				transform.localScale = Vector3.LerpUnclamped(oldScale, myScale, Tclamp);
		}
		else
		{
//			Debug.Log(name + ": playRough, myPos: " + myPos);
			jumpState();        // hard set without smooth
		}
	}
       
	//----------------------------------------------------------------------------------------------------------------
    // apply smoothing to position updates?
	public Boolean playSmooth() {
//		Debug.Log(name + ": smoothTrack: " + smoothTrack + ", replayMode: " + replayMode);
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
		if (ctunity == null) return false;  // async?
		Boolean ilc = isRogue || ctunity.activePlayer(gameObject);
		return ilc;
	}
    
	//----------------------------------------------------------------------------------------------------------------
	public Boolean isLocalObject()
    {
		Boolean localObject = false;
		if (ctunity == null) return false;
//		if (CTunity.fullName(gameObject).StartsWith(ctunity.Player) && !prefab.Equals("Ghost") && !ctunity.observerFlag)
		if (fullName.StartsWith(ctunity.Player+"/") && !prefab.Equals("Ghost") /* && !ctunity.observerFlag */)
			    localObject = true;

//		if (gameObject.name.Equals("Avatar")) Debug.Log("isLocalObject: "+localObject+", name: " + gameObject.name + ", Player: " + ctunity.Player+", observer: "+ctunity.observerFlag);
		return localObject;
    }

	//----------------------------------------------------------------------------------------------------------------
	public Boolean isReplayMode()
    {
        return replayMode;
    }

	//----------------------------------------------------------------------------------------------------------------
	// set object color

	internal void setColor(Color color) {
		if (color == Color.clear) return;          // use default color
//		Debug.Log("setColor(" + color + "), autoColor: " + autoColor+", ctunity: "+ctunity);

		if (autoColor && ctunity != null)
		{
			color = ctunity.objectColor(gameObject);
//			Debug.Log(">setColor(" + color + "), autoColor: " + autoColor);
		}
		myColor = color;

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
		setColor(Color.gray);   // default or auto
//		Debug.Log("setColor()!");//       Color color = ctunity.objectColor(gameObject);
//		if(!color.Equals(Color.gray)) setColor(color);      // don't set if default (no name match)
	}
}

