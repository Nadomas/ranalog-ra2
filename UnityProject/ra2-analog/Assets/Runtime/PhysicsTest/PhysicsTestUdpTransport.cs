using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S2-06 provisional cross-process UDP transport. Same command/pose/combat envelope shape
/// as loopback, but over localhost UDP sockets. No NGO/Unity Transport package.
/// Marked experiment candidate — not a frozen production stack.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestUdpTransport : MonoBehaviour
{
    [SerializeField] PhysicsTestNetRole role = PhysicsTestNetRole.Host;
    [SerializeField] string remoteHost = "127.0.0.1";
    [SerializeField] int hostPort = 7777;
    [SerializeField] int clientPort = 0;

    UdpClient socket;
    IPEndPoint remoteEndpoint;
    bool peerReady;
    bool started;

    readonly Queue<PhysicsTestCommandEnvelope> readyCommands = new Queue<PhysicsTestCommandEnvelope>(64);
    readonly Queue<PhysicsTestPoseSnapshot[]> readySnapshots = new Queue<PhysicsTestPoseSnapshot[]>(16);
    readonly Queue<PhysicsTestCombatRequest> readyCombatRequests = new Queue<PhysicsTestCombatRequest>(16);
    readonly Queue<PhysicsTestCombatEvent> readyCombatEvents = new Queue<PhysicsTestCombatEvent>(16);
    readonly Queue<RobotSpawnRequest> readySpawnRequests = new Queue<RobotSpawnRequest>(8);
    readonly Queue<RobotSpawnEvent> readySpawnEvents = new Queue<RobotSpawnEvent>(8);
    readonly Queue<MatchSummary> readyMatchOutcomes = new Queue<MatchSummary>(4);

    int enqueuedCommands;
    int deliveredCommands;
    int publishedSnapshots;
    int deliveredSnapshots;
    int enqueuedCombatRequests;
    int deliveredCombatRequests;
    int publishedCombatEvents;
    int deliveredCombatEvents;
    int enqueuedSpawnRequests;
    int deliveredSpawnRequests;
    int publishedSpawnEvents;
    int deliveredSpawnEvents;
    int publishedMatchOutcomes;
    int deliveredMatchOutcomes;
    int droppedSpawnPayloads;
    int helloReceived;
    int bytesSent;
    int bytesReceived;
    string lastError = "";

    public PhysicsTestNetRole Role => role;
    public bool PeerReady => peerReady;
    public int EnqueuedCommands => enqueuedCommands;
    public int DeliveredCommands => deliveredCommands;
    public int PublishedSnapshots => publishedSnapshots;
    public int DeliveredSnapshots => deliveredSnapshots;
    public int EnqueuedCombatRequests => enqueuedCombatRequests;
    public int DeliveredCombatRequests => deliveredCombatRequests;
    public int PublishedCombatEvents => publishedCombatEvents;
    public int DeliveredCombatEvents => deliveredCombatEvents;
    public int EnqueuedSpawnRequests => enqueuedSpawnRequests;
    public int DeliveredSpawnRequests => deliveredSpawnRequests;
    public int PublishedSpawnEvents => publishedSpawnEvents;
    public int DeliveredSpawnEvents => deliveredSpawnEvents;
    public int PublishedMatchOutcomes => publishedMatchOutcomes;
    public int DeliveredMatchOutcomes => deliveredMatchOutcomes;
    public int DroppedSpawnPayloads => droppedSpawnPayloads;
    public int HelloReceived => helloReceived;
    public int BytesSent => bytesSent;
    public int BytesReceived => bytesReceived;
    public string LastError => lastError;
    public int HostPort => hostPort;
    public string RemoteHost => remoteHost;

    public void Configure(PhysicsTestNetRole netRole, string host, int port, int bindClientPort = 0)
    {
        role = netRole;
        remoteHost = string.IsNullOrEmpty(host) ? "127.0.0.1" : host;
        hostPort = port > 0 ? port : 7777;
        clientPort = bindClientPort;
    }

    public void StartTransport()
    {
        if (started)
            return;

        try
        {
            if (role == PhysicsTestNetRole.Host || role == PhysicsTestNetRole.Dedicated)
            {
                socket = new UdpClient(hostPort);
                remoteEndpoint = null;
                peerReady = false;
            }
            else
            {
                socket = clientPort > 0 ? new UdpClient(clientPort) : new UdpClient(0);
                remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteHost), hostPort);
                peerReady = true;
                SendRaw(PhysicsTestUdpCodec.WriteHello(PhysicsTestNetRole.Client, 1));
            }

            socket.Client.Blocking = false;
            started = true;
            lastError = "";
            Debug.Log($"[PhysicsTestUdpTransport] started role={role} port={(role == PhysicsTestNetRole.Client ? ((IPEndPoint)socket.Client.LocalEndPoint).Port : hostPort)} remote={remoteHost}:{hostPort}");
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            Debug.LogError($"[PhysicsTestUdpTransport] start failed: {ex.Message}");
        }
    }

    public void StopTransport()
    {
        if (!started)
            return;
        try
        {
            if (peerReady)
                SendRaw(PhysicsTestUdpCodec.WriteGoodbye());
        }
        catch
        {
            // ignore
        }

        try { socket?.Close(); } catch { /* ignore */ }
        socket = null;
        started = false;
        peerReady = false;
        remoteEndpoint = null;
    }

    void OnEnable()
    {
        // StartTransport is owned by PhysicsTestNetRoleBootstrap / explicit callers
        // so cmdline role can reconfigure before bind.
    }

    void OnDisable() => StopTransport();

    void OnDestroy() => StopTransport();

    void Update() => PumpReceive();

    void FixedUpdate() => PumpReceive();

    public void EnqueueCommand(PhysicsTestCommandEnvelope envelope)
    {
        enqueuedCommands++;
        SendRaw(PhysicsTestUdpCodec.WriteCommand(envelope));
    }

    public int DrainCommands(List<PhysicsTestCommandEnvelope> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readyCommands.Count > 0)
        {
            into.Add(readyCommands.Dequeue());
            deliveredCommands++;
            n++;
        }

        return n;
    }

    public void PublishSnapshots(PhysicsTestPoseSnapshot[] snapshots)
    {
        if (snapshots == null || snapshots.Length == 0 || !peerReady)
            return;
        publishedSnapshots++;
        SendRaw(PhysicsTestUdpCodec.WritePoses(snapshots));
    }

    public int DrainSnapshots(List<PhysicsTestPoseSnapshot[]> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readySnapshots.Count > 0)
        {
            into.Add(readySnapshots.Dequeue());
            deliveredSnapshots++;
            n++;
        }

        return n;
    }

    public void EnqueueCombatRequest(PhysicsTestCombatRequest request)
    {
        enqueuedCombatRequests++;
        SendRaw(PhysicsTestUdpCodec.WriteCombatRequest(request));
    }

    public int DrainCombatRequests(List<PhysicsTestCombatRequest> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readyCombatRequests.Count > 0)
        {
            into.Add(readyCombatRequests.Dequeue());
            deliveredCombatRequests++;
            n++;
        }

        return n;
    }

    public void PublishCombatEvent(PhysicsTestCombatEvent evt)
    {
        publishedCombatEvents++;
        if (!peerReady)
            return;
        SendRaw(PhysicsTestUdpCodec.WriteCombatEvent(evt));
    }

    public int DrainCombatEvents(List<PhysicsTestCombatEvent> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readyCombatEvents.Count > 0)
        {
            into.Add(readyCombatEvents.Dequeue());
            deliveredCombatEvents++;
            n++;
        }

        return n;
    }

    public void EnqueueSpawnRequest(RobotSpawnRequest request)
    {
        enqueuedSpawnRequests++;
        var payload = PhysicsTestUdpCodec.WriteSpawnRequest(request);
        if (payload == null)
        {
            droppedSpawnPayloads++;
            lastError = "spawn_request_too_large";
            Debug.LogError("[PhysicsTestUdpTransport] spawn request exceeds MaxSpawnPayloadBytes");
            return;
        }

        SendRaw(payload);
    }

    public int DrainSpawnRequests(List<RobotSpawnRequest> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readySpawnRequests.Count > 0)
        {
            into.Add(readySpawnRequests.Dequeue());
            deliveredSpawnRequests++;
            n++;
        }

        return n;
    }

    public void PublishSpawnEvent(RobotSpawnEvent spawnEvent)
    {
        publishedSpawnEvents++;
        if (!peerReady)
            return;
        var payload = PhysicsTestUdpCodec.WriteSpawnEvent(spawnEvent);
        if (payload == null)
        {
            droppedSpawnPayloads++;
            lastError = "spawn_event_too_large";
            Debug.LogError("[PhysicsTestUdpTransport] spawn event exceeds MaxSpawnPayloadBytes");
            return;
        }

        SendRaw(payload);
    }

    public int DrainSpawnEvents(List<RobotSpawnEvent> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readySpawnEvents.Count > 0)
        {
            into.Add(readySpawnEvents.Dequeue());
            deliveredSpawnEvents++;
            n++;
        }

        return n;
    }

    /// <summary>S9-01: host publishes authoritative match summary to remote peer(s).</summary>
    public void PublishMatchOutcome(MatchSummary summary)
    {
        publishedMatchOutcomes++;
        if (!peerReady)
            return;
        SendRaw(PhysicsTestUdpCodec.WriteMatchOutcome(summary));
    }

    public int DrainMatchOutcomes(List<MatchSummary> into)
    {
        if (into == null)
            return 0;
        PumpReceive();
        var n = 0;
        while (readyMatchOutcomes.Count > 0)
        {
            into.Add(readyMatchOutcomes.Dequeue());
            deliveredMatchOutcomes++;
            n++;
        }

        return n;
    }

    public void ResetMetrics()
    {
        enqueuedCommands = 0;
        deliveredCommands = 0;
        publishedSnapshots = 0;
        deliveredSnapshots = 0;
        enqueuedCombatRequests = 0;
        deliveredCombatRequests = 0;
        publishedCombatEvents = 0;
        deliveredCombatEvents = 0;
        enqueuedSpawnRequests = 0;
        deliveredSpawnRequests = 0;
        publishedSpawnEvents = 0;
        deliveredSpawnEvents = 0;
        publishedMatchOutcomes = 0;
        deliveredMatchOutcomes = 0;
        droppedSpawnPayloads = 0;
        helloReceived = 0;
        bytesSent = 0;
        bytesReceived = 0;
        readyCommands.Clear();
        readySnapshots.Clear();
        readyCombatRequests.Clear();
        readyCombatEvents.Clear();
        readySpawnRequests.Clear();
        readySpawnEvents.Clear();
        readyMatchOutcomes.Clear();
    }

    void PumpReceive()
    {
        if (socket == null)
            return;

        while (true)
        {
            try
            {
                if (socket.Available <= 0)
                    break;

                IPEndPoint from = null;
                var data = socket.Receive(ref from);
                if (data == null || data.Length == 0)
                    continue;

                bytesReceived += data.Length;
                HandlePacket(data, data.Length, from);
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.WouldBlock ||
                                             se.SocketErrorCode == SocketError.TimedOut)
            {
                break;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                break;
            }
        }
    }

    void HandlePacket(byte[] data, int length, IPEndPoint from)
    {
        if (length < 1)
            return;

        switch (data[0])
        {
            case PhysicsTestUdpCodec.MsgHello:
                if (PhysicsTestUdpCodec.TryReadHello(data, length, out var helloRole, out _, out _))
                {
                    helloReceived++;
                    if (role == PhysicsTestNetRole.Host || role == PhysicsTestNetRole.Dedicated)
                    {
                        remoteEndpoint = from;
                        peerReady = true;
                        SendRaw(PhysicsTestUdpCodec.WriteHello(role, 0));
                        Debug.Log($"[PhysicsTestUdpTransport] peer hello from {from} role={helloRole}");
                    }
                    else
                    {
                        peerReady = true;
                    }
                }

                break;

            case PhysicsTestUdpCodec.MsgCommand:
                if (PhysicsTestUdpCodec.TryReadCommand(data, length, out var cmd))
                    readyCommands.Enqueue(cmd);
                break;

            case PhysicsTestUdpCodec.MsgPoses:
                if (PhysicsTestUdpCodec.TryReadPoses(data, length, out var poses))
                    readySnapshots.Enqueue(poses);
                break;

            case PhysicsTestUdpCodec.MsgCombatRequest:
                if (PhysicsTestUdpCodec.TryReadCombatRequest(data, length, out var req))
                    readyCombatRequests.Enqueue(req);
                break;

            case PhysicsTestUdpCodec.MsgCombatEvent:
                if (PhysicsTestUdpCodec.TryReadCombatEvent(data, length, out var evt))
                    readyCombatEvents.Enqueue(evt);
                break;

            case PhysicsTestUdpCodec.MsgSpawnRequest:
                if (PhysicsTestUdpCodec.TryReadSpawnRequest(data, length, out var spawnReq))
                    readySpawnRequests.Enqueue(spawnReq);
                break;

            case PhysicsTestUdpCodec.MsgSpawnEvent:
                if (PhysicsTestUdpCodec.TryReadSpawnEvent(data, length, out var spawnEvt))
                    readySpawnEvents.Enqueue(spawnEvt);
                break;

            case PhysicsTestUdpCodec.MsgMatchOutcome:
                if (PhysicsTestUdpCodec.TryReadMatchOutcome(data, length, out var matchSummary))
                    readyMatchOutcomes.Enqueue(matchSummary);
                break;

            case PhysicsTestUdpCodec.MsgGoodbye:
                peerReady = false;
                break;
        }
    }

    void SendRaw(byte[] payload)
    {
        if (socket == null || payload == null || payload.Length == 0)
            return;
        if (remoteEndpoint == null)
            return;

        try
        {
            socket.Send(payload, payload.Length, remoteEndpoint);
            bytesSent += payload.Length;
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
        }
    }
}
