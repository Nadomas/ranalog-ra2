using System.Globalization;
using System.Text;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S10-01/S10-02 results stub: format + present authoritative <see cref="MatchSummary"/>.
    /// Clients must not recompute winners; they only display/log the host payload.
    /// </summary>
    public static class MatchResultsStub
    {
        public static string Format(MatchSummary summary, string sideLabel = "client")
        {
            var o = summary.Outcome;
            var sb = new StringBuilder(192);
            sb.Append("[S10-01] RESULTS side=").Append(sideLabel);
            sb.Append(" session=").Append(summary.SessionId ?? "");
            sb.Append(" finished=").Append(o.Finished);
            sb.Append(" reason=").Append(o.Reason);
            sb.Append(" winner=").Append(o.WinnerRobotId);
            sb.Append(" loser=").Append(o.LoserRobotId);
            sb.Append(" duration_s=").Append(F2(summary.MatchDurationSeconds));
            sb.Append(" immobile_loser_s=").Append(F2(summary.ImmobileSecondsLoser));
            sb.Append(" immobile_winner_s=").Append(F2(summary.ImmobileSecondsWinner));
            sb.Append(" loser_disabled=").Append(summary.LoserWasDisabled);
            return sb.ToString();
        }

        /// <summary>S10-03 multiline readable panel text (host payload only; no recomputed winner).</summary>
        public static string FormatReadable(MatchSummary summary, string sideLabel = "client")
        {
            var o = summary.Outcome;
            var sb = new StringBuilder(256);
            sb.Append("Side: ").Append(sideLabel).Append('\n');
            sb.Append("Session: ").Append(summary.SessionId ?? "").Append('\n');
            sb.Append("Finished: ").Append(o.Finished).Append('\n');
            sb.Append("Reason: ").Append(FormatReasonLabel(o.Reason)).Append('\n');
            sb.Append("Winner: ").Append(o.WinnerRobotId).Append('\n');
            sb.Append("Loser: ").Append(o.LoserRobotId).Append('\n');
            sb.Append("Duration: ").Append(F2(summary.MatchDurationSeconds)).Append(" s\n");
            sb.Append("Immobile loser: ").Append(F2(summary.ImmobileSecondsLoser)).Append(" s\n");
            sb.Append("Immobile winner: ").Append(F2(summary.ImmobileSecondsWinner)).Append(" s\n");
            sb.Append("Loser disabled: ").Append(summary.LoserWasDisabled);
            if (o.Reason == MatchWinReason.TimeExpired)
                sb.Append("\nNote: clock expired — center / tie-break");
            return sb.ToString();
        }

        /// <summary>S12-02 results card title from host reason (display only).</summary>
        public static string FormatResultsTitle(MatchSummary summary)
        {
            if (!summary.Outcome.Finished)
                return "Fight incomplete";
            return summary.Outcome.Reason switch
            {
                MatchWinReason.TimeExpired => "TIME EXPIRED · STALEMATE BREAK",
                MatchWinReason.DisconnectForfeit => "DISCONNECT FORFEIT",
                MatchWinReason.OpponentDisabled => "OPPONENT DISABLED",
                MatchWinReason.Immobilized => "IMMOBILIZED",
                _ => "Fight complete"
            };
        }

        public static string FormatReasonLabel(MatchWinReason reason) =>
            reason switch
            {
                MatchWinReason.TimeExpired => "TimeExpired (stalemate break)",
                MatchWinReason.DisconnectForfeit => "DisconnectForfeit",
                MatchWinReason.OpponentDisabled => "OpponentDisabled",
                MatchWinReason.Immobilized => "Immobilized",
                _ => reason.ToString()
            };

        /// <summary>Present results (local-only chrome). Optionally persist + push to a view.</summary>
        public static string Present(
            MatchSummary summary,
            string sideLabel = "client",
            bool persist = false,
            MatchResultsView view = null)
        {
            var line = Format(summary, sideLabel);
            Debug.Log(line);

            if (persist)
            {
                if (MatchSummaryStore.TrySave(summary, out var path, out var err))
                    Debug.Log($"[S10-02] PERSIST ok path={path}");
                else
                    Debug.LogWarning($"[S10-02] PERSIST fail err={err}");
            }

            if (view != null)
                view.Show(summary, sideLabel);

            return line;
        }

        static string F2(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
    }
}
