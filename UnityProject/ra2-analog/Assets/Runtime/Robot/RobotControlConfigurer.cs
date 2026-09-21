using System;
using System.Collections.Generic;

namespace Ra2.Robot
{
    /// <summary>
    /// Plain Configure service (S5-01). Mutates control slots / wiring on a blueprint without touching physics hierarchy.
    /// Same map travels with the robot into Test/Battle admit.
    /// </summary>
    public static class RobotControlConfigurer
    {
        public enum DrivePreset : byte
        {
            TankSteer = 0,
            ReversedDrive = 1,
            TurnOnly = 2
        }

        /// <summary>Replace wiring with a named drive preset. Leaves component graph unchanged.</summary>
        public static void ApplyDrivePreset(RobotBlueprint blueprint, DrivePreset preset)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));

            EnsureAnalogDriveSlots(blueprint);

            switch (preset)
            {
                case DrivePreset.ReversedDrive:
                    blueprint.Wirings = BuildTankWirings(forwardSign: 1f, turnSign: 1f, includeTurn: true);
                    break;
                case DrivePreset.TurnOnly:
                    blueprint.Wirings = BuildTankWirings(forwardSign: -1f, turnSign: 1f, includeTurn: true, includeForward: false);
                    break;
                default:
                    // Hinge CW/+effort pushes the sample tank visually backward — flip forward Sign.
                    blueprint.Wirings = BuildTankWirings(forwardSign: -1f, turnSign: -1f, includeTurn: true);
                    break;
            }
        }

        public static void SetSlotBinding(RobotBlueprint blueprint, string slotId, string inputBinding)
        {
            if (blueprint?.ControlSlots == null || string.IsNullOrEmpty(slotId))
                return;

            for (var i = 0; i < blueprint.ControlSlots.Length; i++)
            {
                if (!string.Equals(blueprint.ControlSlots[i].Id, slotId, StringComparison.Ordinal))
                    continue;
                var s = blueprint.ControlSlots[i];
                s.InputBinding = inputBinding ?? string.Empty;
                blueprint.ControlSlots[i] = s;
                return;
            }
        }

        public static string GetSlotBinding(RobotBlueprint blueprint, string slotId)
        {
            if (blueprint?.ControlSlots == null)
                return null;
            for (var i = 0; i < blueprint.ControlSlots.Length; i++)
            {
                if (string.Equals(blueprint.ControlSlots[i].Id, slotId, StringComparison.Ordinal))
                    return blueprint.ControlSlots[i].InputBinding;
            }

            return null;
        }

        /// <summary>Thin conflict policy: last wiring for (component, channel) wins; duplicates warned.</summary>
        public static List<string> FindWiringConflicts(RobotBlueprint blueprint)
        {
            var warnings = new List<string>();
            if (blueprint?.Wirings == null)
                return warnings;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < blueprint.Wirings.Length; i++)
            {
                var w = blueprint.Wirings[i];
                var key = (w.ComponentId ?? "") + "|" + (w.ControlSlotId ?? "") + "|" + (w.Channel ?? "");
                if (!seen.Add(key))
                    warnings.Add($"duplicate_wire:{key}");
            }

            return warnings;
        }

        /// <summary>Named binding groups (S5-02 thin) — convenience over multiple control slots.</summary>
        public enum BindingGroupId : byte
        {
            Drive = 0,
            Turn = 1
        }

        public static string[] SlotIdsForGroup(BindingGroupId group) =>
            group switch
            {
                BindingGroupId.Turn => new[] { "left_right" },
                _ => new[] { "forward_back" }
            };

        public static string DisplayNameForGroup(BindingGroupId group) =>
            group switch
            {
                BindingGroupId.Turn => "Turn (Left-Right)",
                _ => "Drive (Forward-Back)"
            };

        /// <summary>Apply one input binding string to every slot in the group.</summary>
        public static bool TryApplyGroupBinding(
            RobotBlueprint blueprint,
            BindingGroupId group,
            string inputBinding,
            out string error)
        {
            error = null;
            if (blueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            EnsureAnalogDriveSlots(blueprint);
            var slots = SlotIdsForGroup(group);
            for (var i = 0; i < slots.Length; i++)
                SetSlotBinding(blueprint, slots[i], inputBinding);
            return true;
        }

        public static string GetGroupBinding(RobotBlueprint blueprint, BindingGroupId group)
        {
            var slots = SlotIdsForGroup(group);
            if (slots.Length == 0)
                return null;
            return GetSlotBinding(blueprint, slots[0]);
        }

        /// <summary>Cycle Drive/Turn bindings through a small preset list (thin UX).</summary>
        public static bool TryCycleGroupBinding(
            RobotBlueprint blueprint,
            BindingGroupId group,
            out string applied,
            out string error)
        {
            applied = null;
            error = null;
            EnsureAnalogDriveSlots(blueprint);
            var options = group == BindingGroupId.Turn
                ? new[] { "A/D", "Left/Right", "J/L" }
                : new[] { "W/S", "S/W", "Up/Down" };

            var current = GetGroupBinding(blueprint, group) ?? options[0];
            var idx = 0;
            for (var i = 0; i < options.Length; i++)
            {
                if (string.Equals(options[i], current, StringComparison.Ordinal))
                {
                    idx = (i + 1) % options.Length;
                    break;
                }
            }

            applied = options[idx];
            return TryApplyGroupBinding(blueprint, group, applied, out error);
        }

        /// <summary>Warn when Drive and Turn share the same binding string.</summary>
        public static List<string> FindBindingGroupConflicts(RobotBlueprint blueprint)
        {
            var warnings = new List<string>();
            if (blueprint == null)
                return warnings;

            EnsureAnalogDriveSlots(blueprint);
            var drive = GetGroupBinding(blueprint, BindingGroupId.Drive);
            var turn = GetGroupBinding(blueprint, BindingGroupId.Turn);
            if (!string.IsNullOrEmpty(drive) &&
                string.Equals(drive, turn, StringComparison.Ordinal))
                warnings.Add($"group_binding_overlap:{drive}");

            return warnings;
        }

        static void EnsureAnalogDriveSlots(RobotBlueprint blueprint)
        {
            if (blueprint.ControlSlots != null && blueprint.ControlSlots.Length >= 2)
                return;

            blueprint.ControlSlots = new[]
            {
                new RobotControlSlotDef
                {
                    Id = "forward_back",
                    DisplayName = "Forward-Back",
                    Kind = RobotControlKind.Analog,
                    InputBinding = "W/S"
                },
                new RobotControlSlotDef
                {
                    Id = "left_right",
                    DisplayName = "Left-Right",
                    Kind = RobotControlKind.Analog,
                    InputBinding = "A/D"
                }
            };
        }

        static RobotWiringDef[] BuildTankWirings(
            float forwardSign,
            float turnSign,
            bool includeTurn,
            bool includeForward = true)
        {
            var list = new List<RobotWiringDef>(8);
            var wheels = new[] { "wheel_fl", "wheel_fr", "wheel_rl", "wheel_rr" };
            if (includeForward)
            {
                for (var i = 0; i < wheels.Length; i++)
                {
                    list.Add(new RobotWiringDef
                    {
                        ControlSlotId = "forward_back",
                        ComponentId = wheels[i],
                        Channel = "CW",
                        Sign = forwardSign
                    });
                }
            }

            if (includeTurn)
            {
                // Left wheels CCW / right CW for left-positive turn (matches Stage 3 tank).
                list.Add(new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_fl", Channel = "CCW", Sign = turnSign });
                list.Add(new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_fr", Channel = "CW", Sign = turnSign });
                list.Add(new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_rl", Channel = "CCW", Sign = turnSign });
                list.Add(new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_rr", Channel = "CW", Sign = turnSign });
            }

            return list.ToArray();
        }
    }
}
