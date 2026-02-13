using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatterGuild.Core;

public sealed class LlmConfig
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 300;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(Provider)
        && !string.IsNullOrWhiteSpace(Endpoint);

    public static LlmConfig Load()
    {
        string[] searchPaths = {
            Path.Combine(AppContext.BaseDirectory, "llm_config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "llm_config.json"),
        };

        foreach (var path in searchPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var config = JsonSerializer.Deserialize<LlmConfig>(json);
                    if (config != null && config.IsValid)
                        return config;
                }
            }
            catch { }
        }

        return null;
    }
}
