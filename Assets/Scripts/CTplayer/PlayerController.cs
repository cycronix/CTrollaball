﻿/*
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

using System;
using UnityEngine;

//----------------------------------------------------------------------------------------------------------------
public class PlayerController : MonoBehaviour {
    private Rigidbody rb;
    public float ForceFactor = 10F;
    public float TorqueFactor = 5F;
    public float maxSpeed = 5F;
    public float liftForce = 0F;
    public Boolean isVehicle = true;
    public Boolean followLeader = false;

    private CTunity ctunity;
    private Quaternion baseRotation = new Quaternion(0, 0, 0, 0);
    private GameObject targetObject = null;

    private Camera mainCamera = null;

    //----------------------------------------------------------------------------------------------------------------
    // Use this for initialization
    void Start() {
        //		Debug.Log (CTunity.fullName(gameObject)+": Hello World!");

        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();       // reference CTunity script
        rb = GetComponent<Rigidbody>();
        baseRotation = transform.rotation;          // for relative rotation setting
        mainCamera = Camera.main;                   // up front for efficiency
    }

    //----------------------------------------------------------------------------------------------------------------
    // push player around using forces; let in-game physics decide where it goes

    //	void Update() 
    void FixedUpdate()          // fixed update for consistent force-application
    {
        if (!ctunity.activePlayer(gameObject)) return;

        float moveHorizontal = Input.GetAxis("Horizontal"); // Right Left arrow keys +/- 1
        float moveVertical = Input.GetAxis("Vertical");        // Up Down arrow keys
        // if (moveHorizontal == 0 && moveVertical == 0) return;   // notta

        Transform leader = null;
        if (followLeader 
//            && (moveHorizontal == 0 && moveVertical == 0)
        )
        {
            if(targetObject == null) targetObject = GameObject.Find(ctunity.Player + "/Base/Target");
  //          GameObject tgo = GameObject.Find(ctunity.Player + "/Base/Target");
            if (targetObject != null && targetObject != gameObject)
            {
                leader = targetObject.transform;
                //   Debug.Log(CTunity.fullName(gameObject) + ", myleader: " + CTunity.fullName(targetObject));
            }
        }

        rb.angularVelocity = Vector3.zero;      // no spinning
		float liftforce = liftForce;
		if (/* moveHorizontal == 0 && */ moveVertical == 0) liftforce = 0F;

		// move in XY plane
		Vector3 movement;
        if (isVehicle)                // push in player-forward direction
        {
            if (leader != null) transform.LookAt(leader.transform);

            movement = new Vector3(0f, 0f, moveVertical);
            movement = transform.rotation * movement;               // align force to direction of player object

            float fwdVelocity = Vector3.Dot(rb.velocity, transform.forward);
            if (moveVertical <= 0) liftforce = liftForce * fwdVelocity / maxSpeed;  // lift ~ speed
            if (fwdVelocity <= 0F)
            {
                liftforce = 0F;
                rb.velocity = new Vector3(0F, rb.velocity.y, 0F);   // falling...
            }

            rb.AddTorque(transform.up * TorqueFactor * moveHorizontal);         // twist and turn...

            Quaternion targetRotation = Quaternion.Euler(0F, transform.eulerAngles.y, 0F);                  // level off:
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5F);
        }
        else                            // push in camera-look direction
        {
            movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            if (leader != null)
            {
                transform.LookAt(leader);
                movement = transform.forward;
//                Debug.Log("lookat leader: "+leader.transform.position);
            }
            else
            {
                movement = mainCamera.transform.rotation * movement;   // align force to direction of camera 
            }

            if (moveVertical >= 0)      // no spin in reverse
            {
                // rotate in direction of motion (go flat if slow/stopped or unpowered)
                Quaternion targetRotation;
                if (rb.velocity.magnitude > 0.1F /* && liftforce>0 */)
                    targetRotation = baseRotation * Quaternion.LookRotation(rb.velocity);
                else targetRotation = Quaternion.Euler(0F, transform.rotation.eulerAngles.y, 0F);

                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 2F);
            }
        }
        
		movement.y = 0F;
		movement.Normalize ();
		movement = movement * ForceFactor;
		movement.y = 10f * liftforce;       // nominal scaling?
		rb.AddForce (movement);             // scaled, normalized

		// limit max speed
		if (rb.velocity.magnitude > maxSpeed)
        {
            Vector3 newVelocity = rb.velocity.normalized;
            newVelocity *= maxSpeed;
            rb.velocity = newVelocity;
        }
	}
}
