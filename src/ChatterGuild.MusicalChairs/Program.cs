using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ChatterGuild.Core;

// =======================
// Musical Chairs Mode (Console)
// 2 users | 3 playable roles | random role shifts | strict turn-taking
// Visual cues via rhythm bar + STOP banner
// =======================

public sealed class Player
{
    public string Name;
    public RoleType Role;
    public int SetsWon = 0;
    public int TotalIP = 0;

    // Archetype evidence (full 5-role from Core)
    public double SumI, SumL, SumC, SumS, SumE;

    public Player(string name) { Name = name; }
}

public static class Program
{
    // ---- Settings ----
    const int TurnsPerRound = 8;
    const int SetsToWin = 3;
    const int MinMusicMs = 2500;
    const int MaxMusicMs = 6500;

    // Playable roles for Musical Chairs (3-role subset)
    static readonly RoleType[] PlayableRoles = { RoleType.Initiator, RoleType.Listener, RoleType.Challenger };

    static readonly Random Rng = new();

    // ---- Entry ----
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("=== Chatter's Guild: Musical Chairs (Console) ===");
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
                    if (Rng.Next(2) == 0) p1.SetsWon++; else p2.SetsWon++;
                    Console.WriteLine("  Tie-breaker coin flip awarded the set.");
                }
            }

            string roundWinner = p1.SetsWon > p2.SetsWon ? p1.Name : p2.Name;
            Console.WriteLine($"\n  ROUND {round} WINNER: {roundWinner} (set awarded)");
            PrintBoard(p1, p2);

            if (p1.SetsWon >= SetsToWin || p2.SetsWon >= SetsToWin) break;

            // Next round: roles shift unpredictably
            round++;
            Console.WriteLine();
            DoMusicAndStopVisualCue();
            AssignRandomRolesNoRepeat(new[] { p1, p2 }, new Dictionary<string, RoleType> { { p1.Name, p1.Role }, { p2.Name, p2.Role } });
            PrintRoundHeader(round, p1, p2);
        }

        Console.WriteLine("\n  MATCH OVER");
        string winner = (p1.SetsWon > p2.SetsWon) ? p1.Name : (p2.SetsWon > p1.SetsWon ? p2.Name : "TIE");
        Console.WriteLine($"Winner: {winner}");
        PrintBoard(p1, p2);

        Console.WriteLine("\n=== ARCHETYPE TENDENCIES ===");
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

        Player[] order = { p1, p2 };
        int current = 0;

        for (int t = 1; t <= TurnsPerRound; t++)
        {
            var actor = order[current];
            var partner = order[1 - current];

            Console.WriteLine($"\n--- Turn {t}/{TurnsPerRound} | {actor.Name} typing (Role={actor.Role}) ---");
            Console.Write($"{actor.Name}: ");
            string msg = Console.ReadLine() ?? "";

            if (msg.Trim().Equals("end", StringComparison.OrdinalIgnoreCase))
                return (false, p1IP, p2IP, SafeAvg(p1NormSum, p1Turns), SafeAvg(p2NormSum, p2Turns));

            string partnerLast = (actor == p1) ? lastMsgP2 : lastMsgP1;

            var score = ScoringEngine.ScoreTurn(actor.Role, msg, partnerLast, recentTokens);

            actor.TotalIP += score.InsightPoints;
            if (actor == p1)
            {
                p1IP += score.InsightPoints; p1NormSum += score.Norm; p1Turns++;
                lastMsgP1 = msg;
            }
            else
            {
                p2IP += score.InsightPoints; p2NormSum += score.Norm; p2Turns++;
                lastMsgP2 = msg;
            }

            // Accumulate archetype evidence from Core's scoring
            actor.SumI += score.I;
            actor.SumL += score.L;
            actor.SumC += score.C;
            actor.SumS += score.S;
            actor.SumE += score.E;

            BehaviorHeuristics.UpdateRecentTokens(recentTokens, msg);

            PrintTurnScoring(actor, score);

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
        Console.WriteLine("  The music starts... (visual rhythm)");
        int durationMs = Rng.Next(MinMusicMs, MaxMusicMs + 1);

        var sw = Stopwatch.StartNew();
        int beat = 0;

        while (sw.ElapsedMilliseconds < durationMs)
        {
            beat++;
            DrawRhythmBar(beat, durationMs - (int)sw.ElapsedMilliseconds);
            Thread.Sleep(180);
        }

        Console.WriteLine();
        Console.WriteLine("  MUSIC STOPS -- ROLE SHIFT INCOMING");
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
            sb.Append(i == pos ? '*' : '.');
        sb.Append("] ");
        sb.Append($"~ {Math.Max(0, msLeft / 1000.0):0.0}s");

        Console.Write("\r" + sb.ToString().PadRight(50));
    }

    // ---------------------------
    // Role assignment (random, no repeats)
    // ---------------------------
    private static void AssignRandomRolesNoRepeat(Player[] players, Dictionary<string, RoleType> previous)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var shuffled = PlayableRoles.OrderBy(_ => Rng.Next()).ToArray();
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
        players[0].Role = PlayableRoles[0];
        players[1].Role = PlayableRoles[1];
    }

    // ---------------------------
    // UI Helpers
    // ---------------------------
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

    private static void PrintTurnScoring(Player actor, ScoringResult r)
    {
        Console.WriteLine($"  => Role={actor.Role} Beh[OC={r.Behavior.OC},IN={r.Behavior.IN},IQ={r.Behavior.IQ},RF={r.Behavior.RF}] Raw={r.Raw} Norm={r.Norm:0.00} IP=+{r.InsightPoints}");
    }

    private static void PrintArchetype(Player p)
    {
        double sum = p.SumI + p.SumL + p.SumC + p.SumS + p.SumE;
        if (sum <= 1e-9) sum = 1;
        double i = p.SumI / sum, l = p.SumL / sum, c = p.SumC / sum, s = p.SumS / sum, e = p.SumE / sum;

        string className = Archetypes.GetClassName(i, l, c, s, e);

        Console.WriteLine($"\n{p.Name} Archetype Wheel | Class: {className}");
        Console.WriteLine($"  Initiator:   {i:0.00}  {Bar(i)}");
        Console.WriteLine($"  Listener:    {l:0.00}  {Bar(l)}");
        Console.WriteLine($"  Challenger:  {c:0.00}  {Bar(c)}");
        Console.WriteLine($"  Synthesizer: {s:0.00}  {Bar(s)}");
        Console.WriteLine($"  Explorer:    {e:0.00}  {Bar(e)}");
    }

    private static string Bar(double x)
    {
        int width = 20;
        int fill = (int)Math.Round(width * Math.Clamp(x, 0.0, 1.0));
        return "[" + new string('#', fill) + new string('.', width - fill) + "]";
    }

    private static string ReadNonEmpty(string prompt, string fallback)
    {
        Console.Write($"{prompt} [{fallback}]: ");
        string s = (Console.ReadLine() ?? "").Trim();
        return string.IsNullOrEmpty(s) ? fallback : s;
    }

    private static double SafeAvg(double sum, int n) => n <= 0 ? 0 : sum / n;
}
