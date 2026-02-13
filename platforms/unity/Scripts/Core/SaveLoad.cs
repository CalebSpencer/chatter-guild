using System.IO;
using UnityEngine;

public static class SaveLoad
{
    static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "player_profile.json");

    public static PlayerProfile LoadOrCreate()
    {
        try
        {
            if (File.Exists(PathFile))
            {
                string json = File.ReadAllText(PathFile);
                var p = JsonUtility.FromJson<PlayerProfile>(json);
                return p ?? new PlayerProfile();
            }
        }
        catch { /* ignore */ }

        return new PlayerProfile();
    }

    public static void Save(PlayerProfile profile)
    {
        try
        {
            string json = JsonUtility.ToJson(profile, true);
            File.WriteAllText(PathFile, json);
        }
        catch { /* ignore */ }
    }
}