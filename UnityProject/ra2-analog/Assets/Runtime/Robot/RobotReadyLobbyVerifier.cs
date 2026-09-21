using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S9-03: listen-host + client UDP peer link → both seats ready → Start (Lobby→Admitting).
/// No fight — ready/start gate only.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotReadyLobbyVerifier : MonoBehaviour
{
    const int MatchPort = 7793;

    [SerializeField] bool autoRun = true;
    [SerializeField] float peerWaitSeconds = 8f;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        var hostGo = new GameObject("S9_03_ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var hostUdp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        hostUdp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", MatchPort);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(hostUdp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var clientGo = new GameObject("S9_03_UdpClient");
        var clientUdp = clientGo.AddComponent<PhysicsTestUdpTransport>();
        clientUdp.Configure(PhysicsTestNetRole.Client, "127.0.0.1", MatchPort);
        clientGo.AddComponent<PhysicsTestUdpClient>().Configure(clientUdp);

        hostUdp.StartTransport();
        clientUdp.StartTransport();
        hostUdp.ResetMetrics();
        clientUdp.ResetMetrics();

        var chrome = gameObject.AddComponent<RobotReadyLobbyChrome>();
        chrome.EnsureFlow("s9-ready-stub");
        yield return null;

        // Start before peers linked must fail.
        var earlyStartBlocked = !chrome.TryStart(out var earlyErr) && earlyErr == "peers_not_linked";

        var waitEnd = Time.realtimeSinceStartup + peerWaitSeconds;
        while ((!hostUdp.PeerReady || !clientUdp.PeerReady) && Time.realtimeSinceStartup < waitEnd)
            yield return null;

        if (!hostUdp.PeerReady || !clientUdp.PeerReady)
        {
            Finish(false, "peer_timeout", chrome, hostUdp, clientUdp, earlyStartBlocked);
            Cleanup(hostGo, clientGo);
            yield break;
        }

        chrome.BindPeerLinks(hostUdp.PeerReady, clientUdp.PeerReady);

        // Start with peers linked but seats not ready must fail.
        var notReadyBlocked = !chrome.TryStart(out var nrErr) && nrErr == "peers_not_ready";

        var okHost = chrome.TryReady(0, out var e0);
        var okClient = chrome.TryReady(1, out var e1);
        yield return null;

        var okStart = chrome.TryStart(out var e2);
        yield return null;

        var flow = chrome.Flow;
        var pass = earlyStartBlocked && notReadyBlocked &&
                   okHost && okClient && okStart &&
                   flow != null &&
                   flow.Started &&
                   flow.Session.Phase == MatchPhase.Admitting &&
                   flow.AllSeatsReady &&
                   hostUdp.PeerReady && clientUdp.PeerReady;

        var reason = pass
            ? "ok"
            : $"early={earlyStartBlocked} notReady={notReadyBlocked}/{nrErr} " +
              $"readyH={okHost}/{e0} readyC={okClient}/{e1} start={okStart}/{e2} " +
              $"phase={flow?.Session.Phase}";

        Finish(pass, reason, chrome, hostUdp, clientUdp, earlyStartBlocked);
        Cleanup(hostGo, clientGo);
    }

    static void Finish(
        bool pass,
        string reason,
        RobotReadyLobbyChrome chrome,
        PhysicsTestUdpTransport hostUdp,
        PhysicsTestUdpTransport clientUdp,
        bool earlyStartBlocked)
    {
        var flow = chrome?.Flow;
        Debug.Log(
            $"[S9-03] VERIFIER_DONE pass={pass} reason={reason} " +
            $"phase={flow?.Session.Phase} session={flow?.Session.SessionId} " +
            $"started={flow?.Started} all_ready={flow?.AllSeatsReady} " +
            $"seat0={flow?.IsSeatReady(0)} seat1={flow?.IsSeatReady(1)} " +
            $"peer_host={hostUdp?.PeerReady} peer_client={clientUdp?.PeerReady} " +
            $"early_blocked={earlyStartBlocked} status={chrome?.Status}");
    }

    static void Cleanup(GameObject hostGo, GameObject clientGo)
    {
        if (hostGo != null)
        {
            var t = hostGo.GetComponent<PhysicsTestUdpTransport>();
            t?.StopTransport();
            Object.Destroy(hostGo);
        }

        if (clientGo != null)
        {
            var t = clientGo.GetComponent<PhysicsTestUdpTransport>();
            t?.StopTransport();
            Object.Destroy(clientGo);
        }
    }
}
