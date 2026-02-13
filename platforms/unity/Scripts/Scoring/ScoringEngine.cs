using System.Collections.Generic;
using UnityEngine;

public enum RoleType { Initiator, Listener, Challenger, Synthesizer, Explorer }

public class ScoringResult
{
    public BehaviorVector Behavior;
    public int Raw;
    public float Norm;
    public int InsightPoints;
    public int ClarityXP, IntegrationXP, DepthXP, AdaptabilityXP;

    public float I, L, C, S, E; // archetype evidence from this turn
}

public static class ScoringEngine
{
    // Weights per role for behavior (MVP)
    static readonly Dictionary<RoleType, Vector4Int> RoleWeights = new Dictionary<RoleType, Vector4Int>
    {
        { RoleType.Initiator,   new Vector4Int(3,1,2,1) },
        { RoleType.Listener,    new Vector4Int(0,3,2,3) },
        { RoleType.Challenger,  new Vector4Int(2,1,3,1) },
        { RoleType.Synthesizer, new Vector4Int(1,3,1,3) },
        { RoleType.Explorer,    new Vector4Int(2,1,3,1) },
    };

    // Rolling mean normalization per role (simple in-memory; persist later)
    static readonly Dictionary<RoleType, int> Count = new Dictionary<RoleType, int>();
    static readonly Dictionary<RoleType, int> SumRaw = new Dictionary<RoleType, int>();

    public static ScoringResult ScoreTurn(RoleType role, string msg, string partnerLast, HashSet<string> recentTokens)
    {
        if (!Count.ContainsKey(role)) { Count[role] = 0; SumRaw[role] = 0; }

        var b = BehaviorHeuristics.Extract(msg, partnerLast, recentTokens);
        var w = RoleWeights[role];

        int raw = b.OC * w.x + b.IN * w.y + b.IQ * w.z + b.RF * w.w;

        Count[role] += 1;
        SumRaw[role] += raw;

        float expected = (float)SumRaw[role] / Mathf.Max(1, Count[role]);
        expected = Mathf.Max(1f, expected);

        float norm = Mathf.Clamp(raw / expected, 0f, 2.5f);

        // Map to “RPG stats” (very simple)
        int clarity = Mathf.RoundToInt(3f * (b.OC + b.IQ));
        int integration = Mathf.RoundToInt(4f * b.IN);
        int depth = Mathf.RoundToInt(4f * b.RF);
        int adaptability = Mathf.RoundToInt(3f * (b.IN + b.IQ)); // placeholder until you add Adaptability logic

        // Insight Points (IP) — simple and gamey
        int ip = Mathf.Clamp(Mathf.RoundToInt(10f * norm), 0, 30);

        // Archetype evidence from this message (build a vector, normalize later)
        float I = (role == RoleType.Initiator ? 1f : 0f) + 0.5f * b.OC + 0.25f * b.IQ;
        float L = (role == RoleType.Listener ? 1f : 0f) + 0.6f * b.IN + 0.4f * b.RF;
        float C = (role == RoleType.Challenger ? 1f : 0f) + 0.6f * b.IQ + 0.2f * b.OC;
        float S = (role == RoleType.Synthesizer ? 1f : 0f) + 0.7f * b.RF + 0.4f * b.IN;
        float E = (role == RoleType.Explorer ? 1f : 0f) + 0.7f * b.IQ + 0.2f * b.OC;

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
}

public struct Vector4Int
{
    public int x, y, z, w;
    public Vector4Int(int X,int Y,int Z,int W) { x=X; y=Y; z=Z; w=W; }
}