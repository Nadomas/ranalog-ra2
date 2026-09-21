using System;

namespace Ra2.Robot
{
    public enum RobotJointKind : byte
    {
        FixedHierarchy = 0,
        Hinge = 1,
        /// <summary>Linear slide (BurstPiston thin). <see cref="RobotConnectionDef.HingeAxis"/> = slide axis.</summary>
        Slider = 2
    }

    /// <summary>Connection between component ids in a blueprint (S3-01).</summary>
    [Serializable]
    public struct RobotConnectionDef
    {
        public string ParentId;
        public string ChildId;
        public RobotJointKind Joint;
        /// <summary>Hinge axis in child local space when <see cref="Joint"/> is Hinge.</summary>
        public UnityEngine.Vector3 HingeAxis;
    }
}
