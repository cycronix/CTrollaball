using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTstats : MonoBehaviour
{
//    private CTunity ctunity;
    private CTclient ctclient;
    public int hits=0;

    // Start is called before the first frame update
    void Start()
    {
//        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();
        ctclient = GetComponent<CTclient>();
        hits = 0;
        int.TryParse(ctclient.custom, out hits);
        Debug.Log(CTunity.fullName(gameObject) + ", startup hits: " + hits);
    }

    // Update is called once per frame
    void Update()
    {
        ctclient.custom = hits + "";
    }
}
