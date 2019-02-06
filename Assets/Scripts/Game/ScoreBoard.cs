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

// ScoreBoard:  maintain and display score (stats) for collision/interactions

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScoreBoard : MonoBehaviour
{
    private CTunity ctunity;
    private CTclient ctclient;
    //    private CTledger ctledger;

    public int HP = 10;        // max hits before killed
    public int ATK = 1;         // amount of damage
    public int AC = 1;          // damage mitigation
    public Boolean showHP = true;
    public Boolean scaleSize = false;
    public float damageInterval = 1f;            // seconds contact per damage ticks

    private Vector3 initialScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;

    private int initialHP = 1;
    private float stopWatch = 0f;
    private Collider thisCollider = null;
    private ScoreBoard kso = null;

    static internal String custom = null;          // "global" params (shared between objects)

    // Use this for initialization
    void Start()
    {
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        ctclient = GetComponent<CTclient>();

        //       if (showHP) int.TryParse(ctclient.getCustom("HP", HP + ""), out HP);
        if (showHP)
        {
            HP = ctclient.getCustom("HP", 0);
            //            Debug.Log(CTunity.fullName(gameObject)+": startup Custom: " + ctclient.custom);
            if (HP == 0) showHP = false;        // enabled by startup JSON having HP custom field
            else initialHP = HP;
        }

        initialScale = transform.localScale;
        stopWatch = 0;
//        Debug.Log(name + ", showHP: " + showHP);
    }

    //----------------------------------------------------------------------------------------------------------------
    // show HP bar above object 
    void OnGUI()
    {
        if (showHP && ctunity.trackEnabled)
        {
            Vector2 targetPos = Camera.main.WorldToScreenPoint(transform.position);
            int w = 32;
            w = ctclient.custom.Length * 7 + 14;
            int h = 24;
            //         GUI.Box(new Rect(targetPos.x - w / 2, Screen.height - targetPos.y - 2 * h, w, h), HP + "");
            GUI.Box(new Rect(targetPos.x - w / 2, Screen.height - targetPos.y - 2 * h, w, h), ctclient.custom);
            //         GUI.Box(new Rect(targetPos.x - w / 2, Screen.height - targetPos.y - 2 * h, w, h), new GUIContent(HP + "", ctclient.custom));
        }
    }

    private void Update()
    {
        //        if(showHP) int.TryParse(ctclient.getCustom("HP", HP + ""), out HP);
        if (showHP) HP = ctclient.getCustom("HP", HP);

        if (scaleSize && thisCollider != null)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 2f);
        }

        if (showHP && thisCollider != null)
        {
            stopWatch += Time.deltaTime;
            if (stopWatch >= damageInterval)
            {
                doCollision(thisCollider);
                stopWatch = 0;
            }
        }
    }

    //----------------------------------------------------------------------------------------------------------------
    // detect triggers

    void OnTriggerEnter(Collider collider)
    {
//        Debug.Log(CTunity.fullName(gameObject) + ", Trigger with: " + collider.name);
        if (collider == thisCollider) return;  // on going
        doCollision(collider);
    }

    void OnTriggerExit(Collider collider)
    {
 //      Debug.Log(CTunity.fullName(gameObject) + ", END Trigger with: " + collider.name);
        if (thisCollider == collider) thisCollider = null;
    }

    private void OnTriggerStay(Collider collider)
    {
        //       Debug.Log(CTunity.fullName(gameObject) + ", STAY Trigger with: " + collider.name);
        if (collider != thisCollider) return;

        stopWatch += Time.deltaTime;
        if (stopWatch >= damageInterval)
        {
            doCollision(collider);
            stopWatch = 0;
        }
    }

    //----------------------------------------------------------------------------------------------------------------
    // detect collisions

    void OnCollisionEnter(Collision collision)
    {
//        Debug.Log(CTunity.fullName(gameObject) + ", Collision with: " + CTunity.fullName(collision.collider.gameObject));
        if (!showHP) return;

        ScoreBoard tkso = collision.collider.gameObject.GetComponent<ScoreBoard>();
//        if(tkso != null) Debug.Log("new kso.ATK: " + tkso.ATK + ", thisCollider: " + thisCollider);

        if (tkso != null && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(collision.gameObject))
        {
            kso = tkso;
            thisCollider = collision.collider;
            targetScale = transform.localScale;
            stopWatch = damageInterval;     // quick hit to start
        }
    }

    void OnCollisionExit(Collision collision)
    {
  //      Debug.Log(CTunity.fullName(gameObject) + ", END Collision with: " + collision.collider.name);
        if (thisCollider == collision.collider) thisCollider = null;
    }

    //----------------------------------------------------------------------------------------------------------------
    void doCollision(Collider other)
    {
        if (!showHP || kso==null) return;                                        // no game

        String myName = CTunity.fullName(gameObject);
        String otherName = CTunity.fullName(other.gameObject);

        if (ctunity == null) ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        if (other.gameObject == null || ctunity == null)
        {
            Debug.Log(name + ": OnTrigger null other object: " + other.name);
            return;
        }

        // compare hit levels to see who wins
        HP = ctclient.getCustom("HP", HP);
        int damage = (int)Math.Ceiling((float)kso.ATK / (float)AC);
        HP -= damage;
        if (HP < 0) HP = 0;
        ctclient.putCustom("HP", HP);
        if (HP <= 0) ctunity.clearObject(gameObject, false);  // can't destroyImmediate inside collision callback

//        Debug.Log(myName + ", collide with: " + otherName+", damage: "+damage+", kso.ATK: "+kso.ATK+", AC: "+AC);

        if (scaleSize) targetScale = initialScale * (0.1f + 0.9f * ((float)(HP) / (float)initialHP));
    }

    /*
    //----------------------------------------------------------------------------------------------------------------
    public void OnMouseDown()
    {
        String myname = CTunity.fullName(gameObject);
        Debug.Log("target: " + myname + ", oldcustom: "+custom);
        if (!EventSystem.current.IsPointerOverGameObject())     // avoid "click through" from UI elements
        {
            custom = myname;
        }
    }

    public void OnMouseUp()
    {
        String myname = CTunity.fullName(gameObject);
        Debug.Log("UNtarget: " + myname + ", oldcustom: " + custom);
        if (!EventSystem.current.IsPointerOverGameObject())     // avoid "click through" from UI elements
        {
            custom = null;
        }
    }
    */
}
