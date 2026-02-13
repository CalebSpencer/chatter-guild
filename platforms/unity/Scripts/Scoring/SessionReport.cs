using System;
using System.Collections.Generic;

[Serializable]
public class TurnRecord
{
    public string Speaker;
    public string Text;
    public string Role;
    public int Raw;
    public float Norm;
    public int IP;
    public int OC, IN, IQ, RF;
}

[Serializable]
public class SessionReport
{
    public List<TurnRecord> Turns = new List<TurnRecord>();
    public int TotalIP = 0;

    public float SumI, SumL, SumC, SumS, SumE;

    public void Add(string speaker, string text, RoleType role, ScoringResult r)
    {
        Turns.Add(new TurnRecord
        {
            Speaker = speaker,
            Text = text,
            Role = role.ToString(),
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

    public void GetArchetypeVector(out float i, out float l, out float c, out float s, out float e)
    {
        float sum = SumI + SumL + SumC + SumS + SumE;
        if (sum <= 0.0001f) { i=l=c=s=e=0.2f; return; }
        i = SumI / sum; l = SumL / sum; c = SumC / sum; s = SumS / sum; e = SumE / sum;
    }
}