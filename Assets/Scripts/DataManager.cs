using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public void TrySetData(string key, int data)
    {
        if (intData.ContainsKey(key))
        {
            intData[key] = data;
        }

        SaveAll();
    }

    public bool IsDataLoaded() => hasLoaded;

#if UNITY_EDITOR
    public Dictionary<string, int> DebugGetIntData()
    {
        return intData;
    }
#endif

    private Dictionary<string, int> intData = new Dictionary<string, int>();
    private Dictionary<string, int> intDataDefaults = new Dictionary<string, int>();
    private bool hasLoaded = false;

    public void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
        LoadAll();
    }

    void Awake()
    {
        Service.Data = this;
    }

    public void AddDataMember(string key, int def)
    {
        Debug.Assert(!hasLoaded); // Shouldn't call this after having loaded data in from player prefs
        intData.Add(key, def);
        intDataDefaults.Add(key, def);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void LoadAll()
    {
        var keysToIterate = intData.Keys.ToList();
        foreach (var key in keysToIterate)
        {
            intData[key] = PlayerPrefs.GetInt(key, intDataDefaults[key]);
        }

        hasLoaded = true;
    }

    public void SaveAll()
    {
        foreach (var data in intData)
        {
            PlayerPrefs.SetInt(data.Key, intData[data.Key]);
        }
    }
    
}
