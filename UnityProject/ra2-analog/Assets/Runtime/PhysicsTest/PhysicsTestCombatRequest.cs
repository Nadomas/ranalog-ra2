using UnityEngine;

/// <summary>
/// Thin client→host combat intent (S2-07). Not authoritative outcome.
/// </summary>
public struct PhysicsTestCombatRequest
{
    public int AttackerRobotId;
    public int SourceId;
    public int TargetRobotId;
    public Vector3 ImpulseWorld;
}
