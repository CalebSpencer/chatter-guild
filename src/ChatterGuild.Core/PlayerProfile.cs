namespace ChatterGuild.Core;

public sealed class PlayerProfile
{
    public int Level { get; set; } = 1;
    public int TotalXP { get; set; } = 0;

    public int ClarityXP { get; set; } = 0;
    public int IntegrationXP { get; set; } = 0;
    public int DepthXP { get; set; } = 0;
    public int AdaptabilityXP { get; set; } = 0;

    public double Initiator { get; set; } = 0.2;
    public double Listener { get; set; } = 0.2;
    public double Challenger { get; set; } = 0.2;
    public double Synthesizer { get; set; } = 0.2;
    public double Explorer { get; set; } = 0.2;

    public void AddXP(int xp, int clarity, int integration, int depth, int adaptability)
    {
        TotalXP += xp;
        ClarityXP += clarity;
        IntegrationXP += integration;
        DepthXP += depth;
        AdaptabilityXP += adaptability;
        RecomputeLevel();
    }

    public void BlendArchetype(double i, double l, double c, double s, double e, double alpha = 0.15)
    {
        Initiator = Lerp(Initiator, i, alpha);
        Listener = Lerp(Listener, l, alpha);
        Challenger = Lerp(Challenger, c, alpha);
        Synthesizer = Lerp(Synthesizer, s, alpha);
        Explorer = Lerp(Explorer, e, alpha);
        NormalizeArchetype();
    }

    public void NormalizeArchetype()
    {
        double sum = Initiator + Listener + Challenger + Synthesizer + Explorer;
        if (sum <= 1e-9)
        {
            Initiator = Listener = Challenger = Synthesizer = Explorer = 0.2;
            return;
        }
        Initiator /= sum; Listener /= sum; Challenger /= sum; Synthesizer /= sum; Explorer /= sum;
    }

    public static int XPRequiredFor(int targetLevel)
    {
        int d = targetLevel - 1;
        return 100 + d * d * 60;
    }

    void RecomputeLevel()
    {
        int req = XPRequiredFor(Level + 1);
        while (TotalXP >= req)
        {
            Level++;
            req = XPRequiredFor(Level + 1);
        }
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
