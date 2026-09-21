using System;
using UnityEngine;

/// <summary>
/// S2-01 local-only host authority. Collects <see cref="PhysicsTestCommandEnvelope"/>s and is the
/// only writer of <see cref="PhysicsTestDrive.SetCommand"/> for registered robots.
/// Simulates server command application without a net package.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class PhysicsTestLocalAuthority : MonoBehaviour
{
    [Serializable]
    public struct RobotBinding
    {
        public int RobotId;
        public int AllowedSourceId;
        public PhysicsTestDrive Drive;
    }

    [SerializeField] RobotBinding[] bindings = Array.Empty<RobotBinding>();

    PhysicsTestDriveCommand[] pending;
    int rejectedUnauthorized;
    int appliedCommands;

    public int RejectedUnauthorized => rejectedUnauthorized;
    public int AppliedCommands => appliedCommands;

    public void ConfigureBindings(RobotBinding[] next)
    {
        bindings = next ?? Array.Empty<RobotBinding>();
    }

    void Awake()
    {
        pending = new PhysicsTestDriveCommand[bindings.Length];
    }

    /// <summary>
    /// Submit intent for a robot. Wrong sourceId is rejected (EXP-06 local thin).
    /// </summary>
    public bool Submit(PhysicsTestCommandEnvelope envelope)
    {
        for (var i = 0; i < bindings.Length; i++)
        {
            var b = bindings[i];
            if (b.RobotId != envelope.RobotId)
                continue;

            if (b.AllowedSourceId != envelope.SourceId)
            {
                rejectedUnauthorized++;
                return false;
            }

            if (pending == null || pending.Length != bindings.Length)
                pending = new PhysicsTestDriveCommand[bindings.Length];

            pending[i] = envelope.Command;
            return true;
        }

        rejectedUnauthorized++;
        return false;
    }

    void FixedUpdate()
    {
        if (bindings == null || bindings.Length == 0)
            return;

        if (pending == null || pending.Length != bindings.Length)
            pending = new PhysicsTestDriveCommand[bindings.Length];

        for (var i = 0; i < bindings.Length; i++)
        {
            var drive = bindings[i].Drive;
            if (drive == null)
                continue;

            drive.SetCommand(pending[i]);
            appliedCommands++;
        }
    }
}
