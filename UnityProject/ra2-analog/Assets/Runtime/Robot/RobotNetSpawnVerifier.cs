using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-03 verifier. Empty arena → client admits blueprints over loopback → host assembles
/// via shared spawn path → drive + pose smoke → despawn coordination.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotNetSpawnVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.2f;
    [SerializeField] PhysicsTestLoopbackTransport transport;
    [SerializeField] PhysicsTestTransportClient client;
    [SerializeField] PhysicsTestTransportHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] RobotHostSpawner spawner;
    [SerializeField] PhysicsMaterial slideMaterial;

    readonly List<RobotSpawnEvent> spawnEventScratch = new List<RobotSpawnEvent>(8);
    readonly Dictionary<int, string[]> clientGraphs = new Dictionary<int, string[]>(4);
    readonly HashSet<int> clientDespawned = new HashSet<int>();

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(
        PhysicsTestLoopbackTransport loopback,
        PhysicsTestTransportClient transportClient,
        PhysicsTestTransportHost transportHost,
        PhysicsTestLocalAuthority auth,
        RobotHostSpawner hostSpawner,
        PhysicsMaterial slide)
    {
        transport = loopback;
        client = transportClient;
        host = transportHost;
        authority = auth;
        spawner = hostSpawner;
        slideMaterial = slide;
    }

    void Start()
    {
        if (!autoRun)
            return;
        ResolveRefs();
        StartCoroutine(RunSequence());
    }

    void ResolveRefs()
    {
        if (transport == null)
            transport = FindAnyObjectByType<PhysicsTestLoopbackTransport>();
        if (client == null)
            client = FindAnyObjectByType<PhysicsTestTransportClient>();
        if (host == null)
            host = FindAnyObjectByType<PhysicsTestTransportHost>();
        if (authority == null)
            authority = FindAnyObjectByType<PhysicsTestLocalAuthority>();
        if (spawner == null)
            spawner = FindAnyObjectByType<RobotHostSpawner>();
    }

    IEnumerator RunSequence()
    {
        if (transport == null || client == null || spawner == null || authority == null)
        {
            Debug.Log("[S3-03] VERIFIER_DONE pass=False reason=missing_refs");
            yield break;
        }

        var bpA = RobotBlueprint.CreatePhysicsTestSampleA(new Vector3(-4f, 0.75f, 0f), 90f);
        var bpB = RobotBlueprint.CreatePhysicsTestSampleB(new Vector3(4f, 0.75f, 0f), -90f);
        var expectedA = ComponentIds(bpA);
        var expectedB = ComponentIds(bpB);

        transport.EnqueueSpawnRequest(RobotSpawnRequest.CreateSpawn(0, 0, bpA));
        transport.EnqueueSpawnRequest(RobotSpawnRequest.CreateSpawn(1, 1, bpB));

        // Wait for host FixedUpdate + client drain of spawn events.
        for (var i = 0; i < 8; i++)
        {
            DrainClientSpawnEvents();
            if (spawner.LiveCount >= 2 && clientGraphs.Count >= 2)
                break;
            yield return new WaitForFixedUpdate();
        }

        DrainClientSpawnEvents();
        yield return null;
        DrainClientSpawnEvents();

        var spawnOk = spawner.AcceptedSpawns >= 2 && spawner.LiveCount == 2 &&
                      clientGraphs.ContainsKey(0) && clientGraphs.ContainsKey(1);
        var graphOk = spawnOk &&
                      GraphsMatch(expectedA, clientGraphs[0]) &&
                      GraphsMatch(expectedB, clientGraphs[1]);

        if (!spawnOk)
        {
            Debug.Log(
                $"[S3-03] VERIFIER_DONE pass=False reason=spawn_incomplete " +
                $"host_live={spawner.LiveCount} accepted={spawner.AcceptedSpawns} " +
                $"client_graphs={clientGraphs.Count} rejected={spawner.RejectedSpawns}");
            yield break;
        }

        PhysicsTestDrive driveA = null;
        PhysicsTestDrive driveB = null;
        for (var i = 0; i < spawner.Live.Count; i++)
        {
            var inst = spawner.Live[i];
            if (inst.RobotId == 0) driveA = inst.Drive;
            if (inst.RobotId == 1) driveB = inst.Drive;
        }

        if (driveA == null || driveB == null)
        {
            Debug.Log("[S3-03] VERIFIER_DONE pass=False reason=missing_drives_after_spawn");
            yield break;
        }

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var snapsBefore = client.ReceivedSnapshotBatches;

        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f, Turn = 0.2f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 1f, Turn = -0.2f }));
            yield return new WaitForFixedUpdate();
        }

        client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Brake = true }));
        client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Brake = true }));
        yield return new WaitForFixedUpdate();
        yield return null;
        DrainClientSpawnEvents();

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var rbA = driveA.GetComponent<Rigidbody>();
        var rbB = driveB.GetComponent<Rigidbody>();
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(rbA.linearVelocity) || IsBad(rbB.linearVelocity);
        var hingeOk = driveA.transform.Find("wheel_fl") != null &&
                      driveA.transform.Find("wheel_fl").GetComponent<HingeJoint>() != null;
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;

        var poses = client.LastPoses;
        var poseA = FindPose(poses, 0);
        var poseB = FindPose(poses, 1);
        var poseOk = poseA.HasValue && poseB.HasValue &&
                     Vector3.Distance(poseA.Value.Position, driveA.transform.position) < 0.35f &&
                     Vector3.Distance(poseB.Value.Position, driveB.transform.position) < 0.35f;

        var transportOk = transport.DeliveredCommands > 0 &&
                          host.DrainedToAuthority > 0 &&
                          client.ReceivedSnapshotBatches > snapsBefore &&
                          transport.DeliveredSpawnRequests >= 2 &&
                          transport.DeliveredSpawnEvents >= 2;

        // Despawn B and confirm client sees despawn event while A remains.
        transport.EnqueueSpawnRequest(RobotSpawnRequest.CreateDespawn(1, 1));
        for (var i = 0; i < 8; i++)
        {
            DrainClientSpawnEvents();
            if (clientDespawned.Contains(1) && spawner.LiveCount == 1)
                break;
            yield return new WaitForFixedUpdate();
        }

        DrainClientSpawnEvents();
        var despawnOk = spawner.AcceptedDespawns >= 1 &&
                        spawner.LiveCount == 1 &&
                        clientDespawned.Contains(1) &&
                        !clientGraphs.ContainsKey(1);

        // Assembly budget: sample robots must assemble well under match-start targets.
        var assembleBudgetOk = spawner.MaxAssembleMs < 50.0;

        var pass = !nan && bothMoved && hingeOk && graphOk && transportOk && poseOk &&
                   despawnOk && assembleBudgetOk && spawner.RejectedSpawns == 0;

        Debug.Log(
            $"[S3-03] VERIFIER_DONE pass={pass} nan={nan} both_moved={bothMoved} hinge_ok={hingeOk} " +
            $"graph_ok={graphOk} transport_ok={transportOk} pose_ok={poseOk} despawn_ok={despawnOk} " +
            $"assemble_budget_ok={assembleBudgetOk} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} " +
            $"assemble_ms_max={F3((float)spawner.MaxAssembleMs)} assemble_ms_last={F3((float)spawner.LastAssembleMs)} " +
            $"accepted_spawns={spawner.AcceptedSpawns} accepted_despawns={spawner.AcceptedDespawns} " +
            $"spawn_reqs={transport.DeliveredSpawnRequests} spawn_evts={transport.DeliveredSpawnEvents} " +
            $"snaps={client.ReceivedSnapshotBatches} schema={RobotBlueprintSerializer.SchemaId} " +
            $"spawned_from=blueprint_loopback");
    }

    void DrainClientSpawnEvents()
    {
        if (transport == null)
            return;

        spawnEventScratch.Clear();
        if (transport.DrainSpawnEvents(spawnEventScratch) <= 0)
            return;

        for (var i = 0; i < spawnEventScratch.Count; i++)
        {
            var ev = spawnEventScratch[i];
            if (!ev.Accepted)
                continue;

            if (ev.Despawned)
            {
                clientGraphs.Remove(ev.RobotId);
                clientDespawned.Add(ev.RobotId);
                continue;
            }

            try
            {
                var bp = RobotBlueprintSerializer.FromJson(ev.BlueprintJson);
                clientGraphs[ev.RobotId] = ComponentIds(bp);
            }
            catch (System.Exception)
            {
                // Leave graph missing → graphOk fails.
            }
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

    static PhysicsTestPoseSnapshot? FindPose(PhysicsTestPoseSnapshot[] poses, int robotId)
    {
        if (poses == null)
            return null;
        for (var i = 0; i < poses.Length; i++)
        {
            if (poses[i].RobotId == robotId)
                return poses[i];
        }

        return null;
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
