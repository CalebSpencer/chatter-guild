namespace ChatterGuild.Core;

public sealed class TurnRecord
{
    public string Speaker = "";
    public string Text = "";
    public RoleType Role;
    public int Raw;
    public double Norm;
    public int IP;
    public int OC, IN, IQ, RF;
}
