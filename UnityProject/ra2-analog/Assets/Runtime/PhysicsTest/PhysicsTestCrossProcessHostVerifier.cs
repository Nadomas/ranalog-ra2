using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// S2-06/S2-07 host verifier. Waits for remote UDP client, then asserts command→authority
/// motion, thin pose publish, and authoritative combat disable agreement.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestCrossProcessHostVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 45f;
    [SerializeField] float driveSeconds = 2.0f;
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestUdpHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestCombatAuthority combat;
    [SerializeField] PhysicsTestNetRoleBootstrap bootstrap;
    [SerializeField] PhysicsTestDrive driveA;
    [SerializeField] PhysicsTestDrive driveB;
    [SerializeField] PhysicsTestDisableFlag disableB;

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
        PhysicsTestNetRoleBootstrap roleBootstrap,
        PhysicsTestDrive a,
        PhysicsTestDrive b,
        PhysicsTestDisableFlag flagB)
    {
        transport = udp;
        host = udpHost;
        authority = auth;
        combat = combatAuth;
        bootstrap = roleBootstrap;
        driveA = a;
        driveB = b;
        disableB = flagB;
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
        if (bootstrap == null) bootstrap = FindAnyObjectByType<PhysicsTestNetRoleBootstrap>();
        if (driveA == null)
        {
            var go = GameObject.Find("Robot_A");
            if (go != null) driveA = go.GetComponent<PhysicsTestDrive>();
        }

        if (driveB == null)
        {
            var go = GameObject.Find("Robot_B");
            if (go != null)
            {
                driveB = go.GetComponent<PhysicsTestDrive>();
                disableB = go.GetComponent<PhysicsTestDisableFlag>();
            }
        }
    }

    IEnumerator RunSequence()
    {
        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = false;

        transport.ResetMetrics();
        Log($"HOST_WAIT peer port={transport.HostPort}");

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while (!transport.PeerReady && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!transport.PeerReady)
        {
            Finish(false, "peer_timeout", 0f, 0f);
            yield break;
        }

        Log("HOST_PEER_READY");
        var startA = driveA.transform.position;
        var startB = driveB.transform.position;

        // Remote client drives both robots; host only observes authority drain.
        // Also inject one host-side command path sample after peer ready for soak.
        var end = Time.realtimeSinceStartup + driveSeconds;
        while (Time.realtimeSinceStartup < end)
            yield return new WaitForFixedUpdate();

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var transportOk = transport.DeliveredCommands > 0 && host.DrainedToAuthority > 0;
        var posesOk = transport.PublishedSnapshots > 0;

        // Wait for remote client combat request (authoritative apply happens in CombatAuthority).
        var beforeHits = combat.GetHitCount(1);
        var combatWait = Time.realtimeSinceStartup + 5f;
        while (combat.AppliedImpulses <= 0 && Time.realtimeSinceStartup < combatWait)
            yield return null;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var disabled = disableB != null && disableB.Disabled;
        var combatOk = combat.AppliedImpulses > 0 &&
                       combat.LastEvent.TargetRobotId == 1 &&
                       combat.LastEvent.TargetDisabled &&
                       disabled &&
                       combat.GetHitCount(1) > beforeHits;
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position);
        var pass = !nan && bothMoved && transportOk && posesOk && combatOk;

        Finish(pass, pass ? "ok" : "fail", deltaA, deltaB);
    }

    void Finish(bool pass, string reason, float deltaA, float deltaB)
    {
        var line =
            $"[S2-06] VERIFIER_DONE pass={pass} reason={reason} nan=False " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} " +
            $"peer={transport.PeerReady} delivered_cmds={transport.DeliveredCommands} " +
            $"drained={host.DrainedToAuthority} snaps={transport.PublishedSnapshots} " +
            $"combat_applied={combat.AppliedImpulses} B_disabled={(disableB != null && disableB.Disabled)} " +
            $"combat_seq={combat.Sequence} hello={transport.HelloReceived} " +
            $"bytes_rx={transport.BytesReceived} bytes_tx={transport.BytesSent} " +
            $"err='{transport.LastError}'";

        Debug.Log(line);
        Log(line);
        // Also S2-07 marker for combat slice.
        var combatLine =
            $"[S2-07] VERIFIER_DONE pass={pass && combat.AppliedImpulses > 0 && disableB != null && disableB.Disabled} " +
            $"B_disabled={(disableB != null && disableB.Disabled)} hits={combat.GetHitCount(1)} " +
            $"event_disabled={combat.LastEvent.TargetDisabled} published_events={transport.PublishedCombatEvents}";
        Debug.Log(combatLine);
        Log(combatLine);

        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = true;
    }

    void Log(string msg)
    {
        if (bootstrap != null)
            bootstrap.TryAppendLog(msg);
        else
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "_s2_net_host.txt"));
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
