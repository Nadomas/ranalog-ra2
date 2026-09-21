using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>RA2-aligned blueprint validation (v1 + S4-01 construction). Fail-closed for net admit.</summary>
    public static class RobotBlueprintValidator
    {
        public sealed class Result
        {
            public bool Ok => Errors.Count == 0;
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public RobotMassReport Mass;
        }

        public static Result Validate(RobotBlueprint blueprint)
        {
            var result = new Result();
            if (blueprint == null)
            {
                result.Errors.Add("blueprint_null");
                return result;
            }

            if (blueprint.Components == null || blueprint.Components.Length == 0)
            {
                result.Errors.Add("components_empty");
                return result;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var byId = new Dictionary<string, RobotComponentDef>(StringComparer.Ordinal);
            var rootCount = 0;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                if (string.IsNullOrEmpty(c.Id))
                {
                    result.Errors.Add($"components[{i}].id_missing");
                    continue;
                }

                if (!ids.Add(c.Id))
                    result.Errors.Add($"duplicate_id:{c.Id}");
                byId[c.Id] = c;
                if (c.IsRoot)
                    rootCount++;
            }

            if (rootCount != 1)
                result.Errors.Add($"root_count:{rootCount}");

            var chassisPoints = blueprint.Chassis.BaseplatePoints;
            if (chassisPoints != null && chassisPoints.Length > 16)
                result.Errors.Add($"chassis_points:{chassisPoints.Length}>16");
            if (chassisPoints != null && chassisPoints.Length > 0 && chassisPoints.Length < 3)
                result.Errors.Add($"chassis_points:{chassisPoints.Length}<3");

            ValidateConnections(blueprint, byId, result);
            ValidateControlWiring(blueprint, ids, result);
            result.Mass = RobotMassProperties.Compute(blueprint);
            ValidateMassBudgets(blueprint, result);
            ValidateOverlapThin(blueprint, byId, result);

            // Construction attachment / power rules apply to RA2 v1 workshop blueprints only
            // (v0 PhysicsTest samples keep chassis→wheel hinges for Stage 1–3 spikes).
            if (blueprint.HasV1Fields)
            {
                ValidateAttachmentRules(blueprint, byId, result);
                ValidatePowerBudget(blueprint, result);
                if (!ContainsBase(blueprint, RobotComponentBase.ControlBoard))
                    result.Errors.Add("control_board_required");
            }

            return result;
        }

        static void ValidateConnections(
            RobotBlueprint blueprint,
            Dictionary<string, RobotComponentDef> byId,
            Result result)
        {
            if (blueprint.Connections == null)
                return;

            for (var i = 0; i < blueprint.Connections.Length; i++)
            {
                var c = blueprint.Connections[i];
                if (string.IsNullOrEmpty(c.ParentId) || !byId.ContainsKey(c.ParentId))
                    result.Errors.Add($"connections[{i}].unknown_parent:{c.ParentId}");
                if (string.IsNullOrEmpty(c.ChildId) || !byId.ContainsKey(c.ChildId))
                    result.Errors.Add($"connections[{i}].unknown_child:{c.ChildId}");
                if (!string.IsNullOrEmpty(c.ParentId) && c.ParentId == c.ChildId)
                    result.Errors.Add($"connections[{i}].self_loop:{c.ParentId}");
            }
        }

        static void ValidateAttachmentRules(
            RobotBlueprint blueprint,
            Dictionary<string, RobotComponentDef> byId,
            Result result)
        {
            // Wheels must attach to a motor axle (Spin/Burst/Servo), not chassis/baseplate.
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                if (c.ResolvedBase() != RobotComponentBase.Wheel)
                    continue;

                if (!TryFindParent(blueprint, c.Id, out var parentId))
                {
                    result.Errors.Add($"wheel_orphan:{c.Id}");
                    continue;
                }

                if (!byId.TryGetValue(parentId, out var parent))
                {
                    result.Errors.Add($"wheel_parent_missing:{c.Id}->{parentId}");
                    continue;
                }

                var parentBase = parent.ResolvedBase();
                if (!IsMotorAxle(parentBase))
                    result.Errors.Add($"wheel_not_on_axle:{c.Id}->parent={parentBase}");
            }

            // Motors should hang off chassis/structural (thin graph rule).
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                if (!IsMotorAxle(c.ResolvedBase()))
                    continue;

                if (!TryFindParent(blueprint, c.Id, out var parentId))
                {
                    result.Errors.Add($"motor_orphan:{c.Id}");
                    continue;
                }

                if (!byId.TryGetValue(parentId, out var parent))
                    continue;

                var parentBase = parent.ResolvedBase();
                if (parentBase != RobotComponentBase.Chassis &&
                    parentBase != RobotComponentBase.Structural &&
                    parentBase != RobotComponentBase.Steering)
                    result.Warnings.Add($"motor_parent_unusual:{c.Id}->{parentBase}");
            }
        }

        static bool IsMotorAxle(RobotComponentBase b) =>
            b == RobotComponentBase.SpinMotor ||
            b == RobotComponentBase.BurstMotor ||
            b == RobotComponentBase.ServoMotor;

        static bool TryFindParent(RobotBlueprint blueprint, string childId, out string parentId)
        {
            parentId = null;
            if (blueprint.Connections == null)
                return false;

            for (var i = 0; i < blueprint.Connections.Length; i++)
            {
                if (blueprint.Connections[i].ChildId != childId)
                    continue;
                parentId = blueprint.Connections[i].ParentId;
                return !string.IsNullOrEmpty(parentId);
            }

            return false;
        }

        static void ValidatePowerBudget(RobotBlueprint blueprint, Result result)
        {
            var hasMotor = false;
            var hasWheel = false;
            var batteryRate = 0f;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var b = blueprint.Components[i].ResolvedBase();
                if (IsMotorAxle(b))
                    hasMotor = true;
                if (b == RobotComponentBase.Wheel)
                    hasWheel = true;
                if (b == RobotComponentBase.Battery)
                    batteryRate += Mathf.Max(0f, blueprint.Components[i].ElecMaxInOutRate);
            }

            if ((hasMotor || hasWheel) && blueprint.Power.ElectricTotal <= 0f)
                result.Errors.Add("electric_budget_required");

            if ((hasMotor || hasWheel) && !ContainsBase(blueprint, RobotComponentBase.Battery))
                result.Errors.Add("battery_required");

            if (blueprint.Power.ElectricTotal > 0f && batteryRate > 0f &&
                blueprint.Power.ElectricMaxInOutRate > batteryRate + 0.01f)
            {
                result.Errors.Add(
                    $"electric_rate_exceeds_battery:{blueprint.Power.ElectricMaxInOutRate}>{batteryRate}");
            }

            if (blueprint.Power.AirTotal > 0f && !ContainsBase(blueprint, RobotComponentBase.AirTank))
                result.Errors.Add("air_tank_required");

            if (ContainsBase(blueprint, RobotComponentBase.BurstPiston) && blueprint.Power.AirTotal <= 0f)
                result.Errors.Add("air_budget_required_for_burst_piston");

            if (ContainsBase(blueprint, RobotComponentBase.BurstPiston) &&
                !ContainsBase(blueprint, RobotComponentBase.AirTank))
                result.Errors.Add("air_tank_required_for_burst_piston");

            if (ContainsBase(blueprint, RobotComponentBase.ServoPiston) && blueprint.Power.AirTotal <= 0f)
                result.Errors.Add("air_budget_required_for_servo_piston");

            if (ContainsBase(blueprint, RobotComponentBase.ServoPiston) &&
                !ContainsBase(blueprint, RobotComponentBase.AirTank))
                result.Errors.Add("air_tank_required_for_servo_piston");
        }

        static void ValidateMassBudgets(RobotBlueprint blueprint, Result result)
        {
            var mass = result.Mass;
            if (mass.RigidbodyCount > RobotMassProperties.MaxRigidbodies)
                result.Errors.Add($"rb_budget:{mass.RigidbodyCount}>{RobotMassProperties.MaxRigidbodies}");
            if (mass.HingeCount > RobotMassProperties.MaxHinges)
                result.Errors.Add($"hinge_budget:{mass.HingeCount}>{RobotMassProperties.MaxHinges}");

            if (!blueprint.HasV1Fields)
                return;

            var cap = RobotMassProperties.MaxMassFor(blueprint.Chassis.WeightClass);
            if (mass.TotalMass > cap + 0.01f)
                result.Errors.Add($"mass_over_class:{mass.TotalMass:F2}>{cap:F0}_{blueprint.Chassis.WeightClass}");
        }

        /// <summary>
        /// Thin AABB overlap: non-connected pairs with significant box overlap → warning (construction feedback).
        /// Connected pairs ignored (axle/wheel stacks are expected).
        /// </summary>
        static void ValidateOverlapThin(
            RobotBlueprint blueprint,
            Dictionary<string, RobotComponentDef> byId,
            Result result)
        {
            var connected = new HashSet<string>(StringComparer.Ordinal);
            if (blueprint.Connections != null)
            {
                for (var i = 0; i < blueprint.Connections.Length; i++)
                {
                    var c = blueprint.Connections[i];
                    connected.Add(PairKey(c.ParentId, c.ChildId));
                }
            }

            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var a = blueprint.Components[i];
                if (string.IsNullOrEmpty(a.Id) || a.IsRoot)
                    continue;
                // Skip tiny modules without colliders in assembly.
                if (a.ResolvedBase() == RobotComponentBase.ControlBoard ||
                    a.ResolvedBase() == RobotComponentBase.Battery ||
                    a.ResolvedBase() == RobotComponentBase.AirTank)
                    continue;

                for (var j = i + 1; j < blueprint.Components.Length; j++)
                {
                    var b = blueprint.Components[j];
                    if (string.IsNullOrEmpty(b.Id) || b.IsRoot)
                        continue;
                    if (connected.Contains(PairKey(a.Id, b.Id)))
                        continue;
                    if (a.ResolvedBase() == RobotComponentBase.ControlBoard ||
                        b.ResolvedBase() == RobotComponentBase.ControlBoard)
                        continue;

                    if (AabbOverlap(a, b, 0.35f))
                        result.Warnings.Add($"overlap:{a.Id}|{b.Id}");
                }
            }
        }

        static string PairKey(string a, string b)
        {
            if (string.CompareOrdinal(a, b) <= 0)
                return a + "|" + b;
            return b + "|" + a;
        }

        static bool AabbOverlap(RobotComponentDef a, RobotComponentDef b, float minPenetration)
        {
            var ha = AbsScale(a.Scale) * 0.5f;
            var hb = AbsScale(b.Scale) * 0.5f;
            var d = a.LocalPosition - b.LocalPosition;
            var ox = ha.x + hb.x - Mathf.Abs(d.x);
            var oy = ha.y + hb.y - Mathf.Abs(d.y);
            var oz = ha.z + hb.z - Mathf.Abs(d.z);
            return ox > minPenetration && oy > minPenetration && oz > minPenetration;
        }

        static Vector3 AbsScale(Vector3 s) =>
            new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        static void ValidateControlWiring(RobotBlueprint blueprint, HashSet<string> componentIds, Result result)
        {
            var slots = blueprint.ControlSlots ?? Array.Empty<RobotControlSlotDef>();
            var wirings = blueprint.Wirings ?? Array.Empty<RobotWiringDef>();
            var slotIds = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (string.IsNullOrEmpty(s.Id))
                {
                    result.Errors.Add($"control_slots[{i}].id_missing");
                    continue;
                }

                if (!slotIds.Add(s.Id))
                    result.Errors.Add($"duplicate_control_slot:{s.Id}");
            }

            for (var i = 0; i < wirings.Length; i++)
            {
                var w = wirings[i];
                if (string.IsNullOrEmpty(w.ControlSlotId))
                    result.Errors.Add($"wirings[{i}].control_missing");
                else if (!slotIds.Contains(w.ControlSlotId) &&
                         !IsSmartZoneId(blueprint, w.ControlSlotId))
                    result.Errors.Add($"wirings[{i}].unknown_control:{w.ControlSlotId}");

                if (string.IsNullOrEmpty(w.ComponentId))
                    result.Errors.Add($"wirings[{i}].component_missing");
                else if (!componentIds.Contains(w.ComponentId))
                    result.Errors.Add($"wirings[{i}].unknown_component:{w.ComponentId}");

                if (string.IsNullOrEmpty(w.Channel))
                    result.Errors.Add($"wirings[{i}].channel_missing");
            }

            if (wirings.Length > 0 && slots.Length == 0 && !HasSmartZoneWiring(blueprint, wirings))
                result.Errors.Add("wiring_without_controls");
        }

        static bool IsSmartZoneId(RobotBlueprint blueprint, string id)
        {
            if (blueprint?.Components == null || string.IsNullOrEmpty(id))
                return false;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                if (!string.Equals(blueprint.Components[i].Id, id, StringComparison.Ordinal))
                    continue;
                return blueprint.Components[i].ResolvedBase() == RobotComponentBase.SmartZone;
            }

            return false;
        }

        static bool HasSmartZoneWiring(RobotBlueprint blueprint, RobotWiringDef[] wirings)
        {
            for (var i = 0; i < wirings.Length; i++)
            {
                if (IsSmartZoneId(blueprint, wirings[i].ControlSlotId))
                    return true;
            }

            return false;
        }

        static bool ContainsBase(RobotBlueprint blueprint, RobotComponentBase b)
        {
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                if (blueprint.Components[i].ResolvedBase() == b)
                    return true;
            }

            return false;
        }
    }
}
