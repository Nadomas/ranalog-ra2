using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S2-09 remote client verifier. Admits valid/invalid blueprints over UDP, drives via
/// commands only (prediction OFF), asserts graph + poses + combat disable events.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotModularUdpClientVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 20f;
    [SerializeField] float driveSeconds = 2.0f;
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestUdpClient client;
    [SerializeField] PhysicsTestNetRoleBootstrap bootstrap;

    readonly List<RobotSpawnEvent> spawnScratch = new List<RobotSpawnEvent>(8);
    readonly Dictionary<int, string[]> clientGraphs = new Dictionary<int, string[]>(4);
    readonly List<string> rejectReasons = new List<string>(4);

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(
        PhysicsTestUdpTransport udp,
        PhysicsTestUdpClient udpClient,
        PhysicsTestNetRoleBootstrap roleBootstrap)
    {
        transport = udp;
        client = udpClient;
        bootstrap = roleBootstrap;
    }

    void Start()
    {
        if (!autoRun)
            return;
        ResolveRefs();
        if (bootstrap != null && bootstrap.ResolvedRole != PhysicsTestNetRole.Client)
            return;
        StartCoroutine(RunSequence());
    }

    void ResolveRefs()
    {
        if (transport == null) transport = FindAnyObjectByType<PhysicsTestUdpTransport>();
        if (client == null) client = FindAnyObjectByType<PhysicsTestUdpClient>();
        if (bootstrap == null) bootstrap = FindAnyObjectByType<PhysicsTestNetRoleBootstrap>();
    }

    IEnumerator RunSequence()
    {
        if (transport == null || client == null)
        {
            Finish(false, "missing_refs");
            yield break;
        }

        client.ResetMetrics();
        Log($"CLIENT_CONNECT host={transport.RemoteHost}:{transport.HostPort}");

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while (!transport.PeerReady && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!transport.PeerReady)
        {
            Finish(false, "peer_timeout");
            yield break;
        }

        Log("CLIENT_PEER_READY");

        var bpA = RobotBlueprint.CreatePhysicsTestSampleA(new Vector3(-4f, 0.75f, 0f), 90f);
        var bpB = RobotBlueprint.CreatePhysicsTestSampleB(new Vector3(4f, 0.75f, 0f), -90f);
        var expectedA = ComponentIds(bpA);
        var expectedB = ComponentIds(bpB);

        client.SendSpawn(RobotSpawnRequest.CreateSpawn(0, 0, bpA));
        client.SendSpawn(RobotSpawnRequest.CreateSpawn(1, 1, bpB));
        // Invalid: unsupported schema (host must reject; client must see Rejected event).
        client.SendSpawn(new RobotSpawnRequest
        {
            RobotId = 2,
            SourceId = 0,
            Despawn = false,
            Schema = "ra2.robot_blueprint.v999",
            BlueprintJson = "{}"
        });

        var spawnWait = Time.realtimeSinceStartup + 15f;
        while ((clientGraphs.Count < 2 || rejectReasons.Count < 1) &&
               Time.realtimeSinceStartup < spawnWait)
        {
            DrainSpawnEvents();
            yield return new WaitForFixedUpdate();
        }

        DrainSpawnEvents();

        var graphOk = clientGraphs.ContainsKey(0) && clientGraphs.ContainsKey(1) &&
                      GraphsMatch(expectedA, clientGraphs[0]) &&
                      GraphsMatch(expectedB, clientGraphs[1]);
        var rejectOk = rejectReasons.Exists(r =>
            r != null && r.IndexOf("unsupported_schema", System.StringComparison.Ordinal) >= 0);
        if (!graphOk || !rejectOk)
        {
            Finish(false, $"spawn_events_incomplete graph_ok={graphOk} reject_ok={rejectOk}");
            yield break;
        }

        Log("CLIENT_SPAWN_OK");

        var end = Time.realtimeSinceStartup + driveSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f, Turn = 0.2f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 1f, Turn = -0.2f }));
            yield return new WaitForFixedUpdate();
        }

        client.SendCombat(new PhysicsTestCombatRequest
        {
            AttackerRobotId = 0,
            SourceId = 0,
            TargetRobotId = 1,
            ImpulseWorld = new Vector3(0f, 0f, 18f)
        });

        var combatWait = Time.realtimeSinceStartup + 5f;
        while (client.ReceivedCombatEvents < 1 && Time.realtimeSinceStartup < combatWait)
            yield return null;

        yield return new WaitForSeconds(0.5f);
        DrainSpawnEvents();

        var posesOk = client.ReceivedSnapshotBatches > 0 && client.LastPoses.Length >= 2;
        var combatOk = client.ReceivedCombatEvents > 0 && client.LastCombatEvent.TargetDisabled;
        var transportOk = transport.EnqueuedCommands > 0 &&
                          transport.EnqueuedSpawnRequests >= 3 &&
                          transport.DeliveredSpawnEvents >= 3;
        var pass = transport.PeerReady && graphOk && rejectOk && posesOk && combatOk && transportOk;

        Finish(pass, pass ? "ok" : "fail");
    }

    void DrainSpawnEvents()
    {
        if (client == null)
            return;

        spawnScratch.Clear();
        if (client.DrainSpawnEvents(spawnScratch) <= 0)
            return;

        for (var i = 0; i < spawnScratch.Count; i++)
        {
            var ev = spawnScratch[i];
            if (!ev.Accepted)
            {
                rejectReasons.Add(ev.RejectReason ?? "rejected");
                continue;
            }

            if (ev.Despawned)
            {
                clientGraphs.Remove(ev.RobotId);
                continue;
            }

            try
            {
                var bp = RobotBlueprintSerializer.FromJson(ev.BlueprintJson);
                clientGraphs[ev.RobotId] = ComponentIds(bp);
            }
            catch (System.Exception)
            {
                // Leave missing → graphOk fails.
            }
        }
    }

    void Finish(bool pass, string reason)
    {
        var poseA = client != null && client.LastPoses.Length > 0 ? client.LastPoses[0].Position : Vector3.zero;
        var poseB = client != null && client.LastPoses.Length > 1 ? client.LastPoses[1].Position : Vector3.zero;
        var reject = rejectReasons.Count > 0 ? rejectReasons[0] : "";
        var line =
            $"[S2-09] CLIENT_VERIFIER_DONE pass={pass} reason={reason} " +
            $"graphs={clientGraphs.Count} rejects={rejectReasons.Count} reject_reason={reject} " +
            $"cmds={transport?.EnqueuedCommands ?? 0} spawn_reqs={transport?.EnqueuedSpawnRequests ?? 0} " +
            $"spawn_evts={transport?.DeliveredSpawnEvents ?? 0} snaps={client?.ReceivedSnapshotBatches ?? 0} " +
            $"combat_events={client?.ReceivedCombatEvents ?? 0} " +
            $"target_disabled={(client != null && client.LastCombatEvent.TargetDisabled)} " +
            $"poseA={F3(poseA.x)},{F3(poseA.y)},{F3(poseA.z)} " +
            $"poseB={F3(poseB.x)},{F3(poseB.y)},{F3(poseB.z)} " +
            $"dropped_spawn={transport?.DroppedSpawnPayloads ?? 0} err='{transport?.LastError}'";
        Debug.Log(line);
        Log(line);

        if (!Application.isEditor)
        {
            Log("CLIENT_QUIT");
            Application.Quit(pass ? 0 : 1);
        }
    }

    static string[] ComponentIds(RobotBlueprint bp)
    {
        var comps = bp.Components ?? System.Array.Empty<RobotComponentDef>();
        var ids = new string[comps.Length];
        for (var i = 0; i < comps.Length; i++)
            ids[i] = comps[i].Id ?? string.Empty;
        return ids;
    }

    static bool GraphsMatch(string[] expected, string[] actual)
    {
        if (expected == null || actual == null || expected.Length != actual.Length)
            return false;
        for (var i = 0; i < expected.Length; i++)
        {
            if (!string.Equals(expected[i], actual[i], System.StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    void Log(string msg)
    {
        if (bootstrap != null)
            bootstrap.TryAppendLog(msg);
        else
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "_s2_09_client.txt"));
                File.AppendAllText(path, msg + "\n");
            }
            catch
            {
                // ignore
            }
        }
    }

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
