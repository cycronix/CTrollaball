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
    public int ATK = 1;
    public Boolean showHP = true;

    // Use this for initialization
    void Start()
    {
//        Debug.Log(name + ": KillSwitch!");
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        ctclient = GetComponent<CTclient>();

        if (showHP) int.TryParse(ctclient.getCustom("HP", HP + ""), out HP);
    }

    //----------------------------------------------------------------------------------------------------------------
    // show HP bar above object 
    void OnGUI()
    {
        if (showHP && ctunity.trackEnabled)
        {
            Vector2 targetPos = Camera.main.WorldToScreenPoint(transform.position);
            int w = 30;
            int h = 20;
            GUI.Box(new Rect(targetPos.x - w / 2, Screen.height - targetPos.y - 2 * h, w, h), HP + "");
        }
    }

    private void Update()
    {
        if(showHP) int.TryParse(ctclient.getCustom("HP", HP + ""), out HP);
    }

    //----------------------------------------------------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
//        Debug.Log(name + ", Trigger with: " + other.name);
        doCollision(other);
    }

    void OnCollisionEnter(Collision collision)
    {
 //       Debug.Log(name + ", Collision with: " + collision.collider.name);
        doCollision(collision.collider);
    }

    //----------------------------------------------------------------------------------------------------------------
    void doCollision(Collider other)
    {
        String myName = CTunity.fullName(gameObject);
        String otherName = CTunity.fullName(other.gameObject);
//        Debug.Log(myName + ", collide with: " + otherName);

        if(ctunity == null) ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        if (other.gameObject == null || ctunity == null)
        {
            Debug.Log(name + ": OnTrigger null other object: "+other.name);
            return;
        }

        // compare hit levels to see who wins
        int otherATK = 0;
        ScoreBoard kso = other.gameObject.GetComponent<ScoreBoard>();
        if (kso != null)
        {
            otherATK = kso.ATK;
//            Debug.Log(myName+".ATK: " + ATK + ", "+ otherName + ".ATK: " + otherATK);
            if ((ATK < otherATK) && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(other.gameObject))
            // if ((other.gameObject.tag == "Bullet") && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(other.gameObject))
            {
  //              Debug.Log(myName + ": HIT by: " + otherName + ", ATK: "+ ATK+", otherATK: "+otherATK+", myHP: "+HP);
                int.TryParse(ctclient.getCustom("HP", HP+""), out HP);
                HP -= (otherATK - ATK);
                if (HP < 0) HP = 0;
                ctclient.putCustom("HP",""+HP);
                if (HP <= 0)
                {
                    // Debug.Log(name + ": killed!");
                    ctunity.clearObject(gameObject, false);  // can't destroyImmediate inside collision callback
                }
            }
        }
    }
}
