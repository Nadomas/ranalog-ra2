using System.Collections;
using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S11-12: thin UDP listen-host + loopback client match for the MVP player shell.
/// Same stack as S9-01 / S11 MP glue — proves net path inside the Windows player.
/// </summary>
public sealed class RobotMvpUdpLoopbackRunner
{
    public const int DefaultPort = 7795;

    public struct Result
    {
        public bool Ok;
        public string Reason;
        public MatchSummary HostSummary;
        public MatchSummary ClientSummary;
        public string SessionId;
    }

    readonly PhysicsMaterial slide;
    readonly int port;
    readonly float peerWaitSeconds;
    readonly float spawnWaitSeconds;
    readonly float immobileNeed;
    readonly float interactiveSeconds;

    public RobotMvpUdpLoopbackRunner(
        PhysicsMaterial slideMaterial,
        int matchPort = DefaultPort,
        float peerWait = 8f,
        float spawnWait = 12f,
        float immobile = 1.0f,
        float interactiveFightSeconds = 75f)
    {
        slide = slideMaterial;
        port = matchPort;
        peerWaitSeconds = peerWait;
        spawnWaitSeconds = spawnWait;
        immobileNeed = immobile;
        interactiveSeconds = interactiveFightSeconds;
    }

    public IEnumerator Run(
        RobotBlueprint admitBp,
        bool interactive,
        System.Action<RobotSpawnedInstance, RobotSpawnedInstance> onFightLive,
        System.Action onFightCleared,
        System.Action<Result> done)
    {
        var hostGo = new GameObject("MvpUdpListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var hostUdp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        hostUdp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", port);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(hostUdp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(hostUdp, System.Array.Empty<PhysicsTestCombatAuthority.TrackedRobot>(), 1);

        var spawner = hostGo.AddComponent<RobotHostSpawnerUdp>();
        spawner.Configure(hostUdp, authority, udpHost, combat, slide);

        var clientGo = new GameObject("MvpUdpClient");
        var clientUdp = clientGo.AddComponent<PhysicsTestUdpTransport>();
        clientUdp.Configure(PhysicsTestNetRole.Client, "127.0.0.1", port);
        var clientPeer = clientGo.AddComponent<PhysicsTestUdpClient>();
        clientPeer.Configure(clientUdp);

        hostUdp.StartTransport();
        clientUdp.StartTransport();

        var lobby = new MatchLobbySession(2, "mvp-udp-loop");
        lobby.SetPeerReady(0);

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while ((!hostUdp.PeerReady || !clientUdp.PeerReady) && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!hostUdp.PeerReady || !clientUdp.PeerReady)
        {
            done(Fail("peer_timeout", lobby));
            Cleanup(hostGo, clientGo, spawner, onFightCleared);
            yield break;
        }

        lobby.SetPeerReady(1);
        if (!lobby.TryBeginAdmit(out var admitErr))
        {
            done(Fail("admit_gate:" + admitErr, lobby));
            Cleanup(hostGo, clientGo, spawner, onFightCleared);
            yield break;
        }

        var bpA = RobotBlueprintSerializer.FromJson(RobotBlueprintSerializer.ToJson(admitBp));
        bpA.RootPosition = new Vector3(-3.5f, 0.55f, 0f);
        bpA.RootYawDegrees = 90f;
        RobotControlConfigurer.ApplyDrivePreset(bpA, RobotControlConfigurer.DrivePreset.TankSteer);

        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.55f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        string e0 = null;
        string e1 = null;
        if (!lobby.TryMarkAdmitted(0, bpA, out e0) || !lobby.TryMarkAdmitted(1, bpB, out e1))
        {
            done(Fail($"lobby_validate A={e0} B={e1}", lobby));
            Cleanup(hostGo, clientGo, spawner, onFightCleared);
            yield break;
        }

        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(0, 0, bpA));
        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(1, 1, bpB));

        var spawnEnd = Time.realtimeSinceStartup + spawnWaitSeconds;
        while (spawner.LiveCount < 2 && Time.realtimeSinceStartup < spawnEnd)
            yield return new WaitForFixedUpdate();

        string fightErr = null;
        if (spawner.LiveCount < 2 || !lobby.TryBeginFight(out fightErr))
        {
            done(Fail(spawner.LiveCount < 2 ? "spawn_incomplete" : "fight_gate:" + fightErr, lobby));
            Cleanup(hostGo, clientGo, spawner, onFightCleared);
            yield break;
        }

        RobotSpawnedInstance instA = null;
        RobotSpawnedInstance instB = null;
        for (var i = 0; i < spawner.Live.Count; i++)
        {
            if (spawner.Live[i].RobotId == 0) instA = spawner.Live[i];
            if (spawner.Live[i].RobotId == 1) instB = spawner.Live[i];
        }

        if (instA?.Drive == null || instB?.Drive == null)
        {
            done(Fail("missing_drives", lobby));
            Cleanup(hostGo, clientGo, spawner, onFightCleared);
            yield break;
        }

        EnsureContactProbe(instA);
        EnsureContactProbe(instB);
        onFightLive?.Invoke(instA, instB);

        MatchSummary summary = MatchSummary.None;
        if (interactive)
            yield return RunInteractive(instA, instB, bpA, bpB, lobby, immobileNeed, interactiveSeconds, s => summary = s);
        else
            yield return RunSmoke(instA, instB, bpA, bpB, lobby, immobileNeed, s => summary = s);

        if (!summary.Outcome.Finished)
        {
            onFightCleared?.Invoke();
            done(Fail("no_outcome", lobby));
            Cleanup(hostGo, clientGo, spawner, null);
            yield break;
        }

        lobby.CompleteWithSummary(summary);
        hostUdp.PublishMatchOutcome(summary);

        var received = new List<MatchSummary>(2);
        var outcomeWait = Time.realtimeSinceStartup + 3f;
        while (received.Count < 1 && Time.realtimeSinceStartup < outcomeWait)
        {
            clientUdp.DrainMatchOutcomes(received);
            yield return null;
        }

        var clientSum = received.Count > 0 ? received[received.Count - 1] : MatchSummary.None;
        lobby.Close();

        var same = received.Count > 0 &&
                   clientSum.Outcome.Finished == summary.Outcome.Finished &&
                   clientSum.Outcome.WinnerRobotId == summary.Outcome.WinnerRobotId &&
                   clientSum.Outcome.LoserRobotId == summary.Outcome.LoserRobotId &&
                   clientSum.SessionId == summary.SessionId;

        onFightCleared?.Invoke();
        done(new Result
        {
            Ok = same && hostUdp.PublishedMatchOutcomes >= 1,
            Reason = same ? "ok" : "outcome_mismatch",
            HostSummary = summary,
            ClientSummary = clientSum,
            SessionId = lobby.SessionId
        });
        Cleanup(hostGo, clientGo, spawner, null);
    }

    static IEnumerator RunInteractive(
        RobotSpawnedInstance a,
        RobotSpawnedInstance b,
        RobotBlueprint bpA,
        RobotBlueprint bpB,
        MatchLobbySession lobby,
        float immobileNeed,
        float seconds,
        System.Action<MatchSummary> done)
    {
        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.25f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var start = Time.time;
        var safety = start + seconds;
        const float arenaForfeitRadius = 12.5f;

        while (Time.time < safety && !outcome.Finished)
        {
            var ai = RobotSimpleChaseAi.Seek(b.Drive.transform, a.Drive.transform.position);
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, ai));
            // Player seat owned by PhysicsTestPlayerInput on host.

            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);

            if (!outcome.Finished)
            {
                if (IsOut(positions[0], arenaForfeitRadius))
                    rules.ForceOutcome(1, 0, MatchWinReason.Immobilized);
                else if (IsOut(positions[1], arenaForfeitRadius))
                    rules.ForceOutcome(0, 1, MatchWinReason.Immobilized);
                outcome = rules.LastOutcome;
            }

            yield return new WaitForFixedUpdate();
        }

        if (!outcome.Finished)
        {
            var aDist = new Vector3(positions[0].x, 0f, positions[0].z).magnitude;
            var bDist = new Vector3(positions[1].x, 0f, positions[1].z).magnitude;
            if (aDist <= bDist)
                rules.ForceOutcome(0, 1, MatchWinReason.Immobilized);
            else
                rules.ForceOutcome(1, 0, MatchWinReason.Immobilized);
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

    static IEnumerator RunSmoke(
        RobotSpawnedInstance a,
        RobotSpawnedInstance b,
        RobotBlueprint bpA,
        RobotBlueprint bpB,
        MatchLobbySession lobby,
        float immobileNeed,
        System.Action<MatchSummary> done)
    {
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0.1f };
        var driveEnd = Time.time + 0.4f;
        while (Time.time < driveEnd)
        {
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        if (!RobotDamageService.TryFunctionalDisable(a, out _))
        {
            done(MatchSummary.None);
            yield break;
        }

        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.2f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var start = Time.time;
        var safety = start + immobileNeed + 2.5f;
        while (Time.time < safety && !outcome.Finished)
        {
            var ai = RobotSimpleChaseAi.Seek(b.Drive.transform, a.Drive.transform.position);
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, ai));
            a.Drive.SetCommand(default);
            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);
            yield return new WaitForFixedUpdate();
        }

        done(new MatchSummary(
            outcome,
            Time.time - start,
            rules.GetImmobileSeconds(0),
            rules.GetImmobileSeconds(1),
            true,
            lobby.SessionId));
    }

    static bool IsOut(Vector3 pos, float radius) =>
        pos.y < -2f || new Vector3(pos.x, 0f, pos.z).magnitude > radius;

    static Result Fail(string reason, MatchLobbySession lobby) =>
        new Result
        {
            Ok = false,
            Reason = reason,
            HostSummary = MatchSummary.None,
            ClientSummary = MatchSummary.None,
            SessionId = lobby?.SessionId
        };

    static void EnsureContactProbe(RobotSpawnedInstance inst)
    {
        if (inst?.Assembly?.Root == null)
            return;
        var probe = inst.Assembly.Root.GetComponent<RobotContactWeaponProbe>();
        if (probe == null)
            probe = inst.Assembly.Root.AddComponent<RobotContactWeaponProbe>();
        probe.Bind(inst);
        probe.ConfigureMix(0.85f, 0.35f);
        probe.HostAuthority = true;
    }

    static void Cleanup(
        GameObject hostGo,
        GameObject clientGo,
        RobotHostSpawnerUdp spawner,
        System.Action onFightCleared)
    {
        onFightCleared?.Invoke();
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

        if (clientGo != null)
        {
            clientGo.GetComponent<PhysicsTestUdpTransport>()?.StopTransport();
            Object.Destroy(clientGo);
        }
    }
}
