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

//-------------------------------------------------------------------------------------------------------
/// <summary>
/// Core logic for CT/Unity interface
/// </summary>
/// 

public class CTunity : MonoBehaviour
{
	#region Globals

    private Dictionary<String, GameObject> CTlist = new Dictionary<String, GameObject>();

    public Boolean remoteReplay = false;            // ghost (Player2) replay mode?
	public double latestTime = 0F;
    
	public Boolean showMenu = true;
    public string Server = "http://localhost:8000";
    public string Player = "Red";
    public Boolean Ghost = false;
    public string Model = "Ball";
	public string Session = "";

//    public string ReplayMode = "Action";
    public int MaxPts = 100;
    public float TrackDur = 10f;
    public int BlockPts = 5;                        // 5/50 = 0.1s
    public Boolean VidCapMode = false;
//    public String worldState = "";                  // storage bin...
    public Double clocksync = 0f;                   // add this to local clock to sync to CTweb
    public double lastSubmitTime = 0;
    public Boolean observerFlag = false;

    public CTlib.CThttp ctplayer = null;            // storage
    public CTlib.CThttp ctvideo = null;

	public double replayTime = 0;
	private Boolean replayActive = false;
	private String replayText = "Live";
	public Text debugText;

	public List<String> PlayerList = new List<String>();

	public Boolean trackEnabled = true;             // enable player-tracks
	#endregion

	//-------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start()
    {
        StartCoroutine("getWorldState");
    }

	//-------------------------------------------------------------------------------------------------------
    // build list of players/CT objects

	public void CTregister(GameObject go) {
//		UnityEngine.Debug.Log("CTregister: " + go.name);

		if (!CTlist.ContainsKey(go.name))
		{
			CTlist.Add(go.name, go);
		}
	}

    //-------------------------------------------------------------------------------------------------------
    // get States from objects, putData to CT as CTstates.txt (pb or rt)

    int PtCounter = 0;
    void Update()
    {
        if (ctplayer == null || observerFlag) return;
        if (showMenu)
        {
            PtCounter = 0;
            return;
        }
        
		// header line:
		string CTstateString = "#" + replayText + ":" + ServerTime().ToString() + ":" + Player + "\n";

		string delim = ";";
        foreach (GameObject ct in CTlist.Values)
        {
			if (ct==null) continue;  
			CTclient ctp = ct.GetComponent<CTclient>();
			if (ctp == null) continue;
//			UnityEngine.Debug.Log("CTput: " + ct.name+", active: "+ct.activeSelf);

			String prefab = ctp.prefab;
            if (prefab.Equals("Ghost")) continue;                                   // no save ghosts												
			if (!replayActive && !ct.name.StartsWith(Player)) continue;  // only save locally owned objects

            CTstateString += ct.name;
//            CTstateString += (delim + ct.tag);
			CTstateString += (delim + prefab);
            CTstateString += (delim + (ct.activeSelf ? "1" : "0"));
            CTstateString += (delim + ct.transform.localPosition.ToString("F4"));
            CTstateString += (delim + ct.transform.localRotation.eulerAngles.ToString("F4"));
			if (ctp.custom != null && ctp.custom.Length>0) CTstateString += (delim + ctp.custom);
            CTstateString += "\n";
        }

        ctplayer.setTime(ServerTime());
        ctplayer.putData("CTstates.txt", CTstateString);

        if ((++PtCounter % BlockPts) == 0)     // BlockPts per CT block
            ctplayer.flush();
    }

    //-------------------------------------------------------------------------------------------------------
    // parseCTworld:  utilities to parse CTworld/CTstates.txt into CTworld/CTobject List structures

    // CTworld class structures:

    public class CTworld
    {
        public string name { get; set; }
        public double time { get; set; }
        public string mode { get; set; }
//        public List<CTobject> objects;
		public Dictionary<String, CTobject> objects;
    }

    public class CTobject
    {
        public string id { get; set; }
		public string prefab { get; set; }
        public Boolean state { get; set; }
        public Vector3 pos { get; set; }
        public Quaternion rot { get; set; }
        public string custom { get; set; }
    }

	String masterWorldName = null;

	//-------------------------------------------------------------------------------------------------------
	CTworld parseCTworld(string wtext, double timeout)
	{
//		double masterTime = replayTime;
		double masterTime = ServerTime();

		double updateTime = 0F;

		CTworld CTW = new CTworld();

		String[] worlds = wtext.Split('#');     // parse game-by-game

		//  first pass:  screen for replay/master world
        if (replayActive)             // Replay mode:  we are the master
		{
			masterWorldName = Player;
			masterTime = latestTime = replayTime;
			remoteReplay = false;
		}
		else
		{
			foreach (String world in worlds)            // scan worlds for possible remote Replay
			{
				if (world.Length < 2) continue;                     // skip empty lines
				String[] lines = world.Split('\n');
				String[] header = lines[0].Split(':');
				if (header.Length != 3) continue;

				String mode = header[0];
				String name = header[2];

				// check for end of Remote mode
				if (remoteReplay)
				{
					if (name.Equals(masterWorldName) && !mode.Equals("Replay"))
					{
						remoteReplay = false;
						break;
					}
				}

				// check for someone asserting Replay
				else if (mode.Equals("Replay"))                    // world mode
				{
					double time = Double.Parse(header[1]);
					double delta = Math.Abs(time - ServerTime());   // replay leader needs current TOD time        
					if ((timeout > 0) && (delta > timeout))
					{
						continue;
					}
               
					masterWorldName = name;                   // world name
					masterTime = time;          // world time
					remoteReplay = true;
					break;
				}
			}
		}
        
		// second pass:  build CTWorld structure:
        //                  - from replay master
		//                  - or from consolidated objects as owned (prefix) by each world
        
//		CTW.objects = new List<CTobject>();
		CTW.objects = new Dictionary<String, CTobject>();
		List<String> tsourceList = new List<String>();
		tsourceList.Add(Player);    // always include self
        
        // second pass, screen masterTime, consolidate masterWorld
		foreach (String world in worlds)
		{
//			UnityEngine.Debug.Log("world: " + world);
			if (world.Length < 2) continue;    // TODO: reduce redundancy with first-pass (save data structure, function?)
            String[] lines = world.Split('\n');
            String[] header = lines[0].Split(':');
			if (header.Length != 3) continue;
            
			String thisName = header[2];

			if (remoteReplay && !thisName.Equals(masterWorldName)) continue;        // consolidate to replayWorld

			CTW.mode = header[0];
            CTW.time = Double.Parse(header[1]);
			if (CTW.time > updateTime) updateTime = CTW.time;      // keep track of most-recent CTW time

			double delta = Math.Abs(CTW.time - masterTime);   // masterTime NG on Remote... ???         
			if ((timeout > 0) && (delta > timeout))         // reject stale times
			{
				if (!thisName.Equals(Player) || observerFlag) clearWorld(thisName);    // inefficient?
//				clearWorld(thisName);
				continue;       
			}
            
			if(!tsourceList.Contains(thisName)) tsourceList.Add(thisName);                   // build list of active worlds
			CTW.name = thisName;

			foreach (String line in lines)
			{
				if (line.Length < 2 || line.StartsWith("<")) continue;  // skip empty & html lines
				String[] parts = line.Split(';');
				if (parts.Length < 3) continue;

				CTobject ctobject = new CTobject();

				ctobject.id = parts[0];

			//	UnityEngine.Debug.Log("line: " + line+", thisName: "+thisName+", objectID: "+ctobject.id+", remoteReplay: "+remoteReplay);
				if (remoteReplay                                // remotePlay: this is master world get all objects
				    || ctobject.id.StartsWith(thisName))         // Live mode:  accumulate objects owned by each world      
				{
					ctobject.prefab = parts[1];
			 		ctobject.state = !parts[2].Equals("0");

                    // parse ctobject.pos
					string pstate = parts[3].Substring(1, parts[3].Length - 2);     // drop parens
					string[] pvec = pstate.Split(',');
					ctobject.pos = new Vector3(float.Parse(pvec[0]), float.Parse(pvec[1]), float.Parse(pvec[2]));

					// parse ctobject.rot
					pstate = parts[4].Substring(1, parts[4].Length - 2);     // drop parens
                    pvec = pstate.Split(',');
					ctobject.rot = Quaternion.Euler(float.Parse(pvec[0]), float.Parse(pvec[1]), float.Parse(pvec[2]));

					String custom = null;
					if (parts.Length > 5)
					{
						custom = parts[5];
//						Debug.Log("parse custom: " + custom);
						ctobject.custom = custom;
					}

					try
					{
						CTW.objects.Add(ctobject.id, ctobject);
					}
					catch(Exception e)
                    {
						UnityEngine.Debug.Log("CTW.object.Add error: "+e);
                    }

					// instantiate new players and objects
					if (!CTlist.ContainsKey(ctobject.id) && (observerFlag || !thisName.Equals(Player)))
					{
						newGameObject(ctobject.id, ctobject.prefab, ctobject.pos, ctobject.rot, false, ctobject.state);
					}
				}
			}
		}

		// scan for missing objects
		clearMissing(CTW);

		latestTime = updateTime;                    // for replay reference
		PlayerList = tsourceList;                   // update list of active sources

//		printCTworld(CTW);
		return CTW;                                 // consolidated CTworld
	}
    
	//-------------------------------------------------------------------------------------------------------
	void printCTworld(CTworld ctworld) {
		String p = "CTworld:\n";
        p += ("name: " + ctworld.name + "\n");      // redundant Key, name?
        p += ("mode: " + ctworld.mode + "\n");
        p += ("time: " + ctworld.time + "\n");
        p += ("Objects:" + "\n");
		if (ctworld.objects == null) p += "<null>";
		else
		{
			foreach (CTobject ctobject in ctworld.objects.Values)
			{
				p += ("\tkey: " + ctobject.id + "\n");          // redundant Key, id?
				p += ("\tprefab: " + ctobject.prefab + "\n");         // object class
				p += ("\tstate: " + ctobject.state + "\n");
				p += ("\tpos: " + ctobject.pos + "\n");
			}
		}
        UnityEngine.Debug.Log(p);
	}

    //-------------------------------------------------------------------------------------------------------
    // clone new player or player-owned object...

	public void newPlayer(String playerName, String model, Boolean ghost)
    {
		newGameObject(playerName, model, new Vector3(0F, 5F, 0F), transform.rotation, ghost, true);      
    }

	public GameObject newGameObject(String pName, String prefab, Vector3 position, Quaternion rotation, Boolean ghost, Boolean isactive)
	{
//		UnityEngine.Debug.Log("newGameObject: " + pName);
		String playerName = pName + (ghost ? "g" : "");
		if (CTlist.ContainsKey(playerName))
		{
			// CTlist[playerName].SetActive(true);     // let setState activate 
			return CTlist[playerName];            // already there
		}

		GameObject tgo = GameObject.Find(playerName);
		if (tgo != null)
		{
			UnityEngine.Debug.Log("Can't create duplicate: " + pName);
			return tgo;
		}

		GameObject go = ((GameObject)getPrefab(prefab));
		if (go == null)
		{
			UnityEngine.Debug.Log("NULL prefab: " + prefab);
			return null;
		}
		go.SetActive(isactive);
		//		go.SetActive(true);     // Bleh: this flashes inactive players on long enough for them to register...           

      
		//		newp.parent = GameObject.Find("Players/" + Player).transform;
		String parent = "Players";

		String[] pathparts = playerName.Split('/');
		for (int i = 0; i < pathparts.Length - 1; i++) parent += ("/" + pathparts[i]);

		Transform tparent = GameObject.Find(parent).transform;
		Transform pf = go.transform;
        Transform newp = Instantiate(pf, position, rotation * pf.rotation);    // parent

		//		newp.parent = GameObject.Find(parent).transform;
		newp.SetParent(tparent, pathparts.Length <= 1);     // 2nd arg T/F: child-local vs global position

		newp.name = playerName;
//		newp.gameObject.tag = prefab;            // keep track of object prefab
        if(newp.GetComponent<CTclient>() != null)
		    newp.GetComponent<CTclient>().prefab = prefab;

		// make sure in CTlist (inactive objects won't call CTregister...)
		if (!CTlist.ContainsKey(playerName))
		{
			CTlist.Add(newp.name, newp.gameObject);
		}
        
        // from here to end of method split into 2 new methods...
		if (ghost)
		{
			Physics.IgnoreCollision(                    // no self-bump
				 GameObject.Find(pName).GetComponent<Collider>(),
				 GameObject.Find(playerName).GetComponent<Collider>(),
				 true
			);
		}

        // set new object trim colors to match player
		Color color = Text2Color(playerName, ghost ? 0.4F : 1.0F);
		Renderer renderer = newp.gameObject.GetComponent<Renderer>();
		if (renderer != null) renderer.material.color = color;

		// apply color to any model component labelled "Trim":
		Component[] renderers = newp.GetComponentsInChildren(typeof(Renderer));
		foreach (Renderer childRenderer in renderers)
		{
			if(childRenderer.material.name.StartsWith("Trim"))    // clugy
			    childRenderer.material.color = color;
		}

		return newp.gameObject;
	}

	//-------------------------------------------------------------------------------------------------------
	public void clearPlayer(String playerName) {
		if (!CTlist.ContainsKey(playerName)) return;            // not already there
		GameObject go = GameObject.Find(playerName);
		if (go != null)
		{
			go.SetActive(false);
			Destroy(go);
		}
		CTlist.Remove(playerName);
//		UnityEngine.Debug.Log("clearPlayer: " + playerName);
	}

	//-------------------------------------------------------------------------------------------------------
    public void clearWorld(String worldName)
    {
		List<GameObject> gos = new List<GameObject>(CTlist.Values);     // make copy; avoid sync error

		foreach(GameObject go in gos) {
			if (go.name.StartsWith(worldName))
			{
				//				UnityEngine.Debug.Log("clearWorld go: " + go.name);
				go.SetActive(false);
				Destroy(go);                 // keep object; inactivate it only?
				CTlist.Remove(go.name);
			}
		}
    }
    
	//-------------------------------------------------------------------------------------------------------
    // disable objects that are in CTlist but go missing from CTworld list
	// to delete (vs disable) objects during live play; need to skip delay instantiate-until-ctworld-appearance

	public void clearMissing(CTworld ctworld)
    {
		if (!isReplayMode()) return;          // check missing only in replay mode

		foreach (GameObject go in CTlist.Values)      // cycle through objects in world
        {
//			if (go.name.Equals(Player)) continue;           // leave local world Player alone

			if (!ctworld.objects.ContainsKey(go.name))
			{
//				Debug.Log("Missing object: " + go.name);
				go.SetActive(false);
			}
        }
    }

    //-------------------------------------------------------------------------------------------------------
    // getWorldState: GET PlayerX/CTstates.txt from CT, update world objects (all modes)

    static double oldTime = 0;
    public IEnumerator getWorldState()
    {
		while (true)
		{
			yield return new WaitForSeconds(BlockPts / 50.0f);     // sleep for block duration

			if (showMenu) continue;                                                // no-op unless run-mode
			if (replayActive && (replayTime == oldTime)) continue;      // no dupes (e.g. paused)

			oldTime = replayTime;
            
			// form HTTP GET URL
			String urlparams = "?f=d";
			if (replayActive) urlparams = "?f=d&t=" + replayTime;     // replay at masterTime
			string url1 = Server + "/CT/"+Session+"/GamePlay/*/CTstates.txt" + urlparams;
//			UnityEngine.Debug.Log("url1: " + url1);
			WWW www1 = new WWW(url1);
			yield return www1;          // wait for results to HTTP GET

			// proceed with parsing CTstates.txt
			if (!string.IsNullOrEmpty(www1.error) || www1.text.Length < 10)
			{
				CTdebug(www1.error + " : " + url1);
				continue;
			}
			CTdebug("");   // clear error

			// parse to class structure...
			CTworld CTW = parseCTworld(www1.text, 5f);    // skip stale player data	
			if (CTW == null || CTW.objects == null) continue;          // notta      

			foreach (CTobject ctobject in CTW.objects.Values)      // cycle through objects in world
			{
				if (Ghost && ctobject.id.Equals(Player))          // extra "ghost" player
					setState(Player+"g", ctobject);

				setState(ctobject.id, ctobject);
			}

		}                   // end while(true)      
    }


	//-------------------------------------------------------------------------------------------------------
	// CTsetstate wrapper

	private void setState(String objectID, CTobject ctobject) {
		GameObject ct;
//		UnityEngine.Debug.Log("<<<setstate object: " + objectID);

        if (!CTlist.TryGetValue(objectID, out ct)) return;
//		UnityEngine.Debug.Log(">>>setstate object: " + ctobject.id);

		CTclient ctp = ct.GetComponent<CTclient>();
        if (ctp != null)
            ctp.setState(ctobject.state, ctobject.pos, ctobject.rot, isReplayMode(), ctobject.custom);
	}

	public Boolean isReplayMode() {
		return replayActive || remoteReplay;
	}

	//-------------------------------------------------------------------------------------------------------
    // set timeControl time/state
    
	public Boolean setTime(double ireplayTime, String ireplayText) {
		replayTime = ireplayTime;
//		replayActive = ireplayActive;
		replayText = ireplayText;

		return replayActive;
	}

	public void toggleReplay() {
		replayActive = !replayActive;
	}

	public void setReplay(bool ireplayActive)
    {
        replayActive = ireplayActive;
    }

	//-------------------------------------------------------------------------------------------------------
    // "pull" object state from list
	// INACTIVE:  logic not worked out, inefficient double-loop search

	public void getState(String id, ref Vector3 pos) {
	}

	//-------------------------------------------------------------------------------------------------------
    // get prefab model from Resources/Prefabs folder

    public UnityEngine.Object getPrefab(string objName)
    {
        UnityEngine.Object obj;
		try
		{
			obj = Resources.Load("Prefabs/"+objName);
//			UnityEngine.Debug.Log("load prefab: " + objName+", obj: "+obj);
			return obj;
		}
		catch {
			UnityEngine.Debug.Log("failed to load prefab: " + objName);
			return null;
		}
    }
    
    //----------------------------------------------------------------------------------------------------------------
    // sync clock to remote CTweb
    
    //  public IEnumerator 
    public Boolean doSyncClock()
    {
        string url1 = Server + "/sysclock";
        WWW www1 = new WWW(url1);
        //      yield return www1;          // wait for results to HTTP GET
        while (!www1.isDone)
        {
            //          UnityEngine.Debug.Log ("waiting on www...");
            new WaitForSeconds(0.1f);
        }

        if (!string.IsNullOrEmpty(www1.error))
        {
			CTdebug(www1.error + ": " + Server);
			return false;
        }
        else
        {
            double now = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            clocksync = (Double.Parse(www1.text) / 1000f) - now;
            UnityEngine.Debug.Log("syncClock: " + clocksync + ", CT/sysclock: " + www1.text);
			return true;
        }

    }

    //----------------------------------------------------------------------------------------------------------------
    // synchronized server time
    public double ServerTime()
    {
        double now = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        return (now + clocksync);
    }

	public void CTdebug(String debug) {
		debugText.text = debug;
		UnityEngine.Debug.Log(debug);
	}

	//----------------------------------------------------------------------------------------------------------------
    // convert label string to color

    public Color Text2Color(String color, float alpha)
    {
        Color c = new Color(0.5F, 0.5F, 0.5F, 0.4F);        // default is semi-transparent gray
        if (color.Equals("Player1")) c = new Color(0.9F, 0.2F, 0.2F, alpha);
        if (color.Equals("Player2")) c = new Color(0.2F, 0.2F, 0.9F, alpha);
        if (color.ToLower().StartsWith("red")) c = new Color(0.9F, 0.2F, 0.2F, alpha);
        if (color.ToLower().StartsWith("blue")) c = new Color(0.2F, 0.2F, 0.9F, alpha);
        if (color.ToLower().StartsWith("green")) c = new Color(0.1F, 0.5F, 0.1F, alpha);
        if (color.ToLower().StartsWith("yellow")) c = new Color(0.8F, 0.8F, 0F, alpha);
        //      Debug.Log("text2color, color: " + color + ", alpha: "+alpha+", c: " + c);
        return c;
    }
    
	//----------------------------------------------------------------------------------------------------------------
    // position of player-owned ground platform

    public Vector3 groundPos(string player)
    {
        switch (player)
        {
            case "Red":
                return new Vector3(0F, 0F, 20F);           // Far
            case "Blue":
                return new Vector3(-20F, 0F, 0F);           // Left
            case "Green":
                return new Vector3(20F, 0F, 0F);            // Right
            case "Yellow":
                return new Vector3(0F, 0F, -20F);            // Near
            default:
                return Vector3.zero;
        }
    }
}


