using System;

namespace Ra2.Robot
{
    public enum MatchPhase : byte
    {
        Lobby = 0,
        Admitting = 1,
        Fighting = 2,
        Results = 3,
        Closed = 4
    }

    /// <summary>
    /// Authoritative match end payload (S9/S10). Host computes; clients display only.
    /// </summary>
    public readonly struct MatchSummary
    {
        public readonly MatchOutcome Outcome;
        public readonly float MatchDurationSeconds;
        public readonly float ImmobileSecondsLoser;
        public readonly float ImmobileSecondsWinner;
        public readonly bool LoserWasDisabled;
        public readonly string SessionId;

        public MatchSummary(
            MatchOutcome outcome,
            float matchDurationSeconds,
            float immobileSecondsLoser,
            float immobileSecondsWinner,
            bool loserWasDisabled,
            string sessionId)
        {
            Outcome = outcome;
            MatchDurationSeconds = matchDurationSeconds;
            ImmobileSecondsLoser = immobileSecondsLoser;
            ImmobileSecondsWinner = immobileSecondsWinner;
            LoserWasDisabled = loserWasDisabled;
            SessionId = sessionId ?? string.Empty;
        }

        public static MatchSummary None =>
            new MatchSummary(MatchOutcome.None, 0f, 0f, 0f, false, string.Empty);
    }

    /// <summary>
    /// Thin lobby → admit → fight → results gate (S9-01). Plain C#; host owns transitions.
    /// </summary>
    public sealed class MatchLobbySession
    {
        readonly bool[] seatReady;
        readonly bool[] seatAdmitted;
        readonly int seatCount;

        public MatchLobbySession(int seats = 2, string sessionId = null)
        {
            seatCount = Math.Max(2, seats);
            seatReady = new bool[seatCount];
            seatAdmitted = new bool[seatCount];
            SessionId = string.IsNullOrEmpty(sessionId)
                ? $"s9-{Guid.NewGuid():N}".Substring(0, 12)
                : sessionId;
            Phase = MatchPhase.Lobby;
            LastSummary = MatchSummary.None;
        }

        public string SessionId { get; }
        public MatchPhase Phase { get; private set; }
        public MatchSummary LastSummary { get; private set; }
        public int AdmittedCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < seatCount; i++)
                    if (seatAdmitted[i]) n++;
                return n;
            }
        }

        public bool AllPeersReady
        {
            get
            {
                for (var i = 0; i < seatCount; i++)
                    if (!seatReady[i]) return false;
                return true;
            }
        }

        public void SetPeerReady(int seat)
        {
            if (seat < 0 || seat >= seatCount || Phase != MatchPhase.Lobby)
                return;
            seatReady[seat] = true;
        }

        public bool IsPeerReady(int seat)
        {
            if (seat < 0 || seat >= seatCount)
                return false;
            return seatReady[seat];
        }

        public bool TryBeginAdmit(out string error)
        {
            error = null;
            if (Phase != MatchPhase.Lobby)
            {
                error = "not_lobby";
                return false;
            }

            if (!AllPeersReady)
            {
                error = "peers_not_ready";
                return false;
            }

            Phase = MatchPhase.Admitting;
            return true;
        }

        public bool TryMarkAdmitted(int seat, RobotBlueprint blueprint, out string error)
        {
            error = null;
            if (Phase != MatchPhase.Admitting)
            {
                error = "not_admitting";
                return false;
            }

            if (seat < 0 || seat >= seatCount)
            {
                error = "bad_seat";
                return false;
            }

            if (!RobotSpawnService.TryValidate(blueprint, out error))
                return false;

            seatAdmitted[seat] = true;
            return true;
        }

        public bool TryBeginFight(out string error)
        {
            error = null;
            if (Phase != MatchPhase.Admitting)
            {
                error = "not_admitting";
                return false;
            }

            for (var i = 0; i < seatCount; i++)
            {
                if (!seatAdmitted[i])
                {
                    error = "admit_incomplete";
                    return false;
                }
            }

            Phase = MatchPhase.Fighting;
            return true;
        }

        public void CompleteWithSummary(MatchSummary summary)
        {
            LastSummary = summary;
            Phase = MatchPhase.Results;
        }

        /// <summary>
        /// S9-02: host applies disconnect forfeit while Fighting. Does not silently continue the match.
        /// </summary>
        public bool TryCompleteDisconnectForfeit(
            int disconnectedSeat,
            float matchDurationSeconds,
            out MatchSummary summary,
            out string error)
        {
            summary = MatchSummary.None;
            if (Phase != MatchPhase.Fighting)
            {
                error = "not_fighting";
                return false;
            }

            if (!MatchDisconnectPolicy.TryResolveForfeit(
                    Phase, disconnectedSeat, seatCount, out var outcome, out error))
                return false;

            summary = new MatchSummary(
                outcome,
                matchDurationSeconds,
                immobileSecondsLoser: 0f,
                immobileSecondsWinner: 0f,
                loserWasDisabled: false,
                SessionId);
            CompleteWithSummary(summary);
            return true;
        }

        public void Close()
        {
            Phase = MatchPhase.Closed;
        }
    }
}
