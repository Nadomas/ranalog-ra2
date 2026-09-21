using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S7-03 thin collision → impact proxy → existing host weapon hit apply.
    /// Impact from relative contact speed; clients must not decide outcomes.
    /// </summary>
    public static class RobotContactWeaponHit
    {
        public const float DefaultImpactPerSpeed = 0.15f;
        public const float MaxImpact = 2.5f;
        public const float MinRelativeSpeed = 0.75f;

        public static float ImpactFromRelativeSpeed(
            float relativeSpeed,
            float impactPerSpeed = DefaultImpactPerSpeed)
        {
            if (relativeSpeed < MinRelativeSpeed)
                return 0f;
            return Mathf.Min(MaxImpact, relativeSpeed * Mathf.Max(0f, impactPerSpeed));
        }

        /// <summary>
        /// Host/sim: map contact relative speed → <see cref="RobotWeaponHit"/> → damage service.
        /// Returns true when the call is valid (including intentional no-op below min speed).
        /// </summary>
        public static bool TryApplyFromContact(
            RobotSpawnedInstance victim,
            float concussion,
            float piercing,
            float relativeSpeed,
            ref float accumulatedSeverity,
            out RobotWeaponHitOutcome outcome,
            out float appliedSeverity,
            out float impactUsed,
            out string error,
            float armorAbsorb = 0f,
            float impactPerSpeed = DefaultImpactPerSpeed)
        {
            outcome = RobotWeaponHitOutcome.None;
            appliedSeverity = 0f;
            impactUsed = ImpactFromRelativeSpeed(relativeSpeed, impactPerSpeed);
            error = null;

            if (victim == null)
            {
                error = "no_victim";
                return false;
            }

            if (impactUsed <= 0f)
                return true;

            var hit = new RobotWeaponHit(concussion, piercing, impactUsed, armorAbsorb);
            return RobotDamageService.TryApplyWeaponHit(
                victim, hit, ref accumulatedSeverity, out outcome, out appliedSeverity, out error);
        }
    }
}
