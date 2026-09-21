using System;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>Component definition for <see cref="RobotAssembler"/> (S3-01+, v1 RA2 fields).</summary>
    [Serializable]
    public struct RobotComponentDef
    {
        public string Id;
        /// <summary>Legacy S3-01 kind; v1 blueprints also set <see cref="Base"/>.</summary>
        public RobotComponentKind Kind;
        /// <summary>RA2-style runtime class (v1). When unset, inferred from <see cref="Kind"/>.</summary>
        public RobotComponentBase Base;
        /// <summary>Catalog / def id (e.g. ztek, battery2). Optional for spike primitives.</summary>
        public string CatalogId;
        public Vector3 LocalPosition;
        public Vector3 LocalEuler;
        public Vector3 Scale;
        public float Mass;
        public bool HasRigidbody;
        public bool IsRoot;
        /// <summary>Per-part electric draw cap (negative = consumer), RA2 elecMaxInOutRate.</summary>
        public float ElecMaxInOutRate;
        public float AirMaxInOutRate;
        public float Concussion;
        public float Piercing;

        public RobotComponentBase ResolvedBase()
        {
            if (Kind == RobotComponentKind.Module)
                return Base;
            return MapKindToBase(Kind);
        }

        public static RobotComponentBase MapKindToBase(RobotComponentKind kind) =>
            kind switch
            {
                RobotComponentKind.Chassis => RobotComponentBase.Chassis,
                RobotComponentKind.Wheel => RobotComponentBase.Wheel,
                RobotComponentKind.NoseMarker => RobotComponentBase.Structural,
                _ => RobotComponentBase.Structural
            };
    }
}
