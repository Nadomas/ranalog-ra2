using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Maps analog slots / drive command through wiring into per-component motor effort [-1, 1].
    /// Uses wiring:
    /// - ControlSlotId picks the drive analog value (Move or Turn) or digital Fire (Button/Switch)
    /// - Channel CW/CCW/Fire/Extend sets base direction
    /// - Sign is an extra multiplier (differential wiring / sign flip)
    /// </summary>
    public static class RobotWiringDriveResolver
    {
        public struct ControlState
        {
            public float ForwardBack;
            public float LeftRight;
            /// <summary>Button held / Switch on in [0, 1].</summary>
            public float Fire;
        }

        public static PhysicsTestDriveCommand ResolveTankDrive(RobotBlueprint blueprint, ControlState state)
        {
            if (blueprint == null)
                return default;

            var forward = state.ForwardBack;
            var turn = state.LeftRight;
            var fire = Mathf.Clamp01(state.Fire);

            if (blueprint.Wirings != null && blueprint.Wirings.Length > 0)
            {
                forward = ReadAnalogSlot(blueprint, "forward_back", state.ForwardBack);
                turn = ReadAnalogSlot(blueprint, "left_right", state.LeftRight);
            }

            return new PhysicsTestDriveCommand
            {
                Move = Mathf.Clamp(forward, -1f, 1f),
                Turn = Mathf.Clamp(turn, -1f, 1f),
                Fire = fire
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
            var fire = Mathf.Clamp01(command.Fire);

            for (var i = 0; i < blueprint.Wirings.Length; i++)
            {
                var w = blueprint.Wirings[i];
                if (string.IsNullOrEmpty(w.ComponentId) || string.IsNullOrEmpty(w.ControlSlotId))
                    continue;

                // Burst / SmartZone edge targets are handled by RobotActuatorDrive — skip continuous effort.
                if (IsBurstActuator(blueprint, w.ComponentId))
                    continue;
                if (IsSmartZoneComponent(blueprint, w.ControlSlotId))
                    continue;

                float slot;
                if (string.Equals(w.ControlSlotId, "forward_back", StringComparison.Ordinal))
                    slot = move;
                else if (string.Equals(w.ControlSlotId, "left_right", StringComparison.Ordinal))
                    slot = turn;
                else if (IsDigitalSlot(blueprint, w.ControlSlotId))
                    slot = fire;
                else if (IsAnalogSlot(blueprint, w.ControlSlotId))
                    slot = move; // thin: dedicated Analog slots map to Move axis
                else
                    continue;

                var sign = w.Sign == 0f ? 1f : w.Sign;
                var dir = ChannelDir(w.Channel);
                if (!into.TryGetValue(w.ComponentId, out var acc))
                    acc = 0f;
                into[w.ComponentId] = Mathf.Clamp(acc + slot * sign * dir, -1f, 1f);
            }
        }

        /// <summary>
        /// Rising-edge Fire targets for BurstMotor / BurstPiston (Button one-shot / Switch edge).
        /// </summary>
        public static void ResolveFireTargets(
            RobotBlueprint blueprint,
            PhysicsTestDriveCommand command,
            bool fireRisingEdge,
            List<string> into)
        {
            into.Clear();
            if (!fireRisingEdge || blueprint?.Wirings == null)
                return;
            if (Mathf.Clamp01(command.Fire) < 0.5f)
                return;

            for (var i = 0; i < blueprint.Wirings.Length; i++)
            {
                var w = blueprint.Wirings[i];
                if (string.IsNullOrEmpty(w.ComponentId) || string.IsNullOrEmpty(w.ControlSlotId))
                    continue;
                if (!IsBurstActuator(blueprint, w.ComponentId))
                    continue;
                if (!IsDigitalSlot(blueprint, w.ControlSlotId))
                    continue;
                if (!IsFireLikeChannel(w.Channel))
                    continue;
                if (!into.Contains(w.ComponentId))
                    into.Add(w.ComponentId);
            }
        }

        /// <summary>
        /// S7-09: optional SmartZone contact → Fire targets. ControlSlotId names a SmartZone component.
        /// </summary>
        public static void ResolveSmartZoneFireTargets(
            RobotBlueprint blueprint,
            System.Func<string, bool> zoneContactRising,
            List<string> into)
        {
            into.Clear();
            if (blueprint?.Wirings == null || zoneContactRising == null)
                return;

            for (var i = 0; i < blueprint.Wirings.Length; i++)
            {
                var w = blueprint.Wirings[i];
                if (string.IsNullOrEmpty(w.ComponentId) || string.IsNullOrEmpty(w.ControlSlotId))
                    continue;
                if (!IsBurstActuator(blueprint, w.ComponentId))
                    continue;
                if (!IsSmartZoneComponent(blueprint, w.ControlSlotId))
                    continue;
                if (!IsFireLikeChannel(w.Channel))
                    continue;
                if (!zoneContactRising(w.ControlSlotId))
                    continue;
                if (!into.Contains(w.ComponentId))
                    into.Add(w.ComponentId);
            }
        }

        public static bool IsSmartZoneComponent(RobotBlueprint blueprint, string componentId)
        {
            if (blueprint?.Components == null || string.IsNullOrEmpty(componentId))
                return false;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                if (!string.Equals(blueprint.Components[i].Id, componentId, StringComparison.Ordinal))
                    continue;
                return blueprint.Components[i].ResolvedBase() == RobotComponentBase.SmartZone;
            }

            return false;
        }

        public static bool IsServoActuator(RobotBlueprint blueprint, string componentId)
        {
            if (blueprint?.Components == null || string.IsNullOrEmpty(componentId))
                return false;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                if (!string.Equals(blueprint.Components[i].Id, componentId, StringComparison.Ordinal))
                    continue;
                var b = blueprint.Components[i].ResolvedBase();
                return b == RobotComponentBase.ServoMotor ||
                       b == RobotComponentBase.ServoPiston ||
                       b == RobotComponentBase.Steering;
            }

            return false;
        }

        public static bool IsDigitalSlot(RobotBlueprint blueprint, string slotId)
        {
            if (blueprint?.ControlSlots == null || string.IsNullOrEmpty(slotId))
                return false;
            for (var i = 0; i < blueprint.ControlSlots.Length; i++)
            {
                var s = blueprint.ControlSlots[i];
                if (!string.Equals(s.Id, slotId, StringComparison.Ordinal))
                    continue;
                return s.Kind == RobotControlKind.Button || s.Kind == RobotControlKind.Switch;
            }

            // Unknown id used as Fire-like digital (thin verifier convenience).
            return string.Equals(slotId, "fire", StringComparison.OrdinalIgnoreCase) ||
                   slotId.StartsWith("fire_", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAnalogSlot(RobotBlueprint blueprint, string slotId)
        {
            if (blueprint?.ControlSlots == null || string.IsNullOrEmpty(slotId))
                return false;
            for (var i = 0; i < blueprint.ControlSlots.Length; i++)
            {
                var s = blueprint.ControlSlots[i];
                if (!string.Equals(s.Id, slotId, StringComparison.Ordinal))
                    continue;
                return s.Kind == RobotControlKind.Analog;
            }

            return false;
        }

        public static bool IsBurstActuator(RobotBlueprint blueprint, string componentId)
        {
            if (blueprint?.Components == null || string.IsNullOrEmpty(componentId))
                return false;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                if (!string.Equals(blueprint.Components[i].Id, componentId, StringComparison.Ordinal))
                    continue;
                var b = blueprint.Components[i].ResolvedBase();
                return b == RobotComponentBase.BurstMotor || b == RobotComponentBase.BurstPiston;
            }

            return false;
        }

        static bool IsFireLikeChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return true;
            return string.Equals(channel, "Fire", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(channel, "Extend", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(channel, "CW", StringComparison.OrdinalIgnoreCase);
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

        static float ReadAnalogSlot(RobotBlueprint blueprint, string slotId, float fallback)
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
