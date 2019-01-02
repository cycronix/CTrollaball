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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

// Config Menu for CTrollaball
// Matt Miller, Cycronix, 6-16-2017

//----------------------------------------------------------------------------------------------------------------
public class CTsetup: MonoBehaviour
{
    public int defaultPort = 8000;

    private maxCamera myCamera;
	private CTunity ctunity;
	private GameObject replayControl;
	private Dropdown playerDrop = null;

	private enum MenuPass
    {
        Connection,
        Session,
        PlayerSelect
    }
	private MenuPass menuPass = MenuPass.Connection;
	private GameObject Server, Session, Player, Deploy, Login, User, Password;

    //----------------------------------------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
		replayControl = GameObject.Find("replayControl");
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script

        // define menu objects
		myCamera = GameObject.Find("Main Camera").GetComponent<maxCamera>();

        Server = GameObject.Find("Server");
        Session = GameObject.Find("Session");
        Player = GameObject.Find("Player1");
        //      Avatar = GameObject.Find("Model");
        Deploy = GameObject.Find("Deploy");
        //      Play = GameObject.Find("Submit");
        //      View = GameObject.Find("View");
        Login = GameObject.Find("Login");
        User = GameObject.Find("User");
        Password = GameObject.Find("Password");

        // setup callbacks

        Button[] buttons = gameObject.GetComponentsInChildren<Button>();
        foreach (Button b in buttons)
        {
            switch (b.name)
            {
				case "Login":
                    b.onClick.AddListener(loginButton);
                    break;

                case "Quit":
                    b.onClick.AddListener(quitButton);
                    break;
            }
        }

		Dropdown[] drops = gameObject.GetComponentsInChildren<Dropdown>();
		foreach (Dropdown d in drops)
		{
			switch (d.name)
			{
				case "Session":
                    ctunity.Session = d.GetComponent<Dropdown>().options[d.value].text;  // initialize

                    // add listener to update session settings
					d.onValueChanged.AddListener(delegate
					{
						ctunity.Session = d.GetComponent<Dropdown>().options[d.value].text;  // set selected session
						updateSession();

						if (playerDrop != null)
						{
							ctunity.Player = playerDrop.GetComponent<Dropdown>().options[0].text;
							ctunity.serverConnect();  // reset player path
							playerDrop.value = 0;
						}

					});

                    break;

				case "Deploy":
//					StartCoroutine("getInventoryList");         // init list of "World" prefabs

                    // add listener to deploy new world 
                    d.onValueChanged.AddListener(delegate
					{   if (d.value != 0)
						{
							string svalue = d.GetComponent<Dropdown>().options[d.value].text;

							if (svalue.Equals(ctunity.Save))
							{
								ctunity.SnapShot();
								StartCoroutine("getInventoryList");         // update list of "World" prefabs
							}
							else if (svalue.Equals(ctunity.Clear))  ctunity.clearWorld();
							else                                    ctunity.deployInventory(svalue);

							d.value = 0;        // reset to blank
						}
                    });

                    break;
                   
				case "Player1":
					playerDrop = d;
					ctunity.Player = d.GetComponent<Dropdown>().options[d.value].text;      // init?
					d.onValueChanged.AddListener(delegate
					{
						ctunity.Player = d.GetComponent<Dropdown>().options[d.value].text;
						ctunity.serverConnect();  // reset player path
					});
                    break;
			}
		}

		modeSelect();
    }

	//----------------------------------------------------------------------------------------------------------------
	// keep menu state updated (inefficient!)

	private void LateUpdate()
	{
		if (ctunity.syncError)
		{
			menuPass = MenuPass.Connection;
		}
		else
		{
			menuPass = MenuPass.Session;
			ctunity.gamePaused = false;
		}
		modeSelect();
	}

	//----------------------------------------------------------------------------------------------------------------
	// glean server/session status from menu fields

	void updateServer()
	{
		ctunity.clearWorlds();      // clean slate all worlds

		InputField[] fields = gameObject.GetComponentsInChildren<InputField>();
		foreach (InputField c in fields)
		{
			switch (c.name)
			{
				case "Server":
					ctunity.Server = c.text;
					if (!ctunity.Server.Contains(":")) ctunity.Server += ":" + defaultPort;             // default port :8000
					if (!ctunity.Server.StartsWith("http://")) ctunity.Server = "http://" + ctunity.Server;     // enforce leading http://
					break;
				case "User":
					ctunity.user = c.text;
					break;
				case "Password":
					ctunity.password = c.text;
					break;
			}
		}

        // update player to current dropdown value
		ctunity.Player = playerDrop.GetComponent<Dropdown>().options[playerDrop.value].text;      // init

		StartCoroutine("getSessionList");       // get list of current GamePlay Sessions
		StartCoroutine("getInventoryList");         // init list of "World" prefabs
		ctunity.doSyncClock();                  // sync client/server clocks
	}

	//----------------------------------------------------------------------------------------------------------------
    // update Session, refresh connection and view

	private void updateSession()
	{
		ctunity.gamePaused = true;                // turn off CTstates recording while clear world
		StartCoroutine("getInventoryList");         // get list of "World" prefabs
        
		ctunity.clearWorlds();              // clean slate all worlds

		ctunity.setReplay(false);               // !live
		ctunity.CTdebug(null);                  // clear debug msg
		ctunity.newSession = true;
		ctunity.gamePaused = false;               // start updating world (set at completion of async getWorldState)
	}

	//----------------------------------------------------------------------------------------------------------------
    // Login

	void loginButton() {
		updateServer();
	}
    
	//----------------------------------------------------------------------------------------------------------------
	// modeSelect:  set menu-pass, e.g. login vs setup

    void modeSelect()
    {
		//		Debug.Log("modeSelect!");
		switch (menuPass)
		{
			case MenuPass.Connection:
				Server.SetActive(true);
				Login.SetActive(true);
				User.SetActive(true);
				Password.SetActive(true);
				Session.SetActive(false);
				Player.SetActive(false);
				Deploy.SetActive(false);
//				replayControl.SetActive(false);
				break;
                
			case MenuPass.Session:
				Server.SetActive(false);
				Login.SetActive(false);
				User.SetActive(false);
                Password.SetActive(false);
				Session.SetActive(true);
				Player.SetActive(true);
				Deploy.SetActive(!ctunity.observerMode() && !ctunity.replayActive);
//				replayControl.SetActive(ctunity.observerMode());
				break;

			case MenuPass.PlayerSelect:                 // not used
				break;
		}

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
    }

	//----------------------------------------------------------------------------------------------------------------
    // get session list
    List<String> sessionList = new List<String>();

    public IEnumerator getSessionList()
    {
        while (true)
        {
//            UnityEngine.Debug.Log("getSessionList!");
            yield return new WaitForSeconds(0.1F);

            string url1 = ctunity.Server + "/CT";
			UnityWebRequest www1 = UnityWebRequest.Get(url1);
            www1.SetRequestHeader("AUTHORIZATION", ctunity.CTauthorization());
            yield return www1.SendWebRequest();
         
            if (!string.IsNullOrEmpty(www1.error))
            {
                UnityEngine.Debug.Log("getSessionList www1 error: " + www1.error + ", url: " + url1);
                yield break;
            }

            Regex regex = new Regex("\".*?\"", RegexOptions.IgnoreCase);
            sessionList.Clear();
			sessionList.Add("CTrollaball");         // seed with a default session
            
            Match match;
            for (match = regex.Match(www1.downloadHandler.text); match.Success; match = match.NextMatch())
            {
                foreach (Group group in match.Groups)
                {
                    //UnityEngine.Debug.Log ("Group: "+group);
                    String gstring = group.ToString();
					String prefix = "\"/CT/";
					String gamePlay = "/GamePlay/";
					String world = "/" + ctunity.Inventory + "/";
					if (gstring.StartsWith(prefix) && (gstring.Contains(gamePlay) || gstring.Contains(world)))
                    {
						String thisSession = gstring.Split('/')[2];
                        if(!sessionList.Contains(thisSession)) sessionList.Add(thisSession);
                    }
                }
            }

			//            foreach (String s in sessionList) UnityEngine.Debug.Log("Session: " + s);
			// reset Session dropdown option list:
			Dropdown d = transform.Find("Session").gameObject.GetComponent<Dropdown>();
			d.ClearOptions();
            d.AddOptions(sessionList);
			d.value = 0;
            yield break;
        }
    }
     
	//----------------------------------------------------------------------------------------------------------------
    // get/set world list for dropdown selection

    public IEnumerator getInventoryList()
    {
//		Debug.Log("getInventoryList!");
		List<String> sourceList = new List<String>();

        while (true)
        {
            yield return new WaitForSeconds(0.1F);

            string url1 = ctunity.Server + "/CT";
//            WWW www1 = new WWW(url1);
//            yield return www1;          // wait for results to HTTP GET

            UnityWebRequest www1 = UnityWebRequest.Get(url1);
            www1.SetRequestHeader("AUTHORIZATION", ctunity.CTauthorization());
            //            yield return www1.Send();
            yield return www1.SendWebRequest();

            if (!string.IsNullOrEmpty(www1.error))
            {
                ctunity.CTdebug("getInventoryList www1 error: " + www1.error + ", url: " + url1);
                yield break;
            }

            Regex regex = new Regex("\".*?\"", RegexOptions.IgnoreCase);
            sourceList.Clear();
			sourceList.Add("");             // seed with blank
			sourceList.Add(ctunity.Clear);
			sourceList.Add(ctunity.Save);

            Match match;
            for (match = regex.Match(www1.downloadHandler.text); match.Success; match = match.NextMatch())
            {
                foreach (Group group in match.Groups)
                {
                    String gstring = group.ToString();
                    String prefix = "\"/CT/" + ctunity.Session + "/" + ctunity.Inventory + "/";

                    if (gstring.StartsWith(prefix))
                    {
                        //UnityEngine.Debug.Log("gstring: " + gstring + ", prefix: " + prefix);
                        String thisSource = gstring.Substring(prefix.Length, gstring.Length - prefix.Length - 2);
                        if (!sourceList.Contains(thisSource)) sourceList.Add(thisSource);
                    }
                }
            }

            //            foreach (String s in sourceList) UnityEngine.Debug.Log("source: " + s);

            // reset Player dropdown option list:
            Dropdown d = transform.Find("Deploy").gameObject.GetComponent<Dropdown>();
            d.ClearOptions();
            d.AddOptions(sourceList);
            d.value = 0;
            yield break;
        }
    }

	// out-of-service code follows...   

	/*

    //----------------------------------------------------------------------------------------------------------------
    // Play!

    void submitButton()
    {
        String deploy = "";
        String player = "unknown";
//      String model="unknown";
        Dropdown[] drops = gameObject.GetComponentsInChildren<Dropdown>();
        foreach (Dropdown d in drops)
        {
            switch (d.name)
            {
                case "Session":
                    ctunity.Session = d.GetComponent<Dropdown>().options[d.value].text;
                    break;
                case "Player1":
                    player = d.GetComponent<Dropdown>().options[d.value].text;
                    break;
//              case "Model":
//                  model = d.GetComponent<Dropdown>().options[d.value].text;
//                  break;
                case "Deploy":
                    deploy = d.GetComponent<Dropdown>().options[d.value].text;
                    break;
            }
        }
        
        ctunity.Player = player;
        ctunity.serverConnect();        // connect to CTweb server
        
        gameObject.SetActive(false);                                    // turn menu off (starts play)
        ctunity.lastSubmitTime = ctunity.ServerTime();
        ctunity.gamePaused = false;
        ctunity.CTdebug(null);                // clear warnings/debug text
    }

    //----------------------------------------------------------------------------------------------------------------
    // Player select
    void playButton() {
        submitButton();
    }

    //----------------------------------------------------------------------------------------------------------------
    void cancelButton()
    {
        ctunity.gamePaused = false;
        gameObject.SetActive(ctunity.gamePaused);
    }

    //----------------------------------------------------------------------------------------------------------------
    void viewButton()
    {
//      ctunity.observerFlag = true;
        ctunity.serverConnect();
//      ctunity.Player = "Observer";
        ctunity.lastSubmitTime = ctunity.ServerTime();
//      replayControl.SetActive(true);
        ctunity.CTdebug(null);                // clear warnings/debug te
        ctunity.gamePaused = false;
        gameObject.SetActive(ctunity.gamePaused);
    }

    //----------------------------------------------------------------------------------------------------------------
    // get source list

    public IEnumerator getSourceList()
    {
        List<String> sourceList = new List<String>();

        while (true)
        {
//          UnityEngine.Debug.Log("getSourceList!");
            yield return new WaitForSeconds(0.1F);

            string url1 = ctunity.Server + "/CT";
            WWW www1 = new WWW(url1);
            yield return www1;          // wait for results to HTTP GET

            if (!string.IsNullOrEmpty(www1.error))
            {
                ctunity.CTdebug("getSourceList www1 error: " + www1.error + ", url: " + url1);
                yield break;
            }

            Regex regex = new Regex("\".*?\"", RegexOptions.IgnoreCase);
            sourceList.Clear();
            sourceList.Add("Red"); sourceList.Add("Blue"); sourceList.Add("Green"); sourceList.Add("Yellow");       // for now: init to RBGY

            Match match;
            for (match = regex.Match(www1.text); match.Success; match = match.NextMatch())
            {
                foreach (Group group in match.Groups)
                {
                    //              UnityEngine.Debug.Log ("Group: "+group);
                    String gstring = group.ToString();
                    String prefix = "\"/CT/" + ctunity.Session + "/GamePlay/";

                    if (gstring.StartsWith(prefix))
                    {
                        //UnityEngine.Debug.Log("gstring: " + gstring + ", prefix: " + prefix);
                        String thisSource = gstring.Substring(prefix.Length, gstring.Length - prefix.Length - 2);
                        if(!sourceList.Contains(thisSource)) sourceList.Add(thisSource);
                    }
                }
            }

//            foreach (String s in sourceList) UnityEngine.Debug.Log("source: " + s);

            // reset Player dropdown option list:
            Dropdown d = transform.Find("Player1").gameObject.GetComponent<Dropdown>();
            d.ClearOptions();
            d.AddOptions(sourceList);
            d.value = 0;
            yield break;
        }
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
        new WWW(ctunity.Server + "/addroute?" + ctunity.Player + "=" + localIP + ":8000");
    }
    */
}
