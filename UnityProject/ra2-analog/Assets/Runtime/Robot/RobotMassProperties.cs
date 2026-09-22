using System;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Plain mass / CoM readout from blueprint data (S4-01). Same numbers for construction preview and spawn admit.
    /// </summary>
    public readonly struct RobotMassReport
    {
        public readonly float TotalMass;
        public readonly Vector3 CenterOfMassLocal;
        public readonly int RigidbodyCount;
        public readonly int HingeCount;
        public readonly int ComponentCount;

        public RobotMassReport(float totalMass, Vector3 comLocal, int rbCount, int hingeCount, int componentCount)
        {
            TotalMass = totalMass;
            CenterOfMassLocal = comLocal;
            RigidbodyCount = rbCount;
            HingeCount = hingeCount;
            ComponentCount = componentCount;
        }
    }

    public static class RobotMassProperties
    {
        /// <summary>Provisional weight-class mass caps (kg). Tunable; not frozen balance.</summary>
        public const float LightweightMaxKg = 50f;
        public const float MiddleweightMaxKg = 100f;
        public const float HeavyweightMaxKg = 200f;

        public const int MaxRigidbodies = 12;
        public const int MaxHinges = 8;

        public static float MaxMassFor(RobotWeightClass weightClass) =>
            weightClass switch
            {
                RobotWeightClass.Lightweight => LightweightMaxKg,
                RobotWeightClass.Middleweight => MiddleweightMaxKg,
                RobotWeightClass.Heavyweight => HeavyweightMaxKg,
                _ => LightweightMaxKg
            };

        public static RobotMassReport Compute(RobotBlueprint blueprint)
        {
            if (blueprint?.Components == null || blueprint.Components.Length == 0)
                return new RobotMassReport(0f, Vector3.zero, 0, 0, 0);

            var total = 0f;
            var moment = Vector3.zero;
            var rbCount = 0;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                var m = Mathf.Max(0f, c.Mass);
                if (m <= 0f && !c.HasRigidbody && !c.IsRoot)
                    continue;

                // Root chassis always contributes mass; hierarchy-fixed parts with Mass>0 add to CoM.
                if (c.IsRoot || c.HasRigidbody || m > 0f)
                {
                    var useMass = m > 0f ? m : (c.IsRoot ? 0.01f : 0f);
                    if (useMass <= 0f)
                        continue;
                    total += useMass;
                    moment += c.LocalPosition * useMass;
                }

                if (c.IsRoot || c.HasRigidbody)
                    rbCount++;
            }

            var hingeCount = 0;
            if (blueprint.Connections != null)
            {
                for (var i = 0; i < blueprint.Connections.Length; i++)
                {
                    if (blueprint.Connections[i].Joint == RobotJointKind.Hinge)
                        hingeCount++;
                }
            }

            var com = total > 1e-6f ? moment / total : Vector3.zero;
            return new RobotMassReport(total, com, rbCount, hingeCount, blueprint.Components.Length);
        }

        /// <summary>
        /// Mass + CoM for the chassis root Rigidbody only (excludes parts that spawn their own RB).
        /// Prevents double-counting wheel/actuator masses that made bots feel floaty after hits.
        /// </summary>
        public static void ApplyToRootBody(Rigidbody rootBody, RobotBlueprint blueprint)
        {
            if (rootBody == null || blueprint?.Components == null)
                return;

            var total = 0f;
            var moment = Vector3.zero;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                // Separate dynamic bodies keep their own mass; do not fold into root.
                if (c.HasRigidbody && !c.IsRoot)
                    continue;

                var m = Mathf.Max(0f, c.Mass);
                if (m <= 0f && !c.IsRoot)
                    continue;
                var useMass = m > 0f ? m : 0.01f;
                total += useMass;
                moment += c.LocalPosition * useMass;
            }

            rootBody.mass = Mathf.Max(0.01f, total);
            var com = total > 1e-6f ? moment / total : Vector3.zero;
            // Bias CoM slightly downward so wheels plant after impacts without FreezeRotation hacks.
            com.y = Mathf.Min(com.y, 0.05f) - 0.12f;
            rootBody.centerOfMass = com;
            rootBody.useGravity = true;
            rootBody.isKinematic = false;
            rootBody.constraints = RigidbodyConstraints.None;
        }

        /// <summary>Legacy overload — prefer <see cref="ApplyToRootBody(Rigidbody, RobotBlueprint)"/>.</summary>
        public static void ApplyToRootBody(Rigidbody rootBody, RobotMassReport report)
        {
            if (rootBody == null || report.TotalMass <= 1e-6f)
                return;

            // Without blueprint we cannot strip separate RBs — keep prior behavior but clear freeze.
            rootBody.mass = Mathf.Max(0.01f, report.TotalMass);
            var com = report.CenterOfMassLocal;
            com.y = Mathf.Min(com.y, 0.05f) - 0.12f;
            rootBody.centerOfMass = com;
            rootBody.useGravity = true;
            rootBody.constraints = RigidbodyConstraints.None;
        }
    }
}
