using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingChatController : MonoBehaviour
{
    public ChatUI chatUI;
    public AIResponder ai;

    public string PlayerName = "Tyler";
    public string AIName = "CoachBot";

    public RoleType playerRole = RoleType.Initiator;
    public RoleType aiRole = RoleType.Listener;

    PlayerProfile profile;
    SessionReport report;
    HashSet<string> recentTokens = new HashSet<string>();

    string lastPlayer = "";
    string lastAI = "";

    void Start()
    {
        profile = SaveLoad.LoadOrCreate();
        report = new SessionReport();

        chatUI.SetHeader($"Level {profile.Level} • Class: {Archetypes.GetClassName(profile.Initiator, profile.Listener, profile.Challenger, profile.Synthesizer, profile.Explorer)}");
        chatUI.OnSend += OnPlayerSend;

        chatUI.AddSystem("Training Mode: have a conversation. End anytime to see your report.");
        chatUI.AddSystem($"Roles this session: You={playerRole}  AI={aiRole}");
    }

    void OnDestroy()
    {
        chatUI.OnSend -= OnPlayerSend;
    }

    public void EndSession()
    {
        // Apply rewards
        float i,l,c,s,e;
        report.GetArchetypeVector(out i, out l, out c, out s, out e);

        // Translate IP to XP (simple)
        int xp = report.TotalIP;
        profile.AddXP(xp, xp/4, xp/4, xp/4, xp/4);
        profile.BlendArchetype(i,l,c,s,e, 0.15f);

        SaveLoad.Save(profile);

        SessionCache.LastReport = report; // pass to report scene
        SceneManager.LoadScene("ReportScene");
    }

    void OnPlayerSend(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Player message
        chatUI.AddMessage(PlayerName, text);

        var r1 = ScoringEngine.ScoreTurn(playerRole, text, lastAI, recentTokens);
        report.Add(PlayerName, text, playerRole, r1);
        BehaviorHeuristics.UpdateRecentTokens(recentTokens, text);
        lastPlayer = text;

        // AI reply
        string reply = ai.GetReply(aiRole, lastPlayer);
        chatUI.AddMessage(AIName, reply);

        var r2 = ScoringEngine.ScoreTurn(aiRole, reply, lastPlayer, recentTokens);
        report.Add(AIName, reply, aiRole, r2);
        BehaviorHeuristics.UpdateRecentTokens(recentTokens, reply);
        lastAI = reply;

        chatUI.SetFooter($"Session IP: {report.TotalIP}  •  Recent turn: You +{r1.InsightPoints} IP");
    }
}

public static class SessionCache
{
    public static SessionReport LastReport;
}