namespace Ra2.Robot
{
    /// <summary>RA2-style runtime component class (<c>base</c> in original defs). Data-driven assembly key.</summary>
    public enum RobotComponentBase : byte
    {
        Chassis = 0,
        ControlBoard = 1,
        Battery = 2,
        AirTank = 3,
        SpinMotor = 4,
        BurstMotor = 5,
        ServoMotor = 6,
        BurstPiston = 7,
        ServoPiston = 8,
        Wheel = 9,
        Weapon = 10,
        SmartZone = 11,
        Steering = 12,
        Structural = 13
    }
}
