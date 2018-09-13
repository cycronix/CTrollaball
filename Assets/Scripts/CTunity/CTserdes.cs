
using System;
using System.Collections;
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
        public string name;
        public double time;
        public string mode;
        public List<CTobjectJson> objects;
    }
    [Serializable]
    public class CTobjectJson
    {
        public string id;
        public string prefab;
        public Boolean state;
        public List<Double> pos;
        public List<Double> rot;
        public string custom;
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

        return worlds;
    }

    /// <summary>
    /// Deserialize the given JSON string into a List of CTworld objects.
    /// 
    /// JSON example:
    /// {"mode":"Live","time":1.536844452785E9,"name":"Blue","objects":[{"id":"Blue","prefab":"Ball","state":true,"pos":[-8.380356788635254,0.25,3.8628578186035156],"rot":[0.0,0.0,0.0],"custom":""}]}
    /// 
    /// </summary>
    /// <param name="strI">The JSON serialized world objects.</param>
    /// <returns>A List of CTworlds, parsed from the given string.</returns>
    private static List<CTworld> deserialize_json(string strI)
    {
        CTworldJson dataFromJson = null;
        try
        {
            dataFromJson = JsonUtility.FromJson<CTworldJson>(strI);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Exception parsing JSON: " + e.Message);
            return null;
        }
        if (dataFromJson == null || dataFromJson.objects == null)
        {
            return null;
        }
        // Create CTworld object from CTworldJson (these classes are very similar but there are differences, see definitions above)
        CTworld jCTW = new CTworld();
        jCTW.name = dataFromJson.name;
        jCTW.time = dataFromJson.time;
        jCTW.mode = dataFromJson.mode;
        jCTW.objects = new Dictionary<String, CTobject>();
        foreach (CTobjectJson ctobject in dataFromJson.objects)
        {
            CTobject cto = new CTobject();
            cto.id = ctobject.id;
            cto.prefab = ctobject.prefab;
            cto.state = ctobject.state;
            cto.custom = ctobject.custom;
            cto.pos = new Vector3((float)ctobject.pos[0], (float)ctobject.pos[1], (float)ctobject.pos[2]);
            cto.rot = Quaternion.Euler((float)ctobject.rot[0], (float)ctobject.rot[1], (float)ctobject.rot[2]);
            jCTW.objects.Add(cto.id, cto);
        }
        List<CTworld> worlds = new List<CTworld>();
        worlds.Add(jCTW);
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
            if (!ctunityI.replayActive && !ct.name.StartsWith(ctunityI.Player)) continue;  // only save locally owned objects

            CTstateString += ct.name;
            //            CTstateString += (delim + ct.tag);
            CTstateString += (delim + prefab);
            CTstateString += (delim + (ct.activeSelf ? "1" : "0"));
            CTstateString += (delim + ct.transform.localPosition.ToString("F4"));
            CTstateString += (delim + ct.transform.localRotation.eulerAngles.ToString("F4"));
            if (ctp.custom != null && ctp.custom.Length > 0) CTstateString += (delim + ctp.custom);
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
        string serStr = null;

        return serStr;
    }

}
