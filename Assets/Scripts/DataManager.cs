using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DataManager : MonoBehaviour
{
    public int? TryGetData(string key)
    {
        if (intData.ContainsKey(key))
        {
            return intData[key];
        }

        return null;
    }

    public bool IsDataLoaded() => hasLoaded;

#if UNITY_EDITOR
    public Dictionary<string, int> DebugGetIntData()
    {
        return intData;
    }
#endif

    private Dictionary<string, int> intData = new Dictionary<string, int>();
    private bool hasLoaded = false;

    public void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
    }

    void Awake()
    {
        Service.Data = this;

        intData.Add("NumTimesPlayed", 0);
    }

    public void AddDataMember(string key, int def)
    {
        Debug.Assert(!hasLoaded); // Shouldn't call this after having loaded data in from player prefs
        intData.Add(key, def);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void LoadAll()
    {
        foreach (var data in intData)
        {
            intData[data.Key] = PlayerPrefs.GetInt(data.Key, intData[data.Key]);
        }

        hasLoaded = true;
    }

    void SaveAll()
    {
        foreach (var data in intData)
        {
            PlayerPrefs.SetInt(data.Key, intData[data.Key]);
        }
    }
    
}
