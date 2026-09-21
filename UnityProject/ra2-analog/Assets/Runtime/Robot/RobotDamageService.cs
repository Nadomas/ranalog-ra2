using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>S7-02 thin weapon mix (0–1 concussion + piercing) × impact proxy.</summary>
    public readonly struct RobotWeaponHit
    {
        public readonly float Concussion;
        public readonly float Piercing;
        public readonly float Impact;
        public readonly float ArmorAbsorb;

        public RobotWeaponHit(float concussion, float piercing, float impact, float armorAbsorb = 0f)
        {
            Concussion = concussion;
            Piercing = piercing;
            Impact = impact;
            ArmorAbsorb = armorAbsorb;
        }
    }

    public enum RobotWeaponHitOutcome
    {
        None = 0,
        Degraded = 1,
        Disabled = 2
    }

    /// <summary>
    /// S7-01 functional disable + S7-02 thin host-applied weapon hit → degrade/disable.
    /// No detach / catalog; severity is plain C# for host/sim authority.
    /// </summary>
    public static class RobotDamageService
    {
        public const float DegradeSeverityThreshold = 0.5f;
        public const float DisableSeverityThreshold = 1.0f;
        public const float DegradedDrivePowerScale = 0.35f;

        public static bool TryFunctionalDisable(RobotSpawnedInstance instance, out string error)
        {
            error = null;
            if (instance == null)
            {
                error = "no_instance";
                return false;
            }

            RobotSpawnService.SetDisabled(instance, true);
            // Kill residual coast with Rigidbody velocity writes (not Transform teleport).
            StopDynamics(instance);
            return true;
        }

        public static bool TryFunctionalEnable(RobotSpawnedInstance instance, out string error)
        {
            error = null;
            if (instance == null)
            {
                error = "no_instance";
                return false;
            }

            RobotSpawnService.SetDisabled(instance, false);
            return true;
        }

        public static bool IsFunctionallyDisabled(RobotSpawnedInstance instance) =>
            instance?.DisableFlag != null && instance.DisableFlag.Disabled;

        /// <summary>
        /// Thin formula: impact × mean(concussion, piercing) × (1 − armor).
        /// Host/sim only — clients must not decide outcomes.
        /// </summary>
        public static float ComputeHitSeverity(in RobotWeaponHit hit)
        {
            var concussion = Mathf.Clamp01(hit.Concussion);
            var piercing = Mathf.Clamp01(hit.Piercing);
            var impact = Mathf.Max(0f, hit.Impact);
            var armor = Mathf.Clamp01(hit.ArmorAbsorb);
            var mix = 0.5f * (concussion + piercing);
            return impact * mix * (1f - armor);
        }

        /// <summary>
        /// Apply one host weapon hit: accumulate severity → degrade drive scale → functional disable.
        /// </summary>
        public static bool TryApplyWeaponHit(
            RobotSpawnedInstance instance,
            in RobotWeaponHit hit,
            ref float accumulatedSeverity,
            out RobotWeaponHitOutcome outcome,
            out float appliedSeverity,
            out string error)
        {
            outcome = RobotWeaponHitOutcome.None;
            appliedSeverity = 0f;
            error = null;
            if (instance == null)
            {
                error = "no_instance";
                return false;
            }

            if (IsFunctionallyDisabled(instance))
            {
                error = "already_disabled";
                return false;
            }

            appliedSeverity = ComputeHitSeverity(hit);
            accumulatedSeverity += appliedSeverity;

            if (accumulatedSeverity >= DisableSeverityThreshold)
            {
                if (!TryFunctionalDisable(instance, out error))
                    return false;
                ApplyDrivePowerScale(instance, 0f);
                outcome = RobotWeaponHitOutcome.Disabled;
                return true;
            }

            if (accumulatedSeverity >= DegradeSeverityThreshold)
            {
                ApplyDrivePowerScale(instance, DegradedDrivePowerScale);
                outcome = RobotWeaponHitOutcome.Degraded;
                return true;
            }

            outcome = RobotWeaponHitOutcome.None;
            return true;
        }

        public static void ApplyDrivePowerScale(RobotSpawnedInstance instance, float scale)
        {
            if (instance == null)
                return;
            var clamped = Mathf.Clamp01(scale);
            if (instance.Drive != null)
                instance.Drive.DrivePowerScale = clamped;
            if (instance.MotorDrive != null)
                instance.MotorDrive.DrivePowerScale = clamped;
        }

        static void StopDynamics(RobotSpawnedInstance instance)
        {
            if (instance.Assembly?.RootBody != null)
            {
                instance.Assembly.RootBody.linearVelocity = Vector3.zero;
                instance.Assembly.RootBody.angularVelocity = Vector3.zero;
            }

            if (instance.Assembly?.Parts == null)
                return;

            foreach (var kv in instance.Assembly.Parts)
            {
                if (kv.Value == null)
                    continue;
                var rb = kv.Value.GetComponent<Rigidbody>();
                if (rb == null || rb == instance.Assembly.RootBody)
                    continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
