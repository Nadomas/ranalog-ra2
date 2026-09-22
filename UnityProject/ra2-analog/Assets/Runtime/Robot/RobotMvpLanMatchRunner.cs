using System.Collections;
using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// S11-14: two-process LAN match — Host injects seat 0, Client spawns seat 1 + sends drive commands.
/// </summary>
public sealed class RobotMvpLanMatchRunner
{
    public const int DefaultPort = 7796;

    public struct Result
    {
        public bool Ok;
        public string Reason;
        public MatchSummary Summary;
        public string SessionId;
        public bool WasHost;
    }

    readonly PhysicsMaterial slide;
    readonly float peerWait;
    readonly float spawnWait;
    readonly float immobileNeed;
    readonly float fightSeconds;

    public RobotMvpLanMatchRunner(
        PhysicsMaterial slideMaterial,
        float peerWaitSeconds = 60f,
        float spawnWaitSeconds = 20f,
        float immobile = 1f,
        float interactiveFightSeconds = 90f)
    {
        slide = slideMaterial;
        peerWait = peerWaitSeconds;
        spawnWait = spawnWaitSeconds;
        immobileNeed = immobile;
        fightSeconds = interactiveFightSeconds;
    }

    public IEnumerator RunHost(
        RobotBlueprint hostBp,
        System.Action<string> status,
        System.Action<RobotSpawnedInstance, RobotSpawnedInstance> onLive,
        System.Action onCleared,
        System.Action<Result> done)
    {
        var hostGo = new GameObject("MvpLanHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var hostUdp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        hostUdp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", DefaultPort);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(hostUdp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(hostUdp, System.Array.Empty<PhysicsTestCombatAuthority.TrackedRobot>(), 1);

        var spawner = hostGo.AddComponent<RobotHostSpawnerUdp>();
        spawner.Configure(hostUdp, authority, udpHost, combat, slide);

        // Local WASD → authority for seat 0 (must beat LocalAuthority FixedUpdate wipe of pending).
        var localDrive = hostGo.AddComponent<LanHostLocalSeatDrive>();
        localDrive.Configure(authority, robotId: 0, sourceId: 0);

        hostUdp.StartTransport();
        status?.Invoke($"LAN host listening :{DefaultPort} — waiting for client…");

        var waitEnd = Time.realtimeSinceStartup + peerWait;
        while (!hostUdp.PeerReady && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!hostUdp.PeerReady)
        {
            done(Fail("peer_timeout", true));
            CleanupHost(hostGo, spawner, onCleared);
            yield break;
        }

        status?.Invoke("client connected — admitting…");
        var lobby = new MatchLobbySession(2, "mvp-lan");
        lobby.SetPeerReady(0);
        lobby.SetPeerReady(1);
        lobby.TryBeginAdmit(out _);

        var bpA = RobotBlueprintSerializer.FromJson(RobotBlueprintSerializer.ToJson(hostBp));
        bpA.RootPosition = new Vector3(-3.5f, 0.55f, 0f);
        bpA.RootYawDegrees = 90f;
        RobotControlConfigurer.ApplyDrivePreset(bpA, RobotControlConfigurer.DrivePreset.TankSteer);
        lobby.TryMarkAdmitted(0, bpA, out _);

        // Seat 0 from host (local inject). Seat 1 arrives from remote client.
        hostUdp.InjectSpawnRequestLocal(RobotSpawnRequest.CreateSpawn(0, 0, bpA));

        var spawnEnd = Time.realtimeSinceStartup + spawnWait;
        while (spawner.LiveCount < 2 && Time.realtimeSinceStartup < spawnEnd)
            yield return new WaitForFixedUpdate();

        if (spawner.LiveCount < 2)
        {
            done(Fail("spawn_incomplete_need_client_bot", true));
            CleanupHost(hostGo, spawner, onCleared);
            yield break;
        }

        // Mark seat 1 admitted from live instance blueprint.
        RobotSpawnedInstance instA = null;
        RobotSpawnedInstance instB = null;
        for (var i = 0; i < spawner.Live.Count; i++)
        {
            if (spawner.Live[i].RobotId == 0) instA = spawner.Live[i];
            if (spawner.Live[i].RobotId == 1) instB = spawner.Live[i];
        }

        if (instA?.Drive == null || instB?.Drive == null)
        {
            done(Fail("missing_drives", true));
            CleanupHost(hostGo, spawner, onCleared);
            yield break;
        }

        lobby.TryMarkAdmitted(1, instB.Blueprint ?? bpA, out _);
        lobby.TryBeginFight(out _);
        status?.Invoke("LAN fight — WASD = you (blue)");
        onLive?.Invoke(instA, instB);

        MatchSummary summary = MatchSummary.None;
        yield return RunHostFight(instA, instB, lobby, s => summary = s);

        if (!summary.Outcome.Finished)
        {
            done(Fail("no_outcome", true));
            CleanupHost(hostGo, spawner, onCleared);
            yield break;
        }

        lobby.CompleteWithSummary(summary);
        hostUdp.PublishMatchOutcome(summary);
        lobby.Close();
        onCleared?.Invoke();
        done(new Result { Ok = true, Reason = "ok", Summary = summary, SessionId = lobby.SessionId, WasHost = true });
        CleanupHost(hostGo, spawner, null);
    }

    public IEnumerator RunClient(
        RobotBlueprint clientBp,
        string hostAddress,
        System.Action<string> status,
        System.Action onCleared,
        System.Action<Result> done)
    {
        var addr = string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress.Trim();
        var clientGo = new GameObject("MvpLanClient");
        var clientUdp = clientGo.AddComponent<PhysicsTestUdpTransport>();
        clientUdp.Configure(PhysicsTestNetRole.Client, addr, DefaultPort);
        var clientPeer = clientGo.AddComponent<PhysicsTestUdpClient>();
        clientPeer.Configure(clientUdp);
        clientUdp.StartTransport();
        status?.Invoke($"joining {addr}:{DefaultPort}…");

        var waitEnd = Time.realtimeSinceStartup + peerWait;
        while (!clientUdp.PeerReady && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!clientUdp.PeerReady)
        {
            done(Fail("peer_timeout", false));
            CleanupClient(clientGo, onCleared);
            yield break;
        }

        var bpB = RobotBlueprintSerializer.FromJson(RobotBlueprintSerializer.ToJson(clientBp));
        bpB.RootPosition = new Vector3(3.5f, 0.55f, 0f);
        bpB.RootYawDegrees = -90f;
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);
        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(1, 1, bpB));
        status?.Invoke("spawned seat 1 — driving with WASD (commands → host)");

        var received = new List<MatchSummary>(2);
        var fightEnd = Time.realtimeSinceStartup + fightSeconds + 12f;
        while (received.Count < 1 && Time.realtimeSinceStartup < fightEnd)
        {
            // Client never applies forces — only sends intent for robot 1.
            var cmd = ReadKeyboardCommand();
            clientPeer.Send(new PhysicsTestCommandEnvelope(1, 1, cmd));
            clientUdp.DrainMatchOutcomes(received);
            yield return new WaitForFixedUpdate();
        }

        onCleared?.Invoke();
        if (received.Count < 1)
        {
            done(Fail("outcome_timeout", false));
            CleanupClient(clientGo, null);
            yield break;
        }

        var summary = received[received.Count - 1];
        done(new Result
        {
            Ok = summary.Outcome.Finished,
            Reason = summary.Outcome.Finished ? "ok" : "incomplete",
            Summary = summary,
            SessionId = summary.SessionId,
            WasHost = false
        });
        CleanupClient(clientGo, null);
    }

    IEnumerator RunHostFight(
        RobotSpawnedInstance a,
        RobotSpawnedInstance b,
        MatchLobbySession lobby,
        System.Action<MatchSummary> done)
    {
        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.25f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var start = Time.time;
        var safety = start + fightSeconds;

        while (Time.time < safety && !outcome.Finished)
        {
            // Seat 0: LanHostLocalSeatDrive → authority. Seat 1: UDP remote commands.
            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);
            yield return new WaitForFixedUpdate();
        }

        if (!outcome.Finished)
        {
            var aDist = new Vector3(positions[0].x, 0f, positions[0].z).magnitude;
            var bDist = new Vector3(positions[1].x, 0f, positions[1].z).magnitude;
            if (aDist <= bDist)
                rules.ForceOutcome(0, 1, MatchWinReason.TimeExpired);
            else
                rules.ForceOutcome(1, 0, MatchWinReason.TimeExpired);
            outcome = rules.LastOutcome;
        }

        done(new MatchSummary(
            outcome,
            Time.time - start,
            rules.GetImmobileSeconds(0),
            rules.GetImmobileSeconds(1),
            (outcome.LoserRobotId == 0 && disabled[0]) || (outcome.LoserRobotId == 1 && disabled[1]),
            lobby.SessionId));
    }

    static PhysicsTestDriveCommand ReadKeyboardCommand()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return default;

        float move = 0f;
        if (keyboard.wKey.isPressed) move += 1f;
        if (keyboard.sKey.isPressed) move -= 1f;
        float turn = 0f;
        if (keyboard.aKey.isPressed) turn -= 1f;
        if (keyboard.dKey.isPressed) turn += 1f;
        return new PhysicsTestDriveCommand
        {
            Move = Mathf.Clamp(move, -1f, 1f),
            Turn = Mathf.Clamp(turn, -1f, 1f),
            Brake = keyboard.spaceKey.isPressed
        };
    }

    static Result Fail(string reason, bool wasHost) =>
        new Result { Ok = false, Reason = reason, Summary = MatchSummary.None, WasHost = wasHost };

    static void CleanupHost(GameObject hostGo, RobotHostSpawnerUdp spawner, System.Action onCleared)
    {
        onCleared?.Invoke();
        if (spawner != null)
        {
            for (var i = spawner.Live.Count - 1; i >= 0; i--)
                RobotSpawnService.Despawn(spawner.Live[i]);
        }

        if (hostGo != null)
        {
            hostGo.GetComponent<PhysicsTestUdpTransport>()?.StopTransport();
            Object.Destroy(hostGo);
        }
    }

    static void CleanupClient(GameObject clientGo, System.Action onCleared)
    {
        onCleared?.Invoke();
        if (clientGo != null)
        {
            clientGo.GetComponent<PhysicsTestUdpTransport>()?.StopTransport();
            Object.Destroy(clientGo);
        }
    }
}

/// <summary>
/// Submits host keyboard into <see cref="PhysicsTestLocalAuthority"/> for seat 0.
/// Runs after UDP drain (−150) and before authority apply (−100).
/// </summary>
[DefaultExecutionOrder(-120)]
public sealed class LanHostLocalSeatDrive : MonoBehaviour
{
    PhysicsTestLocalAuthority authority;
    int robotId;
    int sourceId;

    public void Configure(PhysicsTestLocalAuthority auth, int robotId, int sourceId)
    {
        authority = auth;
        this.robotId = robotId;
        this.sourceId = sourceId;
    }

    void FixedUpdate()
    {
        if (authority == null)
            return;
        authority.Submit(new PhysicsTestCommandEnvelope(robotId, sourceId, ReadKeyboardCommand()));
    }

    static PhysicsTestDriveCommand ReadKeyboardCommand()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return default;

        float move = 0f;
        if (keyboard.wKey.isPressed) move += 1f;
        if (keyboard.sKey.isPressed) move -= 1f;
        float turn = 0f;
        if (keyboard.aKey.isPressed) turn -= 1f;
        if (keyboard.dKey.isPressed) turn += 1f;
        return new PhysicsTestDriveCommand
        {
            Move = Mathf.Clamp(move, -1f, 1f),
            Turn = Mathf.Clamp(turn, -1f, 1f),
            Brake = keyboard.spaceKey.isPressed
        };
    }
}
