using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatterGuild.Core;

public sealed class LlmConnector : IDisposable
{
    private readonly LlmConfig _config;
    private readonly HttpClient _http;

    private LlmConnector(LlmConfig config)
    {
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public static LlmConnector TryCreate()
    {
        var config = LlmConfig.Load();
        if (config == null || !config.IsValid)
            return null;
        return new LlmConnector(config);
    }

    public string ProviderLabel => $"{_config.Provider}/{_config.Model}";

    public async Task<string> RequestReplyAsync(
        RoleType aiRole,
        string playerLast,
        List<(string role, string content)> history)
    {
        try
        {
            string systemPrompt = BuildSystemPrompt(aiRole);
            var request = new HttpRequestMessage(HttpMethod.Post, _config.Endpoint);
            request.Headers.Add("Accept", "application/json");

            string jsonBody;

            if (_config.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
                jsonBody = BuildOpenAIBody(systemPrompt, playerLast, history);
            }
            else if (_config.Provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Add("x-api-key", _config.ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                jsonBody = BuildAnthropicBody(systemPrompt, playerLast, history);
            }
            else
            {
                return null;
            }

            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            string responseJson = await response.Content.ReadAsStringAsync();
            return ExtractReplyText(responseJson);
        }
        catch
        {
            return null;
        }
    }

    private string BuildOpenAIBody(string systemPrompt, string playerLast, List<(string role, string content)> history)
    {
        var messages = new List<object> { new { role = "system", content = systemPrompt } };

        foreach (var (role, content) in history)
            messages.Add(new { role, content });

        messages.Add(new { role = "user", content = playerLast });

        var body = new
        {
            model = _config.Model,
            max_tokens = _config.MaxTokens,
            temperature = _config.Temperature,
            messages
        };

        return JsonSerializer.Serialize(body);
    }

    private string BuildAnthropicBody(string systemPrompt, string playerLast, List<(string role, string content)> history)
    {
        var messages = new List<object>();

        foreach (var (role, content) in history)
            messages.Add(new { role, content });

        messages.Add(new { role = "user", content = playerLast });

        var body = new
        {
            model = _config.Model,
            max_tokens = _config.MaxTokens,
            temperature = _config.Temperature,
            system = systemPrompt,
            messages
        };

        return JsonSerializer.Serialize(body);
    }

    private string ExtractReplyText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (_config.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    return choices[0].GetProperty("message").GetProperty("content").GetString();
            }
            else if (_config.Provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("content", out var content) && content.GetArrayLength() > 0)
                    return content[0].GetProperty("text").GetString();
            }
        }
        catch { }

        return null;
    }

    private static string BuildSystemPrompt(RoleType role)
    {
        string roleDesc = role switch
        {
            RoleType.Initiator =>
                "You are the Initiator. You bring up new topics, propose ideas, and drive the conversation forward. " +
                "Ask open-ended questions about interesting topics. Share your own perspectives to spark discussion. " +
                "When the conversation stalls, introduce a fresh angle.",
            RoleType.Listener =>
                "You are the Listener. You focus on understanding and validating what the other person says. " +
                "Reflect their ideas back, ask clarifying follow-up questions, and show that you genuinely hear them. " +
                "Use phrases like 'tell me more' or 'what matters most to you about that'. Avoid changing the subject.",
            RoleType.Challenger =>
                "You are the Challenger. You respectfully push back on ideas, ask for evidence, and test assumptions. " +
                "Play devil's advocate when appropriate. Ask 'why' and 'what if the opposite were true'. " +
                "Never be hostile -- be intellectually curious and constructively skeptical.",
            RoleType.Synthesizer =>
                "You are the Synthesizer. You connect ideas, find common threads, and summarize what has been discussed. " +
                "Use phrases like 'it sounds like', 'in other words', and 'to bring these together'. " +
                "Help the conversation reach deeper understanding by linking concepts.",
            RoleType.Explorer =>
                "You are the Explorer. You are curious and adventurous in conversation. " +
                "Ask unexpected questions, make creative connections between topics, and venture into new territory. " +
                "Be enthusiastic about discovering new ideas together.",
            _ => "You are a conversational partner."
        };

        return $"""
            You are a conversational training partner in a communication skills game called ChatterGuild.
            You are playing the role of {role}.

            {roleDesc}

            Guidelines:
            - Keep responses to 1-3 sentences. Be concise.
            - Stay in character for your role.
            - Do not mention that you are an AI.
            - Respond naturally as a human conversation partner would.
            """;
    }

    public void Dispose() => _http?.Dispose();
}
