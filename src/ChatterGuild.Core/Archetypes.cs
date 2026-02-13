namespace ChatterGuild.Core;

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
