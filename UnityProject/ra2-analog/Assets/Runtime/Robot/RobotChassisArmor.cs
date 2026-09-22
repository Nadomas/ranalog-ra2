using System;

namespace Ra2.Robot
{
    /// <summary>
    /// S16-02 RA2 armor tradeoff (thin): cycle Polymer/Aluminum/Titanium/Steel and scale chassis mass.
    /// Presentation + mass only — not a full damage table freeze.
    /// </summary>
    public static class RobotChassisArmor
    {
        /// <summary>Relative density vs Aluminum baseline (RA2-style strength↔weight feel).</summary>
        public static float DensityFactor(RobotArmorType armor) =>
            armor switch
            {
                RobotArmorType.Polymer => 0.65f,
                RobotArmorType.Aluminum => 1.0f,
                RobotArmorType.Titanium => 1.35f,
                RobotArmorType.Steel => 1.75f,
                _ => 1f
            };

        public static string CatalogIdFor(RobotArmorType armor) =>
            armor switch
            {
                RobotArmorType.Polymer => "chassis_plast",
                RobotArmorType.Aluminum => "chassis_alum",
                RobotArmorType.Titanium => "chassis_tit",
                RobotArmorType.Steel => "chassis_steel",
                _ => "chassis_alum"
            };

        public static bool TryCycleArmor(RobotBlueprint blueprint, out RobotArmorType applied, out string error)
        {
            applied = RobotArmorType.Aluminum;
            error = null;
            if (blueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            var next = (RobotArmorType)(((int)blueprint.Chassis.Armor + 1) % 4);
            return TryApplyArmor(blueprint, next, out applied, out error);
        }

        public static bool TryApplyArmor(
            RobotBlueprint blueprint,
            RobotArmorType armor,
            out RobotArmorType applied,
            out string error)
        {
            applied = armor;
            error = null;
            if (blueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            var chassis = blueprint.Chassis;
            var prevFactor = DensityFactor(chassis.Armor);
            chassis.Armor = armor;
            blueprint.Chassis = chassis;

            if (blueprint.Components == null)
                return true;

            var scale = DensityFactor(armor) / Math.Max(0.01f, prevFactor);
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var c = blueprint.Components[i];
                if (c.ResolvedBase() != RobotComponentBase.Chassis &&
                    !string.Equals(c.Id, "chassis", StringComparison.Ordinal))
                    continue;
                c.Mass = Math.Max(0.01f, c.Mass * scale);
                c.CatalogId = CatalogIdFor(armor);
                blueprint.Components[i] = c;
            }

            applied = armor;
            return true;
        }
    }
}
