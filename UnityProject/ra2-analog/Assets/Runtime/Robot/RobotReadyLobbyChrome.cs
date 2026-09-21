using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Thin ready/lobby chrome (IMGUI) for listen-host + client seats.
/// Not product polish — proves two roles can ready → start without scene hacking.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotReadyLobbyChrome : MonoBehaviour
{
    MatchReadyLobbyFlow flow;
    bool peerHostLinked;
    bool peerClientLinked;
    string status = "ready";

    public MatchReadyLobbyFlow Flow => flow;
    public string Status => status;
    public bool PeerHostLinked => peerHostLinked;
    public bool PeerClientLinked => peerClientLinked;

    public void EnsureFlow(string sessionId = null)
    {
        if (flow != null)
            return;
        flow = new MatchReadyLobbyFlow(2, sessionId ?? "s9-ready-stub");
        status = "lobby_open";
    }

    public void BindPeerLinks(bool hostLinked, bool clientLinked)
    {
        peerHostLinked = hostLinked;
        peerClientLinked = clientLinked;
        status = peerHostLinked && peerClientLinked ? "peers_linked" : "waiting_peers";
    }

    public bool TryReady(int seat, out string error)
    {
        EnsureFlow();
        error = null;
        if (flow.Session.Phase != MatchPhase.Lobby)
        {
            error = "not_lobby";
            status = error;
            return false;
        }

        if (seat < 0 || seat > 1)
        {
            error = "bad_seat";
            status = error;
            return false;
        }

        flow.MarkReady(seat);
        status = $"seat{seat}_ready all={flow.AllSeatsReady}";
        return true;
    }

    public bool TryStart(out string error)
    {
        EnsureFlow();
        if (!peerHostLinked || !peerClientLinked)
        {
            error = "peers_not_linked";
            status = error;
            return false;
        }

        var ok = flow.TryStart(out error);
        status = ok
            ? $"started phase={flow.Session.Phase}"
            : $"start_fail={error}";
        return ok;
    }

    void Start() => EnsureFlow();

    void OnGUI()
    {
        EnsureFlow();
        const float w = 300f;
        var y = 12f;
        GUI.Box(new Rect(12f, y, w, 220f), "Ready Lobby (thin)");
        y += 28f;

        GUI.Label(new Rect(22f, y, w - 20f, 20f),
            $"Phase: {flow.Session.Phase}  session={flow.Session.SessionId}");
        y += 22f;
        GUI.Label(new Rect(22f, y, w - 20f, 20f),
            $"Peers: host={peerHostLinked} client={peerClientLinked}");
        y += 22f;
        GUI.Label(new Rect(22f, y, w - 20f, 20f),
            $"Seats: host={flow.IsSeatReady(0)} client={flow.IsSeatReady(1)}");
        y += 28f;

        if (GUI.Button(new Rect(22f, y, 120f, 28f), "Ready Host"))
            TryReady(0, out _);
        if (GUI.Button(new Rect(152f, y, 120f, 28f), "Ready Client"))
            TryReady(1, out _);
        y += 36f;

        if (GUI.Button(new Rect(22f, y, 160f, 28f), "Start Match"))
            TryStart(out _);
        y += 36f;

        GUI.Label(new Rect(22f, y, w - 20f, 40f), status);
    }
}
