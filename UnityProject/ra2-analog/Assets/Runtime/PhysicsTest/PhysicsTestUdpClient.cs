using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S2-06/S2-09 remote client peer over UDP. Sends commands/combat/spawn requests only;
/// never writes Drive. Prediction OFF. Pose snapshots are presentation samples only.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestUdpClient : MonoBehaviour
{
    [SerializeField] PhysicsTestUdpTransport transport;

    readonly List<PhysicsTestPoseSnapshot[]> snapshotScratch = new List<PhysicsTestPoseSnapshot[]>(8);
    readonly List<PhysicsTestCombatEvent> combatScratch = new List<PhysicsTestCombatEvent>(8);
    readonly List<RobotSpawnEvent> spawnScratch = new List<RobotSpawnEvent>(8);

    PhysicsTestPoseSnapshot[] lastPoses = System.Array.Empty<PhysicsTestPoseSnapshot>();
    PhysicsTestCombatEvent lastCombatEvent;
    int receivedSnapshotBatches;
    int receivedCombatEvents;
    int receivedSpawnEvents;
    float maxPoseStep;
    PhysicsTestPoseSnapshot[] stepPrev = System.Array.Empty<PhysicsTestPoseSnapshot>();

    public PhysicsTestPoseSnapshot[] LastPoses => lastPoses;
    public PhysicsTestCombatEvent LastCombatEvent => lastCombatEvent;
    public int ReceivedSnapshotBatches => receivedSnapshotBatches;
    public int ReceivedCombatEvents => receivedCombatEvents;
    public int ReceivedSpawnEvents => receivedSpawnEvents;
    public float MaxPoseStep => maxPoseStep;

    public void Configure(PhysicsTestUdpTransport udp)
    {
        transport = udp;
    }

    public void Send(PhysicsTestCommandEnvelope envelope)
    {
        if (transport == null)
            return;
        transport.EnqueueCommand(envelope);
    }

    public void SendCombat(PhysicsTestCombatRequest request)
    {
        if (transport == null)
            return;
        transport.EnqueueCombatRequest(request);
    }

    public void SendSpawn(RobotSpawnRequest request)
    {
        if (transport == null)
            return;
        transport.EnqueueSpawnRequest(request);
    }

    public int DrainSpawnEvents(List<RobotSpawnEvent> into)
    {
        if (transport == null || into == null)
            return 0;
        Pump();
        spawnScratch.Clear();
        var n = transport.DrainSpawnEvents(spawnScratch);
        for (var i = 0; i < spawnScratch.Count; i++)
            into.Add(spawnScratch[i]);
        receivedSpawnEvents += n;
        return n;
    }

    public void ResetMetrics()
    {
        receivedSnapshotBatches = 0;
        receivedCombatEvents = 0;
        receivedSpawnEvents = 0;
        maxPoseStep = 0f;
        lastPoses = System.Array.Empty<PhysicsTestPoseSnapshot>();
        stepPrev = System.Array.Empty<PhysicsTestPoseSnapshot>();
        lastCombatEvent = default;
    }

    void Update() => Pump();

    void Pump()
    {
        if (transport == null)
            return;

        snapshotScratch.Clear();
        if (transport.DrainSnapshots(snapshotScratch) > 0)
        {
            lastPoses = snapshotScratch[snapshotScratch.Count - 1];
            receivedSnapshotBatches += snapshotScratch.Count;
            TrackStep(lastPoses);
        }

        combatScratch.Clear();
        if (transport.DrainCombatEvents(combatScratch) > 0)
        {
            lastCombatEvent = combatScratch[combatScratch.Count - 1];
            receivedCombatEvents += combatScratch.Count;
        }
    }

    void TrackStep(PhysicsTestPoseSnapshot[] poses)
    {
        if (poses == null || poses.Length == 0)
            return;

        if (stepPrev.Length == poses.Length)
        {
            for (var i = 0; i < poses.Length; i++)
            {
                var step = Vector3.Distance(stepPrev[i].Position, poses[i].Position);
                if (step > maxPoseStep)
                    maxPoseStep = step;
            }
        }

        stepPrev = new PhysicsTestPoseSnapshot[poses.Length];
        for (var i = 0; i < poses.Length; i++)
            stepPrev[i] = poses[i];
    }
}
