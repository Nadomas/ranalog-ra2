using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-05 verifier. Loopback spawn of RA2 v1 blueprint; payload without Control Board is rejected.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotNetSpawnV1Verifier : MonoBehaviour
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
    readonly List<string> rejectReasons = new List<string>(4);

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
        if (transport == null || client == null || spawner == null)
        {
            Debug.Log("[S3-05] VERIFIER_DONE pass=False reason=missing_refs");
            yield break;
        }

        var valid = RobotBlueprint.CreateRa2TankSteerSample(new Vector3(-4f, 0.75f, 0f), 90f);
        var invalid = RobotBlueprint.CreateInvalidNoControlBoard(new Vector3(4f, 0.75f, 0f), -90f);
        var expected = ComponentIds(valid);

        transport.EnqueueSpawnRequest(RobotSpawnRequest.CreateSpawn(0, 0, valid));
        transport.EnqueueSpawnRequest(RobotSpawnRequest.CreateSpawn(1, 1, invalid));

        for (var i = 0; i < 10; i++)
        {
            DrainClientSpawnEvents();
            if (spawner.LiveCount >= 1 && rejectReasons.Count >= 1 && clientGraphs.ContainsKey(0))
                break;
            yield return new WaitForFixedUpdate();
        }

        DrainClientSpawnEvents();

        var rejectOk = spawner.RejectedSpawns >= 1 &&
                       rejectReasons.Exists(r => r != null && r.IndexOf("control_board", System.StringComparison.Ordinal) >= 0);
        var spawnOk = spawner.AcceptedSpawns >= 1 &&
                      spawner.LiveCount == 1 &&
                      clientGraphs.ContainsKey(0) &&
                      GraphsMatch(expected, clientGraphs[0]);
        var schemaOk = valid.HasV1Fields &&
                       string.Equals(RobotSpawnRequest.CreateSpawn(0, 0, valid).Schema,
                           RobotBlueprintSerializer.SchemaIdV1, System.StringComparison.Ordinal);

        PhysicsTestDrive drive = null;
        for (var i = 0; i < spawner.Live.Count; i++)
        {
            if (spawner.Live[i].RobotId == 0)
                drive = spawner.Live[i].Drive;
        }

        if (drive == null)
        {
            Debug.Log(
                $"[S3-05] VERIFIER_DONE pass=False reason=no_live_drive " +
                $"accepted={spawner.AcceptedSpawns} rejected={spawner.RejectedSpawns} " +
                $"reject_ok={rejectOk} spawn_ok={spawnOk}");
            yield break;
        }

        var start = drive.transform.position;
        var cmd = RobotWiringDriveResolver.ResolveTankDrive(
            valid,
            new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0.2f });

        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, cmd));
            yield return new WaitForFixedUpdate();
        }

        client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Brake = true }));
        yield return new WaitForFixedUpdate();

        var delta = Vector3.Distance(start, drive.transform.position);
        var nan = IsBad(drive.transform.position);
        var moved = delta > 0.05f;
        var boardOk = drive.transform.Find("control_board") != null;
        var assembleOk = spawner.MaxAssembleMs < 50.0;
        var pass = !nan && moved && spawnOk && rejectOk && schemaOk && boardOk && assembleOk &&
                   !clientGraphs.ContainsKey(1);

        var reason = rejectReasons.Count > 0 ? rejectReasons[0] : "";
        Debug.Log(
            $"[S3-05] VERIFIER_DONE pass={pass} nan={nan} moved={moved} spawn_ok={spawnOk} " +
            $"reject_ok={rejectOk} schema_ok={schemaOk} board_ok={boardOk} assemble_ok={assembleOk} " +
            $"delta={F3(delta)} live={spawner.LiveCount} accepted={spawner.AcceptedSpawns} " +
            $"rejected={spawner.RejectedSpawns} reject_reason={reason} " +
            $"schema={RobotBlueprintSerializer.SchemaIdV1} spawned_from=v1_loopback");
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
            {
                rejectReasons.Add(ev.RejectReason ?? "rejected");
                continue;
            }

            try
            {
                var bp = RobotBlueprintSerializer.FromJson(ev.BlueprintJson);
                clientGraphs[ev.RobotId] = ComponentIds(bp);
            }
            catch (System.Exception)
            {
                // graph missing → spawnOk fails
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

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
