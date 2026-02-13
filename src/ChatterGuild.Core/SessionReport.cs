using System.Collections.Generic;

namespace ChatterGuild.Core;

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
