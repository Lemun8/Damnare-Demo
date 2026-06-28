using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global persistent world state (defeated enemies, looted tiles, flags).
/// Stores a dictionary<string,bool> to PlayerPrefs as JSON.
/// </summary>
public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance;

    private Dictionary<string, bool> worldFlags = new();

    private const string SAVE_KEY = "WORLD_STATE_V1"; // bump version if structure changes

    private Dictionary<string, Vector3> worldPositions = new();
    private const string POS_KEY = "WORLD_POSITIONS_V1";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWorldState();
            LoadWorldPositions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool GetFlag(string key)
    {
        return worldFlags.ContainsKey(key) && worldFlags[key];
    }

    public void SetFlag(string key, bool value)
    {
        worldFlags[key] = value;
        SaveWorldState();
    }

    public void SetPosition(string key, Vector3 pos)
    {
        worldPositions[key] = pos;
        SaveWorldPositions();
    }

    public bool TryGetPosition(string key, out Vector3 pos)
    {
        return worldPositions.TryGetValue(key, out pos);
    }

    private void SaveWorldState()
    {
        var wrapper = new SerializationWrapper(worldFlags);
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadWorldState()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        string json = PlayerPrefs.GetString(SAVE_KEY);
        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
        if (wrapper != null)
            worldFlags = wrapper.ToDictionary();
    }

    private void SaveWorldPositions()
    {
        var wrapper = new PositionWrapper(worldPositions);
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(POS_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadWorldPositions()
    {
        if (!PlayerPrefs.HasKey(POS_KEY)) return;
        string json = PlayerPrefs.GetString(POS_KEY);
        var wrapper = JsonUtility.FromJson<PositionWrapper>(json);
        if (wrapper != null)
            worldPositions = wrapper.ToDictionary();
    }

    public void ClearAll()
    {
        worldFlags.Clear();
        worldPositions.Clear();

        PlayerPrefs.DeleteKey("WORLD_STATE_V1");
        PlayerPrefs.DeleteKey("WORLD_POSITIONS_V1");

        PlayerPrefs.Save();
    }

    [System.Serializable]
    public class SerializationWrapper
    {
        public List<string> keys = new();
        public List<bool> values = new();

        public SerializationWrapper() { }

        public SerializationWrapper(Dictionary<string, bool> dict)
        {
            foreach (var kv in dict)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public Dictionary<string, bool> ToDictionary()
        {
            var dict = new Dictionary<string, bool>();
            for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
                dict[keys[i]] = values[i];
            return dict;
        }
    }

    [System.Serializable]
    public class PositionWrapper
    {
        public List<string> keys = new();
        public List<Vector3> values = new();

        public PositionWrapper() { }

        public PositionWrapper(Dictionary<string, Vector3> dict)
        {
            foreach (var kv in dict)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public Dictionary<string, Vector3> ToDictionary()
        {
            var dict = new Dictionary<string, Vector3>();
            for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
                dict[keys[i]] = values[i];
            return dict;
        }
    }
}
