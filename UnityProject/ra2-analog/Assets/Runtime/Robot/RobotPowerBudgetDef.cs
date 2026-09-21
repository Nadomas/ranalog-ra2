using System;

namespace Ra2.Robot
{
    /// <summary>Aggregated electric + pneumatic budgets (RA2 battery / air tank totals).</summary>
    [Serializable]
    public struct RobotPowerBudgetDef
    {
        public float ElectricTotal;
        public float ElectricMaxInOutRate;
        public float AirTotal;
        public float AirMaxInOutRate;
    }
}
