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
//	public int BlockPts = 5;                        // 5/50 = 0.1s
	internal float pollInterval = 0.1F;       // global update-interval (sets BlockPts)
	[Range(1,100)]
	public int blockRate = 10;                   // rate to send file updates (Hz)
	[Range(1, 200)]
	public int maxPointRate = 50;                 // (max) rate to update points
	public int blocksPerSegment = 200;          // CTlib.CThttp setting
	public float fpsInterval = 0.1f;     // update interval for FPS info   

	// flags:
	public Boolean AsyncMode = true;
	internal Boolean gamePaused = true;
	internal static Boolean activeWrite = false;
    internal static Boolean activeRead = false;         // to do...
    internal Boolean newSession = true;
	//    internal Boolean observerFlag = true;
    internal Boolean trackEnabled = true;             // enable player-tracks
	internal Boolean replayActive = false;

	private Boolean goingLive = false;                  // flag transition playback to real-time...

    // internal variable accessible from other scripts but not Editor Inspector
    internal Dictionary<String, GameObject> CTlist = new Dictionary<String, GameObject>();
	internal List<String> PlayerList = new List<String>();

	internal double latestTime = 0F;
    internal string Server = "http://localhost:8000";
    internal string Player = "";
    internal string Model = "Ball";
	internal string Session = "";
//    internal int MaxPts = 100;
    internal Double clocksync = 0f;                   // add this to local clock to sync to CTweb
    internal double lastSubmitTime = 0;

    // string constants for Deploy pulldown
	internal String Cancel = "<Cancel>";        // dropdown value to cancel/delete un-played deployed objects
	internal String Clear = "<Clear>";          // dropdown value to delete all player objects
	internal String Save = "<Save>";            // dropdown value to save player world
	internal String Observer = "Observer";      // reserved player-name for observe-only mode
	internal String Inventory = "Inventory";    // folder name for deployables

    // CThttp IO classes
    internal CTlib.CThttp ctplayer = null;            // storage
    internal CTlib.CThttp ctvideo = null;
	internal CTlib.CThttp ctsnapshot = null;
    
	internal double replayTime = 0;
	internal String replayText = "Live";

    // login user/pw
	internal string user = "CloudTurbine";
	internal String password = "RBNB";
	#endregion


    private Text debugText;
    private Text fpsText;

	private Boolean JSON_Format = true;
	private String CTchannel = "CTstates.txt";
	private Boolean playPaused = false;             // slider auto vs drag replay mode

	//-------------------------------------------------------------------------------------------------------
	// Use this for initialization
	void Start()
    {
		debugText = GameObject.Find("debugText").GetComponent<Text>();
		fpsText = GameObject.Find("fpsText").GetComponent<Text>();

		pollInterval = 1F / blockRate;
		CTchannel = JSON_Format ? "CTstates.json" : "CTstates.txt";
        StartCoroutine("getGameState");
    }

	//-------------------------------------------------------------------------------------------------------
    // build list of players/CT objects
    
	public void CTregister(GameObject go) {        
		if (go.GetComponent<CTclient>()!=null && !CTlist.ContainsKey(fullName(go)))   // only register objects with CTclient
		{
			CTlist.Add(fullName(go), go);
//			Debug.Log("CTregister, go.name: " +go.name+ ", fullname: "+ fullName(go));
		}
	}

    //-------------------------------------------------------------------------------------------------------
    // get States from objects, putData to CT as CTstates.txt (pb or rt)

	float stopWatchB = 0f;              // block timer
	float stopWatchP = 0f;              // point timer
	float stopWatchF = 0f;              // FPS timer
	int npts =0, nups = 0;              // point counter
	double BPS=10, PPS=10, FPS=10;      // block, point, frame per second info

	void Update()
	{
		nups++;
		stopWatchF += Time.deltaTime;
		if (ctplayer == null) serverConnect();
//		if (newSession) Debug.Log("Update RT, newsession: "+newSession);

		if (ctplayer == null || newSession || replayActive || gamePaused || Player.Equals(Observer) /* || CTlist.Count==0 */) {
			// No writes for you!
			if (stopWatchF > fpsInterval)
			{
				fpsText.text = "FPS: " + Math.Round(nups / stopWatchF);           // info
				stopWatchF = stopWatchP = stopWatchB = nups = npts = 0;
			}
			activeWrite = false;
			return;
		}
		activeWrite = true;         // flag important global state:  actively creating files via CTput

		if (maxPointRate < blockRate) maxPointRate = blockRate;   // firewall
		float pointInterval = 1f / maxPointRate;
		stopWatchP += Time.deltaTime;
		if (stopWatchP >= pointInterval)
		{
			string CTstateString = CTserdes.serialize(this, JSON_Format ? CTserdes.Format.JSON : CTserdes.Format.CSV);
			if (CTstateString == null) return;      // meh

			ctplayer.setTime(ServerTime());
			ctplayer.putData(CTchannel, CTstateString);
			npts++;
			stopWatchP = 0f;
		}
        
		float blockInterval = 1f / blockRate;
		pollInterval = blockInterval;          // CTget poll at fraction of block interval?
        
		stopWatchB += Time.deltaTime;
		if(stopWatchB >= blockInterval) {
			BPS = Math.Round(1F / stopWatchB);        // moving average
            PPS = Math.Round(npts / stopWatchB);
			stopWatchB = npts = 0;
			ctplayer.flush();           // flush data to CT
		}
        
		// calc/display metrics
		if(stopWatchF > fpsInterval) {
            FPS = Math.Round(nups / stopWatchF);
            fpsText.text = "B/P/FPS: " + BPS + "/" + PPS + "/" + FPS;
			stopWatchF = nups = 0;
		}
    }

	//-------------------------------------------------------------------------------------------------------
	// SnapShot:  single-shot save current Player state to World-saves

	internal void SnapShot() {
//		Debug.Log("SnapShot: " + Player);
		if (Player.Equals("")) return;  // no active player
        
		serverConnect();  // reset Player folder paths
      
		string CTstateString = CTserdes.serialize(this, JSON_Format ? CTserdes.Format.JSON : CTserdes.Format.CSV);
		if (CTstateString == null) return;      // meh

        // archive World state
        ctsnapshot.setTime(ServerTime());
        ctsnapshot.putData(CTchannel, CTstateString);
		ctsnapshot.flush();
	}

	//-------------------------------------------------------------------------------------------------------
    // OneShot:  single-shot save current Player state to GamePlay

    internal void OneShot()
    {
		Debug.Log("OneShot, Player: " + Player);
		if (Player.Equals("")) return;  // no active player

        serverConnect();  // reset Player folder paths

        string CTstateString = CTserdes.serialize(this, JSON_Format ? CTserdes.Format.JSON : CTserdes.Format.CSV);
        if (CTstateString == null) return;      // meh

        // update GamePlay
        ctplayer.setTime(ServerTime());
        ctplayer.putData(CTchannel, CTstateString);
        ctplayer.flush();

//		Debug.Log("Oneshot: " + Player+", state: "+CTstateString);
    }

    //-------------------------------------------------------------------------------------------------------
    // parseCTworld:  utilities to parse CTworld/CTstates.txt into CTworld/CTobject List structures

    String masterWorldName = null;

	//-------------------------------------------------------------------------------------------------------
	// mergeCTworlds:  consolidate CTworlds from each player into local world-state

	CTworld mergeCTworlds(List<CTworld> worlds)
	{
//		Debug.Log("mergeCTworlds, CTlist.Count: " + CTlist.Count);
		if (worlds == null)
		{
			CTdebug("Warning: null CTworlds!");
			return null;                     // null if empty start?
		}

		double masterTime = ServerTime();
		double updateTime = 0F;

		// build CTWorld structure from consolidated objects as owned (prefix) by each world

		CTworld CTW = new CTworld();
		CTW.player = masterWorldName;                 // nominal CTW header values (not actually used)
		CTW.time = masterTime;

		CTW.objects = new Dictionary<String, CTobject>();
		List<String> tsourceList = new List<String>();
		if (!Player.Equals("")) tsourceList.Add(Player);    // always include self

		// second pass, screen masterTime, consolidate masterWorld
		foreach (CTworld world in worlds)
		{
			if (world.time > updateTime) updateTime = world.time;      // keep track of most-recent CTW time

			double delta = Math.Abs(world.time - masterTime);   // masterTime NG on Remote... ???         
			if ((SyncTime > 0) && (delta > SyncTime))         // reject stale times
			{
				if (!world.player.Equals(Player) /* || observerFlag */) clearWorld(world.player);    // inefficient?
				continue;
			}

			if (!tsourceList.Contains(world.player)) tsourceList.Add(world.player);     // build list of active worlds

			foreach (KeyValuePair<String, CTobject> ctpair in world.objects)
			{
				CTobject ctobject = ctpair.Value;
				if (!ctobject.id.StartsWith(world.player + "/"))
					ctobject.id = world.player + "/" + ctobject.id;      // auto-prepend world name to object

				//   if (ctobject.id.StartsWith(world.player))            // accumulate objects owned by each world      
				//   {
				try
				{
					CTW.objects.Add(ctobject.id, ctobject);
				}
				catch (Exception e)
				{
					Debug.Log("CTW exception on object: " + ctobject.id + ", e: " + e);
				}

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
						Debug.Log("change prefab: " + ctobject.model + " --> " + pf);
						newGameObject(ctobject);
					}
				}

				// instantiate new players and objects
				// Notes:  following line has tricky/obscure ramifications:  
				// don't merge local-Player (under user control) except:
				// - once up front for each new session
				// - during replay
				if (!CTlist.ContainsKey(ctobject.id) && (newSession || replayActive || !world.player.Equals(Player)))  // mjm 12/3/18
				{
					// Debug.Log("newGameObject, name: " + ctobject.id+", world.player: "+world.player+", Player: "+Player);
					newGameObject(ctobject);
				}
			}
		}
        
		// scan for missing objects
		clearMissing(CTW);

		latestTime = updateTime;                    // for replay reference
		PlayerList = tsourceList;                   // update list of active sources

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
    // clone new player-owned object...
    
    public GameObject clonePlayer(GameObject go)
    {
 //       Debug.Log("clonePlayer: " + go.name);

        if (CTlist.ContainsKey(fullName(go)))  // clone existing?
        {
			CTclient ctc = go.GetComponent<CTclient>();
			CTobject cto = new CTobject();
			cto.id = fullName(go)+"X";
			cto.model = ctc.prefab;
            cto.pos = go.transform.position;
            cto.rot = go.transform.rotation;
            cto.scale = go.transform.localScale;
            cto.state = true;
            cto.color = ctc.myColor;

			int gen = ctc.Generation;
			go = newGameObject(cto);
			go.GetComponent<CTclient>().Generation = gen + 1;
			return go;
        }
        else {
            Debug.Log("Missing Clone!");
			return null;
        }
    }

	//-------------------------------------------------------------------------------------------------------
	// shortcut to add local object

	public GameObject newPlayer(String objID, String model)
	{
		if (!objID.StartsWith(Player + "/")) objID = Player + "/" + objID;  // enforce full path name
//		Debug.Log("newPlayer: " + objID);

		CTobject cto = new CTobject();
		cto.id = objID;
		cto.model = model;
		cto.pos = Vector3.zero;
		cto.rot = transform.rotation;
		cto.scale = Vector3.zero;
		cto.state = true;
		cto.color = Color.gray;

		return newGameObject(cto);
	}

	//-------------------------------------------------------------------------------------------------------
	// newGameObject:  create and instantiate new CTobject 

	public GameObject newGameObject(CTobject ctobject)
    {
		// define parameters
		String objID = ctobject.id;
		String prefab = ctobject.model;
		Vector3 position = ctobject.pos;
		Quaternion rotation = ctobject.rot;
		Vector3 scale = ctobject.scale;
		Boolean isactive = ctobject.state;
		Color color = ctobject.color;

//		Debug.Log("newGameObject: " + objID+", prefab: "+prefab);
		if (prefab.Equals("")) return null;         // in-game player without prefab

		if (CTlist.ContainsKey(objID))
		{
//			Debug.Log("existing object: " + objID);
			CTlist[objID].SetActive(true);     // let setState activate?
            
			CTclient ctc = CTlist[objID].gameObject.transform.GetComponent<CTclient>();
			string ctpf = (ctc==null)?"":CTlist[objID].gameObject.transform.GetComponent<CTclient>().prefab;
			if (!ctpf.Equals(prefab) || scale==Vector3.zero)
			{
				position = CTlist[objID].transform.position;   // rebuild to new prefab (in-place)
				rotation = CTlist[objID].transform.rotation;
				scale = Vector3.zero;   // flag to use native prefab scale
				clearObject(objID);
			}
			else
			{
				return CTlist[objID];            // already there
			}
		}

		GameObject tgo = GameObject.Find(objID);
		if (tgo != null)
		{
//			UnityEngine.Debug.Log("Can't create duplicate: " + pName);      // unregistered prefabs hit this check
			return tgo;
		}

        // load prefab
		GameObject pfgo = ((GameObject)getPrefab(prefab));
		if (pfgo == null)
		{
			UnityEngine.Debug.Log("NULL prefab: " + prefab);
			return null;
		}
		pfgo.SetActive(isactive);

		// build hierarchical path to new object: Players/<Player>/object-path
		String parent = "Players";
		String fullpath = parent + "/" + objID;
		char[] delims = { '/', ':' };
		String[] pathparts = fullpath.Split(delims);
        
		GameObject pgo = GameObject.Find(parent);   // "Players" at this point
		for (int i = 1; i < pathparts.Length-1; i++)
		{
			parent = parent + "/" + pathparts[i];
			GameObject cgo = GameObject.Find(parent);

			if(cgo == null) {
				cgo = new GameObject();    // empty gameObject; trunk-node in hierarchy
                cgo.name = pathparts[i];
                cgo.transform.SetParent(pgo.transform, true);
			}
			pgo = cgo;
		}

		pgo = GameObject.Find(parent);
		if(pgo == null) {    // parent missing
			UnityEngine.Debug.Log("Missing parent: " + parent+", child: "+objID);  // init issue, catch it next update...
			return null;
		}
		Transform tparent = pgo.transform;
		Transform pf = pfgo.transform;

        // Instantiate!
//        Transform newp = Instantiate(pf, position, rotation * pf.rotation);    // rez prefab
//        newp.SetParent(tparent, pathparts.Length <= 1);     // 2nd arg T/F: child-local vs global position
		Transform newp = Instantiate(pf, position, rotation * pf.rotation, tparent);    // rez prefab with set parent
		if (scale != Vector3.zero) newp.localScale = scale;                     // zero scale means use initial prefab scale
		newp.name = pathparts[pathparts.Length - 1];
//		Debug.Log(newp.name+": instantiate at: " + position+", actual pos: "+newp.position);

//		Debug.Log("newp.name: " + newp.name+", parent: "+parent+", pf: "+pf);
		CTclient myctc = newp.GetComponent<CTclient>();
		if (myctc != null)
		{
			myctc.prefab = prefab;
            myctc.setColor(color);
		}

		// make sure in CTlist (inactive objects won't call CTregister...)
		CTregister(newp.gameObject);

		return newp.gameObject;
	}

	//-------------------------------------------------------------------------------------------------------
	//  clear and destroy a game object by name:

	public void clearObject(String objectName) {
//		Debug.Log("clearObject: " + objectName);
		if (!CTlist.ContainsKey(objectName)) return;            // not already there

		GameObject go = GameObject.Find(objectName);
		clearObject(go);
	}
    
	// more efficient if know the gameObject itself:
	public void clearObject(GameObject go) {
//		Debug.Log("clearObject: " + go.name);

        if (go != null)
        {
			string objectName = fullName(go);
            if (!CTlist.ContainsKey(objectName)) return;            // not already there
            
            foreach (Transform c in go.transform)
            {
				CTlist.Remove(fullName(c.gameObject));  // children will be destroyed with parent
            }

            go.SetActive(false);
			CTlist.Remove(objectName);
            Destroy(go);
        }
	}

	//-------------------------------------------------------------------------------------------------------
	// clear all object from given world; all worlds if null
    
	public void clearWorlds()
	{
		clearWorld(null, false);        // clear all gameObjects without save to CT
	}
    
	public void clearWorld() {
		clearWorld(Player, true);       // clear local Player and save to CT
	}

	public void clearWorld(String worldName)    // clear remote Player w/o save to CT
    {
			clearWorld(worldName, false);    // clear particular world
    }

	public void clearWorld(String worldName, Boolean syncFlag)
	{
//		Debug.Log("clearWorld: " + worldName);
		activeWrite = false;  // whoa

		// null worldName means all worlds
		if(worldName == null) {
			GameObject players = GameObject.Find("Players");
			List<Transform> destroyList = new List<Transform>();  // make copy; avoid sync error

			foreach (Transform player in players.transform)
            {
				player.gameObject.SetActive(false);   // hide for starters
				destroyList.Add(player);
            }
            // Destroy or DestroyImmediate?
			foreach (Transform t in destroyList) DestroyImmediate(t.gameObject);  // destroy while iterating wrecks list
			CTlist.Clear();  // all gone 
		}
		else {
			GameObject worldObject = GameObject.Find(worldName);
			if (worldObject == null) Debug.Log("Null World! " + worldName);
			else
			{
				worldObject.SetActive(false);        // hide for starters
				DestroyImmediate(worldObject);
			}
		}

		// clean up CTlist
		List<string> removals = new List<string>();  // make copy; avoid sync error
		foreach (KeyValuePair<string, GameObject> entry in CTlist)
        {
			if (entry.Value == null)
			{
				removals.Add(entry.Key);
			}
        }
		foreach (string obj in removals) CTlist.Remove(obj);
	}
    
	//-------------------------------------------------------------------------------------------------------
    // disable objects that are in CTlist but go missing from CTworld list
	// to delete (vs disable) objects during live play; need to skip delay instantiate-until-ctworld-appearance

	public void clearMissing(CTworld ctworld)
    {
		foreach (GameObject go in CTlist.Values)      // cycle through objects in world
        {
//			Debug.Log("clearMissing check: " + fullName(go) + ", state: " + go.activeSelf);
			if(go == null) {
//				Debug.Log("Oops: CTlist gameObject missing!");
				continue;
			}

			if (!go.activeSelf)
			{
//				Debug.Log("clearMissing Destroy: " + fullName(go));
				clearObject(go);        // inactive?  buh bye
				return;
			}

			String goName = fullName(go);
			if (!ctworld.objects.ContainsKey(goName))  
			{            
				//  don't deactivate locally owned Player objects (might be instantiated but not yet seen in ctworld)
				if (!goName.StartsWith(Player) || replayActive)
				{    
					CTclient ctc = go.GetComponent<CTclient>();
					if (!(ctc != null && ctc.isRogue))   // let Rogue objects persist
					{                       
						go.SetActive(false);
//						Debug.Log("clearMissing: " + goName+", Player: "+Player);
					}
				}
			}
        }

    }

    //-------------------------------------------------------------------------------------------------------
    // getGameState: GET <Session>/GamePlay/* from CT, update world objects (all modes)

    static double oldTime = 0;
    public IEnumerator getGameState()
    {
		Boolean pendingSession = false;
		while (true)
		{
			float waitInterval = replayActive ? (1f / maxPointRate) : pollInterval;     // pointInterval for faster response
			yield return new WaitForSeconds(waitInterval);      
         
			if(newSession) {
//				Debug.Log("getGameState, newSession, replayActive: "+replayActive);
				pendingSession = true;
			}

			if (gamePaused) continue;                                     // no-op unless run-mode
			if (replayActive && (replayTime == oldTime)) continue;      // no dupes (e.g. paused)
			oldTime = replayTime;

			// form HTTP GET URL
			String urlparams = "";    // "?f=d" is no-op for wildcard request
			if (replayActive)   urlparams = "?t=" + replayTime;     // replay at masterTime
			else                urlparams = "?c=" + Math.Round(pollInterval * 1000F);        //  set cache interval 
			string url1 = Server + "/CT/" + Session + "/GamePlay/*/"+CTchannel + urlparams;
//			Debug.Log("url1: " + url1);
            
			UnityWebRequest www1 = UnityWebRequest.Get(url1);
            www1.SetRequestHeader("AUTHORIZATION", CTauthorization());
//            yield return www1.Send();
			yield return www1.SendWebRequest();

			if (newSession && !pendingSession)
			{
				Debug.Log("WHOA wait for pending session!");
				continue;
			}

			// proceed with parsing CTstates.txt
			if (!string.IsNullOrEmpty(www1.error) || www1.downloadHandler.text.Length < 10)
			{
//				Debug.Log("HTTP Error: " + www1.error + ": " + url1);
				if(isReplayMode()) clearWorlds();              // presume problem is empty world...
				pendingSession = newSession = false;            // bail (presume empty all-around)
				continue;
			}
			CTdebug(null);          // clear error

			// parse to class structure...
            List<CTworld> ws = CTserdes.deserialize(www1.downloadHandler.text);
			CTworld CTW = mergeCTworlds(ws);
			if (CTW == null || CTW.objects == null) continue;          // notta      
            
			foreach (CTobject ctobject in CTW.objects.Values)      // cycle through objects in world
			{
//				Debug.Log("get/setState: "+ctobject.id+", position: "+ctobject.pos);
				setState(ctobject.id, ctobject);
			}

			if(pendingSession) {
//				Debug.Log("END newSession!");
				pendingSession = newSession = false;               // (re)enable Update getData
			}
		}              // end while(true)   
    }

	//-------------------------------------------------------------------------------------------------------
	// getWorldState: GET <Session>/World/* from CT, update world objects (all modes)

	public void deployInventory(String world)           // get specific world or "*" for all
	{
		StartCoroutine(deployInventoryItem(world));
	}

	private IEnumerator deployInventoryItem(String deploy)
    {
//		Debug.Log("deployInventory: " + deploy);
        while (true)
        {
            yield return new WaitForSeconds(pollInterval); 

            // form HTTP GET URL
			string url1 = Server + "/CT/" + Session + "/" + Inventory + "/" +deploy +"/" + CTchannel+"?f=d";

            UnityWebRequest www1 = UnityWebRequest.Get(url1);
            www1.SetRequestHeader("AUTHORIZATION", CTauthorization());
            yield return www1.SendWebRequest();

            // proceed with parsing CTstates.txt
            if (!string.IsNullOrEmpty(www1.error) || www1.downloadHandler.text.Length < 10) 
            {
//                Debug.Log("getWorldState HTTP error: " + www1.error + ": " + url1);
                yield break;
            }
            CTdebug(null);          // clear error

            // parse to class structure...
            List<CTworld> worlds = CTserdes.deserialize(www1.downloadHandler.text);
			if(worlds == null) {
				Debug.Log("Null worlds found for: " + url1);
				yield break;
			}

//			Debug.Log("getWorldState, url1: " + url1 + ", text: " + www1.downloadHandler.text);
			// instantiate World objects here (vs waiting for CT GET loop)
			foreach (CTworld world in worlds)
            {
				foreach (KeyValuePair<String, CTobject> ctpair in world.objects)
				{
					CTobject ctobject = ctpair.Value;
					if (!ctobject.id.StartsWith(Player+"/")) ctobject.id = Player + "/" + ctobject.id;      // auto-prepend Player name to object
					newGameObject(ctobject);
				}
            }

			yield break;
        }              // end while(true)   
    }

	//-------------------------------------------------------------------------------------------------------
	// CTsetstate wrapper

	private void setState(String objectID, CTobject ctobject) 
	{
		GameObject ct;
        if (!CTlist.TryGetValue(objectID, out ct)) return;
		if (ct == null) return;         // fire wall

		CTclient ctp = ct.GetComponent<CTclient>();
		if (ctp != null)
		{
			ctp.setState(ctobject, replayActive, playPaused);
			if (newSession) ctp.jumpState();                    // do it now (don't wait for next ctclient.Update cycle)
		}
	}
    
	//-------------------------------------------------------------------------------------------------------
	public Boolean isReplayMode() {
		return replayActive;
//		return replayActive || !activeWrite;
	}

	public Boolean isPaused() {
		return replayActive && playPaused;
	}

	//-------------------------------------------------------------------------------------------------------
    // set timeControl time/state
    
	public Boolean setTime(double ireplayTime, String ireplayText, Boolean iPlayPaused) {
		replayTime = ireplayTime;
		replayText = ireplayText;
		playPaused = iPlayPaused;          // to detect auto-replay vs slider pause/drag
//		Debug.Log("setTime playPaused: " + playPaused);
        
		return replayActive;
	}
    
	public void toggleReplay() {
		if (replayActive)
        {
			newSession = true;
//            clearWorlds();
//			Debug.Log("Go live! newSession: "+newSession);
        }

        replayActive = !replayActive;      
	}

	public void setReplay(bool ireplayActive)
    {
		if (ireplayActive == replayActive) return;

        toggleReplay();

//        replayActive = ireplayActive;

//		activeWrite = !replayActive;  // ???
//		gamePaused = !activeWrite;      // ??
		if (ctplayer == null) serverConnect();  // firewall
    }
    
    // return True if this object should be written to CT
	public Boolean activePlayer(GameObject go) {
		Boolean isactive = localPlayer(go) && activeWrite && !newSession;  // whew
//		Debug.Log("activePlayer check: " + fullName(go) + ", Player: " + Player+", activeWrite: "+activeWrite+", gamePaused: "+gamePaused+", replayActive: "+replayActive+", isactive: "+isactive);
		return isactive;
	}

	internal Boolean localPlayer(GameObject go) {
		return fullName(go).StartsWith(Player);
	}

	internal Boolean observerMode() {
		return Player.Equals(Observer);
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
	// fullName:  return full hierarchical path name of game object

	public static string fullName(GameObject go)
    {
		if (go == null)
		{
			Debug.Log("fullName Destroyed object!");
			return null;
		}
		string oname = go.name;
        string fname = go.name;

		while (go.transform.parent != null)
//        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
			if (go.name.Equals("Players")) break;       // got it
            fname = go.name + "/" + fname;
//			Debug.Log("parent: " + go.name + ", fname: " + fname);
        }
		fname = fname.Replace(".", "/");                  // convert legacy names

//		Debug.Log("fullName, original: " + oname + ", full: " + fname);
        return fname;
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
		//        Color c = new Color(0.5F, 0.5F, 0.5F, 0.4F);        // default is semi-transparent gray
		Color c = Color.gray;
        if      (color.ToLower().StartsWith("red")) c = new Color(0.9F, 0.2F, 0.2F, alpha);
        else if (color.ToLower().StartsWith("blue")) c = new Color(0.2F, 0.2F, 0.9F, alpha);
        else if (color.ToLower().StartsWith("green")) c = new Color(0.1F, 0.5F, 0.1F, alpha);
        else if (color.ToLower().StartsWith("yellow")) c = new Color(0.8F, 0.8F, 0F, alpha);

//        Debug.Log("text2color, color: " + color + ", alpha: "+alpha+", c: " + c);
        return c;
    }
    
	public Color objectColor(GameObject go) {
		return(Text2Color(fullName(go), 1f));
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

	//----------------------------------------------------------------------------------------------------------------
	// Connect to CTweb server
	// Note:  this updates *Player* in PUT file path

	public void serverConnect()
	{
		if(Player == null || Player.Equals("")) {
//			Debug.Log("OOPS serverConnect null Player!!!!!!");
			return;
		}

		// CT vidcap source 
		if (ctvideo != null) ctvideo.close();
		ctvideo = new CTlib.CThttp(Session + "/ScreenCap/" + Player, blocksPerSegment, true, false, false, Server);
		ctvideo.login(user, password);
		ctvideo.setAsync(AsyncMode);

		// CT snapshot source
		if (ctsnapshot != null) ctsnapshot.close();
		ctsnapshot = new CTlib.CThttp(Session + "/" + Inventory + "/" + Player, blocksPerSegment, true, true, true, Server);
		ctsnapshot.login(user, password);
		ctsnapshot.setAsync(AsyncMode);

		// CT player source
		if (ctplayer != null) ctplayer.close();
		ctplayer = new CTlib.CThttp(Session + "/GamePlay/" + Player, blocksPerSegment, true, true, true, Server);
		ctplayer.login(user, password);
		ctplayer.setAsync(AsyncMode);
	}
}
