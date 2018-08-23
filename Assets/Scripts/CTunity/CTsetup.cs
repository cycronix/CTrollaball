﻿/*
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
//using System.Threading;
using UnityEngine.SceneManagement;

// Config Menu for CloudTurbine chart displays
// Matt Miller, Cycronix, 6-16-2017

//----------------------------------------------------------------------------------------------------------------
public class CTsetup: MonoBehaviour
{
    public int defaultPort = 8000;

    private maxCamera myCamera;
	private CTunity ctunity;

    //----------------------------------------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script

        Button[] buttons = gameObject.GetComponentsInChildren<Button>();
        foreach (Button b in buttons)
        {
            switch (b.name)
            {
                case "Submit":
                    b.onClick.AddListener(submitButton);
                    break;
                case "Cancel":
                    b.onClick.AddListener(cancelButton);
                    break;
                case "Quit":
                    b.onClick.AddListener(quitButton);
                    break;
            }
        }

        myCamera = GameObject.Find("Main Camera").GetComponent<maxCamera>();
    }
    
	private void OnEnable()
	{
		if (ctunity != null)
		{

			ctunity.showMenu = true;        // on startup, async ctunity my not yet be defined

			//        StartCoroutine("getSourceList");
//			foreach (String s in ctunity.sourceList) UnityEngine.Debug.Log("source: " + s);
		}
	}

	private void OnDisable()
	{
		ctunity.showMenu = false;
	}

	//----------------------------------------------------------------------------------------------------------------
	void updateServer()
    {
		ctunity.clearWorld(ctunity.Player);  // clean slate?

        InputField[] fields = gameObject.GetComponentsInChildren<InputField>();
        foreach (InputField c in fields)
        {
            switch (c.name)
            {
                case "Server":
                    ctunity.Server = c.text;
                    if (!ctunity.Server.Contains(":"))           ctunity.Server += ":" + defaultPort;             // default port :8000
                    if (!ctunity.Server.StartsWith("http://"))   ctunity.Server = "http://" + ctunity.Server;     // enforce leading http://
                    break;

				case "Session":
                    ctunity.Session = c.text;
                    break;
            }
        }

        ctunity.doSyncClock();
        
    }

    //----------------------------------------------------------------------------------------------------------------
    void submitButton()
    {

        updateServer();

        Dropdown[] drops = gameObject.GetComponentsInChildren<Dropdown>();
        foreach (Dropdown d in drops)
        {
            switch (d.name)
            {
                case "Player1":
                    ctunity.Player = d.GetComponent<Dropdown>().options[d.value].text;
                    break;
                case "Model":
					ctunity.Model = d.GetComponent<Dropdown>().options[d.value].text;
                    break;
                case "TrackDur":
					ctunity.TrackDur = Single.Parse(d.GetComponent<Dropdown>().options[d.value].text);
					ctunity.MaxPts = (int)Math.Round(ctunity.TrackDur * 50.0f);         // sec @50 Hz sampling
                    break;
                case "BlockDur":
                    float blockdur = Single.Parse(d.GetComponent<Dropdown>().options[d.value].text);
                    ctunity.BlockPts = (int)Math.Round(blockdur * 0.05f);       // msec @ 50 Hz sampling
                    break;
            }
        }

        // setup for video players and observers both
		if (ctunity.ctvideo != null) ctunity.ctvideo.close();
        ctunity.ctvideo = new CTlib.CThttp(ctunity.Session+"/ScreenCap/" + ctunity.Player, 100, true, true, true, ctunity.Server);
        ctunity.ctvideo.login(ctunity.Player, "CloudTurbine");
        ctunity.ctvideo.setAsync(true);

		myCamera.setTarget(GameObject.Find("Ground").transform);

        if (ctunity.Model.Equals("Observer"))
        {
//            ctunity.newPlayer(ctunity.Player, ctunity.Model, false);     // instantiate observer
            ctunity.observerFlag = true;
        }
        else
        {
			ctunity.observerFlag = false;

			if (ctunity.ctplayer != null) ctunity.ctplayer.close();
            ctunity.ctplayer = new CTlib.CThttp(ctunity.Session+"/GamePlay/" + ctunity.Player, 100, true, true, true, ctunity.Server);
            ctunity.ctplayer.login(ctunity.Player, "CloudTurbine");
            ctunity.ctplayer.setAsync(true);

//            if (ctunity.ctvideo != null) ctunity.ctvideo.close();
//            ctunity.ctvideo = new CTlib.CThttp("ScreenCap/" + ctunity.Player, 100, true, true, true, ctunity.Server);
//            ctunity.ctvideo.login(ctunity.Player, "CloudTurbine");
//            ctunity.ctvideo.setAsync(true);

            ctunity.newPlayer(ctunity.Player, ctunity.Model, false);              // instantiate local player

            if (ctunity.Ghost) ctunity.newPlayer(ctunity.Player, "Ghost", true);
            else ctunity.clearPlayer(ctunity.Player + "g");

			GameObject.Find("pickupDispenser").GetComponent<pickupDispenser>().dispensePickups(); 
        }

        //      CTroute();      // register CTweb routing connection
        
        ctunity.lastSubmitTime = ctunity.ServerTime();
        ctunity.showMenu = false;
        gameObject.SetActive(ctunity.showMenu);
    }

    //----------------------------------------------------------------------------------------------------------------
    void cancelButton()
    {
        ctunity.showMenu = false;
        gameObject.SetActive(ctunity.showMenu);
    }

    //----------------------------------------------------------------------------------------------------------------
    void quitButton()
    {
        Debug.Log("Quitting...");
        Application.Quit();
    }
    
    //----------------------------------------------------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
//        ctunity.showMenu = gameObject.activeSelf;
    }

	/* 
	//----------------------------------------------------------------------------------------------------------------
    // get source list
    List<String> sourceList = new List<String>();

	IEnumerator getSourceList()
	{

		yield return new WaitForSeconds(0.1F);

		string url1 = ctunity.Server + "/CT";
		WWW www1 = new WWW(url1);
		yield return www1;          // wait for results to HTTP GET

		if (!string.IsNullOrEmpty(www1.error))
		{
			UnityEngine.Debug.Log("getSourceList www1 error: " + www1.error + ", url: " + url1);
		}
		else
		{
//			UnityEngine.Debug.Log("getWorldState: " + www1.text);
		}

		Regex regex = new Regex("\".*?\"", RegexOptions.IgnoreCase);
		sourceList.Clear();
		Match match;
		for (match = regex.Match(www1.text); match.Success; match = match.NextMatch())
		{
			foreach (Group group in match.Groups)
			{
//				UnityEngine.Debug.Log ("Group value: {0}: "+group);
				String gstring = group.ToString();
				String prefix = "\"/CT/"+ctunity.Session+"/GamePlay/";

				if (gstring.StartsWith(prefix))
				{
//					UnityEngine.Debug.Log("gstring: " + gstring + ", prefix: " + prefix);
					String thisSource = gstring.Substring(prefix.Length, gstring.Length - prefix.Length - 2);               
					sourceList.Add(thisSource);
				}
			}
		}

		foreach (String s in sourceList) UnityEngine.Debug.Log("source: " + s);
		//			yield break;
        
	}
      
    //----------------------------------------------------------------------------------------------------------------
    // establish route for remote to local CTweb proxy-connection
    private void CTroute()
    {
        // register CTweb routing connection:
        string localIP;
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        new WWW(ctg.Server + "/addroute?" + ctg.Player + "=" + localIP + ":8000");
    }
    */
    
}
