using System;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    public int Level = 1;
    public int TotalXP = 0;

    // RPG stats (grow over time)
    public int ClarityXP = 0;
    public int IntegrationXP = 0;
    public int DepthXP = 0;
    public int AdaptabilityXP = 0;

    // Archetype vector (5-way)
    public float Initiator = 0.2f;
    public float Listener = 0.2f;
    public float Challenger = 0.2f;
    public float Synthesizer = 0.2f;
    public float Explorer = 0.2f;

    public void AddXP(int xp, int clarity, int integration, int depth, int adaptability)
    {
        TotalXP += xp;
        ClarityXP += clarity;
        IntegrationXP += integration;
        DepthXP += depth;
        AdaptabilityXP += adaptability;

        RecomputeLevel();
    }

    void RecomputeLevel()
    {
        // simple curve; tune later
        int required = XPRequiredFor(Level + 1);
        while (TotalXP >= required)
        {
            Level++;
            required = XPRequiredFor(Level + 1);
        }
    }

    public static int XPRequiredFor(int targetLevel)
    {
        // gentle ramp
        return 100 + (targetLevel - 1) * (targetLevel - 1) * 60;
    }

    public void BlendArchetype(float i, float l, float c, float s, float e, float alpha = 0.12f)
    {
        Initiator   = Mathf.Lerp(Initiator, i, alpha);
        Listener    = Mathf.Lerp(Listener, l, alpha);
        Challenger  = Mathf.Lerp(Challenger, c, alpha);
        Synthesizer = Mathf.Lerp(Synthesizer, s, alpha);
        Explorer    = Mathf.Lerp(Explorer, e, alpha);

        NormalizeArchetype();
    }

    void NormalizeArchetype()
    {
        float sum = Initiator + Listener + Challenger + Synthesizer + Explorer;
        if (sum <= 0.0001f)
        {
            Initiator = Listener = Challenger = Synthesizer = Explorer = 0.2f;
            return;
        }
        Initiator /= sum; Listener /= sum; Challenger /= sum; Synthesizer /= sum; Explorer /= sum;
    }
}