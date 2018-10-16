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
using CTworldNS;
using UnityEngine.Networking;

//-------------------------------------------------------------------------------------------------------
/// <summary>
/// Core logic for CT/Unity interface
/// </summary>
/// 

public class CTunity : MonoBehaviour
{
	#region Globals
	public float TrackDur = 10f;
	public float SyncTime = 600.0F;        // nix inactive players beyond this time (sec)
	public int BlockPts = 5;                        // 5/50 = 0.1s
	public Boolean JSON_Format = false;
	public Text debugText;

    // internal variable accessible from other scripts but not Editor Inspector
    internal Dictionary<String, GameObject> CTlist = new Dictionary<String, GameObject>();
	internal List<String> PlayerList = new List<String>();

    internal Boolean remoteReplay = false;            // ghost (Player2) replay mode?
	internal double latestTime = 0F;
	internal Boolean showMenu = true;
    internal string Server = "http://localhost:8000";
    internal string Player = "Observer";
    internal Boolean Ghost = false;
    internal string Model = "Ball";
	internal string Session = "";
    internal int MaxPts = 100;
    internal Double clocksync = 0f;                   // add this to local clock to sync to CTweb
    internal double lastSubmitTime = 0;
    internal Boolean observerFlag = true;
	internal Boolean trackEnabled = true;             // enable player-tracks

    internal CTlib.CThttp ctplayer = null;            // storage
    internal CTlib.CThttp ctvideo = null;

	internal double replayTime = 0;
	internal Boolean replayActive = false;
	internal String replayText = "Live";

	internal string user = "CloudTurbine";
	internal String password = "RBNB";

	//	public Boolean commanderMode = false;       // commander-mode asserts remote-control on replay (not working)
	#endregion

	private readonly object objectLock = new object();
	private Boolean clearWorldsFlag = false;
	private String CTchannel = "CTstates.txt";

	//-------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start()
    {
		CTchannel = JSON_Format ? "CTstates.json" : "CTstates.txt";
        StartCoroutine("getWorldState");
    }

	//-------------------------------------------------------------------------------------------------------
    // build list of players/CT objects
    
	public void CTregister(GameObject go) {
		if (!CTlist.ContainsKey(go.name))
		{
			CTlist.Add(go.name, go);
			UnityEngine.Debug.Log("CTregister: " + go.name);
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

		string CTstateString = CTserdes.serialize(this, JSON_Format?CTserdes.Format.JSON:CTserdes.Format.CSV);

        ctplayer.setTime(ServerTime());
        ctplayer.putData(CTchannel, CTstateString);

        if ((++PtCounter % BlockPts) == 0)     // BlockPts per CT block
            ctplayer.flush();
    }

    //-------------------------------------------------------------------------------------------------------
    // parseCTworld:  utilities to parse CTworld/CTstates.txt into CTworld/CTobject List structures

    String masterWorldName = null;

	//-------------------------------------------------------------------------------------------------------
	// mergeCTworlds:  consolidate CTworlds from each player into local world-state

	CTworld mergeCTworlds(List<CTworld> worlds) {

		if (worlds == null)
		{
			CTdebug("Warning: null CTworlds!");
			return null;                     // null if empty start?
		}

		double masterTime = ServerTime();
        double updateTime = 0F;
        
        //  first pass:  screen for replay/master world
        if (replayActive)
//        if (replayActive || !commanderMode)             // Replay mode:  we are the master
        {
            masterWorldName = Player;
            masterTime = latestTime = replayTime;
            remoteReplay = false;
        }
        else if(!observerFlag)
        {
            remoteReplay = false;  // mjm 9-12-18: default out of remoteReplay if no active Replay mode
            foreach (CTworld world in worlds)            // scan worlds for possible remote Replay
            {
                // check for end of Remote mode
                if (remoteReplay)
                {
                    if (world.player.Equals(masterWorldName) && !world.mode.Equals("Replay"))
                    {
                        remoteReplay = false;
                        break;
                    }
                }

                // check for someone asserting Replay
                else if (world.mode.Equals("Replay"))                    // world mode
                {
                    double delta = Math.Abs(world.time - ServerTime());   // replay leader needs current TOD time        
                    if ((SyncTime > 0) && (delta > SyncTime))
                    {
                        continue;
                    }

                    masterWorldName = world.player;                   // world name
                    masterTime = world.time;          // world time
                    remoteReplay = true;
                    break;
                }
            }
        }

        // second pass:  build CTWorld structure:
        //                  - from replay master
        //                  - or from consolidated objects as owned (prefix) by each world

		CTworld CTW = new CTworld();
		CTW.player = masterWorldName;                 // nominal CTW header values (not actually used)
        CTW.time = masterTime;
		CTW.mode = replayActive?"Replay":(remoteReplay?"Remote":"Live");

        CTW.objects = new Dictionary<String, CTobject>();
        List<String> tsourceList = new List<String>();
		if (!Player.Equals("") && !Player.Equals("Observer")) tsourceList.Add(Player);    // always include self

        // second pass, screen masterTime, consolidate masterWorld
        foreach (CTworld world in worlds)
        {
            if (remoteReplay && !world.player.Equals(masterWorldName)) continue;        // consolidate to replayWorld

            if (world.time > updateTime) updateTime = world.time;      // keep track of most-recent CTW time

            double delta = Math.Abs(world.time - masterTime);   // masterTime NG on Remote... ???         
            if ((SyncTime > 0) && (delta > SyncTime))         // reject stale times
            {
                if (!world.player.Equals(Player) || observerFlag) clearWorld(world.player);    // inefficient?
                continue;
            }

			if (!tsourceList.Contains(world.player)) tsourceList.Add(world.player);             // build list of active worlds

			foreach (KeyValuePair<String, CTobject> ctpair in world.objects)
            {
				CTobject ctobject = ctpair.Value;
                //  UnityEngine.Debug.Log("line: " + line+", world.name: "+world.name+", objectID: "+ctobject.id+", remoteReplay: "+remoteReplay);
                if (remoteReplay                                // remotePlay: this is master world get all objects
                    || ctobject.id.StartsWith(world.player))         // Live mode:  accumulate objects owned by each world      
                {               
                    CTW.objects.Add(ctobject.id, ctobject);

					// check for change of prefab
                    if (CTlist.ContainsKey(ctobject.id) && (isReplayMode() || !world.player.Equals(Player)))  // Live mode
                    {
						GameObject mygo = CTlist[ctobject.id].gameObject;
						if (mygo == null)
						{
							UnityEngine.Debug.Log("missing go: " + ctobject.id);
							continue;
						}
                        string pf = CTlist[ctobject.id].gameObject.transform.GetComponent<CTclient>().prefab;
                        if (!pf.Equals(ctobject.model))   // recheck showMenu for async newPlayer
                        {
//                            Debug.Log(ctobject.id + ": change prefab: " + pf + " --> " + ctobject.prefab);
//                            clearObject(ctobject.id);
                            newGameObject(ctobject);
                        }
                    }

                    // instantiate new players and objects
                    if (!CTlist.ContainsKey(ctobject.id) && (observerFlag || !world.player.Equals(Player)))
                    {
                        newGameObject(ctobject);
                    }
                }
            }
        }

        // scan for missing objects
        clearMissing(CTW);

        latestTime = updateTime;                    // for replay reference
        PlayerList = tsourceList;                   // update list of active sources

//        printCTworld(CTW);
        return CTW;                                 // consolidated CTworld
	}

	//-------------------------------------------------------------------------------------------------------
	void printCTworld(CTworld ctworld) {
		String p = "CTworld:\n";
        p += ("name: " + ctworld.player + "\n");      // redundant Key, name?
        p += ("mode: " + ctworld.mode + "\n");
        p += ("time: " + ctworld.time + "\n");
        p += ("Objects:" + "\n");
		if (ctworld.objects == null) p += "<null>";
		else
		{
			foreach (CTobject ctobject in ctworld.objects.Values)
			{
				p += ("\tkey: " + ctobject.id + "\n");          // redundant Key, id?
				p += ("\tprefab: " + ctobject.model + "\n");         // object class
				p += ("\tstate: " + ctobject.state + "\n");
				p += ("\tpos: " + ctobject.pos + "\n");
			}
		}
        UnityEngine.Debug.Log(p);
	}

    //-------------------------------------------------------------------------------------------------------
    // clone new player or player-owned object...

	public GameObject newPlayer(String playerName, String model, Boolean ghost)
    {
		return newGameObject(playerName, model, new Vector3(0F, 5F, 0F), transform.rotation,  Vector3.zero, ghost, true);      
    }

	public GameObject newGameObject(CTobject ctobject)
    {
		return newGameObject(ctobject.id, ctobject.model, ctobject.pos, ctobject.rot, ctobject.scale, false, ctobject.state);
    }
    
	public GameObject newGameObject(String pName, String prefab, Vector3 position, Quaternion rotation, Vector3 scale, Boolean ghost, Boolean isactive)
	{
//		UnityEngine.Debug.Log("newGameObject: " + pName+", prefab: "+prefab);
		String playerName = pName + (ghost ? "g" : "");
		if (CTlist.ContainsKey(playerName))
		{
			CTlist[playerName].SetActive(true);     // let setState activate?
			string ctpf = CTlist[playerName].gameObject.transform.GetComponent<CTclient>().prefab;
			if (!ctpf.Equals(prefab) || scale==Vector3.zero)
			{
//				Debug.Log(playerName + ": newPlayer prefab: " + ctpf + " --> " + prefab);
				position = CTlist[playerName].transform.position;   // rebuild to new prefab (in-place)
				clearObject(playerName);    
			}
			else
			{
				return CTlist[playerName];            // already there
			}
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
		String parent = "Players";

		String[] pathparts = playerName.Split('/');
		for (int i = 0; i < pathparts.Length - 1; i++) parent += ("/" + pathparts[i]);

		GameObject pgo = GameObject.Find(parent);
		if(pgo == null) {
			UnityEngine.Debug.Log("Missing parent object: " + parent);  // init issue, catch it next update...
			return null;
		}
		Transform tparent = GameObject.Find(parent).transform;

		Transform pf = go.transform;
        Transform newp = Instantiate(pf, position, rotation * pf.rotation);    // parent

		if (scale != Vector3.zero) newp.localScale = scale;                     // zero scale means use initial prefab scale
//		newp.gameObject.SetActive(true);  // mjm 9-12-18:  make sure (re)instantiated objects are active
        
		//		newp.parent = GameObject.Find(parent).transform;
		newp.SetParent(tparent, pathparts.Length <= 1);     // 2nd arg T/F: child-local vs global position

		newp.name = playerName;
        if(newp.GetComponent<CTclient>() != null)
		    newp.GetComponent<CTclient>().prefab = prefab;

		// make sure in CTlist (inactive objects won't call CTregister...)
		if (!CTlist.ContainsKey(playerName))
		{
			CTlist.Add(newp.name, newp.gameObject);
//			UnityEngine.Debug.Log("CTlist.Add: " + playerName+", prefab: "+prefab);
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
	//  clear and destroy a game object by name:

	public void clearObject(String objectName) {
		if (!CTlist.ContainsKey(objectName)) return;            // not already there

		GameObject go = GameObject.Find(objectName);
		if (go != null)
		{
//			UnityEngine.Debug.Log("clearObject: " + objectName);
			foreach (Transform c in go.transform)
			{
//				UnityEngine.Debug.Log("clear child: " + c.name);
				CTlist.Remove(c.name);  // children will be destroyed with parent
			}
			go.SetActive(false);
			Destroy(go);
		}
        
		CTlist.Remove(objectName);
	}
    
	// more efficient if know the gameObject itself:
	public void clearObject(GameObject go) {
        if (go != null)
        {
			string objectName = go.name;
            if (!CTlist.ContainsKey(objectName)) return;            // not already there
            
            foreach (Transform c in go.transform)
            {
                CTlist.Remove(c.name);  // children will be destroyed with parent
            }
            go.SetActive(false);
            Destroy(go);
			CTlist.Remove(objectName);
        }
	}

	//-------------------------------------------------------------------------------------------------------
	// clear all object from given world; all worlds if null

	public void clearWorlds()
	{
		clearWorldsFlag = true;
	}

	public void clearWorld(String worldName)
	{
		
		List<GameObject> gos = new List<GameObject>(CTlist.Values);     // make copy; avoid sync error

		foreach (GameObject go in gos)
		{
			if (worldName == null || go.name.StartsWith(worldName))
			{
				go.SetActive(false);
				Destroy(go);                 // keep object; inactivate it only?
				CTlist.Remove(go.name);
			}
		}

		clearWorldsFlag = false;        // done
	}
    
	//-------------------------------------------------------------------------------------------------------
    // disable objects that are in CTlist but go missing from CTworld list
	// to delete (vs disable) objects during live play; need to skip delay instantiate-until-ctworld-appearance

	public void clearMissing(CTworld ctworld)
    {
//		if (!isReplayMode()) return;     // check missing Player only in replay mode

		foreach (GameObject go in CTlist.Values)      // cycle through objects in world
        {
			if(go == null) {
				Debug.Log("oops: CTlist go missing!");
				continue;
			}
			if (!ctworld.objects.ContainsKey(go.name))  
			{
				if(!go.name.StartsWith(Player))     //  don't deactivate locally owned Player objects (might be instantiated but not yet seen in ctworld)
				    go.SetActive(false);
//				Destroy(go);                        // ??
//				CTlist.Remove(go.name);             // can't change CTlist in CTlist-iterator
			}
        }
/*
		foreach (KeyValuePair<String, CTobject> ctpair in ctworld.objects)
		{
			CTobject go = ctpair.Value;
			string ctname = ctpair.Key;
			if(go == null) {
				Debug.Log("oops: CTlist null gameObject: "+ctname);
				CTlist.Remove(ctname);
			}
			else if(!ctworld.objects.ContainsKey(ctname)) {
				Debug.Log("CTlist clear missing: " + ctname);
				CTlist[ctname].SetActive(false);
			}
		}
*/
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

			if (clearWorldsFlag) clearWorld(null);

			oldTime = replayTime;

			// form HTTP GET URL
			String urlparams = "";    // "?f=d" is no-op for wildcard request
			if (replayActive) urlparams = "?t=" + replayTime;     // replay at masterTime
			string url1 = Server + "/CT/" + Session + "/GamePlay/*/"+CTchannel + urlparams;

			UnityWebRequest www1 = UnityWebRequest.Get(url1);
            www1.SetRequestHeader("AUTHORIZATION", CTauthorization());
            yield return www1.Send();

//			WWW www1 = new WWW(url1);
//			yield return www1;          // wait for results to HTTP GET
//			CTdebug("www.text: " + www1.text);
            
			// proceed with parsing CTstates.txt
			if (!string.IsNullOrEmpty(www1.error) || www1.downloadHandler.text.Length < 10)
			{
				Debug.Log("HTTP Error: " + www1.error + ": " + url1);
				if(isReplayMode()) clearWorlds();              // presume problem is empty world...
				continue;
			}
			CTdebug(null);          // clear error

			// parse to class structure...
            List<CTworld> ws = CTserdes.deserialize(www1.downloadHandler.text);
			CTworld CTW = mergeCTworlds(ws);
			if (CTW == null || CTW.objects == null) continue;          // notta      
            
			foreach (CTobject ctobject in CTW.objects.Values)      // cycle through objects in world
			{
				if (Ghost && ctobject.id.Equals(Player))          // extra "ghost" player
					setState(Player + "g", ctobject);

				setState(ctobject.id, ctobject);
			}
		}              // end while(true)   
    }

	//-------------------------------------------------------------------------------------------------------
	// CTsetstate wrapper

	private void setState(String objectID, CTobject ctobject) {
		GameObject ct;
        if (!CTlist.TryGetValue(objectID, out ct)) return;
		if (ct == null) return;         // fire wall

		CTclient ctp = ct.GetComponent<CTclient>();
        if (ctp != null)
            ctp.setState(ctobject, isReplayMode());
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

//		if (commanderMode)  
		return replayActive;
//		else   return replayActive || observerFlag;
	}

	public void toggleReplay() {
		replayActive = !replayActive;
//		if (!commanderMode) observerFlag = replayActive;
	}

	public void setReplay(bool ireplayActive)
    {
        replayActive = ireplayActive;
//		if (!commanderMode) observerFlag = replayActive;
    }

    // return True if this object should be written to CT
	public Boolean doCTwrite(String objName) {
//		if(commanderMode) 
		return (replayActive || objName.StartsWith(Player));
//		else    return (objName.StartsWith(Player));
	}

	public string CTauthorization() {
		string auth = user + ":" + password;
        auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        auth = "Basic " + auth;
		return auth;
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
    
	internal Boolean syncError = true;
	public Boolean doSyncClock() {
		StartCoroutine(getSyncClock());
		return true;  // bleh need to wait for syncError status ??
	}
 
	private IEnumerator getSyncClock()
//    public Boolean doSyncClock()
    {
        string url1 = Server + "/sysclock";
		syncError = true;
		int maxTry = 5;

		while (true)
		{
			yield return new WaitForSeconds(0.1F);
            
			UnityWebRequest www1 = UnityWebRequest.Get(url1);
			www1.SetRequestHeader("AUTHORIZATION", CTauthorization());
			www1.chunkedTransfer = false;           // unity bug work-around?
			yield return www1.SendWebRequest();     // wait for results to HTTP GET

			//        WWW www1 = new WWW(url1);
//			yield return www1;          // wait for results to HTTP GET
//			UnityEngine.Debug.Log("getSyncClock, text: " + www1.downloadHandler.text);

			//			UnityEngine.Debug.Log("text: " + www1.downloadHandler.text);
			if (!string.IsNullOrEmpty(www1.error) || www1.isHttpError || www1.isNetworkError || www1.responseCode != 200)
			{
				syncError = true;
				if (www1.responseCode == 401)
				{
					CTdebug("Unauthorized Server Connection! (" + www1.responseCode + ")");
					yield break;
				}
				else
					CTdebug("Server Connection Error (" + www1.responseCode + "): " + www1.error);
			}
			else
			{
				double now = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
				try
				{
					clocksync = (Double.Parse(www1.downloadHandler.text) / 1000f) - now;
					UnityEngine.Debug.Log("syncClock: " + clocksync);
					syncError = false;
				} 
				catch (Exception e) {
					UnityEngine.Debug.Log(e.Message);
				}
				CTdebug(null);
			}

			if (syncError && ((maxTry--) > 0))
			{
				UnityEngine.Debug.Log("maxTry: " + maxTry+", syncError: "+syncError+", url1: "+url1);
				continue;
			}

//			syncError = false;  // foo
//			UnityEngine.Debug.Log("sync!");
			yield break;
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
		if (debug == null)  debug = "Host: " + Server + ", Session: " + Session;  // default info
		else                UnityEngine.Debug.Log(debug);
        
		debugText.text = debug;
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
