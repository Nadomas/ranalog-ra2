using System;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>Chassis shape + armor (RA2 workshop step 1–2). Polygon is XZ baseplate in robot-local space.</summary>
    [Serializable]
    public struct RobotChassisDef
    {
        /// <summary>Baseplate outline (≤16 points). Empty = use root scale box proxy.</summary>
        public Vector2[] BaseplatePoints;

        public float Height;
        public RobotArmorType Armor;
        public RobotWeightClass WeightClass;
    }
}
