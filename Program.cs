using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public enum RoleType { Initiator, Listener, Challenger, Synthesizer, Explorer }

public struct BehaviorVector
{
    public int OC, IN, IQ, RF;
}

public sealed class ScoringResult
{
    public BehaviorVector Behavior;
    public int Raw;
    public double Norm;
    public int InsightPoints;
    public int ClarityXP, IntegrationXP, DepthXP, AdaptabilityXP;
    public double I, L, C, S, E; // archetype evidence
}

public sealed class TurnRecord
{
    public string Speaker = "";
    public string Text = "";
    public RoleType Role;
    public int Raw;
    public double Norm;
    public int IP;
    public int OC, IN, IQ, RF;
}

public sealed class SessionReport
{
    public List<TurnRecord> Turns = new();
    public int TotalIP = 0;

    public double SumI, SumL, SumC, SumS, SumE;

    public void Add(string speaker, string text, RoleType role, ScoringResult r)
    {
        Turns.Add(new TurnRecord
        {
            Speaker = speaker,
            Text = text,
            Role = role,
            Raw = r.Raw,
            Norm = r.Norm,
            IP = r.InsightPoints,
            OC = r.Behavior.OC,
            IN = r.Behavior.IN,
            IQ = r.Behavior.IQ,
            RF = r.Behavior.RF
        });

        TotalIP += r.InsightPoints;
        SumI += r.I; SumL += r.L; SumC += r.C; SumS += r.S; SumE += r.E;
    }

    public (double i, double l, double c, double s, double e) ArchetypeVector()
    {
        double sum = SumI + SumL + SumC + SumS + SumE;
        if (sum <= 1e-9) return (0.2, 0.2, 0.2, 0.2, 0.2);
        return (SumI / sum, SumL / sum, SumC / sum, SumS / sum, SumE / sum);
    }
}

public sealed class PlayerProfile
{
    public int Level { get; set; } = 1;
    public int TotalXP { get; set; } = 0;

    public int ClarityXP { get; set; } = 0;
    public int IntegrationXP { get; set; } = 0;
    public int DepthXP { get; set; } = 0;
    public int AdaptabilityXP { get; set; } = 0;

    public double Initiator { get; set; } = 0.2;
    public double Listener { get; set; } = 0.2;
    public double Challenger { get; set; } = 0.2;
    public double Synthesizer { get; set; } = 0.2;
    public double Explorer { get; set; } = 0.2;

    public void AddXP(int xp, int clarity, int integration, int depth, int adaptability)
    {
        TotalXP += xp;
        ClarityXP += clarity;
        IntegrationXP += integration;
        DepthXP += depth;
        AdaptabilityXP += adaptability;
        RecomputeLevel();
    }

    public void BlendArchetype(double i, double l, double c, double s, double e, double alpha = 0.15)
    {
        Initiator = Lerp(Initiator, i, alpha);
        Listener = Lerp(Listener, l, alpha);
        Challenger = Lerp(Challenger, c, alpha);
        Synthesizer = Lerp(Synthesizer, s, alpha);
        Explorer = Lerp(Explorer, e, alpha);
        NormalizeArchetype();
    }

    public void NormalizeArchetype()
    {
        double sum = Initiator + Listener + Challenger + Synthesizer + Explorer;
        if (sum <= 1e-9)
        {
            Initiator = Listener = Challenger = Synthesizer = Explorer = 0.2;
            return;
        }
        Initiator /= sum; Listener /= sum; Challenger /= sum; Synthesizer /= sum; Explorer /= sum;
    }

    public static int XPRequiredFor(int targetLevel)
    {
        // gentle ramp
        int d = targetLevel - 1;
        return 100 + d * d * 60;
    }

    void RecomputeLevel()
    {
        int req = XPRequiredFor(Level + 1);
        while (TotalXP >= req)
        {
            Level++;
            req = XPRequiredFor(Level + 1);
        }
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * t;
}

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

public static class Archetypes
{
    public static string GetClassName(double i, double l, double c, double s, double e)
    {
        double[] v = { i, l, c, s, e };
        int top1 = 0, top2 = 1;

        for (int k = 0; k < v.Length; k++)
        {
            if (v[k] > v[top1]) { top2 = top1; top1 = k; }
            else if (k != top1 && v[k] > v[top2]) { top2 = k; }
        }

        string a = CoreName(top1);
        string b = CoreName(top2);

        if ((a == "Initiator" && b == "Explorer") || (a == "Explorer" && b == "Initiator")) return "Trailblazer";
        if ((a == "Listener" && b == "Synthesizer") || (a == "Synthesizer" && b == "Listener")) return "Harmonizer";
        if ((a == "Challenger" && b == "Synthesizer") || (a == "Synthesizer" && b == "Challenger")) return "Dialectician";
        if ((a == "Explorer" && b == "Listener") || (a == "Listener" && b == "Explorer")) return "Curious Companion";
        if ((a == "Initiator" && b == "Challenger") || (a == "Challenger" && b == "Initiator")) return "Provocateur";

        return a;
    }

    static string CoreName(int idx)
    {
        return idx switch
        {
            0 => "Initiator",
            1 => "Listener",
            2 => "Challenger",
            3 => "Synthesizer",
            _ => "Explorer",
        };
    }
}

public static class BehaviorHeuristics
{
    public static BehaviorVector Extract(string msg, string partnerLast, HashSet<string> recentTokens)
    {
        string m = (msg ?? "").ToLowerInvariant();
        string pl = (partnerLast ?? "").ToLowerInvariant();

        bool partnerAsked = pl.Contains("?");
        bool iAsked = m.Contains("?");
        int iq = iAsked ? 1 : 0;

        int rf = 0;
        if (m.StartsWith("so ") || m.Contains("it sounds like") || m.Contains("in other words") || m.Contains("to summarize"))
            rf = 1;

        bool cue = (m.Contains("you said") || m.Contains("you mentioned") || m.Contains("that makes sense") || m.Contains("i hear you") || m.Contains("good point"));
        bool answered = DetectAnswer(m);

        int inn = 0;
        int overlap = OverlapScore(m, pl);
        if (cue) inn = 1;
        else if (partnerAsked && answered) inn = 1;
        else if (overlap >= 2 && answered) inn = 1;

        int oc = 0;
        if (m.Length >= 60) oc++;
        if (m.Contains("because") || m.Contains("for example") || m.Contains("in my experience")) oc++;
        if (NovelTokenCount(m, recentTokens) >= 4) oc++;

        if (partnerAsked && answered) oc++;
        if (partnerAsked && !answered && iAsked) oc--;

        return new BehaviorVector
        {
            OC = Clamp02(oc),
            IN = Clamp02(inn),
            IQ = Clamp02(iq),
            RF = Clamp02(rf),
        };
    }

    public static void UpdateRecentTokens(HashSet<string> recent, string msg)
    {
        string m = (msg ?? "").ToLowerInvariant();
        if (recent.Count > 220) recent.Clear();

        foreach (var raw in m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string tok = CleanToken(raw);
            if (tok.Length >= 4) recent.Add(tok);
        }
    }

    static bool DetectAnswer(string m)
    {
        if (m.Contains("because")) return true;
        if (m.Contains("for me")) return true;
        if (m.Contains("i think")) return true;
        if (m.Contains("i feel")) return true;
        if (m.Contains("my favorite")) return true;
        if (m.Contains("i prefer")) return true;
        if (m.Contains("i like")) return true;
        if (m.Contains("i have")) return true;
        if (m.Contains("i'm ") || m.Contains("im ")) return true;

        foreach (char ch in m)
            if (ch >= '0' && ch <= '9') return true;

        return false;
    }

    static int OverlapScore(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        var ta = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int count = 0;
        for (int i = 0; i < ta.Length; i++)
        {
            string x = CleanToken(ta[i]);
            if (x.Length < 4) continue;

            for (int j = 0; j < tb.Length; j++)
            {
                string y = CleanToken(tb[j]);
                if (x == y && x.Length >= 4) { count++; break; }
            }
        }
        return count;
    }

    static int NovelTokenCount(string msgLower, HashSet<string> recent)
    {
        int novel = 0;
        foreach (var raw in msgLower.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string tok = CleanToken(raw);
            if (tok.Length < 4) continue;
            if (!recent.Contains(tok)) novel++;
        }
        return novel;
    }

    static string CleanToken(string s)
    {
        if (s == null) return "";
        s = s.Trim().Trim(',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>');
        return s;
    }

    static int Clamp02(int v) => v < 0 ? 0 : (v > 2 ? 2 : v);
}

public static class ScoringEngine
{
    static readonly Dictionary<RoleType, (int OC, int IN, int IQ, int RF)> RoleWeights = new()
    {
        { RoleType.Initiator,   (3,1,2,1) },
        { RoleType.Listener,    (0,3,2,3) },
        { RoleType.Challenger,  (2,1,3,1) },
        { RoleType.Synthesizer, (1,3,1,3) },
        { RoleType.Explorer,    (2,1,3,1) },
    };

    static readonly Dictionary<RoleType, int> Count = new();
    static readonly Dictionary<RoleType, int> SumRaw = new();

    public static ScoringResult ScoreTurn(RoleType role, string msg, string partnerLast, HashSet<string> recentTokens)
    {
        if (!Count.ContainsKey(role)) { Count[role] = 0; SumRaw[role] = 0; }

        var b = BehaviorHeuristics.Extract(msg, partnerLast, recentTokens);
        var w = RoleWeights[role];

        int raw = b.OC * w.OC + b.IN * w.IN + b.IQ * w.IQ + b.RF * w.RF;

        Count[role] += 1;
        SumRaw[role] += raw;

        double expected = (double)SumRaw[role] / Math.Max(1, Count[role]);
        expected = Math.Max(1.0, expected);

        double norm = raw / expected;
        norm = Clamp(norm, 0.0, 2.5);

        // Convert to RPG-ish stat XP (simple for now)
        int clarity = (int)Math.Round(3.0 * (b.OC + b.IQ));
        int integration = (int)Math.Round(4.0 * b.IN);
        int depth = (int)Math.Round(4.0 * b.RF);
        int adaptability = (int)Math.Round(3.0 * (b.IN + b.IQ)); // placeholder

        int ip = (int)Clamp(Math.Round(10.0 * norm), 0, 30);

        // Archetype evidence
        double I = (role == RoleType.Initiator ? 1.0 : 0.0) + 0.5 * b.OC + 0.25 * b.IQ;
        double L = (role == RoleType.Listener ? 1.0 : 0.0) + 0.6 * b.IN + 0.4 * b.RF;
        double C = (role == RoleType.Challenger ? 1.0 : 0.0) + 0.6 * b.IQ + 0.2 * b.OC;
        double S = (role == RoleType.Synthesizer ? 1.0 : 0.0) + 0.7 * b.RF + 0.4 * b.IN;
        double E = (role == RoleType.Explorer ? 1.0 : 0.0) + 0.7 * b.IQ + 0.2 * b.OC;

        return new ScoringResult
        {
            Behavior = b,
            Raw = raw,
            Norm = norm,
            InsightPoints = ip,
            ClarityXP = clarity,
            IntegrationXP = integration,
            DepthXP = depth,
            AdaptabilityXP = adaptability,
            I = I, L = L, C = C, S = S, E = E
        };
    }

    static double Clamp(double x, double lo, double hi) => x < lo ? lo : (x > hi ? hi : x);
}

public static class AIResponder
{
    static readonly string[] Topics = { "movies","music","work","burnout","kids","gaming","habits","goals","friendship","travel" };

    public static string Reply(RoleType role, string playerLast, Random rng)
    {
        string topic = Topics[rng.Next(Topics.Length)];
        bool asked = (playerLast ?? "").Contains("?");

        string Pick(params string[] arr) => arr[rng.Next(arr.Length)];

        if (role == RoleType.Listener)
        {
            return asked
                ? Pick("That’s fair. What makes you ask?",
                       "I hear you. What part matters most to you?",
                       "I can answer, but what’s your take first?")
                : Pick("That makes sense. Tell me more.",
                       "Good point—when did you first notice that?",
                       "What’s the best example you’ve seen?");
        }

        if (role == RoleType.Challenger)
        {
            return asked
                ? Pick("Before I answer, why does that matter to you?",
                       "What do you mean exactly—can you define it?",
                       "Are we assuming that’s true, or testing it?")
                : Pick("What if the opposite is true?",
                       "What evidence would change your mind?",
                       "Is there a simpler explanation?");
        }

        // Initiator / Synthesizer / Explorer (simple MVP)
        return asked
            ? Pick($"Good question. I think {topic} matters because it shapes choices.",
                   $"Honestly, {topic} affects my energy more than I expected.",
                   $"For me, {topic} changes how I show up around people.")
            : Pick($"Let’s talk about {topic}. What’s your experience?",
                   $"I’ve been thinking about {topic} lately—what do you think?",
                   $"Topic idea: {topic}. Are you into that?");
    }
}

public static class Program
{
    static void Main()
    {
        Console.WriteLine("=== Conversation Lab (Console Training MVP) ===");
        var profile = SaveLoad.LoadOrCreate();
        var rng = new Random();

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

        // Role selection (keep simple)
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
            else
            {
                partner = AIResponder.Reply(partnerRole, lastYou, rng);
                Console.WriteLine($"Partner: {partner}");
            }

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

        // Apply rewards to profile (simple: XP == IP)
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