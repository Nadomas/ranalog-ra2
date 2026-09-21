using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// S2-09 host verifier. Empty arena → remote UDP client admits blueprints → host assembles
/// via <see cref="RobotHostSpawnerUdp"/> → motion + thin combat disable over UDP.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotModularUdpHostVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 45f;
    [SerializeField] float spawnWaitSeconds = 20f;
    [SerializeField] float driveSeconds = 2.0f;
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestUdpHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestCombatAuthority combat;
    [SerializeField] RobotHostSpawnerUdp spawner;
    [SerializeField] PhysicsTestNetRoleBootstrap bootstrap;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(
        PhysicsTestUdpTransport udp,
        PhysicsTestUdpHost udpHost,
        PhysicsTestLocalAuthority auth,
        PhysicsTestCombatAuthority combatAuth,
        RobotHostSpawnerUdp hostSpawner,
        PhysicsTestNetRoleBootstrap roleBootstrap)
    {
        transport = udp;
        host = udpHost;
        authority = auth;
        combat = combatAuth;
        spawner = hostSpawner;
        bootstrap = roleBootstrap;
    }

    void Start()
    {
        if (!autoRun)
            return;
        if (bootstrap != null && bootstrap.ResolvedRole == PhysicsTestNetRole.Client)
            return;

        ResolveRefs();
        StartCoroutine(RunSequence());
    }

    void ResolveRefs()
    {
        if (transport == null) transport = FindAnyObjectByType<PhysicsTestUdpTransport>();
        if (host == null) host = FindAnyObjectByType<PhysicsTestUdpHost>();
        if (authority == null) authority = FindAnyObjectByType<PhysicsTestLocalAuthority>();
        if (combat == null) combat = FindAnyObjectByType<PhysicsTestCombatAuthority>();
        if (spawner == null) spawner = FindAnyObjectByType<RobotHostSpawnerUdp>();
        if (bootstrap == null) bootstrap = FindAnyObjectByType<PhysicsTestNetRoleBootstrap>();
    }

    IEnumerator RunSequence()
    {
        if (transport == null || host == null || spawner == null || combat == null)
        {
            Finish(false, "missing_refs", 0f, 0f, null, null);
            yield break;
        }

        transport.ResetMetrics();
        Log($"HOST_WAIT peer port={transport.HostPort}");

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while (!transport.PeerReady && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!transport.PeerReady)
        {
            Finish(false, "peer_timeout", 0f, 0f, null, null);
            yield break;
        }

        Log("HOST_PEER_READY");

        var spawnEnd = Time.realtimeSinceStartup + spawnWaitSeconds;
        while ((spawner.LiveCount < 2 || spawner.RejectedSpawns < 1) &&
               Time.realtimeSinceStartup < spawnEnd)
            yield return new WaitForFixedUpdate();

        if (spawner.LiveCount < 2)
        {
            Finish(false, "spawn_incomplete", 0f, 0f, null, null);
            yield break;
        }

        PhysicsTestDrive driveA = null;
        PhysicsTestDrive driveB = null;
        PhysicsTestDisableFlag flagB = null;
        for (var i = 0; i < spawner.Live.Count; i++)
        {
            var inst = spawner.Live[i];
            if (inst.RobotId == 0) driveA = inst.Drive;
            if (inst.RobotId == 1)
            {
                driveB = inst.Drive;
                flagB = inst.DisableFlag;
            }
        }

        if (driveA == null || driveB == null)
        {
            Finish(false, "missing_drives", 0f, 0f, null, null);
            yield break;
        }

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var hingeOk = driveA.transform.Find("wheel_fl") != null &&
                      driveA.transform.Find("wheel_fl").GetComponent<HingeJoint>() != null;

        var driveEnd = Time.realtimeSinceStartup + driveSeconds;
        while (Time.realtimeSinceStartup < driveEnd)
            yield return new WaitForFixedUpdate();

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var transportOk = transport.DeliveredCommands > 0 &&
                          host.DrainedToAuthority > 0 &&
                          transport.DeliveredSpawnRequests >= 3 &&
                          transport.PublishedSpawnEvents >= 3 &&
                          transport.PublishedSnapshots > 0;

        var beforeHits = combat.GetHitCount(1);
        var combatWait = Time.realtimeSinceStartup + 8f;
        while (combat.AppliedImpulses <= 0 && Time.realtimeSinceStartup < combatWait)
            yield return null;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var disabled = flagB != null && flagB.Disabled;
        var combatOk = combat.AppliedImpulses > 0 &&
                       combat.LastEvent.TargetRobotId == 1 &&
                       combat.LastEvent.TargetDisabled &&
                       disabled &&
                       combat.GetHitCount(1) > beforeHits;
        var rejectOk = spawner.RejectedSpawns >= 1;
        var assembleOk = spawner.MaxAssembleMs < 50.0;
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position);
        var pass = !nan && bothMoved && hingeOk && transportOk && combatOk && rejectOk && assembleOk;

        Finish(pass, pass ? "ok" : "fail", deltaA, deltaB, driveA, driveB);
    }

    void Finish(bool pass, string reason, float deltaA, float deltaB, PhysicsTestDrive driveA, PhysicsTestDrive driveB)
    {
        var hingeOk = driveA != null &&
                      driveA.transform.Find("wheel_fl") != null &&
                      driveA.transform.Find("wheel_fl").GetComponent<HingeJoint>() != null;
        var line =
            $"[S2-09] VERIFIER_DONE pass={pass} reason={reason} " +
            $"live={spawner?.LiveCount ?? 0} accepted={spawner?.AcceptedSpawns ?? 0} " +
            $"rejected={spawner?.RejectedSpawns ?? 0} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} hinge_ok={hingeOk} " +
            $"peer={transport?.PeerReady} delivered_cmds={transport?.DeliveredCommands ?? 0} " +
            $"drained={host?.DrainedToAuthority ?? 0} snaps={transport?.PublishedSnapshots ?? 0} " +
            $"spawn_reqs={transport?.DeliveredSpawnRequests ?? 0} spawn_evts={transport?.PublishedSpawnEvents ?? 0} " +
            $"dropped_spawn={transport?.DroppedSpawnPayloads ?? 0} " +
            $"combat_applied={combat?.AppliedImpulses ?? 0} B_disabled={(combat != null && combat.IsDisabled(1))} " +
            $"assemble_ms_max={F3((float)(spawner?.MaxAssembleMs ?? 0))} " +
            $"err='{transport?.LastError}'";
        Debug.Log(line);
        Log(line);

        var combatLine =
            $"[S2-09] COMBAT_DONE pass={pass && combat != null && combat.AppliedImpulses > 0 && combat.IsDisabled(1)} " +
            $"B_disabled={(combat != null && combat.IsDisabled(1))} hits={(combat != null ? combat.GetHitCount(1) : 0)} " +
            $"event_disabled={(combat != null && combat.LastEvent.TargetDisabled)} " +
            $"published_events={transport?.PublishedCombatEvents ?? 0}";
        Debug.Log(combatLine);
        Log(combatLine);
    }

    void Log(string msg)
    {
        if (bootstrap != null)
            bootstrap.TryAppendLog(msg);
        else
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "_s2_09_host.txt"));
                File.AppendAllText(path, msg + "\n");
            }
            catch
            {
                // ignore
            }
        }
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
