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
                    blueprint.Wirings = BuildTankWirings(forwardSign: -1f, turnSign: 1f, includeTurn: true);
                    break;
                case DrivePreset.TurnOnly:
                    blueprint.Wirings = BuildTankWirings(forwardSign: 1f, turnSign: 1f, includeTurn: true, includeForward: false);
                    break;
                default:
                    blueprint.Wirings = BuildTankWirings(forwardSign: 1f, turnSign: 1f, includeTurn: true);
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
