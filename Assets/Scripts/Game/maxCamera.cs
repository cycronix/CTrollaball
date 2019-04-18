//
//Filename: maxCamera.cs
//
// original: http://wiki.unity3d.com/index.php?title=MouseOrbitZoom
//

// extensively modified Cycronix 3/2018

/*
Copyright 2019 Cycronix
 
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

using UnityEngine;
using System;
using System.Collections;
//using System.Diagnostics;
using UnityEngine.EventSystems;

[AddComponentMenu("Camera-Control/3dsMax Camera Style")]
public class maxCamera : MonoBehaviour
{
	internal Transform target;
	public Vector3 targetOffset;
	public float distance = 5.0f;
	public float maxDistance = 20;
	public float minDistance = 0.1f;
	public float xSpeed = 200.0f;
	public float ySpeed = 200.0f;
	public int yLimit = 89;                     // 90 deg (vertical)
	public int zoomRate = 40;
//	public float panSpeed = 0.3f;
	public float zoomDampening = 5.0f;
	private double clickTime = 0F;          // double-click timer
    private float extraOffset = 0f;

    public float snapIn = 0.5f;
    public float snapOut = 2f;
    private Boolean snapOn = false;

	private float xDeg = 0.0f;
	private float yDeg = 0.0f;
	private float currentDistance;
	private float desiredDistance;
	private Quaternion desiredRotation;
	private Quaternion cameraRotation= Quaternion.identity;

	private Boolean rightMouseDown = false;
	private String targetName = "";
	private Vector3 oldTarget = Vector3.zero;
	private CTunity ctunity;
    private Boolean newTarget = false;
    private Vector3 velocity = Vector3.zero;

    private Vector3 initPosition = Vector3.zero;
    private Quaternion initRotation = Quaternion.identity;

    //----------------------------------------------------------------------------------------------------------------
    void Start() { 
		ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
//		Init(); 
	}

	//----------------------------------------------------------------------------------------------------------------
	void OnEnable() {
        initPosition = transform.position;
        initRotation = transform.rotation;
//        Debug.Log("initPostion: " + initPosition + ", initRotation: " + initRotation);
        Init(); 
    }
    
	//----------------------------------------------------------------------------------------------------------------
	public void Init()
	{
//        Debug.Log("RESET initPostion: " + initPosition + ", initRotation: " + initRotation);
        transform.position = initPosition;         // reset original pos/rot
        transform.rotation = initRotation;

        //        if(target == null) 
        target = GameObject.Find("Players").transform;  // something for init
		distance = Vector3.Distance(transform.position, target.position);
		currentDistance = distance;
		desiredDistance = distance;

		xDeg = Vector3.Angle(Vector3.right, transform.right );
		yDeg = Vector3.Angle(Vector3.up, transform.up );
	}

	//----------------------------------------------------------------------------------------------------------------
	public void setTarget(Transform itarget)
	{
        if (itarget == null) target = GameObject.Find("Players").transform;
        else
        {
            target = itarget;
            targetName = itarget.name;
            newTarget = true;
            oldTarget = target.position;
        }
        //       Debug.Log("setTarget: "+transform.position);
    }

    //----------------------------------------------------------------------------------------------------------------
    // handle near/far camera modes, auto-target Player

    private void Update()
	{
		// check for right-mouse double-click 
		Boolean mouseToggle = false;
		if (Input.GetMouseButtonDown(1))
		{
			if ((nowTime() - clickTime) < 0.25F) mouseToggle = true;
			clickTime = nowTime();
		}

		if (Input.GetKeyDown("space") || mouseToggle)        // lock camera on player
		{
            // auto-target player
            GameObject go = GameObject.Find(ctunity.Player + "/Avatar");
            if(go == null) go = GameObject.Find(ctunity.Player + "/Base/Avatar");
            if (go != null) setTarget(go.transform);
  //          Debug.Log("auto target: " + ctunity.Player + "/Avatar");
		}
    }

    //----------------------------------------------------------------------------------------------------------------
    /*
     * Camera logic on LateUpdate to only update after all character movement logic has been handled. 
     */
    void LateUpdate()               // LateUpdate reduces playback jitter
	{
		if (rightMouseDown)         // right-mouse drag cammera rotation
		{
			// deltaPos is normalized mouse-motion relative to mouse-down
			xDeg = deltaPos.y * 360f;               // scale such that 1x screen.width = full-circle orbit
			yDeg = deltaPos.x * 360f;
			// set camera rotation 
			cameraRotation = Quaternion.Euler(new Vector3(xDeg, yDeg, 0) + startCameraRotation);
		}
		else
		{
			startRotation = transform.rotation;
		}

        Vector3 tposition = oldTarget;
		if (target == null)
        {
			GameObject gotarget = GameObject.Find(targetName);        // try to re-target
			if (gotarget != null)
			{
	//			target = gotarget.transform;
	//			tposition = target.position;
			}
		}
		else {
            if (Vector3.Magnitude(target.position - oldTarget) > 0.01f) newTarget = false;          // no smoothing if moving target
            tposition = target.position;
 //           Debug.Log("target.postion: " + target.position + ", oldTarget: " + oldTarget + ", newTarget: " + newTarget);
        }
        oldTarget = tposition;
        
		////////Zoom Position
		// affect the desired Zoom distance with mouse scrollwheel
		float dd = Input.GetAxis("Mouse ScrollWheel");
		desiredDistance -= dd * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
		//clamp the zoom min/max
		desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
		// For smoothing of the zoom, lerp distance
		currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

		// special close-up logic:  for "vehicles" with pushForward, lock camera rotation to object-direction
		Vector3 snapOffset = Vector3.zero;
        float snapCheck = snapOn ? snapOut : snapIn;

        if (rightMouseDown)
        {
            newTarget = false;
            if (deltaPos.magnitude > 0F)
            {
                Vector3 desiredRotationDeg = transform.eulerAngles + new Vector3(xDeg, yDeg, 0F);
                float clampX = desiredRotationDeg.x;
                float tymin = 360F - yLimit;
                if (clampX > 0 && clampX < 180 && clampX > yLimit)
                    desiredRotationDeg = new Vector3(yLimit, transform.eulerAngles.y, 0F);
                else if (clampX > 180 && clampX < tymin)
                    desiredRotationDeg = new Vector3(tymin, transform.eulerAngles.y, 0F);

                transform.eulerAngles = desiredRotationDeg;
                startPos = Input.mousePosition;  // reset (incremental rotations more reliable?)
            }
        }
        else if ( (currentDistance < snapCheck) && isTargetVehicle() ) 
		{
            snapOn = true;
            extraOffset = (snapCheck - currentDistance) / snapCheck;
			desiredRotation = Quaternion.Euler(new Vector3(0, target.transform.rotation.eulerAngles.y, 0)) * cameraRotation;

            // Lerp jiggles with PlayerController Lerp.  But instant-rot doesn't ease-in distant/closeup transition
            // Lerp if large delta, jump if small
            float deltaRot = Math.Abs((desiredRotation.eulerAngles.y - transform.rotation.eulerAngles.y));
			while (deltaRot > 180) deltaRot -= 360F;

			if(Math.Abs(deltaRot) < 30F || ctunity.isReplayMode())  
				transform.rotation = desiredRotation;  // actually smoother to have camera lock-on rotation (let playerController dampen motion)
			else
			    transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * zoomDampening);  // fast!?
		}

		else {
			cameraRotation = Quaternion.Euler(new Vector3(30F, 0, 0));      // default closeup start angle
            extraOffset = 0;
            snapOn = false;
		}

		// calculate position based on the new currentDistance 
        Vector3 tpos = tposition - (transform.rotation * Vector3.forward * currentDistance) + targetOffset;
        tpos = new Vector3(tpos.x, tpos.y + extraOffset, tpos.z);
        float dx = Vector3.Magnitude(tpos - transform.position);
        if (newTarget)
        {
            //transform.position = Vector3.SmoothDamp(targetPos1, targetPos2, ref velocity, 1f / zoomDampening);
            transform.position = Vector3.Lerp(transform.position, tpos, Time.deltaTime * zoomDampening);
//            Debug.Log("Lerp target dx: "+dx);
            if (dx < 0.01F) newTarget = false;
        }
        else
        {
            //         velocity = Vector3.zero;
//            Debug.Log("jump target");
            transform.position = tpos;
        }
    }

    //----------------------------------------------------------------------------------------------------------------
    private Boolean isTargetVehicle()
    {
        if (target == null) return false;
        PlayerController pc = target.GetComponent<PlayerController>();
        if (pc == null) return false;
        return pc.isVehicle;
    }

    //----------------------------------------------------------------------------------------------------------------
    // more robust way to detect right-mouse drag...
    Vector3 startPos = Vector3.zero;
	Vector3 deltaPos = Vector3.zero;
	Quaternion startRotation = Quaternion.identity;
	Vector3 startCameraRotation = Vector3.zero;
   
	void OnGUI()
	{
		Event m_Event = Event.current;
		deltaPos = Vector3.zero;   // no floating

		if (m_Event.button != 1) return;					// only check right-mouse button?

		if (EventSystem.current.IsPointerOverGameObject())          // no orbit if clicking on UI element
        {
	//		return;
        }

		if (m_Event.type == EventType.MouseDown)
		{
			rightMouseDown = true;
			startPos = Input.mousePosition;
			startRotation = transform.rotation;
			startCameraRotation = cameraRotation.eulerAngles;
            //           Debug.Log("RMDown");
        }

        if (m_Event.type == EventType.MouseDrag && rightMouseDown)
		{
			//			rightMouseDown = true;
			deltaPos = (Input.mousePosition - startPos) / Screen.width;
			if (deltaPos.magnitude < 0.001F) deltaPos = Vector3.zero;
//            Debug.Log("RMDrag");

        }
        else 
       if (m_Event.type == EventType.MouseUp)
		{
			rightMouseDown = false;
//			deltaPos = Vector3.zero;   // cancel
//            Debug.Log("RMup");
        }
    }

	//----------------------------------------------------------------------------------------------------------------
    private double nowTime()
    {
        return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
