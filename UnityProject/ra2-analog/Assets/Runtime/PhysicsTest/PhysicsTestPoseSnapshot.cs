using UnityEngine;

/// <summary>
/// Thin host→client pose sample for S2-02 loopback (not full physics state replication).
/// </summary>
public struct PhysicsTestPoseSnapshot
{
    public int RobotId;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 LinearVelocity;
}
