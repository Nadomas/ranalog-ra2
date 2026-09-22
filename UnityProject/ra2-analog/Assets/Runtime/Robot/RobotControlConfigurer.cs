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

        /// <summary>Replace drive wiring with a named preset. Preserves existing Fire-slot wires.</summary>
        public static void ApplyDrivePreset(RobotBlueprint blueprint, DrivePreset preset)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));

            EnsureAnalogDriveSlots(blueprint);
            var fireWires = CollectFireWirings(blueprint);

            RobotWiringDef[] drive;
            switch (preset)
            {
                case DrivePreset.ReversedDrive:
                    drive = BuildTankWirings(forwardSign: 1f, turnSign: 1f, includeTurn: true);
                    break;
                case DrivePreset.TurnOnly:
                    drive = BuildTankWirings(forwardSign: -1f, turnSign: 1f, includeTurn: true, includeForward: false);
                    break;
                default:
                    // Hinge CW/+effort pushes the sample tank visually backward — flip forward Sign.
                    drive = BuildTankWirings(forwardSign: -1f, turnSign: -1f, includeTurn: true);
                    break;
            }

            if (fireWires.Count == 0)
            {
                blueprint.Wirings = drive;
                return;
            }

            var merged = new RobotWiringDef[drive.Length + fireWires.Count];
            Array.Copy(drive, merged, drive.Length);
            for (var i = 0; i < fireWires.Count; i++)
                merged[drive.Length + i] = fireWires[i];
            blueprint.Wirings = merged;
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

        /// <summary>Named binding groups (S5-02 / S14-02) — convenience over multiple control slots.</summary>
        public enum BindingGroupId : byte
        {
            Drive = 0,
            Turn = 1,
            Fire = 2
        }

        public static string[] SlotIdsForGroup(BindingGroupId group) =>
            group switch
            {
                BindingGroupId.Turn => new[] { "left_right" },
                BindingGroupId.Fire => new[] { "fire" },
                _ => new[] { "forward_back" }
            };

        public static string DisplayNameForGroup(BindingGroupId group) =>
            group switch
            {
                BindingGroupId.Turn => "Turn (Left-Right)",
                BindingGroupId.Fire => "Fire",
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

            if (group == BindingGroupId.Fire)
                EnsureFireSlot(blueprint);
            else
                EnsureAnalogDriveSlots(blueprint);
            var slots = SlotIdsForGroup(group);
            for (var i = 0; i < slots.Length; i++)
                SetSlotBinding(blueprint, slots[i], inputBinding);
            return true;
        }

        /// <summary>Read-only: returns binding for the group slot, or null if the slot is missing.</summary>
        public static string GetGroupBinding(RobotBlueprint blueprint, BindingGroupId group)
        {
            var slots = SlotIdsForGroup(group);
            if (slots.Length == 0)
                return null;
            return GetSlotBinding(blueprint, slots[0]);
        }

        /// <summary>Cycle Drive/Turn/Fire bindings through a small preset list (thin UX).</summary>
        public static bool TryCycleGroupBinding(
            RobotBlueprint blueprint,
            BindingGroupId group,
            out string applied,
            out string error)
        {
            applied = null;
            error = null;
            if (group == BindingGroupId.Fire)
                EnsureFireSlot(blueprint);
            else
                EnsureAnalogDriveSlots(blueprint);

            string[] options;
            if (group == BindingGroupId.Turn)
                options = new[] { "A/D", "Left/Right", "J/L" };
            else if (group == BindingGroupId.Fire)
                options = new[] { "Space", "F", "Mouse0" };
            else
                options = new[] { "W/S", "S/W", "Up/Down" };

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

        /// <summary>Warn when Drive/Turn/Fire share the same binding string. Read-only (no slot creation).</summary>
        public static List<string> FindBindingGroupConflicts(RobotBlueprint blueprint)
        {
            var warnings = new List<string>();
            if (blueprint == null)
                return warnings;

            var drive = GetGroupBinding(blueprint, BindingGroupId.Drive);
            var turn = GetGroupBinding(blueprint, BindingGroupId.Turn);
            var fire = GetGroupBinding(blueprint, BindingGroupId.Fire);
            if (!string.IsNullOrEmpty(drive) &&
                string.Equals(drive, turn, StringComparison.Ordinal))
                warnings.Add($"group_binding_overlap:{drive}");
            if (!string.IsNullOrEmpty(fire) &&
                (string.Equals(fire, drive, StringComparison.Ordinal) ||
                 string.Equals(fire, turn, StringComparison.Ordinal)))
                warnings.Add($"fire_binding_overlap:{fire}");

            return warnings;
        }

        /// <summary>
        /// S14-02: ensure Button Fire slot + wire it to a weapon/burst actuator (not drive-axle SpinMotors).
        /// </summary>
        public static bool TryApplyFireWirePreset(RobotBlueprint blueprint, out string detail, out string error)
        {
            detail = null;
            error = null;
            if (blueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            EnsureFireSlot(blueprint);
            var actuatorId = FindFirstFireActuatorId(blueprint);
            if (string.IsNullOrEmpty(actuatorId))
            {
                error = "no_fire_actuator";
                return false;
            }

            var channel = IsBurstLike(blueprint, actuatorId) ? "Fire" : "CW";
            // Remove existing wires from fire slot, then add one.
            var list = new List<RobotWiringDef>();
            if (blueprint.Wirings != null)
            {
                for (var i = 0; i < blueprint.Wirings.Length; i++)
                {
                    if (string.Equals(blueprint.Wirings[i].ControlSlotId, "fire", StringComparison.Ordinal))
                        continue;
                    list.Add(blueprint.Wirings[i]);
                }
            }

            list.Add(new RobotWiringDef
            {
                ControlSlotId = "fire",
                ComponentId = actuatorId,
                Channel = channel,
                Sign = 1f
            });
            blueprint.Wirings = list.ToArray();
            detail = $"fire→{actuatorId}/{channel}";
            return true;
        }

        static List<RobotWiringDef> CollectFireWirings(RobotBlueprint blueprint)
        {
            var list = new List<RobotWiringDef>();
            if (blueprint?.Wirings == null)
                return list;
            for (var i = 0; i < blueprint.Wirings.Length; i++)
            {
                if (string.Equals(blueprint.Wirings[i].ControlSlotId, "fire", StringComparison.Ordinal))
                    list.Add(blueprint.Wirings[i]);
            }

            return list;
        }

        /// <summary>
        /// Prefer Burst* then non-axle SpinMotor/Servo. Skip wheel-drive SpinMotors (motor_fl… / parents of wheels).
        /// </summary>
        static string FindFirstFireActuatorId(RobotBlueprint blueprint)
        {
            if (blueprint?.Components == null)
                return null;

            string spinFallback = null;
            string servoFallback = null;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                var b = c.ResolvedBase();
                if (b == RobotComponentBase.BurstMotor || b == RobotComponentBase.BurstPiston)
                    return c.Id;

                if (b == RobotComponentBase.SpinMotor)
                {
                    if (IsDriveAxleSpinMotor(blueprint, c))
                        continue;
                    if (spinFallback == null)
                        spinFallback = c.Id;
                }
                else if ((b == RobotComponentBase.ServoMotor || b == RobotComponentBase.ServoPiston) &&
                         servoFallback == null)
                {
                    servoFallback = c.Id;
                }
            }

            return spinFallback ?? servoFallback;
        }

        static bool IsDriveAxleSpinMotor(RobotBlueprint blueprint, RobotComponentDef component)
        {
            if (component.ResolvedBase() != RobotComponentBase.SpinMotor)
                return false;

            var id = component.Id;
            if (!string.IsNullOrEmpty(id) &&
                (string.Equals(id, "motor_fl", StringComparison.Ordinal) ||
                 string.Equals(id, "motor_fr", StringComparison.Ordinal) ||
                 string.Equals(id, "motor_rl", StringComparison.Ordinal) ||
                 string.Equals(id, "motor_rr", StringComparison.Ordinal)))
                return true;

            if (blueprint?.Connections == null || blueprint.Components == null || string.IsNullOrEmpty(id))
                return false;

            for (var i = 0; i < blueprint.Connections.Length; i++)
            {
                var conn = blueprint.Connections[i];
                if (!string.Equals(conn.ParentId, id, StringComparison.Ordinal))
                    continue;
                for (var j = 0; j < blueprint.Components.Length; j++)
                {
                    if (!string.Equals(blueprint.Components[j].Id, conn.ChildId, StringComparison.Ordinal))
                        continue;
                    if (blueprint.Components[j].ResolvedBase() == RobotComponentBase.Wheel ||
                        blueprint.Components[j].Kind == RobotComponentKind.Wheel)
                        return true;
                }
            }

            return false;
        }

        static bool IsBurstLike(RobotBlueprint blueprint, string componentId)
        {
            if (blueprint?.Components == null)
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

        static void EnsureFireSlot(RobotBlueprint blueprint)
        {
            if (blueprint.ControlSlots != null)
            {
                for (var i = 0; i < blueprint.ControlSlots.Length; i++)
                {
                    if (string.Equals(blueprint.ControlSlots[i].Id, "fire", StringComparison.Ordinal))
                        return;
                }
            }

            EnsureAnalogDriveSlots(blueprint);
            var slots = new List<RobotControlSlotDef>(blueprint.ControlSlots ?? Array.Empty<RobotControlSlotDef>())
            {
                new RobotControlSlotDef
                {
                    Id = "fire",
                    DisplayName = "Fire",
                    Kind = RobotControlKind.Button,
                    InputBinding = "Space"
                }
            };
            blueprint.ControlSlots = slots.ToArray();
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

        /// <summary>S11-13: flip Sign on one wiring row (thin canvas edit).</summary>
        public static bool TryFlipWireSign(RobotBlueprint blueprint, int wireIndex, out string error)
        {
            error = null;
            if (blueprint?.Wirings == null)
            {
                error = "no_wirings";
                return false;
            }

            if (wireIndex < 0 || wireIndex >= blueprint.Wirings.Length)
            {
                error = "bad_index";
                return false;
            }

            var w = blueprint.Wirings[wireIndex];
            w.Sign = w.Sign >= 0f ? -1f : 1f;
            blueprint.Wirings[wireIndex] = w;
            return true;
        }

        /// <summary>S11-13: cycle Channel CW↔CCW on one wiring row.</summary>
        public static bool TryCycleWireChannel(RobotBlueprint blueprint, int wireIndex, out string error)
        {
            error = null;
            if (blueprint?.Wirings == null)
            {
                error = "no_wirings";
                return false;
            }

            if (wireIndex < 0 || wireIndex >= blueprint.Wirings.Length)
            {
                error = "bad_index";
                return false;
            }

            var w = blueprint.Wirings[wireIndex];
            w.Channel = string.Equals(w.Channel, "CW", StringComparison.OrdinalIgnoreCase) ? "CCW" : "CW";
            blueprint.Wirings[wireIndex] = w;
            return true;
        }

        static readonly string[] BindingCycle =
        {
            "W/S", "A/D", "Up/Down", "Left/Right", "MouseY", "MouseX", "Space", "F"
        };

        /// <summary>S11-17: cycle ControlSlot Kind Switch→Button→Analog.</summary>
        public static bool TryCycleSlotKind(RobotBlueprint blueprint, int slotIndex, out string error)
        {
            error = null;
            if (blueprint?.ControlSlots == null)
            {
                error = "no_slots";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= blueprint.ControlSlots.Length)
            {
                error = "bad_index";
                return false;
            }

            var s = blueprint.ControlSlots[slotIndex];
            s.Kind = s.Kind switch
            {
                RobotControlKind.Switch => RobotControlKind.Button,
                RobotControlKind.Button => RobotControlKind.Analog,
                _ => RobotControlKind.Switch
            };
            blueprint.ControlSlots[slotIndex] = s;
            return true;
        }

        /// <summary>S11-17: cycle ControlSlot InputBinding through a thin preset list.</summary>
        public static bool TryCycleSlotBinding(RobotBlueprint blueprint, int slotIndex, out string error)
        {
            error = null;
            if (blueprint?.ControlSlots == null)
            {
                error = "no_slots";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= blueprint.ControlSlots.Length)
            {
                error = "bad_index";
                return false;
            }

            var s = blueprint.ControlSlots[slotIndex];
            var cur = s.InputBinding ?? string.Empty;
            var idx = 0;
            for (var i = 0; i < BindingCycle.Length; i++)
            {
                if (string.Equals(BindingCycle[i], cur, StringComparison.OrdinalIgnoreCase))
                {
                    idx = (i + 1) % BindingCycle.Length;
                    break;
                }
            }

            s.InputBinding = BindingCycle[idx];
            blueprint.ControlSlots[slotIndex] = s;
            return true;
        }
    }
}
