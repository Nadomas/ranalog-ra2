using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Maps analog slots / drive command through wiring into per-component motor effort [-1, 1].
    /// Uses wiring:
    /// - ControlSlotId picks the drive analog value (Move or Turn)
    /// - Channel CW/CCW sets base direction
    /// - Sign is an extra multiplier (differential wiring / sign flip)
    /// </summary>
    public static class RobotWiringDriveResolver
    {
        public struct ControlState
        {
            public float ForwardBack;
            public float LeftRight;
        }

        public static PhysicsTestDriveCommand ResolveTankDrive(RobotBlueprint blueprint, ControlState state)
        {
            if (blueprint == null)
                return default;

            var forward = state.ForwardBack;
            var turn = state.LeftRight;

            if (blueprint.Wirings != null && blueprint.Wirings.Length > 0)
            {
                forward = ReadSlot(blueprint, "forward_back", state.ForwardBack);
                turn = ReadSlot(blueprint, "left_right", state.LeftRight);
            }

            return new PhysicsTestDriveCommand
            {
                Move = Mathf.Clamp(forward, -1f, 1f),
                Turn = Mathf.Clamp(turn, -1f, 1f)
            };
        }

        public static void ResolveMotorEfforts(
            RobotBlueprint blueprint,
            PhysicsTestDriveCommand command,
            Dictionary<string, float> into)
        {
            into.Clear();
            if (blueprint?.Wirings == null || blueprint.Wirings.Length == 0)
                return;

            var move = Mathf.Clamp(command.Move, -1f, 1f);
            var turn = Mathf.Clamp(command.Turn, -1f, 1f);

            for (var i = 0; i < blueprint.Wirings.Length; i++)
            {
                var w = blueprint.Wirings[i];
                if (string.IsNullOrEmpty(w.ComponentId) || string.IsNullOrEmpty(w.ControlSlotId))
                    continue;

                float slot;
                if (string.Equals(w.ControlSlotId, "forward_back", StringComparison.Ordinal))
                    slot = move;
                else if (string.Equals(w.ControlSlotId, "left_right", StringComparison.Ordinal))
                    slot = turn;
                else
                    continue;

                var sign = w.Sign == 0f ? 1f : w.Sign;
                var dir = ChannelDir(w.Channel);
                if (!into.TryGetValue(w.ComponentId, out var acc))
                    acc = 0f;
                into[w.ComponentId] = Mathf.Clamp(acc + slot * sign * dir, -1f, 1f);
            }
        }

        static float ChannelDir(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return 1f;

            // RA2 naming: CW = positive torque, CCW = negative torque.
            if (string.Equals(channel, "CCW", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(channel, "Reverse", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(channel, "Retract", StringComparison.OrdinalIgnoreCase))
                return -1f;

            return 1f;
        }

        static float ReadSlot(RobotBlueprint blueprint, string slotId, float fallback)
        {
            for (var i = 0; i < blueprint.ControlSlots.Length; i++)
            {
                if (string.Equals(blueprint.ControlSlots[i].Id, slotId, StringComparison.Ordinal))
                    return fallback;
            }

            return fallback;
        }
    }
}
