/// <summary>
/// Plain command envelope for S2-01 local authority bus (and later net RPC payload shape).
/// </summary>
public readonly struct PhysicsTestCommandEnvelope
{
    public readonly int RobotId;
    public readonly int SourceId;
    public readonly PhysicsTestDriveCommand Command;

    public PhysicsTestCommandEnvelope(int robotId, int sourceId, PhysicsTestDriveCommand command)
    {
        RobotId = robotId;
        SourceId = sourceId;
        Command = command;
    }
}
