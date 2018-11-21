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
using System.Collections.Generic;
using UnityEngine;
using CTworldNS;

/// <summary>
/// CTserdes
/// 
/// This class provides support for serializing from or deserializing to CTworld/CTobject classes.
/// JSON and text/csv formats are supported.
/// </summary>

public class CTserdes
{

	public enum Format
	{
		CSV,
		JSON
	}

	public CTserdes()
	{
	}

	// A version of CTworld classes used with JSON serialization
	// https://docs.unity3d.com/Manual/JSONSerialization.html
	// To use Unity's JsonUtility, classes must obey the following:
	// - marked "[Serializable]"
	// - only public fields will receive data from JSON (not private or fields marked NonSerialized)
	// - not all types are supported; specific to our use here, Dictionary<>, Vector3 and Quaternion aren't serializable
	// - can't use the C# auto-implemented properties (i.e., the "{ get; set; }" methods
	// Thus, the issues with using CTworld and CTobject classes are:
	// - auto-implemented properties (get/set)
	// - use of Dictionary, Vector3 and Quaternion types
	// - would need to add "[Serializable]" to both classes (not a problem)
	[Serializable]
	public class CTworldJson
	{
		public string player;
		public double time = 0F;            // header-time used for optional "staleness" test
		public string mode = "Live";        // default mode
		public List<CTobjectJson> objects;
	}
	[Serializable]
	public class CTobjectJson
	{
		public string id;
		public string model;
		public Boolean state = true;                                    // default state is true
		public List<Double> pos = new List<Double> { 0, 0, 0 };
		public List<Double> rot = new List<Double> { 0, 0, 0 };
		public List<Double> scale = new List<Double> { 0, 0, 0 };       // default Vector3.zero means no change native scale
		public string link;
		public List<Double> color = new List<Double> { 0, 0, 0, 0 };    // default Color.clear means use native object-color
		public string custom;
	}

	/// <summary>
	/// Find all indeces of a given substring within a string.
	/// 
	/// This method was (almost entirely) copied from Matti Virkkunen's sample code found on Stack Overflow at
	/// https://stackoverflow.com/questions/2641326/finding-all-positions-of-substring-in-a-larger-string-in-c-sharp.
	/// Sample author: Matti Virkkunen, https://stackoverflow.com/users/227267/matti-virkkunen
	/// License: Stack Overflow content is covered by the Creative Commons license, https://creativecommons.org/licenses/by-sa/3.0/legalcode
	/// </summary>
	/// <param name="str">The string to search.</param>
	/// <param name="value">The value to search for in the given string.</param>
	/// <returns>List of integer indeces.</returns>
	public static List<int> AllIndexesOf(string str, string value)
	{
		if (String.IsNullOrEmpty(value))
			throw new ArgumentException("the string to find may not be empty", "value");
		List<int> indexes = new List<int>();
		for (int index = 0; ; index += value.Length)
		{
			index = str.IndexOf(value, index);
			if (index == -1)
				return indexes;
			indexes.Add(index);
		}
	}

	/// <summary>
	/// Limit the precision of a given floating point value.
	/// </summary>
	/// <param name="valI">Input floating point value.</param>
	/// <param name="precI">Desired number of decimal places of precision.</param>
	/// <returns>The double with the desired number of decimal places of precision.</returns>
	public static double LimitPrecision(double valI, int precI)
	{
		return ((long)(valI * Math.Pow(10.0, precI))) / Math.Pow(10.0, precI);
	}

	/// <summary>
	/// Deserialize a string into a List of one or more CTworld objects.
	/// This method supports parsing ".txt"/csv and JSON strings.
	/// </summary>
	/// <param name="strI">The serialized world objects.</param>
	/// <returns>A List of CTworlds, parsed from the given string.</returns>
	public static List<CTworld> deserialize(string strI)
	{
		List<CTworld> worlds = null;
		if (strI[0] == '#')
		{
			worlds = CTserdes.deserialize_csv(strI);
		}
		else if (strI[0] == '{')
		{
			worlds = CTserdes.deserialize_json(strI);
		}
		return worlds;
	}

	/// <summary>
	/// Deserialize the given txt/csv string into a List of CTworld objects.
	/// 
	/// CSV example:
	/// #Live:1536843969.4578:Red 
	/// Red;Ball;1;(2.5764, 0.2600, 8.5905);(0.0277, 60.7938, -0.0043) 
	/// Red.Ground;Ground;1;(0.0000, 0.0000, 20.0000);(0.0000, 0.0000, 0.0000) 
	/// Red.Pickup0;Pickup;1;(0.2000, 0.4000, 23.0000);(45.0800, 20.8186, 21.9459) 
	/// Red.Pickup1;Pickup;1;(8.1000, 0.4000, 27.4000);(45.0800, 20.8186, 21.9459) 
	/// Red.Pickup2;Pickup;1;(-1.3000, 0.4000, 13.3000);(45.0800, 20.8186, 21.9459) 
	/// Red.Pickup3;Pickup;1;(2.0000, 0.4000, 23.6000);(45.0800, 20.8186, 21.9459) 
	/// Red.Pickup4;Pickup;1;(0.9000, 0.4000, 22.7000);(45.0800, 20.8186, 21.9459)
	/// 
	/// </summary>
	/// <param name="strI">The csv serialized world objects.</param>
	/// <returns>A List of CTworlds, parsed from the given string.</returns>
	private static List<CTworld> deserialize_csv(string strI)
	{
		List<CTworld> worlds = new List<CTworld>();
		String[] sworlds = strI.Split('#');                             // parse world-by-world

		// consolidate objects for each world
		foreach (String world in sworlds)
		{

//			Debug.Log("world: " + world);
			if (world.Length < 2) continue;
			String[] lines = world.Split('\n');
			String[] header = lines[0].Split(':');
			if (header.Length != 3) continue;
            
			CTworld CTW = new CTworld();
			CTW.objects = new Dictionary<String, CTobject>();

			CTW.mode = header[0];
			CTW.time = Double.Parse(header[1]);
			CTW.player = header[2];

			foreach (String line in lines)
			{
				// sanity checks:
				if (line.Length < 2 || line.StartsWith("<")) continue;  // skip empty & html lines
				String[] parts = line.Split(';');
				if (parts.Length < 3) continue;

				CTobject ctobject = new CTobject();
				ctobject.id = parts[0];
				ctobject.model = parts[1];
				ctobject.state = !parts[2].Equals("0");

				// parse ctobject.pos
				string pstate = parts[3].Substring(1, parts[3].Length - 2);     // drop parens
				string[] pvec = pstate.Split(',');
				ctobject.pos = new Vector3(float.Parse(pvec[0]), float.Parse(pvec[1]), float.Parse(pvec[2]));

				// parse ctobject.rot
				pstate = parts[4].Substring(1, parts[4].Length - 2);     // drop parens
				pvec = pstate.Split(',');
				ctobject.rot = Quaternion.Euler(float.Parse(pvec[0]), float.Parse(pvec[1]), float.Parse(pvec[2]));
                
				// custom field (used for indirect URL):
				if (parts.Length > 5) ctobject.link = parts[5];

                CTW.objects.Add(ctobject.id, ctobject);
			}

			worlds.Add(CTW);
		}

		return worlds;
	}

	/// <summary>
	/// Deserialize the given JSON string into a List of CTworld objects.
	/// The given string may contain one or a concatenation of several "world" objects.
	/// 
	/// JSON example:
	/// {"mode":"Live","time":1.536844452785E9,"name":"Blue","objects":[{"id":"Blue","prefab":"Ball","state":true,"pos":[-8.380356788635254,0.25,3.8628578186035156],"rot":[0.0,0.0,0.0],"custom":""}]}
	/// 
	/// </summary>
	/// <param name="strI">The JSON serialized world objects.</param>
	/// <returns>A List of CTworlds, parsed from the given string.</returns>
	private static List<CTworld> deserialize_json(string strI)
	{
		if ((strI == null) || (strI.Length < 10)) return null;

		List<CTworld> worlds = new List<CTworld>();

		// Break up the given string into different "world" objects
		// NOTE: This assumes that "player" is the first field in the JSON string
		List<int> indexes = AllIndexesOf(strI, @"{""player"":""");
		if (indexes.Count == 0) return null;
		for (int i = 0; i < indexes.Count; ++i)
		{
			int startIdx = indexes[i];
			int endIdx = strI.Length - 1;
			if (i < (indexes.Count - 1))
			{
				endIdx = indexes[i + 1];
			}
			string nextWorldStr = strI.Substring(startIdx, endIdx - startIdx);
			CTworldJson dataFromJson = null;
			try
			{
				dataFromJson = JsonUtility.FromJson<CTworldJson>(nextWorldStr);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.Log("Exception deserializing JSON: " + e.Message);
				continue;
			}
			if (dataFromJson == null || dataFromJson.objects == null)
			{
				continue;
			}
			// Create CTworld object from CTworldJson (these classes are very similar but there are differences, see definitions above)
			CTworld jCTW = new CTworld();
			jCTW.player = dataFromJson.player;
			jCTW.time = dataFromJson.time;
			jCTW.mode = dataFromJson.mode;
			jCTW.objects = new Dictionary<String, CTobject>();
			foreach (CTobjectJson ctobject in dataFromJson.objects)
			{
				CTobject cto = new CTobject();
				cto.id = ctobject.id;
				cto.model = ctobject.model;
				cto.state = ctobject.state;
				cto.link = ctobject.link;
				cto.pos = new Vector3((float)ctobject.pos[0], (float)ctobject.pos[1], (float)ctobject.pos[2]);
				cto.rot = Quaternion.Euler((float)ctobject.rot[0], (float)ctobject.rot[1], (float)ctobject.rot[2]);
				cto.scale = new Vector3((float)ctobject.scale[0], (float)ctobject.scale[1], (float)ctobject.scale[2]);
				cto.color = new Color((float)ctobject.color[0], (float)ctobject.color[1], (float)ctobject.color[2], (float)ctobject.color[3]);
				cto.custom = ctobject.custom;
				jCTW.objects.Add(cto.id, cto);
			}
			worlds.Add(jCTW);
		}

		if (worlds.Count == 0)
		{
			return null;
		}

		return worlds;
	}

	/// <summary>
	/// Create a serialized version of the player information.
	/// This method supports serializing to ".txt"/csv and JSON formats.
	/// </summary>
	/// <param name="ctunityI">Source of the information to be serialized.</param>
	/// <param name="formatI">Specifies the desired output serialization format.</param>
	/// <returns>Serialized player information.</returns>
	public static string serialize(CTunity ctunityI, Format formatI)
	{
		string serStr = null;
		if (formatI == Format.CSV)
		{
			serStr = serialize_csv(ctunityI);
		}
		else if (formatI == Format.JSON)
		{
			serStr = serialize_json(ctunityI);
		}
		return serStr;
	}

	/// <summary>
	/// Create a CSV serialized version of the player information.
	/// </summary>
	/// <param name="ctunityI">Source of the information to be serialized.</param>
	/// <returns>Serialized player information.</returns>
	private static string serialize_csv(CTunity ctunityI)
	{
		// header line:
		string CTstateString = "#" + ctunityI.replayText + ":" + ctunityI.ServerTime().ToString() + ":" + ctunityI.Player + "\n";

		string delim = ";";
		foreach (GameObject ct in ctunityI.CTlist.Values)
		{
			if (ct == null) continue;
			CTclient ctp = ct.GetComponent<CTclient>();
			if (ctp == null) continue;
			//			UnityEngine.Debug.Log("CTput: " + ct.name+", active: "+ct.activeSelf);
            
			String prefab = ctp.prefab;
			if (prefab.Equals("Ghost")) continue;                                   // no save ghosts												
//			if (!ctunityI.replayActive && !ct.name.StartsWith(ctunityI.Player)) continue;  // only save locally owned objects
			if (!ctunityI.doCTwrite(ct.name)) continue;          // only save locally owned objects
				
			CTstateString += ct.name;
			//            CTstateString += (delim + ct.tag);
			CTstateString += (delim + prefab);
			CTstateString += (delim + (ct.activeSelf ? "1" : "0"));
			CTstateString += (delim + ct.transform.localPosition.ToString("F4"));
			CTstateString += (delim + ct.transform.localRotation.eulerAngles.ToString("F4"));
			if (ctp.link != null && ctp.link.Length > 0) CTstateString += (delim + ctp.link);
			CTstateString += "\n";
		}

		return CTstateString;
	}

	/// <summary>
	/// Create a JSON serialized version of the player information.
	/// </summary>
	/// <param name="ctunityI">Source of the information to be serialized.</param>
	/// <returns>Serialized player information.</returns>
	private static string serialize_json(CTunity ctunityI)
	{

		CTworldJson world = new CTworldJson();
		world.player = ctunityI.Player;
		world.time = ctunityI.ServerTime();
		world.mode = ctunityI.replayText;
		world.objects = new List<CTobjectJson>();
		foreach (GameObject ct in ctunityI.CTlist.Values)
		{
			if (ct == null) continue;
			CTclient ctp = ct.GetComponent<CTclient>();
			if (ctp == null) continue;
			String prefab = ctp.prefab;
			if (prefab.Equals("Ghost")) continue;  // no save ghosts												
//			if (!ctunityI.replayActive && !ct.name.StartsWith(ctunityI.Player)) continue;  // only save locally owned objects
			if (!ctunityI.doCTwrite(ct.name)) continue;          // only save locally owned objects
            
			CTobjectJson obj = new CTobjectJson();
			obj.id = ct.name;
			obj.model = prefab;
			obj.state = (ct.activeSelf ? true : false);
			// NOTE: limit floating point values to 4 decimal places
			obj.pos = new List<Double>();
			obj.pos.Add(LimitPrecision(ct.transform.localPosition.x, 4));
			obj.pos.Add(LimitPrecision(ct.transform.localPosition.y, 4));
			obj.pos.Add(LimitPrecision(ct.transform.localPosition.z, 4));
			obj.rot = new List<Double>();
			obj.rot.Add(LimitPrecision(ct.transform.localRotation.eulerAngles.x, 4));
			obj.rot.Add(LimitPrecision(ct.transform.localRotation.eulerAngles.y, 4));
			obj.rot.Add(LimitPrecision(ct.transform.localRotation.eulerAngles.z, 4));
			obj.scale = new List<Double>();
            obj.scale.Add(LimitPrecision(ct.transform.localScale.x, 4));
            obj.scale.Add(LimitPrecision(ct.transform.localScale.y, 4));
            obj.scale.Add(LimitPrecision(ct.transform.localScale.z, 4));

			Renderer renderer = ct.transform.gameObject.GetComponent<Renderer>();
			if (renderer != null)
			{
//				Color mycolor = renderer.material.color;    // this NG for multi-part prefabs (e.g. biplane)
				Color mycolor = ctp.myColor;                // NG?
				obj.color = new List<Double>();
				obj.color.Add(LimitPrecision(mycolor.r, 4));
				obj.color.Add(LimitPrecision(mycolor.g, 4));
				obj.color.Add(LimitPrecision(mycolor.b, 4));
				obj.color.Add(LimitPrecision(mycolor.a, 4));
			}

			if (ctp.link != null && ctp.link.Length > 0) obj.link = ctp.link;
			if (ctp.custom != null && ctp.custom.Length > 0) obj.custom = ctp.custom;

			world.objects.Add(obj);
		}
		string jsonData = null;
		try
		{
			jsonData = JsonUtility.ToJson(world);
		}
		catch (Exception e)
		{
			UnityEngine.Debug.Log("Exception serializing JSON: " + e.Message);
			return null;
		}
		return jsonData;
	}

}
