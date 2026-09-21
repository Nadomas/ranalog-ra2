using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// S2-06/S2-07 remote client verifier. Sends drive + combat over UDP; asserts pose/combat events.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestCrossProcessClientVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 20f;
    [SerializeField] float driveSeconds = 2.0f;
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestUdpClient client;
    [SerializeField] PhysicsTestNetRoleBootstrap bootstrap;

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
        var end = Time.realtimeSinceStartup + driveSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f, Turn = 0.2f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 1f, Turn = -0.2f }));
            yield return new WaitForFixedUpdate();
        }

        // Request thin combat impulse on B (host is authoritative).
        client.SendCombat(new PhysicsTestCombatRequest
        {
            AttackerRobotId = 0,
            SourceId = 0,
            TargetRobotId = 1,
            ImpulseWorld = new Vector3(0f, 0f, 18f)
        });

        var combatWait = Time.realtimeSinceStartup + 3f;
        while (client.ReceivedCombatEvents < 1 && Time.realtimeSinceStartup < combatWait)
            yield return null;

        // Give host time to publish more poses after combat.
        yield return new WaitForSeconds(0.5f);

        var posesOk = client.ReceivedSnapshotBatches > 0 && client.LastPoses.Length >= 2;
        var combatOk = client.ReceivedCombatEvents > 0 && client.LastCombatEvent.TargetDisabled;
        var pass = transport.PeerReady && posesOk && combatOk && transport.EnqueuedCommands > 0;

        Finish(pass, pass ? "ok" : "fail");
    }

    void Finish(bool pass, string reason)
    {
        var poseA = client.LastPoses.Length > 0 ? client.LastPoses[0].Position : Vector3.zero;
        var poseB = client.LastPoses.Length > 1 ? client.LastPoses[1].Position : Vector3.zero;
        var line =
            $"[S2-06] CLIENT_VERIFIER_DONE pass={pass} reason={reason} " +
            $"cmds={transport.EnqueuedCommands} snaps={client.ReceivedSnapshotBatches} " +
            $"combat_events={client.ReceivedCombatEvents} target_disabled={client.LastCombatEvent.TargetDisabled} " +
            $"poseA={F3(poseA.x)},{F3(poseA.y)},{F3(poseA.z)} " +
            $"poseB={F3(poseB.x)},{F3(poseB.y)},{F3(poseB.z)} " +
            $"max_pose_step={F3(client.MaxPoseStep)} err='{transport.LastError}'";
        Debug.Log(line);
        Log(line);

        var combatLine =
            $"[S2-07] CLIENT_VERIFIER_DONE pass={(pass && client.LastCombatEvent.TargetDisabled)} " +
            $"event_seq={client.LastCombatEvent.Sequence} target={client.LastCombatEvent.TargetRobotId} " +
            $"disabled={client.LastCombatEvent.TargetDisabled} hits={client.LastCombatEvent.TargetHitCount}";
        Debug.Log(combatLine);
        Log(combatLine);

        // Quit player builds after smoke so automation can join.
        if (!Application.isEditor)
        {
            Log("CLIENT_QUIT");
            Application.Quit(pass ? 0 : 1);
        }
    }

    void Log(string msg)
    {
        if (bootstrap != null)
            bootstrap.TryAppendLog(msg);
        else
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "_s2_net_client.txt"));
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
