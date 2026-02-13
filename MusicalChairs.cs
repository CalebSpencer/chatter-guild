using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

// =======================
// Musical Chairs Mode (Console)
// 2 users | 3 roles | random role shifts | strict turn-taking
// Visual cues via rhythm bar + STOP banner
// =======================

public enum RoleType { Initiator, Listener, Challenger }

public struct BehaviorVector { public int OC, IN, IQ, RF; }

public sealed class ScoringResult
{
    public BehaviorVector B;
    public int Raw;
    public double Norm;
    public int IP;
}

public sealed class Player
{
    public string Name;
    public RoleType Role;
    public int SetsWon = 0;
    public int TotalIP = 0;

    // Archetype evidence (3-role version)
    public double SumI, SumL, SumC;

    public Player(string name) { Name = name; }
}

public static class Program
{
    // ---- Settings ----
    const int TurnsPerRound = 8;         // total turns in a round (both players combined)
    const int SetsToWin = 3;
    const int MinMusicMs = 2500;         // how long until STOP can happen (random)
    const int MaxMusicMs = 6500;

    // "role normalization" (mean raw) per role across the whole run
    static readonly Dictionary<RoleType, int> RoleCount = new();
    static readonly Dictionary<RoleType, int> RoleSumRaw = new();

    static readonly Random Rng = new();

    // anti-repetition memory (optional for AI; kept for extension)
    // ---- Entry ----
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("=== Chatter’s Guild: Musical Chairs (Console) ===");
        Console.WriteLine("2 users | 3 roles | strict turn-taking | random role shifts");
        Console.WriteLine("Type 'end' at any prompt to stop.\n");

        // Names
        string p1Name = ReadNonEmpty("Player 1 name", "U1");
        string p2Name = ReadNonEmpty("Player 2 name", "U2");

        var p1 = new Player(p1Name);
        var p2 = new Player(p2Name);

        // Initial roles
        AssignRandomRolesNoRepeat(new[] { p1, p2 }, null);
        PrintRoundHeader(1, p1, p2);

        int round = 1;
        while (p1.SetsWon < SetsToWin && p2.SetsWon < SetsToWin)
        {
            var roundResult = RunOneRound(p1, p2, round);
            if (!roundResult.Completed) break;

            // Award set to higher IP in this round
            if (roundResult.P1RoundIP > roundResult.P2RoundIP) p1.SetsWon++;
            else if (roundResult.P2RoundIP > roundResult.P1RoundIP) p2.SetsWon++;
            else
            {
                // tie-breaker: whoever had higher average Norm
                if (roundResult.P1AvgNorm > roundResult.P2AvgNorm) p1.SetsWon++;
                else if (roundResult.P2AvgNorm > roundResult.P1AvgNorm) p2.SetsWon++;
                else
                {
                    // true tie: random but announced
                    if (Rng.Next(2) == 0) p1.SetsWon++; else p2.SetsWon++;
                    Console.WriteLine("🎲 Tie-breaker coin flip awarded the set.");
                }
            }

            Console.WriteLine($"\n🎯 ROUND {round} WINNER: {(p1.SetsWon + p2.SetsWon == round ? (p1.SetsWon > p2.SetsWon ? p1.Name : p2.Name) : "—")} (set awarded)");
            PrintBoard(p1, p2);

            if (p1.SetsWon >= SetsToWin || p2.SetsWon >= SetsToWin) break;

            // Next round: roles shift unpredictably
            round++;
            Console.WriteLine();
            DoMusicAndStopVisualCue();
            AssignRandomRolesNoRepeat(new[] { p1, p2 }, new Dictionary<string, RoleType> { { p1.Name, p1.Role }, { p2.Name, p2.Role } });
            PrintRoundHeader(round, p1, p2);
        }

        Console.WriteLine("\n🏁 MATCH OVER");
        string winner = (p1.SetsWon > p2.SetsWon) ? p1.Name : (p2.SetsWon > p1.SetsWon ? p2.Name : "TIE");
        Console.WriteLine($"Winner: {winner}");
        PrintBoard(p1, p2);

        Console.WriteLine("\n=== ROLE NORMALIZATION (mean raw) ===");
        foreach (var role in Enum.GetValues(typeof(RoleType)).Cast<RoleType>())
        {
            int c = RoleCount.TryGetValue(role, out var cc) ? cc : 0;
            int s = RoleSumRaw.TryGetValue(role, out var ss) ? ss : 0;
            double mean = c == 0 ? 0 : (double)s / c;
            Console.WriteLine($"{role,-11} meanRaw={mean:0.00}  samples={c}");
        }

        Console.WriteLine("\n=== ARCHETYPE TENDENCIES (3-role wheel evidence) ===");
        PrintArchetype(p1);
        PrintArchetype(p2);
    }

    // ---------------------------
    // Round + Chat
    // ---------------------------
    private static (bool Completed, int P1RoundIP, int P2RoundIP, double P1AvgNorm, double P2AvgNorm) RunOneRound(Player p1, Player p2, int round)
    {
        int p1IP = 0, p2IP = 0;
        double p1NormSum = 0, p2NormSum = 0;
        int p1Turns = 0, p2Turns = 0;

        string lastMsgP1 = "";
        string lastMsgP2 = "";
        var recentTokens = new HashSet<string>();

        // Strict alternating turns
        Player[] order = { p1, p2 };
        int current = 0;

        for (int t = 1; t <= TurnsPerRound; t++)
        {
            var actor = order[current];
            var partner = order[1 - current];

            // Lock-out concept: only actor can type now.
            Console.WriteLine($"\n--- Turn {t}/{TurnsPerRound} | {actor.Name} typing (Role={actor.Role}) ---");
            Console.Write($"{actor.Name}: ");
            string msg = Console.ReadLine() ?? "";

            if (msg.Trim().Equals("end", StringComparison.OrdinalIgnoreCase))
                return (false, p1IP, p2IP, SafeAvg(p1NormSum, p1Turns), SafeAvg(p2NormSum, p2Turns));

            // partnerLast for heuristics
            string partnerLast = (actor == p1) ? lastMsgP2 : lastMsgP1;

            var score = ScoreTurn(actor.Role, msg, partnerLast, recentTokens);

            // Update per-player
            actor.TotalIP += score.IP;
            if (actor == p1)
            {
                p1IP += score.IP; p1NormSum += score.Norm; p1Turns++;
                lastMsgP1 = msg;
            }
            else
            {
                p2IP += score.IP; p2NormSum += score.Norm; p2Turns++;
                lastMsgP2 = msg;
            }

            // Archetype evidence: in 3-role version, just attribute to role
            AddRoleEvidence(actor, actor.Role, score);

            // Token memory
            UpdateRecentTokens(recentTokens, msg);

            PrintTurnScoring(actor, score);

            // Next turn
            current = 1 - current;
        }

        Console.WriteLine($"\n=== ROUND {round} SUMMARY ===");
        Console.WriteLine($"{p1.Name} RoundIP={p1IP} AvgNorm={SafeAvg(p1NormSum, p1Turns):0.00}");
        Console.WriteLine($"{p2.Name} RoundIP={p2IP} AvgNorm={SafeAvg(p2NormSum, p2Turns):0.00}");

        return (true, p1IP, p2IP, SafeAvg(p1NormSum, p1Turns), SafeAvg(p2NormSum, p2Turns));
    }

    // ---------------------------
    // Musical Cue (Console)
    // ---------------------------
    private static void DoMusicAndStopVisualCue()
    {
        Console.WriteLine("🎵 The music starts... (visual rhythm)");
        int durationMs = Rng.Next(MinMusicMs, MaxMusicMs + 1);

        var sw = Stopwatch.StartNew();
        int beat = 0;

        // rhythm bar animation
        while (sw.ElapsedMilliseconds < durationMs)
        {
            beat++;
            DrawRhythmBar(beat, durationMs - (int)sw.ElapsedMilliseconds);
            Thread.Sleep(180);
        }

        Console.WriteLine();
        Console.WriteLine("🛑🛑🛑  MUSIC STOPS — ROLE SHIFT INCOMING  🛑🛑🛑");
        Console.WriteLine("Locking turns until the next speaker begins...\n");
        Thread.Sleep(600);
    }

    private static void DrawRhythmBar(int beat, int msLeft)
    {
        int width = 24;
        int pos = beat % width;

        var sb = new StringBuilder();
        sb.Append("  [");
        for (int i = 0; i < width; i++)
            sb.Append(i == pos ? '●' : '·');
        sb.Append("] ");
        sb.Append($"~ {Math.Max(0, msLeft / 1000.0):0.0}s");

        Console.Write("\r" + sb.ToString().PadRight(50));
    }

    // ---------------------------
    // Role assignment (random, no repeats)
    // ---------------------------
    private static void AssignRandomRolesNoRepeat(Player[] players, Dictionary<string, RoleType>? previous)
    {
        // With 2 players and 3 roles, choose 2 distinct roles each shift.
        RoleType[] roles = Enum.GetValues(typeof(RoleType)).Cast<RoleType>().ToArray();

        // Try a few times to ensure no immediate repeats if previous is provided.
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var shuffled = roles.OrderBy(_ => Rng.Next()).ToArray();
            var r1 = shuffled[0];
            var r2 = shuffled[1];

            if (previous != null)
            {
                if (previous.TryGetValue(players[0].Name, out var prev0) && prev0 == r1) continue;
                if (previous.TryGetValue(players[1].Name, out var prev1) && prev1 == r2) continue;
            }

            players[0].Role = r1;
            players[1].Role = r2;
            return;
        }

        // Fallback: just assign distinct roles
        players[0].Role = roles[0];
        players[1].Role = roles[1];
    }

    private static void PrintRoundHeader(int round, Player p1, Player p2)
    {
        Console.WriteLine($"=== ROUND {round} (Musical Chairs) ===");
        Console.WriteLine($"  {p1.Name} -> {p1.Role}");
        Console.WriteLine($"  {p2.Name} -> {p2.Role}");
        Console.WriteLine($"Turns per round: {TurnsPerRound} | Sets to win: {SetsToWin}");
        Console.WriteLine();
    }

    private static void PrintBoard(Player p1, Player p2)
    {
        Console.WriteLine("\n=== BOARD ===");
        Console.WriteLine($"{p1.Name} Sets={p1.SetsWon} TotalIP={p1.TotalIP}");
        Console.WriteLine($"{p2.Name} Sets={p2.SetsWon} TotalIP={p2.TotalIP}");
    }

    // ---------------------------
    // Scoring (OC/IN/IQ/RF -> Raw -> Norm -> IP)
    // ---------------------------
    private static ScoringResult ScoreTurn(RoleType role, string msg, string partnerLast, HashSet<string> recentTokens)
    {
        if (!RoleCount.ContainsKey(role)) { RoleCount[role] = 0; RoleSumRaw[role] = 0; }

        var b = ExtractBehaviors(msg, partnerLast, recentTokens);
        var w = RoleWeights(role);

        int raw = b.OC * w.oc + b.IN * w.iN + b.IQ * w.iQ + b.RF * w.rF;

        RoleCount[role] += 1;
        RoleSumRaw[role] += raw;

        double expected = (double)RoleSumRaw[role] / Math.Max(1, RoleCount[role]);
        expected = Math.Max(1.0, expected);

        double norm = Clamp(raw / expected, 0.0, 2.5);
        int ip = (int)Clamp(Math.Round(10.0 * norm), 0, 30);

        return new ScoringResult { B = b, Raw = raw, Norm = norm, IP = ip };
    }

    private static (int oc, int iN, int iQ, int rF) RoleWeights(RoleType role)
    {
        // simple weights: tune later
        return role switch
        {
            RoleType.Initiator => (3, 1, 2, 1),
            RoleType.Listener => (0, 3, 2, 3),
            RoleType.Challenger => (2, 1, 3, 1),
            _ => (1, 1, 1, 1)
        };
    }

    private static BehaviorVector ExtractBehaviors(string msg, string partnerLast, HashSet<string> recentTokens)
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

    private static void AddRoleEvidence(Player actor, RoleType role, ScoringResult score)
    {
        // In this 3-role demo: role counts + slight quality weighting via Norm
        double q = Clamp(score.Norm / 1.2, 0.0, 1.0); // quality scalar
        double add = 1.0 + 0.4 * q;

        switch (role)
        {
            case RoleType.Initiator: actor.SumI += add; break;
            case RoleType.Listener: actor.SumL += add; break;
            case RoleType.Challenger: actor.SumC += add; break;
        }
    }

    private static void PrintTurnScoring(Player actor, ScoringResult r)
    {
        Console.WriteLine($"  => Role={actor.Role} Beh[OC={r.B.OC},IN={r.B.IN},IQ={r.B.IQ},RF={r.B.RF}] Raw={r.Raw} Norm={r.Norm:0.00} IP=+{r.IP}");
    }

    // ---------------------------
    // Archetype tendency print (3-role wheel)
    // ---------------------------
    private static void PrintArchetype(Player p)
    {
        double sum = p.SumI + p.SumL + p.SumC;
        if (sum <= 1e-9) sum = 1;
        double i = p.SumI / sum, l = p.SumL / sum, c = p.SumC / sum;

        Console.WriteLine($"\n{p.Name} Archetype Wheel (3-role):");
        Console.WriteLine($"  Initiator:  {i:0.00}  {Bar(i)}");
        Console.WriteLine($"  Listener:   {l:0.00}  {Bar(l)}");
        Console.WriteLine($"  Challenger: {c:0.00}  {Bar(c)}");
    }

    private static string Bar(double x)
    {
        int width = 20;
        int fill = (int)Math.Round(width * Clamp(x, 0.0, 1.0));
        return "[" + new string('█', fill) + new string('·', width - fill) + "]";
    }

    // ---------------------------
    // Helpers
    // ---------------------------
    private static string ReadNonEmpty(string prompt, string fallback)
    {
        Console.Write($"{prompt} [{fallback}]: ");
        string s = (Console.ReadLine() ?? "").Trim();
        return string.IsNullOrEmpty(s) ? fallback : s;
    }

    private static bool DetectAnswer(string m)
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

    private static int OverlapScore(string a, string b)
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
                if (x == y) { count++; break; }
            }
        }
        return count;
    }

    private static int NovelTokenCount(string msgLower, HashSet<string> recent)
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

    private static void UpdateRecentTokens(HashSet<string> recent, string msg)
    {
        string m = (msg ?? "").ToLowerInvariant();
        if (recent.Count > 220) recent.Clear();

        foreach (var raw in m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string tok = CleanToken(raw);
            if (tok.Length >= 4) recent.Add(tok);
        }
    }

    private static string CleanToken(string s)
    {
        if (s == null) return "";
        return s.Trim().Trim(',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>');
    }

    private static int Clamp02(int v) => v < 0 ? 0 : (v > 2 ? 2 : v);
    private static double Clamp(double x, double lo, double hi) => x < lo ? lo : (x > hi ? hi : x);
    private static double SafeAvg(double sum, int n) => n <= 0 ? 0 : sum / n;
}