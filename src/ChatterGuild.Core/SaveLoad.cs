using System;
using System.IO;
using System.Text.Json;

namespace ChatterGuild.Core;

public static class SaveLoad
{
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "player_profile.json");

    public static PlayerProfile LoadOrCreate()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var p = JsonSerializer.Deserialize<PlayerProfile>(json);
                if (p != null) { p.NormalizeArchetype(); return p; }
            }
        }
        catch { }
        return new PlayerProfile();
    }

    public static void Save(PlayerProfile profile)
    {
        try
        {
            string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}
