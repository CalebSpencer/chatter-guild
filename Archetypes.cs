using UnityEngine;

public static class Archetypes
{
    // 5 core archetypes
    public enum Core { Initiator, Listener, Challenger, Synthesizer, Explorer }

    // Hybrid naming (simple adjacency feel; you can expand later)
    public static string GetClassName(float i, float l, float c, float s, float e)
    {
        // Find top two
        int top1 = 0, top2 = 1;
        float[] v = { i, l, c, s, e };

        for (int k = 0; k < v.Length; k++)
        {
            if (v[k] > v[top1]) { top2 = top1; top1 = k; }
            else if (k != top1 && v[k] > v[top2]) { top2 = k; }
        }

        string a = ((Core)top1).ToString();
        string b = ((Core)top2).ToString();

        // Hybrid labels (keep fun, not identity-judgy)
        if (a == "Initiator" && b == "Explorer") return "Trailblazer";
        if (a == "Explorer" && b == "Initiator") return "Trailblazer";

        if (a == "Listener" && b == "Synthesizer") return "Harmonizer";
        if (a == "Synthesizer" && b == "Listener") return "Harmonizer";

        if (a == "Challenger" && b == "Synthesizer") return "Dialectician";
        if (a == "Synthesizer" && b == "Challenger") return "Dialectician";

        if (a == "Explorer" && b == "Listener") return "Curious Companion";
        if (a == "Listener" && b == "Explorer") return "Curious Companion";

        if (a == "Initiator" && b == "Challenger") return "Provocateur";
        if (a == "Challenger" && b == "Initiator") return "Provocateur";

        // Fallback: primary archetype name
        return a;
    }
}