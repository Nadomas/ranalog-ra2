using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// JSON round-trip for <see cref="RobotBlueprint"/>.
    /// Supports <c>ra2.robot_blueprint.v0</c> (S3-02) and <c>ra2.robot_blueprint.v1</c> (RA2-aligned).
    /// </summary>
    public static class RobotBlueprintSerializer
    {
        public const string SchemaIdV0 = "ra2.robot_blueprint.v0";
        public const string SchemaIdV1 = "ra2.robot_blueprint.v1";
        /// <summary>Latest schema for new writes.</summary>
        public const string SchemaId = SchemaIdV1;

        [Serializable]
        sealed class Envelope
        {
            public string schema = SchemaIdV1;
            public RobotBlueprint blueprint;
        }

        public static string ToJson(RobotBlueprint blueprint, bool pretty = false, string schema = null)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));
            EnsureArrays(blueprint);
            schema ??= blueprint.HasV1Fields ? SchemaIdV1 : SchemaIdV0;
            var envelope = new Envelope { schema = schema, blueprint = blueprint };
            return JsonUtility.ToJson(envelope, pretty);
        }

        public static RobotBlueprint FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("json is null or empty", nameof(json));

            var envelope = JsonUtility.FromJson<Envelope>(json);
            if (envelope == null || envelope.blueprint == null)
                throw new InvalidOperationException("Failed to deserialize RobotBlueprint envelope.");

            if (!IsSupportedSchema(envelope.schema))
            {
                throw new InvalidOperationException(
                    "Unsupported blueprint schema '" + envelope.schema + "' (expected v0 or v1).");
            }

            EnsureArrays(envelope.blueprint);
            return envelope.blueprint;
        }

        public static bool IsSupportedSchema(string schema) =>
            string.IsNullOrEmpty(schema) ||
            string.Equals(schema, SchemaIdV0, StringComparison.Ordinal) ||
            string.Equals(schema, SchemaIdV1, StringComparison.Ordinal);

        public static void WriteFile(RobotBlueprint blueprint, string path, bool pretty = true, string schema = null)
        {
            var json = ToJson(blueprint, pretty, schema);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public static RobotBlueprint ReadFile(string path)
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return FromJson(json);
        }

        /// <summary>Compares fields that must survive U-SER for assembly + drive.</summary>
        public static List<string> DiffCritical(RobotBlueprint a, RobotBlueprint b)
        {
            var diffs = new List<string>();
            if (a == null || b == null)
            {
                diffs.Add("null_blueprint");
                return diffs;
            }

            if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal))
                diffs.Add($"Name '{a.Name}'!='{b.Name}'");
            if (!Approx(a.RootYawDegrees, b.RootYawDegrees))
                diffs.Add($"RootYawDegrees {a.RootYawDegrees}!={b.RootYawDegrees}");
            if (!Approx(a.RootPosition, b.RootPosition))
                diffs.Add($"RootPosition {a.RootPosition}!={b.RootPosition}");

            DiffChassis(a.Chassis, b.Chassis, diffs);
            DiffPower(a.Power, b.Power, diffs);
            DiffComponents(a.Components, b.Components, diffs);
            DiffConnections(a.Connections, b.Connections, diffs);
            DiffControlSlots(a.ControlSlots, b.ControlSlots, diffs);
            DiffWirings(a.Wirings, b.Wirings, diffs);

            return diffs;
        }

        public static long MeasureRoundTripBytes(RobotBlueprint blueprint, out double serializeMs, out double deserializeMs)
        {
            var sw = Stopwatch.StartNew();
            var json = ToJson(blueprint, pretty: false);
            serializeMs = sw.Elapsed.TotalMilliseconds;
            var bytes = Encoding.UTF8.GetByteCount(json);
            sw.Restart();
            _ = FromJson(json);
            deserializeMs = sw.Elapsed.TotalMilliseconds;
            return bytes;
        }

        static void DiffChassis(RobotChassisDef a, RobotChassisDef b, List<string> diffs)
        {
            if (!Approx(a.Height, b.Height))
                diffs.Add($"Chassis.Height {a.Height}!={b.Height}");
            if (a.Armor != b.Armor)
                diffs.Add($"Chassis.Armor {a.Armor}!={b.Armor}");
            if (a.WeightClass != b.WeightClass)
                diffs.Add($"Chassis.WeightClass {a.WeightClass}!={b.WeightClass}");

            var ap = a.BaseplatePoints ?? Array.Empty<Vector2>();
            var bp = b.BaseplatePoints ?? Array.Empty<Vector2>();
            if (ap.Length != bp.Length)
                diffs.Add($"Chassis.BaseplatePoints.Length {ap.Length}!={bp.Length}");
            else
            {
                for (var i = 0; i < ap.Length; i++)
                {
                    if (!Approx(ap[i], bp[i]))
                        diffs.Add($"Chassis.BaseplatePoints[{i}]");
                }
            }
        }

        static void DiffPower(RobotPowerBudgetDef a, RobotPowerBudgetDef b, List<string> diffs)
        {
            if (!Approx(a.ElectricTotal, b.ElectricTotal))
                diffs.Add("Power.ElectricTotal");
            if (!Approx(a.ElectricMaxInOutRate, b.ElectricMaxInOutRate))
                diffs.Add("Power.ElectricMaxInOutRate");
            if (!Approx(a.AirTotal, b.AirTotal))
                diffs.Add("Power.AirTotal");
            if (!Approx(a.AirMaxInOutRate, b.AirMaxInOutRate))
                diffs.Add("Power.AirMaxInOutRate");
        }

        static void DiffComponents(RobotComponentDef[] a, RobotComponentDef[] b, List<string> diffs)
        {
            a ??= Array.Empty<RobotComponentDef>();
            b ??= Array.Empty<RobotComponentDef>();
            if (a.Length != b.Length)
                diffs.Add($"Components.Length {a.Length}!={b.Length}");
            else
            {
                for (var i = 0; i < a.Length; i++)
                {
                    var x = a[i];
                    var y = b[i];
                    if (!string.Equals(x.Id, y.Id, StringComparison.Ordinal))
                        diffs.Add($"Components[{i}].Id");
                    if (x.Kind != y.Kind)
                        diffs.Add($"Components[{i}].Kind");
                    if (x.Base != y.Base)
                        diffs.Add($"Components[{i}].Base");
                    if (!SameText(x.CatalogId, y.CatalogId))
                        diffs.Add($"Components[{i}].CatalogId");
                    if (!Approx(x.LocalPosition, y.LocalPosition))
                        diffs.Add($"Components[{i}].LocalPosition");
                    if (!Approx(x.LocalEuler, y.LocalEuler))
                        diffs.Add($"Components[{i}].LocalEuler");
                    if (!Approx(x.Scale, y.Scale))
                        diffs.Add($"Components[{i}].Scale");
                    if (!Approx(x.Mass, y.Mass))
                        diffs.Add($"Components[{i}].Mass");
                    if (x.HasRigidbody != y.HasRigidbody)
                        diffs.Add($"Components[{i}].HasRigidbody");
                    if (x.IsRoot != y.IsRoot)
                        diffs.Add($"Components[{i}].IsRoot");
                    if (!Approx(x.ElecMaxInOutRate, y.ElecMaxInOutRate))
                        diffs.Add($"Components[{i}].ElecMaxInOutRate");
                    if (!Approx(x.AirMaxInOutRate, y.AirMaxInOutRate))
                        diffs.Add($"Components[{i}].AirMaxInOutRate");
                    if (!Approx(x.Concussion, y.Concussion))
                        diffs.Add($"Components[{i}].Concussion");
                    if (!Approx(x.Piercing, y.Piercing))
                        diffs.Add($"Components[{i}].Piercing");
                }
            }
        }

        static void DiffConnections(RobotConnectionDef[] a, RobotConnectionDef[] b, List<string> diffs)
        {
            a ??= Array.Empty<RobotConnectionDef>();
            b ??= Array.Empty<RobotConnectionDef>();
            if (a.Length != b.Length)
                diffs.Add($"Connections.Length {a.Length}!={b.Length}");
            else
            {
                for (var i = 0; i < a.Length; i++)
                {
                    var x = a[i];
                    var y = b[i];
                    if (!string.Equals(x.ParentId, y.ParentId, StringComparison.Ordinal))
                        diffs.Add($"Connections[{i}].ParentId");
                    if (!string.Equals(x.ChildId, y.ChildId, StringComparison.Ordinal))
                        diffs.Add($"Connections[{i}].ChildId");
                    if (x.Joint != y.Joint)
                        diffs.Add($"Connections[{i}].Joint");
                    if (!Approx(x.HingeAxis, y.HingeAxis))
                        diffs.Add($"Connections[{i}].HingeAxis");
                }
            }
        }

        static void DiffControlSlots(RobotControlSlotDef[] a, RobotControlSlotDef[] b, List<string> diffs)
        {
            a ??= Array.Empty<RobotControlSlotDef>();
            b ??= Array.Empty<RobotControlSlotDef>();
            if (a.Length != b.Length)
                diffs.Add($"ControlSlots.Length {a.Length}!={b.Length}");
            else
            {
                for (var i = 0; i < a.Length; i++)
                {
                    var x = a[i];
                    var y = b[i];
                    if (!string.Equals(x.Id, y.Id, StringComparison.Ordinal))
                        diffs.Add($"ControlSlots[{i}].Id");
                    if (!SameText(x.DisplayName, y.DisplayName))
                        diffs.Add($"ControlSlots[{i}].DisplayName");
                    if (x.Kind != y.Kind)
                        diffs.Add($"ControlSlots[{i}].Kind");
                    if (!SameText(x.InputBinding, y.InputBinding))
                        diffs.Add($"ControlSlots[{i}].InputBinding");
                }
            }
        }

        static void DiffWirings(RobotWiringDef[] a, RobotWiringDef[] b, List<string> diffs)
        {
            a ??= Array.Empty<RobotWiringDef>();
            b ??= Array.Empty<RobotWiringDef>();
            if (a.Length != b.Length)
                diffs.Add($"Wirings.Length {a.Length}!={b.Length}");
            else
            {
                for (var i = 0; i < a.Length; i++)
                {
                    var x = a[i];
                    var y = b[i];
                    if (!string.Equals(x.ControlSlotId, y.ControlSlotId, StringComparison.Ordinal))
                        diffs.Add($"Wirings[{i}].ControlSlotId");
                    if (!string.Equals(x.ComponentId, y.ComponentId, StringComparison.Ordinal))
                        diffs.Add($"Wirings[{i}].ComponentId");
                    if (!string.Equals(x.Channel, y.Channel, StringComparison.Ordinal))
                        diffs.Add($"Wirings[{i}].Channel");
                    if (!Approx(x.Sign, y.Sign))
                        diffs.Add($"Wirings[{i}].Sign");
                }
            }
        }

        static void EnsureArrays(RobotBlueprint bp)
        {
            if (bp.Components == null)
                bp.Components = Array.Empty<RobotComponentDef>();
            if (bp.Connections == null)
                bp.Connections = Array.Empty<RobotConnectionDef>();
            if (bp.ControlSlots == null)
                bp.ControlSlots = Array.Empty<RobotControlSlotDef>();
            if (bp.Wirings == null)
                bp.Wirings = Array.Empty<RobotWiringDef>();
            if (bp.Chassis.BaseplatePoints == null)
                bp.Chassis.BaseplatePoints = Array.Empty<Vector2>();
        }

        static bool SameText(string a, string b) =>
            string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal);

        static bool Approx(float a, float b) => Mathf.Abs(a - b) <= 1e-4f;

        static bool Approx(Vector2 a, Vector2 b) =>
            Approx(a.x, b.x) && Approx(a.y, b.y);

        static bool Approx(Vector3 a, Vector3 b) =>
            Approx(a.x, b.x) && Approx(a.y, b.y) && Approx(a.z, b.z);
    }
}
