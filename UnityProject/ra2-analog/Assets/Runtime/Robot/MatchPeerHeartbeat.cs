namespace Ra2.Robot
{
    /// <summary>
    /// S9-04 thin heartbeat: silence while peer was ready ⇒ timed out (no goodbye required).
    /// Pure clock helper — transport owns walls; forfeit still goes through MatchDisconnectPolicy.
    /// </summary>
    public sealed class MatchPeerHeartbeat
    {
        public const string PolicyTag = "heartbeat-timeout-v0";

        public float TimeoutSeconds { get; }
        public float LastReceiveTime { get; private set; }
        public bool HasReceived { get; private set; }
        public int TimeoutCount { get; private set; }

        public MatchPeerHeartbeat(float timeoutSeconds)
        {
            TimeoutSeconds = timeoutSeconds > 0.05f ? timeoutSeconds : 0.05f;
        }

        public void Reset()
        {
            LastReceiveTime = 0f;
            HasReceived = false;
            TimeoutCount = 0;
        }

        public void RecordReceive(float now)
        {
            LastReceiveTime = now;
            HasReceived = true;
        }

        /// <summary>
        /// True when peer was ready, at least one packet arrived, and silence exceeds timeout.
        /// </summary>
        public bool IsTimedOut(float now, bool peerWasReady)
        {
            if (!peerWasReady || !HasReceived)
                return false;
            return (now - LastReceiveTime) >= TimeoutSeconds;
        }

        public bool TryMarkTimedOut(float now, bool peerWasReady)
        {
            if (!IsTimedOut(now, peerWasReady))
                return false;
            TimeoutCount++;
            return true;
        }
    }
}
