using System;
using UnityEngine;

/// <summary>
/// S2-04 / EXP-08 latency profile for loopback transport (RTT, jitter, loss).
/// One-way delay ≈ RttMs/2; both command and snapshot paths use the same profile.
/// </summary>
[Serializable]
public struct PhysicsTestLatencyProfile
{
    [Tooltip("Round-trip time in milliseconds (split evenly client↔host).")]
    public float RttMs;

    [Tooltip("± jitter applied to each packet's one-way delay (ms).")]
    public float JitterMs;

    [Tooltip("Packet drop probability 0..1 for each enqueue.")]
    [Range(0f, 1f)]
    public float Loss;

    [Tooltip("If true, client presents interpolated poses between last two snapshots.")]
    public bool InterpolatePoses;

    public static PhysicsTestLatencyProfile None => new PhysicsTestLatencyProfile
    {
        RttMs = 0f,
        JitterMs = 0f,
        Loss = 0f,
        InterpolatePoses = false
    };

    public static PhysicsTestLatencyProfile Rtt60 => new PhysicsTestLatencyProfile
    {
        RttMs = 60f,
        JitterMs = 5f,
        Loss = 0f,
        InterpolatePoses = false
    };

    public static PhysicsTestLatencyProfile Rtt100Loss => new PhysicsTestLatencyProfile
    {
        RttMs = 100f,
        JitterMs = 15f,
        Loss = 0.05f,
        InterpolatePoses = false
    };

    public static PhysicsTestLatencyProfile Rtt100Interp => new PhysicsTestLatencyProfile
    {
        RttMs = 100f,
        JitterMs = 15f,
        Loss = 0.05f,
        InterpolatePoses = true
    };

    public float OneWayMs => Mathf.Max(0f, RttMs * 0.5f);

    public string Label =>
        $"rtt={RttMs:0} jitter={JitterMs:0} loss={Loss:0.###} interp={InterpolatePoses}";
}
