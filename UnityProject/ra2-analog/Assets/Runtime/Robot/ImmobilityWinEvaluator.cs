using UnityEngine;

namespace Ra2.Robot
{
    public enum MatchWinReason : byte
    {
        None = 0,
        Immobilized = 1,
        OpponentDisabled = 2,
        /// <summary>S9-02: peer dropped mid-fight; disconnected seat forfeits.</summary>
        DisconnectForfeit = 3,
        /// <summary>S12-02: match clock expired; host applied tie-break (e.g. center rule).</summary>
        TimeExpired = 4
    }

    public readonly struct MatchOutcome
    {
        public readonly bool Finished;
        public readonly int WinnerRobotId;
        public readonly int LoserRobotId;
        public readonly MatchWinReason Reason;

        public MatchOutcome(bool finished, int winner, int loser, MatchWinReason reason)
        {
            Finished = finished;
            WinnerRobotId = winner;
            LoserRobotId = loser;
            Reason = reason;
        }

        public static MatchOutcome None => new MatchOutcome(false, -1, -1, MatchWinReason.None);
    }

    /// <summary>
    /// Authoritative-style immobility rules (S8-01). Plain C# — host/sim calls Tick; clients do not decide winners.
    /// Functionally disabled fighters always accrue immobility time (cannot recover locomotion).
    /// </summary>
    public sealed class ImmobilityWinEvaluator
    {
        readonly float immobileSeconds;
        readonly float speedThreshold;
        readonly float[] immobileAccum;
        readonly Vector3[] lastPos;
        readonly bool[] hasPos;
        readonly int[] robotIds;

        public ImmobilityWinEvaluator(int[] trackedRobotIds, float immobileSeconds = 1.5f, float speedThreshold = 0.15f)
        {
            robotIds = trackedRobotIds;
            this.immobileSeconds = immobileSeconds;
            this.speedThreshold = speedThreshold;
            immobileAccum = new float[trackedRobotIds.Length];
            lastPos = new Vector3[trackedRobotIds.Length];
            hasPos = new bool[trackedRobotIds.Length];
        }

        public MatchOutcome LastOutcome { get; private set; } = MatchOutcome.None;

        public MatchOutcome Tick(float dt, Vector3[] worldPositions) =>
            Tick(dt, worldPositions, null);

        /// <summary>
        /// Feed world positions each sim step. Optional <paramref name="functionallyDisabled"/>:
        /// disabled fighters always accrue (MVP: disable ⇒ immobility path).
        /// </summary>
        public MatchOutcome Tick(float dt, Vector3[] worldPositions, bool[] functionallyDisabled)
        {
            if (LastOutcome.Finished)
                return LastOutcome;

            if (worldPositions == null || worldPositions.Length != robotIds.Length)
                return MatchOutcome.None;

            var newlyImmobile = -1;
            for (var i = 0; i < robotIds.Length; i++)
            {
                var forced = functionallyDisabled != null &&
                             i < functionallyDisabled.Length &&
                             functionallyDisabled[i];

                if (!hasPos[i])
                {
                    lastPos[i] = worldPositions[i];
                    hasPos[i] = true;
                    immobileAccum[i] = 0f;
                    continue;
                }

                var speed = (worldPositions[i] - lastPos[i]).magnitude / Mathf.Max(dt, 1e-4f);
                lastPos[i] = worldPositions[i];

                if (forced || speed < speedThreshold)
                    immobileAccum[i] += dt;
                else
                    immobileAccum[i] = 0f;

                if (immobileAccum[i] < immobileSeconds)
                    continue;

                // Prefer functionally-disabled as loser when multiple cross threshold same tick.
                if (newlyImmobile < 0)
                {
                    newlyImmobile = i;
                }
                else
                {
                    var prevForced = functionallyDisabled != null &&
                                     newlyImmobile < functionallyDisabled.Length &&
                                     functionallyDisabled[newlyImmobile];
                    if (forced && !prevForced)
                        newlyImmobile = i;
                }
            }

            if (newlyImmobile < 0)
                return MatchOutcome.None;

            var loserId = robotIds[newlyImmobile];
            var winnerId = -1;
            for (var i = 0; i < robotIds.Length; i++)
            {
                if (i == newlyImmobile)
                    continue;
                winnerId = robotIds[i];
                break;
            }

            LastOutcome = new MatchOutcome(true, winnerId, loserId, MatchWinReason.Immobilized);
            return LastOutcome;
        }

        public void ForceOutcome(int winnerId, int loserId, MatchWinReason reason)
        {
            LastOutcome = new MatchOutcome(true, winnerId, loserId, reason);
        }

        public float GetImmobileSeconds(int index) =>
            index >= 0 && index < immobileAccum.Length ? immobileAccum[index] : 0f;
    }
}
