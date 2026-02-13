using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatterGuild.Core;

public static class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Conversation Lab (Console Training MVP) ===");
        var profile = SaveLoad.LoadOrCreate();
        var rng = new Random();

        using var llm = LlmConnector.TryCreate();
        bool useLlm = llm != null;
        if (useLlm)
            Console.WriteLine($"LLM mode: active ({llm.ProviderLabel})");
        else
            Console.WriteLine("LLM mode: off (using local AI responder)");

        Console.WriteLine($"Loaded Profile: Level {profile.Level}, TotalXP {profile.TotalXP}");
        Console.WriteLine($"Current Class: {Archetypes.GetClassName(profile.Initiator, profile.Listener, profile.Challenger, profile.Synthesizer, profile.Explorer)}");
        Console.WriteLine();

        // Mode selection
        Console.WriteLine("Choose mode:");
        Console.WriteLine("  1) You type BOTH sides (manual sparring)");
        Console.WriteLine("  2) You vs AI partner");
        Console.Write("Enter 1 or 2: ");
        string mode = (Console.ReadLine() ?? "").Trim();
        bool manualBoth = mode == "1";

        // Role selection
        RoleType playerRole = RoleType.Initiator;
        RoleType partnerRole = RoleType.Listener;

        Console.WriteLine();
        Console.WriteLine("Choose session roles (optional). Press Enter to accept defaults.");
        playerRole = ReadRoleOrDefault("Your role", playerRole);
        partnerRole = ReadRoleOrDefault("Partner role", partnerRole);

        Console.WriteLine();
        Console.WriteLine($"Session Roles: You={playerRole}  Partner={partnerRole}");
        Console.WriteLine("Type 'end' to finish session.\n");

        var report = new SessionReport();
        var recentTokens = new HashSet<string>();
        var history = new List<(string role, string content)>();

        string lastYou = "";
        string lastPartner = "";

        int turn = 0;
        while (true)
        {
            // YOU
            Console.Write("You: ");
            string you = Console.ReadLine() ?? "";
            if (you.Trim().Equals("end", StringComparison.OrdinalIgnoreCase)) break;

            var rYou = ScoringEngine.ScoreTurn(playerRole, you, lastPartner, recentTokens);
            report.Add("You", you, playerRole, rYou);
            BehaviorHeuristics.UpdateRecentTokens(recentTokens, you);
            lastYou = you;

            PrintTurnScore("You", playerRole, rYou);

            // PARTNER
            string partner;
            if (manualBoth)
            {
                Console.Write("Partner: ");
                partner = Console.ReadLine() ?? "";
                if (partner.Trim().Equals("end", StringComparison.OrdinalIgnoreCase)) break;
            }
            else if (useLlm)
            {
                Console.Write("Partner (thinking...): ");
                partner = await llm.RequestReplyAsync(partnerRole, lastYou, history);
                if (partner == null)
                {
                    partner = AIResponder.Reply(partnerRole, lastYou, rng);
                    Console.WriteLine($"[fallback] {partner}");
                }
                else
                {
                    Console.WriteLine(partner);
                }
            }
            else
            {
                partner = AIResponder.Reply(partnerRole, lastYou, rng);
                Console.WriteLine($"Partner: {partner}");
            }

            history.Add(("user", you));
            history.Add(("assistant", partner));

            var rP = ScoringEngine.ScoreTurn(partnerRole, partner, lastYou, recentTokens);
            report.Add("Partner", partner, partnerRole, rP);
            BehaviorHeuristics.UpdateRecentTokens(recentTokens, partner);
            lastPartner = partner;

            PrintTurnScore("Partner", partnerRole, rP);

            turn++;
            Console.WriteLine($"--- Session IP so far: {report.TotalIP} ---\n");
        }

        Console.WriteLine("\n=== SESSION REPORT ===");
        Console.WriteLine($"Turns: {report.Turns.Count}  |  Total IP: {report.TotalIP}");

        var (i, l, c, s, e) = report.ArchetypeVector();
        Console.WriteLine("\nArchetype Evidence (this session):");
        Console.WriteLine($"  Initiator:   {i:0.00}");
        Console.WriteLine($"  Listener:    {l:0.00}");
        Console.WriteLine($"  Challenger:  {c:0.00}");
        Console.WriteLine($"  Synthesizer: {s:0.00}");
        Console.WriteLine($"  Explorer:    {e:0.00}");

        Console.WriteLine($"\nClass (session evidence): {Archetypes.GetClassName(i, l, c, s, e)}");

        // Apply rewards to profile
        int xp = report.TotalIP;
        profile.AddXP(xp, xp / 4, xp / 4, xp / 4, xp / 4);
        profile.BlendArchetype(i, l, c, s, e, 0.15);

        SaveLoad.Save(profile);

        Console.WriteLine("\n=== UPDATED PROFILE ===");
        Console.WriteLine($"Level: {profile.Level}  | TotalXP: {profile.TotalXP}  | NextLevelXP: {PlayerProfile.XPRequiredFor(profile.Level + 1)}");
        Console.WriteLine($"Class: {Archetypes.GetClassName(profile.Initiator, profile.Listener, profile.Challenger, profile.Synthesizer, profile.Explorer)}");
        Console.WriteLine($"Saved to: {SaveLoad.FilePath}");
    }

    static RoleType ReadRoleOrDefault(string label, RoleType def)
    {
        Console.WriteLine($"{label} options: Initiator, Listener, Challenger, Synthesizer, Explorer");
        Console.Write($"{label} [{def}]: ");
        string s = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrEmpty(s)) return def;

        if (Enum.TryParse<RoleType>(s, true, out var role)) return role;
        Console.WriteLine("Invalid role; using default.");
        return def;
    }

    static void PrintTurnScore(string who, RoleType role, ScoringResult r)
    {
        Console.WriteLine($"  => {who} Role={role} Beh[OC={r.Behavior.OC},IN={r.Behavior.IN},IQ={r.Behavior.IQ},RF={r.Behavior.RF}] Raw={r.Raw} Norm={r.Norm:0.00} IP=+{r.InsightPoints}");
    }
}
