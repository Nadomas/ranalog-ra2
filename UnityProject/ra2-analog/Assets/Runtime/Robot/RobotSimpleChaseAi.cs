using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Thin local-only chase brain for MVP 1v1. Outputs tank ControlState — no Transform writes.
    /// Prefers face-then-advance so the bot does not reverse off the arena.
    /// </summary>
    public static class RobotSimpleChaseAi
    {
        const float ArenaSoftRadius = 9.5f;

        public static RobotWiringDriveResolver.ControlState Seek(Transform self, Vector3 targetWorld)
        {
            if (self == null)
                return default;

            var pos = self.position;
            var flat = new Vector3(pos.x, 0f, pos.z);
            if (flat.magnitude > ArenaSoftRadius)
            {
                // Pull back toward arena center before chasing.
                targetWorld = Vector3.zero;
            }

            var to = targetWorld - pos;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return default;

            var local = self.InverseTransformDirection(to.normalized);
            float forward;
            float turn;

            if (local.z < 0.1f)
            {
                // Target behind / beside — spin in place with light creep, never full reverse.
                forward = 0.2f;
                turn = local.x >= 0f ? 1f : -1f;
            }
            else
            {
                forward = Mathf.Clamp(0.55f + local.z * 0.45f, 0.55f, 1f);
                turn = Mathf.Clamp(local.x * 1.8f, -1f, 1f);
            }

            return new RobotWiringDriveResolver.ControlState
            {
                ForwardBack = forward,
                LeftRight = turn
            };
        }
    }
}
