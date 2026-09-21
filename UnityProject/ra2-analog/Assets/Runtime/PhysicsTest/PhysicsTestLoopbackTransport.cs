using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S2-02 provisional in-process loopback transport. Moves command envelopes client→host
/// and thin pose snapshots host→client without a net package. S2-04 adds optional
/// RTT/jitter/loss simulation on both directions. S3-03 adds thin spawn request/event
/// channels on the same boundary. Not production networking.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestLoopbackTransport : MonoBehaviour
{
    struct DelayedCommand
    {
        public PhysicsTestCommandEnvelope Envelope;
        public double DeliverAt;
    }

    struct DelayedSnapshot
    {
        public PhysicsTestPoseSnapshot[] Snapshots;
        public double DeliverAt;
    }

    struct DelayedSpawnRequest
    {
        public RobotSpawnRequest Request;
        public double DeliverAt;
    }

    struct DelayedSpawnEvent
    {
        public RobotSpawnEvent Event;
        public double DeliverAt;
    }

    readonly Queue<PhysicsTestCommandEnvelope> readyCommands = new Queue<PhysicsTestCommandEnvelope>(64);
    readonly Queue<PhysicsTestPoseSnapshot[]> readySnapshots = new Queue<PhysicsTestPoseSnapshot[]>(16);
    readonly Queue<RobotSpawnRequest> readySpawnRequests = new Queue<RobotSpawnRequest>(8);
    readonly Queue<RobotSpawnEvent> readySpawnEvents = new Queue<RobotSpawnEvent>(8);
    readonly List<DelayedCommand> delayedCommands = new List<DelayedCommand>(64);
    readonly List<DelayedSnapshot> delayedSnapshots = new List<DelayedSnapshot>(16);
    readonly List<DelayedSpawnRequest> delayedSpawnRequests = new List<DelayedSpawnRequest>(8);
    readonly List<DelayedSpawnEvent> delayedSpawnEvents = new List<DelayedSpawnEvent>(8);

    PhysicsTestLatencyProfile profile = PhysicsTestLatencyProfile.None;
    readonly System.Random rng = new System.Random(42);

    int enqueuedCommands;
    int deliveredCommands;
    int droppedCommands;
    int publishedSnapshots;
    int deliveredSnapshots;
    int droppedSnapshots;
    int enqueuedSpawnRequests;
    int deliveredSpawnRequests;
    int publishedSpawnEvents;
    int deliveredSpawnEvents;
    double delaySumMs;
    int delaySamples;

    public int EnqueuedCommands => enqueuedCommands;
    public int DeliveredCommands => deliveredCommands;
    public int DroppedCommands => droppedCommands;
    public int PublishedSnapshots => publishedSnapshots;
    public int DeliveredSnapshots => deliveredSnapshots;
    public int DroppedSnapshots => droppedSnapshots;
    public int EnqueuedSpawnRequests => enqueuedSpawnRequests;
    public int DeliveredSpawnRequests => deliveredSpawnRequests;
    public int PublishedSpawnEvents => publishedSpawnEvents;
    public int DeliveredSpawnEvents => deliveredSpawnEvents;
    public int PendingDelayedCommands => delayedCommands.Count;
    public int PendingDelayedSnapshots => delayedSnapshots.Count;
    public PhysicsTestLatencyProfile Profile => profile;

    public float AverageDelayMs =>
        delaySamples > 0 ? (float)(delaySumMs / delaySamples) : 0f;

    public void SetLatencyProfile(PhysicsTestLatencyProfile latencyProfile)
    {
        profile = latencyProfile;
    }

    /// <summary>Client peer: enqueue intent for the host (crosses the transport boundary).</summary>
    public void EnqueueCommand(PhysicsTestCommandEnvelope envelope)
    {
        var copy = new PhysicsTestCommandEnvelope(
            envelope.RobotId,
            envelope.SourceId,
            new PhysicsTestDriveCommand
            {
                Move = envelope.Command.Move,
                Turn = envelope.Command.Turn,
                Brake = envelope.Command.Brake,
                Fire = envelope.Command.Fire
            });

        enqueuedCommands++;
        if (ShouldDrop())
        {
            droppedCommands++;
            return;
        }

        var delayMs = SampleOneWayDelayMs();
        RecordDelay(delayMs);
        if (delayMs <= 0.01f)
        {
            readyCommands.Enqueue(copy);
            return;
        }

        delayedCommands.Add(new DelayedCommand
        {
            Envelope = copy,
            DeliverAt = Now() + delayMs * 0.001
        });
    }

    /// <summary>Host peer: drain pending client commands (call before authority FixedUpdate).</summary>
    public int DrainCommands(List<PhysicsTestCommandEnvelope> into)
    {
        if (into == null)
            return 0;

        PromoteReadyCommands();
        var n = 0;
        while (readyCommands.Count > 0)
        {
            into.Add(readyCommands.Dequeue());
            deliveredCommands++;
            n++;
        }

        return n;
    }

    /// <summary>Host peer: publish thin poses for logical clients.</summary>
    public void PublishSnapshots(PhysicsTestPoseSnapshot[] snapshots)
    {
        if (snapshots == null || snapshots.Length == 0)
            return;

        var copy = new PhysicsTestPoseSnapshot[snapshots.Length];
        for (var i = 0; i < snapshots.Length; i++)
            copy[i] = snapshots[i];

        publishedSnapshots++;
        if (ShouldDrop())
        {
            droppedSnapshots++;
            return;
        }

        var delayMs = SampleOneWayDelayMs();
        RecordDelay(delayMs);
        if (delayMs <= 0.01f)
        {
            readySnapshots.Enqueue(copy);
            return;
        }

        delayedSnapshots.Add(new DelayedSnapshot
        {
            Snapshots = copy,
            DeliverAt = Now() + delayMs * 0.001
        });
    }

    /// <summary>Client peer: drain latest host pose batches.</summary>
    public int DrainSnapshots(List<PhysicsTestPoseSnapshot[]> into)
    {
        if (into == null)
            return 0;

        PromoteReadySnapshots();
        var n = 0;
        while (readySnapshots.Count > 0)
        {
            into.Add(readySnapshots.Dequeue());
            deliveredSnapshots++;
            n++;
        }

        return n;
    }

    /// <summary>Client→host: request authoritative spawn/despawn from blueprint JSON.</summary>
    public void EnqueueSpawnRequest(RobotSpawnRequest request)
    {
        enqueuedSpawnRequests++;
        var delayMs = SampleOneWayDelayMs();
        RecordDelay(delayMs);
        if (delayMs <= 0.01f)
        {
            readySpawnRequests.Enqueue(request);
            return;
        }

        delayedSpawnRequests.Add(new DelayedSpawnRequest
        {
            Request = request,
            DeliverAt = Now() + delayMs * 0.001
        });
    }

    /// <summary>Host peer: drain pending spawn/despawn requests.</summary>
    public int DrainSpawnRequests(List<RobotSpawnRequest> into)
    {
        if (into == null)
            return 0;

        PromoteReadySpawnRequests();
        var n = 0;
        while (readySpawnRequests.Count > 0)
        {
            into.Add(readySpawnRequests.Dequeue());
            deliveredSpawnRequests++;
            n++;
        }

        return n;
    }

    /// <summary>Host→client: publish spawn/despawn result with logical blueprint payload.</summary>
    public void PublishSpawnEvent(RobotSpawnEvent spawnEvent)
    {
        publishedSpawnEvents++;
        var delayMs = SampleOneWayDelayMs();
        RecordDelay(delayMs);
        if (delayMs <= 0.01f)
        {
            readySpawnEvents.Enqueue(spawnEvent);
            return;
        }

        delayedSpawnEvents.Add(new DelayedSpawnEvent
        {
            Event = spawnEvent,
            DeliverAt = Now() + delayMs * 0.001
        });
    }

    /// <summary>Client peer: drain host spawn/despawn events.</summary>
    public int DrainSpawnEvents(List<RobotSpawnEvent> into)
    {
        if (into == null)
            return 0;

        PromoteReadySpawnEvents();
        var n = 0;
        while (readySpawnEvents.Count > 0)
        {
            into.Add(readySpawnEvents.Dequeue());
            deliveredSpawnEvents++;
            n++;
        }

        return n;
    }

    public void ClearQueues()
    {
        readyCommands.Clear();
        readySnapshots.Clear();
        readySpawnRequests.Clear();
        readySpawnEvents.Clear();
        delayedCommands.Clear();
        delayedSnapshots.Clear();
        delayedSpawnRequests.Clear();
        delayedSpawnEvents.Clear();
    }

    public void ResetMetrics()
    {
        enqueuedCommands = 0;
        deliveredCommands = 0;
        droppedCommands = 0;
        publishedSnapshots = 0;
        deliveredSnapshots = 0;
        droppedSnapshots = 0;
        enqueuedSpawnRequests = 0;
        deliveredSpawnRequests = 0;
        publishedSpawnEvents = 0;
        deliveredSpawnEvents = 0;
        delaySumMs = 0;
        delaySamples = 0;
        ClearQueues();
    }

    void PromoteReadyCommands()
    {
        var now = Now();
        for (var i = 0; i < delayedCommands.Count;)
        {
            if (delayedCommands[i].DeliverAt > now)
            {
                i++;
                continue;
            }

            readyCommands.Enqueue(delayedCommands[i].Envelope);
            delayedCommands.RemoveAt(i);
        }
    }

    void PromoteReadySnapshots()
    {
        var now = Now();
        for (var i = 0; i < delayedSnapshots.Count;)
        {
            if (delayedSnapshots[i].DeliverAt > now)
            {
                i++;
                continue;
            }

            readySnapshots.Enqueue(delayedSnapshots[i].Snapshots);
            delayedSnapshots.RemoveAt(i);
        }
    }

    void PromoteReadySpawnRequests()
    {
        var now = Now();
        for (var i = 0; i < delayedSpawnRequests.Count;)
        {
            if (delayedSpawnRequests[i].DeliverAt > now)
            {
                i++;
                continue;
            }

            readySpawnRequests.Enqueue(delayedSpawnRequests[i].Request);
            delayedSpawnRequests.RemoveAt(i);
        }
    }

    void PromoteReadySpawnEvents()
    {
        var now = Now();
        for (var i = 0; i < delayedSpawnEvents.Count;)
        {
            if (delayedSpawnEvents[i].DeliverAt > now)
            {
                i++;
                continue;
            }

            readySpawnEvents.Enqueue(delayedSpawnEvents[i].Event);
            delayedSpawnEvents.RemoveAt(i);
        }
    }

    bool ShouldDrop()
    {
        if (profile.Loss <= 0f)
            return false;
        if (profile.Loss >= 1f)
            return true;
        return rng.NextDouble() < profile.Loss;
    }

    float SampleOneWayDelayMs()
    {
        var oneWay = profile.OneWayMs;
        if (profile.JitterMs <= 0f)
            return oneWay;
        var jitter = (float)((rng.NextDouble() * 2.0 - 1.0) * profile.JitterMs);
        return Mathf.Max(0f, oneWay + jitter);
    }

    void RecordDelay(float delayMs)
    {
        delaySumMs += delayMs;
        delaySamples++;
    }

    static double Now() => Time.realtimeSinceStartupAsDouble;
}
