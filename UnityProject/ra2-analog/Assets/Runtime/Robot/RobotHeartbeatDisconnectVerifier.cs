using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S9-04: mid-fight client hard abort (no goodbye) → host heartbeat timeout → DisconnectForfeit.
/// Proves silent peer death still ends the match (no silent desync).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotHeartbeatDisconnectVerifier : MonoBehaviour
{
    const int MatchPort = 7793;

    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 8f;
    [SerializeField] float spawnWaitSeconds = 12f;
    [SerializeField] float driveSecondsBeforeDrop = 0.35f;
    [SerializeField] float heartbeatTimeoutSeconds = 0.6f;
    [SerializeField] float heartbeatSendInterval = 0.1f;
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

        var hostGo = new GameObject("S9_04_ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var hostUdp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        hostUdp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", MatchPort);
        hostUdp.ConfigureHeartbeat(heartbeatTimeoutSeconds, sendHeartbeats: false);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(hostUdp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(hostUdp, System.Array.Empty<PhysicsTestCombatAuthority.TrackedRobot>(), 1);

        var spawner = hostGo.AddComponent<RobotHostSpawnerUdp>();
        spawner.Configure(hostUdp, authority, udpHost, combat, slideMaterial);

        var clientGo = new GameObject("S9_04_UdpClient");
        var clientUdp = clientGo.AddComponent<PhysicsTestUdpTransport>();
        clientUdp.Configure(PhysicsTestNetRole.Client, "127.0.0.1", MatchPort);
        clientUdp.ConfigureHeartbeat(heartbeatTimeoutSeconds, sendHeartbeats: true, heartbeatSendInterval);
        var clientPeer = clientGo.AddComponent<PhysicsTestUdpClient>();
        clientPeer.Configure(clientUdp);

        hostUdp.StartTransport();
        clientUdp.StartTransport();
        hostUdp.ResetMetrics();
        clientUdp.ResetMetrics();

        var lobby = new MatchLobbySession(2, "s9-heartbeat-v0");
        lobby.SetPeerReady(0);

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while ((!hostUdp.PeerReady || !clientUdp.PeerReady) && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!hostUdp.PeerReady || !clientUdp.PeerReady)
        {
            Finish(false, "peer_timeout", lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        lobby.SetPeerReady(1);
        if (!lobby.TryBeginAdmit(out var admitErr))
        {
            Finish(false, "admit_gate:" + admitErr, lobby, MatchSummary.None, hostUdp);
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
            Finish(false, $"lobby_validate A={e0} B={e1}", lobby, MatchSummary.None, hostUdp);
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
            Finish(false, "spawn_incomplete", lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        if (!lobby.TryBeginFight(out var fightErr))
        {
            Finish(false, "fight_gate:" + fightErr, lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        hostUdp.ArmHeartbeatWatch();

        var fightStart = Time.time;
        var driveEnd = Time.time + driveSecondsBeforeDrop;
        while (Time.time < driveEnd)
            yield return new WaitForFixedUpdate();

        // Crash-style drop: hard abort without MsgGoodbye. PeerReady must stay true until heartbeat.
        const int disconnectedSeat = 1;
        if (hostUdp.PeerLostByHeartbeat)
        {
            Finish(false, "premature_heartbeat", lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        clientUdp.AbortTransport();

        if (!hostUdp.PeerReady)
        {
            Finish(false, "goodbye_cleared_peer", lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        var hbWait = Time.realtimeSinceStartup + heartbeatTimeoutSeconds + 1.5f;
        while ((!hostUdp.PeerLostByHeartbeat || hostUdp.PeerReady) && Time.realtimeSinceStartup < hbWait)
            yield return null;

        if (!hostUdp.PeerLostByHeartbeat || hostUdp.PeerReady)
        {
            Finish(false, "heartbeat_not_fired", lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        if (!lobby.TryCompleteDisconnectForfeit(
                disconnectedSeat,
                Time.time - fightStart,
                out var summary,
                out var forfeitErr))
        {
            Finish(false, "forfeit:" + forfeitErr, lobby, MatchSummary.None, hostUdp);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        hostUdp.PublishMatchOutcome(summary);
        MatchResultsStub.Present(summary, "host", persist: true);
        lobby.Close();

        var pass = lobby.Phase == MatchPhase.Closed &&
                   summary.Outcome.Finished &&
                   summary.Outcome.Reason == MatchWinReason.DisconnectForfeit &&
                   summary.Outcome.WinnerRobotId == 0 &&
                   summary.Outcome.LoserRobotId == disconnectedSeat &&
                   hostUdp.PublishedMatchOutcomes >= 1 &&
                   hostUdp.PeerLostByHeartbeat &&
                   !hostUdp.PeerReady;

        Finish(pass, pass ? "ok" : "fail", lobby, summary, hostUdp);
        Cleanup(hostGo, clientGo);
    }

    void Finish(
        bool pass,
        string reason,
        MatchLobbySession lobby,
        MatchSummary summary,
        PhysicsTestUdpTransport hostUdp)
    {
        Debug.Log(
            $"[S9-04] VERIFIER_DONE pass={pass} reason={reason} policy={MatchDisconnectPolicy.PolicyId} " +
            $"heartbeat={MatchPeerHeartbeat.PolicyTag} phase={lobby?.Phase} session={lobby?.SessionId} " +
            $"host_pub_outcome={hostUdp?.PublishedMatchOutcomes ?? 0} " +
            $"winner={summary.Outcome.WinnerRobotId} loser={summary.Outcome.LoserRobotId} " +
            $"host_reason={summary.Outcome.Reason} peer_host={hostUdp?.PeerReady} " +
            $"lost_by_hb={hostUdp?.PeerLostByHeartbeat} hb_timeouts={hostUdp?.HeartbeatTimeoutCount}");
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

            hostGo.GetComponent<PhysicsTestUdpTransport>()?.StopTransport();
            Object.Destroy(hostGo);
        }

        if (clientGo != null)
        {
            clientGo.GetComponent<PhysicsTestUdpTransport>()?.AbortTransport();
            Object.Destroy(clientGo);
        }
    }
}
