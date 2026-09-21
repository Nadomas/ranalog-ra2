/// <summary>
/// Process role for S2-06+ cross-process / dedicated spikes.
/// Parsed from <c>-ra2-role=host|client|dedicated</c> (default host in Editor).
/// </summary>
public enum PhysicsTestNetRole
{
    Host = 0,
    Client = 1,
    Dedicated = 2
}
