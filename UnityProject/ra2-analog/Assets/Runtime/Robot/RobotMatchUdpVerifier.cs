using System.Collections;
using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S9-01 + S10-01 thin: in-process UDP host+client lobby → admit blueprints → fight →
/// host emits <see cref="MatchSummary"/>; both sides present via <see cref="MatchResultsStub"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMatchUdpVerifier : MonoBehaviour
{
    const int MatchPort = 7791;

    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 8f;
    [SerializeField] float spawnWaitSeconds = 12f;
    [SerializeField] float driveSecondsBeforeHit = 0.5f;
    [SerializeField] float immobileNeed = 1.2f;
    [SerializeField] PhysicsMaterial slideMaterial;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsMaterial slide)
    {
        slideMaterial = slide;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var hostGo = new GameObject("S9_ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var hostUdp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        hostUdp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", MatchPort);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(hostUdp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(hostUdp, System.Array.Empty<PhysicsTestCombatAuthority.TrackedRobot>(), 1);

        var spawner = hostGo.AddComponent<RobotHostSpawnerUdp>();
        spawner.Configure(hostUdp, authority, udpHost, combat, slideMaterial);

        var clientGo = new GameObject("S9_UdpClient");
        var clientUdp = clientGo.AddComponent<PhysicsTestUdpTransport>();
        clientUdp.Configure(PhysicsTestNetRole.Client, "127.0.0.1", MatchPort);
        var clientPeer = clientGo.AddComponent<PhysicsTestUdpClient>();
        clientPeer.Configure(clientUdp);

        hostUdp.StartTransport();
        clientUdp.StartTransport();
        hostUdp.ResetMetrics();
        clientUdp.ResetMetrics();

        var lobby = new MatchLobbySession(2, "s9-udp-thin");
        lobby.SetPeerReady(0); // listen-host seat
        // remote client seat marked ready after hello

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while ((!hostUdp.PeerReady || !clientUdp.PeerReady) && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!hostUdp.PeerReady || !clientUdp.PeerReady)
        {
            Finish(false, "peer_timeout", lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        lobby.SetPeerReady(1);
        if (!lobby.TryBeginAdmit(out var admitErr))
        {
            Finish(false, "admit_gate:" + admitErr, lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        var bpA = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(-3.5f, 0.85f, 0f), 90f);
        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.85f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpA, RobotControlConfigurer.DrivePreset.TankSteer);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        string e0 = null;
        string e1 = null;
        if (!lobby.TryMarkAdmitted(0, bpA, out e0) || !lobby.TryMarkAdmitted(1, bpB, out e1))
        {
            Finish(false, $"lobby_validate A={e0} B={e1}", lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(0, 0, bpA));
        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(1, 1, bpB));

        var spawnEnd = Time.realtimeSinceStartup + spawnWaitSeconds;
        while (spawner.LiveCount < 2 && Time.realtimeSinceStartup < spawnEnd)
            yield return new WaitForFixedUpdate();

        if (spawner.LiveCount < 2)
        {
            Finish(false, "spawn_incomplete", lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        if (!lobby.TryBeginFight(out var fightErr))
        {
            Finish(false, "fight_gate:" + fightErr, lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
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
            Finish(false, "missing_drives", lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        var fightStart = Time.time;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        var driveEnd = Time.time + driveSecondsBeforeHit;
        while (Time.time < driveEnd)
        {
            instA.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            instB.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        if (!RobotDamageService.TryFunctionalDisable(instA, out var dmgErr))
        {
            Finish(false, "disable:" + dmgErr, lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.2f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var safety = Time.time + immobileNeed + 2.5f;
        while (Time.time < safety && !outcome.Finished)
        {
            instB.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            instA.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            positions[0] = instA.Drive.transform.position;
            positions[1] = instB.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(instA);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(instB);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);
            yield return new WaitForFixedUpdate();
        }

        if (!outcome.Finished)
        {
            Finish(false, "no_outcome", lobby, MatchSummary.None, MatchSummary.None, hostUdp, clientUdp, spawner);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        var loserIdx = outcome.LoserRobotId == 0 ? 0 : 1;
        var winnerIdx = outcome.WinnerRobotId == 0 ? 0 : 1;
        var summary = new MatchSummary(
            outcome,
            Time.time - fightStart,
            rules.GetImmobileSeconds(loserIdx),
            rules.GetImmobileSeconds(winnerIdx),
            loserIdx == 0 ? RobotDamageService.IsFunctionallyDisabled(instA) : RobotDamageService.IsFunctionallyDisabled(instB),
            lobby.SessionId);

        lobby.CompleteWithSummary(summary);
        hostUdp.PublishMatchOutcome(summary);
        var hostPresented = MatchResultsStub.Present(summary, "host");

        var clientSummaries = new List<MatchSummary>(2);
        var outcomeWait = Time.realtimeSinceStartup + 3f;
        while (clientSummaries.Count < 1 && Time.realtimeSinceStartup < outcomeWait)
        {
            clientUdp.DrainMatchOutcomes(clientSummaries);
            yield return null;
        }

        MatchSummary clientSummary = MatchSummary.None;
        var clientGot = clientSummaries.Count > 0;
        if (clientGot)
        {
            clientSummary = clientSummaries[clientSummaries.Count - 1];
            MatchResultsStub.Present(clientSummary, "client");
        }

        lobby.Close();

        var sameFacts = clientGot &&
                        clientSummary.Outcome.Finished == summary.Outcome.Finished &&
                        clientSummary.Outcome.Reason == summary.Outcome.Reason &&
                        clientSummary.Outcome.WinnerRobotId == summary.Outcome.WinnerRobotId &&
                        clientSummary.Outcome.LoserRobotId == summary.Outcome.LoserRobotId &&
                        clientSummary.SessionId == summary.SessionId;

        var pass = lobby.Phase == MatchPhase.Closed &&
                   outcome.Reason == MatchWinReason.Immobilized &&
                   outcome.WinnerRobotId == 1 &&
                   outcome.LoserRobotId == 0 &&
                   hostUdp.PublishedMatchOutcomes >= 1 &&
                   sameFacts &&
                   !string.IsNullOrEmpty(hostPresented);

        Finish(pass, pass ? "ok" : "fail", lobby, summary, clientSummary, hostUdp, clientUdp, spawner);
        Cleanup(hostGo, clientGo);
    }

    void Finish(
        bool pass,
        string reason,
        MatchLobbySession lobby,
        MatchSummary hostSummary,
        MatchSummary clientSummary,
        PhysicsTestUdpTransport hostUdp,
        PhysicsTestUdpTransport clientUdp,
        RobotHostSpawnerUdp spawner)
    {
        Debug.Log(
            $"[S9-01] VERIFIER_DONE pass={pass} reason={reason} phase={lobby?.Phase} " +
            $"session={lobby?.SessionId} live={spawner?.LiveCount ?? 0} admitted={lobby?.AdmittedCount ?? 0} " +
            $"host_pub_outcome={hostUdp?.PublishedMatchOutcomes ?? 0} " +
            $"client_got_outcome={clientUdp?.DeliveredMatchOutcomes ?? 0} " +
            $"winner={hostSummary.Outcome.WinnerRobotId} loser={hostSummary.Outcome.LoserRobotId} " +
            $"host_reason={hostSummary.Outcome.Reason} client_reason={clientSummary.Outcome.Reason} " +
            $"same_session={(hostSummary.SessionId == clientSummary.SessionId)} " +
            $"peer_host={hostUdp?.PeerReady} peer_client={clientUdp?.PeerReady}");
    }

    static void Cleanup(GameObject hostGo, GameObject clientGo)
    {
        if (hostGo != null)
        {
            var spawner = hostGo.GetComponent<RobotHostSpawnerUdp>();
            if (spawner != null)
            {
                for (var i = spawner.Live.Count - 1; i >= 0; i--)
                    RobotSpawnService.Despawn(spawner.Live[i]);
            }

            var hostUdp = hostGo.GetComponent<PhysicsTestUdpTransport>();
            hostUdp?.StopTransport();
            Object.Destroy(hostGo);
        }

        if (clientGo != null)
        {
            var clientUdp = clientGo.GetComponent<PhysicsTestUdpTransport>();
            clientUdp?.StopTransport();
            Object.Destroy(clientGo);
        }
    }
}
