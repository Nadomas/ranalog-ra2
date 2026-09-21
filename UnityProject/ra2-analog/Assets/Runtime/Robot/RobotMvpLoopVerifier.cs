using System.Collections;
using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S11 thin MVP glue: workshop Design→Configure→Test → combat admit (local + one UDP MP path)
/// ending in shared <see cref="MatchResultsStub"/> presentation.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMvpLoopVerifier : MonoBehaviour
{
    const int MatchPort = 7792;

    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 0.45f;
    [SerializeField] float immobileNeed = 1.1f;
    [SerializeField] float peerWaitSeconds = 8f;
    [SerializeField] float spawnWaitSeconds = 12f;
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

        var color = new Color(0.3f, 0.55f, 0.85f);
        var workshop = new RobotWorkshopSession();
        workshop.SetWorkingBlueprint(
            RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(-3.5f, 0.85f, 0f), 90f));

        if (!workshop.TrySwitchMode(WorkshopMode.Design, null, slideMaterial, color, out var err) ||
            !workshop.TrySwitchMode(WorkshopMode.Configure, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S11] VERIFIER_DONE pass=False reason=workshop_cfg err={err}");
            yield break;
        }

        RobotControlConfigurer.ApplyDrivePreset(
            workshop.WorkingBlueprint, RobotControlConfigurer.DrivePreset.TankSteer);

        if (!workshop.TrySwitchMode(WorkshopMode.Test, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S11] VERIFIER_DONE pass=False reason=workshop_test err={err}");
            yield break;
        }

        // Brief test drive proves workshop path still works before combat handoff.
        var testDrive = workshop.TestInstance?.Drive;
        if (testDrive == null)
        {
            Debug.Log("[S11] VERIFIER_DONE pass=False reason=no_test_drive");
            yield break;
        }

        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            testDrive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(workshop.WorkingBlueprint, cmd));
            yield return new WaitForFixedUpdate();
        }

        if (!workshop.TryPrepareCombatAdmit(out var admitBp, out var admitJson, out err))
        {
            Debug.Log($"[S11] VERIFIER_DONE pass=False reason=prepare_admit err={err}");
            yield break;
        }

        // --- Local combat admit path ---
        var localOk = false;
        MatchSummary localSummary = MatchSummary.None;
        yield return RunLocalCombat(admitBp, slideMaterial, cmd, s =>
        {
            localOk = s.Outcome.Finished && s.Outcome.Reason == MatchWinReason.Immobilized;
            localSummary = s;
        });

        if (!localOk)
        {
            Debug.Log(
                $"[S11] VERIFIER_DONE pass=False reason=local_combat " +
                $"finished={localSummary.Outcome.Finished} reason={localSummary.Outcome.Reason}");
            yield break;
        }

        MatchResultsStub.Present(localSummary, "local");

        // --- MP UDP combat admit path (same workshop blueprint) ---
        var mpOk = false;
        MatchSummary hostSummary = MatchSummary.None;
        MatchSummary clientSummary = MatchSummary.None;
        yield return RunMpCombat(admitBp, admitJson, slideMaterial, cmd, (h, c, ok) =>
        {
            hostSummary = h;
            clientSummary = c;
            mpOk = ok;
        });

        var pass = localOk && mpOk &&
                   hostSummary.Outcome.WinnerRobotId == clientSummary.Outcome.WinnerRobotId &&
                   hostSummary.Outcome.LoserRobotId == clientSummary.Outcome.LoserRobotId &&
                   hostSummary.Outcome.Reason == MatchWinReason.Immobilized;

        Debug.Log(
            $"[S11] VERIFIER_DONE pass={pass} local_ok={localOk} mp_ok={mpOk} " +
            $"local_reason={localSummary.Outcome.Reason} " +
            $"mp_winner={hostSummary.Outcome.WinnerRobotId} mp_loser={hostSummary.Outcome.LoserRobotId} " +
            $"client_reason={clientSummary.Outcome.Reason} " +
            $"json_len={admitJson?.Length ?? 0} workshop_switches={workshop.SwitchCount}");
    }

    IEnumerator RunLocalCombat(
        RobotBlueprint workshopBp,
        PhysicsMaterial slide,
        RobotWiringDriveResolver.ControlState cmd,
        System.Action<MatchSummary> done)
    {
        var bpA = RobotBlueprintSerializer.FromJson(RobotBlueprintSerializer.ToJson(workshopBp));
        bpA.RootPosition = new Vector3(-3.5f, 0.85f, 0f);
        bpA.RootYawDegrees = 90f;
        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.85f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        if (!RobotSpawnService.TryValidate(bpA, out _) || !RobotSpawnService.TryValidate(bpB, out _))
        {
            done(MatchSummary.None);
            yield break;
        }

        var a = RobotSpawnService.Spawn(bpA, 0, 0, null, slide, new Color(0.2f, 0.55f, 1f));
        var b = RobotSpawnService.Spawn(bpB, 1, 1, null, slide, new Color(1f, 0.4f, 0.2f));

        var fightStart = Time.time;
        var driveEnd = Time.time + driveSeconds;
        while (Time.time < driveEnd)
        {
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        RobotDamageService.TryFunctionalDisable(a, out _);

        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.2f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var safety = Time.time + immobileNeed + 2.5f;
        while (Time.time < safety && !outcome.Finished)
        {
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);
            yield return new WaitForFixedUpdate();
        }

        var summary = new MatchSummary(
            outcome,
            Time.time - fightStart,
            rules.GetImmobileSeconds(0),
            rules.GetImmobileSeconds(1),
            RobotDamageService.IsFunctionallyDisabled(a),
            "s11-local");

        RobotSpawnService.Despawn(a);
        RobotSpawnService.Despawn(b);
        done(summary);
    }

    IEnumerator RunMpCombat(
        RobotBlueprint workshopBp,
        string admitJson,
        PhysicsMaterial slide,
        RobotWiringDriveResolver.ControlState cmd,
        System.Action<MatchSummary, MatchSummary, bool> done)
    {
        var hostGo = new GameObject("S11_ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var hostUdp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        hostUdp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", MatchPort);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(hostUdp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(hostUdp, System.Array.Empty<PhysicsTestCombatAuthority.TrackedRobot>(), 1);

        var spawner = hostGo.AddComponent<RobotHostSpawnerUdp>();
        spawner.Configure(hostUdp, authority, udpHost, combat, slide);

        var clientGo = new GameObject("S11_UdpClient");
        var clientUdp = clientGo.AddComponent<PhysicsTestUdpTransport>();
        clientUdp.Configure(PhysicsTestNetRole.Client, "127.0.0.1", MatchPort);
        var clientPeer = clientGo.AddComponent<PhysicsTestUdpClient>();
        clientPeer.Configure(clientUdp);

        hostUdp.StartTransport();
        clientUdp.StartTransport();

        var lobby = new MatchLobbySession(2, "s11-mp");
        lobby.SetPeerReady(0);

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while ((!hostUdp.PeerReady || !clientUdp.PeerReady) && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!hostUdp.PeerReady || !clientUdp.PeerReady)
        {
            done(MatchSummary.None, MatchSummary.None, false);
            DestroyPair(hostGo, clientGo, spawner);
            yield break;
        }

        lobby.SetPeerReady(1);
        lobby.TryBeginAdmit(out _);

        var bpA = RobotBlueprintSerializer.FromJson(admitJson);
        bpA.RootPosition = new Vector3(-3.5f, 0.85f, 0f);
        bpA.RootYawDegrees = 90f;
        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.85f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        lobby.TryMarkAdmitted(0, bpA, out _);
        lobby.TryMarkAdmitted(1, bpB, out _);

        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(0, 0, bpA));
        clientPeer.SendSpawn(RobotSpawnRequest.CreateSpawn(1, 1, bpB));

        var spawnEnd = Time.realtimeSinceStartup + spawnWaitSeconds;
        while (spawner.LiveCount < 2 && Time.realtimeSinceStartup < spawnEnd)
            yield return new WaitForFixedUpdate();

        if (spawner.LiveCount < 2 || !lobby.TryBeginFight(out _))
        {
            done(MatchSummary.None, MatchSummary.None, false);
            DestroyPair(hostGo, clientGo, spawner);
            yield break;
        }

        RobotSpawnedInstance instA = null;
        RobotSpawnedInstance instB = null;
        for (var i = 0; i < spawner.Live.Count; i++)
        {
            if (spawner.Live[i].RobotId == 0) instA = spawner.Live[i];
            if (spawner.Live[i].RobotId == 1) instB = spawner.Live[i];
        }

        var fightStart = Time.time;
        var driveEnd = Time.time + driveSeconds;
        while (Time.time < driveEnd)
        {
            instA.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            instB.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        RobotDamageService.TryFunctionalDisable(instA, out _);

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

        var summary = new MatchSummary(
            outcome,
            Time.time - fightStart,
            rules.GetImmobileSeconds(0),
            rules.GetImmobileSeconds(1),
            true,
            lobby.SessionId);
        lobby.CompleteWithSummary(summary);
        hostUdp.PublishMatchOutcome(summary);
        MatchResultsStub.Present(summary, "mp-host");

        var received = new List<MatchSummary>(2);
        var outcomeWait = Time.realtimeSinceStartup + 3f;
        while (received.Count < 1 && Time.realtimeSinceStartup < outcomeWait)
        {
            clientUdp.DrainMatchOutcomes(received);
            yield return null;
        }

        var clientSum = received.Count > 0 ? received[received.Count - 1] : MatchSummary.None;
        if (received.Count > 0)
            MatchResultsStub.Present(clientSum, "mp-client");

        lobby.Close();
        var ok = outcome.Finished &&
                 outcome.Reason == MatchWinReason.Immobilized &&
                 received.Count > 0 &&
                 clientSum.Outcome.WinnerRobotId == summary.Outcome.WinnerRobotId;

        done(summary, clientSum, ok);
        DestroyPair(hostGo, clientGo, spawner);
    }

    static void DestroyPair(GameObject hostGo, GameObject clientGo, RobotHostSpawnerUdp spawner)
    {
        if (spawner != null)
        {
            for (var i = spawner.Live.Count - 1; i >= 0; i--)
                RobotSpawnService.Despawn(spawner.Live[i]);
        }

        hostGo?.GetComponent<PhysicsTestUdpTransport>()?.StopTransport();
        clientGo?.GetComponent<PhysicsTestUdpTransport>()?.StopTransport();
        if (hostGo != null) Object.Destroy(hostGo);
        if (clientGo != null) Object.Destroy(clientGo);
    }
}
