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
    private Camera mainCamera = null;

    public int HP = 10;        // max hits before killed
    public int ATK = 1;         // amount of damage
    public int AC = 1;          // damage mitigation
    public Boolean showHP = true;
    public Boolean scaleSize = false;
    public float damageInterval = 1f;            // seconds contact per damage ticks
    public Boolean debug = false;
    public Boolean killParent = false;          // kill parent (self and siblings) upon death

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
            initialHP = HP;         // init to static global value
            int hp = ctclient.getCustom("HP", 0);
//            Debug.Log(name+",startup HP: "+HP+", hp: " + hp + ", custom: " + ctclient.custom);
            if (hp != 0)
            {
                HP = hp;                   // over-ride baked-in if custom present
                initialHP = HP;
            }
            if (HP == 0) showHP = false;    // nope
        }

        initialScale = transform.localScale;
        stopWatch = 0;
        //        Debug.Log(name + ", showHP: " + showHP);
        mainCamera = Camera.main;                   // up front for efficiency
    }

    //----------------------------------------------------------------------------------------------------------------
    // show HP bar above object 
    void OnGUI()
    {
        if (showHP && ctunity.trackEnabled)
        {
            if (ctclient.custom == null || ctclient.custom.Length == 0) return;     // notta
            Vector2 targetPos = mainCamera.WorldToScreenPoint(transform.position);

            // scale font with screensize, and GUI.box to font
            GUIContent content = new GUIContent(ctclient.custom);
            GUIStyle style = GUI.skin.box;
            // style.fontSize = 12;    // scale 
            // style.fontSize = (Screen.height / 100);
            int fs = Screen.height / 100;
            style.fontSize = (fs < 12) ? 12 : (fs > 20) ? 20 : fs;
            style.alignment = TextAnchor.MiddleCenter;
            Vector2 size = style.CalcSize(content);     // Compute how large the popup window needs to be
            GUI.Box(new Rect(targetPos.x - size.x/2f, Screen.height - targetPos.y - 2*size.y, size.x, size.y), ctclient.custom);
            // Debug.Log("Screen.height: " + Screen.height + ", fontSize: " + style.fontSize + ", fs: " + fs);

            // int w = 32;
            // w = ctclient.custom.Length * 7 + 14;
            // int h = 24;
            // GUI.Box(new Rect(targetPos.x - w / 2, Screen.height - targetPos.y - 2 * h, w, h), ctclient.custom);
        }
    }

    private void Update()
    {
        if (showHP) HP = ctclient.getCustom("HP", HP);
 //       Debug.Log(name + ",Update HP: " + HP + ", custom: " + ctclient.custom);

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
        if(debug) Debug.Log(CTunity.fullName(gameObject) + ", Trigger with: " + collider.name);

        if (!showHP) return;
        ScoreBoard tkso = collider.gameObject.GetComponent<ScoreBoard>();
        //        if(tkso != null) Debug.Log("new kso.ATK: " + tkso.ATK + ", thisCollider: " + thisCollider);

        if (tkso != null && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(collider.gameObject))
        {
            kso = tkso;
            thisCollider = collider;
            targetScale = transform.localScale;
            stopWatch = damageInterval;     // quick hit to start
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if(debug) Debug.Log(CTunity.fullName(gameObject) + ", END Trigger with: " + collider.name);
        if (thisCollider == collider) thisCollider = null;
    }

    //----------------------------------------------------------------------------------------------------------------
    // detect collisions

    void OnCollisionEnter(Collision collision)
    {
        if(debug) Debug.Log(CTunity.fullName(gameObject) + ", Collision with: " + CTunity.fullName(collision.collider.gameObject));
        if (!showHP) return;

        ScoreBoard tkso = collision.collider.gameObject.GetComponent<ScoreBoard>();
        if (debug)
        {
 //           Debug.Log(gameObject.name + ", activePlayer: " + ctunity.activePlayer(gameObject) + ", activeWrite: " + CTunity.activeWrite + ", newSession: " + ctunity.newSession + ", localP: " + ctunity.localPlayer(gameObject));
 //           Debug.Log("ctplayer: " + ctunity.ctplayer + ", paused: " + ctunity.gamePaused + ", replayActive: " + ctunity.replayActive + ", Player: " + ctunity.Player);
        }
        if (tkso != null && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(collision.gameObject))
        {
            kso = tkso;
            thisCollider = collision.collider;
            targetScale = transform.localScale;
            stopWatch = damageInterval;     // quick hit to start
            if (debug) Debug.Log("HIT! thisCollider: "+thisCollider);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if(debug) Debug.Log(CTunity.fullName(gameObject) + ", END Collision with: " + collision.collider.name);
        if (thisCollider == collision.collider) thisCollider = null;
    }

    //----------------------------------------------------------------------------------------------------------------
    void doCollision(Collider other)
    {
        if(debug) Debug.Log(name+": doCollision, showHP: " + showHP + ", kso: " + kso);
        if (!showHP || kso==null) return;                                        // no game
//        if (!showHP || kso == null || !ctunity.activePlayer(gameObject)) return;                                        // no game

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
        if (HP <= 0)
        {
            if(killParent)  // can't destroyImmediate inside collision callback
                    ctunity.clearObject(gameObject.transform.parent.gameObject, false);  
            else    ctunity.clearObject(gameObject, false);  
        }
//        Debug.Log(myName + ", collide with: " + otherName+", damage: "+damage+", kso.ATK: "+kso.ATK+", AC: "+AC);

        if (scaleSize) targetScale = initialScale * (0.1f + 0.9f * ((float)(HP) / (float)initialHP));
    }
}
