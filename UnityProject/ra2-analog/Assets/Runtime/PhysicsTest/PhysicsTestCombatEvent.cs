using UnityEngine;

/// <summary>
/// Host→client authoritative thin combat result (impulse applied + disable flag).
/// </summary>
public struct PhysicsTestCombatEvent
{
    public int Sequence;
    public int AttackerRobotId;
    public int TargetRobotId;
    public Vector3 ImpulseWorld;
    public bool TargetDisabled;
    public int TargetHitCount;
}
