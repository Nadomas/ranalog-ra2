namespace Ra2.Robot
{
    /// <summary>
    /// S9-02 disconnect policy v0 (explicit, host-authoritative).
    /// Mid-fight peer drop ⇒ disconnected seat forfeits; remaining seat wins.
    /// No reconnect window in this spike — disconnect is never rewarded.
    /// Silent desync (continue fighting without outcome) is forbidden.
    /// </summary>
    public static class MatchDisconnectPolicy
    {
        public const string PolicyId = "disconnect-forfeit-v0";

        /// <summary>
        /// Resolve forfeit when <paramref name="disconnectedSeat"/> drops during Fighting.
        /// Seat index maps 1:1 to robot id in thin 1v1 spikes.
        /// </summary>
        public static bool TryResolveForfeit(
            MatchPhase phase,
            int disconnectedSeat,
            int seatCount,
            out MatchOutcome outcome,
            out string error)
        {
            outcome = MatchOutcome.None;
            error = null;

            if (phase != MatchPhase.Fighting)
            {
                error = "not_fighting";
                return false;
            }

            if (seatCount < 2)
            {
                error = "bad_seat_count";
                return false;
            }

            if (disconnectedSeat < 0 || disconnectedSeat >= seatCount)
            {
                error = "bad_seat";
                return false;
            }

            var loser = disconnectedSeat;
            var winner = -1;
            for (var i = 0; i < seatCount; i++)
            {
                if (i == disconnectedSeat)
                    continue;
                winner = i;
                break;
            }

            if (winner < 0)
            {
                error = "no_remaining_seat";
                return false;
            }

            outcome = new MatchOutcome(true, winner, loser, MatchWinReason.DisconnectForfeit);
            return true;
        }
    }
}
