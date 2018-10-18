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
    
	private enum MenuPass
    {
        Connection,
        Session,
        PlayerSelect
    }
	private MenuPass menuPass = MenuPass.Connection;
	private GameObject Server, Session, Player, Avatar, Play, View, Login, User, Password;

    //----------------------------------------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
		replayControl = GameObject.Find("replayControl");
        ctunity = GameObject.Find("CTunity").GetComponent<CTunity>();        // reference CTgroupstate script
		ctunity.observerFlag = true;   // startup in observer mode

        // setup callbacks

        Button[] buttons = gameObject.GetComponentsInChildren<Button>();
        foreach (Button b in buttons)
        {
            switch (b.name)
            {
                case "Submit":
                    b.onClick.AddListener(playButton);
                    break;
                case "View":
                    b.onClick.AddListener(viewButton);
                    break;
				case "Login":
                    b.onClick.AddListener(loginButton);
                    break;
				case "Cancel":
                    b.onClick.AddListener(cancelButton);
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
					d.onValueChanged.AddListener(delegate
					{
						ctunity.Session = d.GetComponent<Dropdown>().options[d.value].text;  // initialize
//						updateServer();
						updateSession();
					});
                    break;
			}
		}

        myCamera = GameObject.Find("Main Camera").GetComponent<maxCamera>();

		Server = GameObject.Find("Server");
		Session = GameObject.Find("Session");
		Player = GameObject.Find("Player1");
		Avatar = GameObject.Find("Model");
		Play = GameObject.Find("Submit");
		View = GameObject.Find("View");
		Login = GameObject.Find("Login");
		User = GameObject.Find("User");
		Password = GameObject.Find("Password");
		modeSelect();
    }

	private void LateUpdate()
	{
		if (ctunity.syncError) menuPass = MenuPass.Connection;
		else
		{
			menuPass = MenuPass.Session;
			ctunity.observerFlag = true;
			ctunity.showMenu = false;
		}
		modeSelect();
	}

	//----------------------------------------------------------------------------------------------------------------
	private void OnEnable()
	{
		if (ctunity != null)  // on startup, async ctunity my not yet be defined
		{
			//			ctunity.showMenu = true;                
			ctunity.Player = "Observer";            // be in live observer mode while in menu
			ctunity.observerFlag = true;
		}
	}

	private void OnDisable()
	{
//		ctunity.showMenu = false;
	}
    
	//----------------------------------------------------------------------------------------------------------------
    // glean server/session status from menu fields
    
	void updateServer()
    {
//		UnityEngine.Debug.Log("updateServer!");

		//		ctunity.clearWorld(ctunity.Player);  // clean slate?
		ctunity.clearWorlds();  // clean slate all worlds

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
        
		StartCoroutine("getSessionList");       // get list of current GamePlay Sessions
		ctunity.doSyncClock();                  // sync client/server clocks
    }

	//----------------------------------------------------------------------------------------------------------------
    // update Session, refresh connection and view

	private void updateSession()
	{
		ctunity.showMenu = true;                                                // turn off CTstates recording while clear world
		ctunity.clearWorlds();                                                  // clean slate all worlds

//		serverConnect();                                                        // reconnect new server/session/player

		//		UnityEngine.Debug.Log("updateSession!");
		ctunity.observerFlag = true;
//		transform.Find("Mode").gameObject.GetComponent<Dropdown>().value = 0;   // reset mode to observer
		ctunity.setReplay(false);
		ctunity.CTdebug(null);
		ctunity.showMenu = false;                                               // start updating world
	}

	//----------------------------------------------------------------------------------------------------------------
    // Connect to CTweb server
	// can't connect to CTweb until Play/Observe is committed (need Session to build source name)

	public void serverConnect() 
	{
//		UnityEngine.Debug.Log("serverConnect, user: " + ctunity.user + ", pw: " + ctunity.password);

		// setup for video players and observers both
        if (ctunity.ctvideo != null) ctunity.ctvideo.close();
        ctunity.ctvideo = new CTlib.CThttp(ctunity.Session + "/ScreenCap/" + ctunity.Player, 100, true, true, true, ctunity.Server);
//        ctunity.ctvideo.login(ctunity.Player, "CloudTurbine");
		ctunity.ctvideo.login(ctunity.user, ctunity.password);
        ctunity.ctvideo.setAsync(true);
        
		if (!ctunity.observerFlag)
		{
			if (ctunity.ctplayer != null) ctunity.ctplayer.close();
			ctunity.ctplayer = new CTlib.CThttp(ctunity.Session + "/GamePlay/" + ctunity.Player, 100, true, true, true, ctunity.Server);
//			ctunity.ctplayer.login(ctunity.Player, "CloudTurbine");
			ctunity.ctplayer.login(ctunity.user, ctunity.password);
			ctunity.ctplayer.setAsync(true);
		}
	}

	//----------------------------------------------------------------------------------------------------------------
    // Login

	void loginButton() {
		updateServer();
	}

	//----------------------------------------------------------------------------------------------------------------
    // Player select

	void playButton() {
//		if(menuPass == MenuPass.Session) {
//			menuPass = MenuPass.PlayerSelect;
//			modeSelect();
//		}
//		else {
			ctunity.observerFlag = false;
			submitButton();
//		}
	}

	//----------------------------------------------------------------------------------------------------------------
	// Play!

	void submitButton()
	{
		Dropdown[] drops = gameObject.GetComponentsInChildren<Dropdown>();
		foreach (Dropdown d in drops)
		{
			switch (d.name)
			{
				case "Session":
					ctunity.Session = d.GetComponent<Dropdown>().options[d.value].text;
					break;
				case "Player1":
					ctunity.Player = d.GetComponent<Dropdown>().options[d.value].text;
					break;
				case "Model":
					ctunity.Model = d.GetComponent<Dropdown>().options[d.value].text;
					break;
			}
		}

		// connect to CTweb server
		serverConnect();

		GameObject go = ctunity.newPlayer(ctunity.Player, ctunity.Model, false);              // instantiate local player

		// kick new player to launch its playerObjects:
		PlayerObjects po = go.GetComponent<PlayerObjects>();
		if (po != null) po.Startup();

		myCamera.setTarget(GameObject.Find(ctunity.Player).transform);

		gameObject.SetActive(false);
		ctunity.lastSubmitTime = ctunity.ServerTime();
		ctunity.showMenu = false;
		replayControl.SetActive(ctunity.observerFlag);
		ctunity.CTdebug(null);                // clear warnings/debug text
	}

	//----------------------------------------------------------------------------------------------------------------
    void modeSelect()
    {
		switch (menuPass)
		{
			case MenuPass.Connection:
				Server.SetActive(true);
				Login.SetActive(true);
				User.SetActive(true);
				Password.SetActive(true);
				Session.SetActive(false);
				Player.SetActive(false);
				Avatar.SetActive(false);
				Play.SetActive(false);
				View.SetActive(false);
				break;
                
			case MenuPass.Session:
				Server.SetActive(false);
				Login.SetActive(false);
				User.SetActive(false);
                Password.SetActive(false);
				Session.SetActive(true);
				Player.SetActive(true);
				Avatar.SetActive(true);
				Play.SetActive(true);
				View.SetActive(true);
				break;

			case MenuPass.PlayerSelect:                 // not used
				break;
		}

		replayControl.SetActive(ctunity.observerFlag);
    }

	//----------------------------------------------------------------------------------------------------------------
    void cancelButton()
    {
        ctunity.showMenu = false;
        gameObject.SetActive(ctunity.showMenu);
    }

	//----------------------------------------------------------------------------------------------------------------
	void viewButton()
	{
		ctunity.observerFlag = true;
		serverConnect();
		ctunity.Player = "Observer";
		ctunity.lastSubmitTime = ctunity.ServerTime();
		replayControl.SetActive(true);
		ctunity.CTdebug(null);                // clear warnings/debug te
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
//			for (match = regex.Match(www1.text); match.Success; match = match.NextMatch())
            for (match = regex.Match(www1.downloadHandler.text); match.Success; match = match.NextMatch())
            {
                foreach (Group group in match.Groups)
                {
                    //UnityEngine.Debug.Log ("Group: "+group);
                    String gstring = group.ToString();
					String prefix = "\"/CT/";
					String gamePlay = "/GamePlay/";
                    
                    if (gstring.StartsWith(prefix) && gstring.Contains(gamePlay))
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

	// out-of-service code follows...
    /*
    //----------------------------------------------------------------------------------------------------------------
    // get source list
    List<String> sourceList = new List<String>();

    public IEnumerator getSourceList()
    {
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
                        sourceList.Add(thisSource);
                    }
                }
            }

            foreach (String s in sourceList) UnityEngine.Debug.Log("source: " + s);
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
