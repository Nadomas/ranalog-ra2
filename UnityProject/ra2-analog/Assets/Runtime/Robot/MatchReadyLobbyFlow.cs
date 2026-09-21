namespace Ra2.Robot
{
    /// <summary>
    /// Thin LAN/listen-host ready gate (S9-03). Host owns start; seats only mark ready.
    /// Stops at Admitting — fight/admit spawn remain S9-01.
    /// </summary>
    public sealed class MatchReadyLobbyFlow
    {
        public MatchReadyLobbyFlow(int seats = 2, string sessionId = null)
        {
            Session = new MatchLobbySession(seats, sessionId ?? "s9-ready-stub");
        }

        public MatchLobbySession Session { get; }
        public bool Started { get; private set; }

        public bool IsSeatReady(int seat) => Session.IsPeerReady(seat);

        public bool AllSeatsReady => Session.AllPeersReady;

        public void MarkReady(int seat) => Session.SetPeerReady(seat);

        /// <summary>
        /// Start match when all seats ready. Transitions Lobby → Admitting.
        /// </summary>
        public bool TryStart(out string error)
        {
            error = null;
            if (Started)
            {
                error = "already_started";
                return false;
            }

            if (!Session.TryBeginAdmit(out error))
                return false;

            Started = true;
            return true;
        }
    }
}
