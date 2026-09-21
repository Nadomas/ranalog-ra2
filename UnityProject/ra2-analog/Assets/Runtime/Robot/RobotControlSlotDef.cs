using System;

namespace Ra2.Robot
{
    [Serializable]
    public struct RobotControlSlotDef
    {
        public string Id;
        public string DisplayName;
        public RobotControlKind Kind;
        /// <summary>Device binding label (e.g. W/S, Axis1). Presentation/input layer interprets.</summary>
        public string InputBinding;
    }
}
