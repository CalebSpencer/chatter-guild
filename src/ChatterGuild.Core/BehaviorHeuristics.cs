using System;
using System.Collections.Generic;

namespace ChatterGuild.Core;

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
