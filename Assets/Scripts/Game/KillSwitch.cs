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
using UnityEngine;
using UnityEngine.EventSystems;

public class KillSwitch : MonoBehaviour
{
    private CTunity ctunity;

    // Use this for initialization
    void Start()
    {
//        Debug.Log(name + ": KillSwitch!");
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
    }

    //----------------------------------------------------------------------------------------------------------------

    void OnCollisionEnter(Collision other)
    {
        //        Debug.Log(name + ": collision with: " + other.gameObject.name);
        // if other is bullet or this is bullet, bang!
        if ((other.gameObject.tag == "Bullet" /* || gameObject.tag == \"Bullet\" */) && ctunity.activePlayer(gameObject))
        {
            //           Debug.Log(name + ": killed by: " + other.gameObject.name);
            ctunity.clearObject(gameObject);
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
        //        Debug.Log(name + ": trigger with: " + other.gameObject.name);
        // if other is bullet or this is bullet, bang!
        //        if (other.gameObject.tag == "Bullet" /* || gameObject.tag == "Bullet" */)
        if ((other.gameObject.tag == "Bullet" /* || gameObject.tag == \"Bullet\" */) && ctunity.activePlayer(gameObject))

        {
            //            Debug.Log(name + ": killed by: " + other.gameObject.name);
            ctunity.clearObject(gameObject);
        }
    }
}
