using System;
using System.Collections.Generic;

namespace ChatterGuild.Core;

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

        int clarity = (int)Math.Round(3.0 * (b.OC + b.IQ));
        int integration = (int)Math.Round(4.0 * b.IN);
        int depth = (int)Math.Round(4.0 * b.RF);
        int adaptability = (int)Math.Round(3.0 * (b.IN + b.IQ));

        int ip = (int)Clamp(Math.Round(10.0 * norm), 0, 30);

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
