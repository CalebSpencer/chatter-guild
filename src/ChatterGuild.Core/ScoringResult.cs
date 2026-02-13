namespace ChatterGuild.Core;

public sealed class ScoringResult
{
    public BehaviorVector Behavior;
    public int Raw;
    public double Norm;
    public int InsightPoints;
    public int ClarityXP, IntegrationXP, DepthXP, AdaptabilityXP;
    public double I, L, C, S, E; // archetype evidence
}
