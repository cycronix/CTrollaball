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

public class KillSwitch : MonoBehaviour
{
    private CTunity ctunity;
    private CTstats ctstats;
    public int maxHits = 0;        // max hits before vulnerable to be killed
    public int hitLevel = 10;

    // Use this for initialization
    void Start()
    {
//        Debug.Log(name + ": KillSwitch!");
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
    }

    //----------------------------------------------------------------------------------------------------------------
    private void setStats()
    {
        if (ctstats != null) return;  // got it

        string stats = ctunity.Player + "/Stats";
        GameObject sgo = GameObject.Find(stats);
        if (sgo != null)
        {
            ctstats = sgo.GetComponent<CTstats>();
        }

        if (ctstats != null)
        {
            ctstats.hits++;
 //           Debug.Log(name + ", ctstats.hits: "+ ctstats.hits);
        }
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
        if(ctunity == null) ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        if (other.gameObject == null || ctunity == null)
        {
            Debug.Log(name + ": OnTrigger null other object: "+other.name);
            return;
        }

        // compare hit levels to see who wins
        int otherHitLevel = 0;
        KillSwitch kso = other.gameObject.GetComponent<KillSwitch>();
        if (kso != null)
        {
            otherHitLevel = kso.hitLevel;

            if ((hitLevel < otherHitLevel) && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(other.gameObject))
            // if ((other.gameObject.tag == "Bullet") && ctunity.activePlayer(gameObject) && !ctunity.localPlayer(other.gameObject))
            {
                setStats();
     //           Debug.Log(name + ": HIT by: " + other.gameObject.name + ", hitLevel: "+ hitLevel+", ohl: "+otherHitLevel);

                if (ctstats==null || ctstats.hits >= maxHits)
                {
                    // Debug.Log(name + ": killed!");
                    ctunity.clearObject(gameObject, false);  // can't destroyImmediate inside collision callback
                }
            }
        }
    }
}
