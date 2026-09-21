/// <summary>
/// Stage-2-ready drive intent. Not authoritative network state.
/// Produced by input adapters / remote command RPC; consumed by host authority → <see cref="PhysicsTestDrive"/>.
/// </summary>
public struct PhysicsTestDriveCommand
{
    /// <summary>Forward/back intent in [-1, 1].</summary>
    public float Move;

    /// <summary>Yaw turn intent in [-1, 1] (negative = left).</summary>
    public float Turn;

    /// <summary>True while brake is held.</summary>
    public bool Brake;

    /// <summary>
    /// Digital Fire / Button / Switch intent in [0, 1] (S7-04+).
    /// Button: held power; Switch: latched on; rising edge drives Burst* Fire.
    /// </summary>
    public float Fire;
}
