using System;

namespace Ra2.Robot
{
    /// <summary>Control slot → component channel (RA2 wiring). Channel names match actuator semantics.</summary>
    [Serializable]
    public struct RobotWiringDef
    {
        public string ControlSlotId;
        public string ComponentId;
        /// <summary>e.g. CW, CCW, Fire, Extend, Retract, Forward, LeftRight.</summary>
        public string Channel;
        /// <summary>Sign for differential wiring (−1 / +1). Tank LeftRight uses ± on paired wheels.</summary>
        public float Sign;
    }
}
