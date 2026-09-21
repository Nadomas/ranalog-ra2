using UnityEngine;

/// <summary>
/// Local-only S1-02 physics drive. Applies <see cref="PhysicsTestDriveCommand"/>
/// in FixedUpdate via Rigidbody forces/torques. Does not read devices.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PhysicsTestDrive : MonoBehaviour
{
    [SerializeField] float driveAcceleration = 28f;
    [SerializeField] float turnAcceleration = 12f;
    [SerializeField] float brakeLinearDamping = 8f;
    [SerializeField] float brakeAngularDamping = 10f;

    Rigidbody body;
    PhysicsTestDisableFlag disableFlag;
    PhysicsTestDriveCommand command;

    public PhysicsTestDriveCommand CurrentCommand => command;

    /// <summary>When true, chassis AddForce is skipped — wheel hinge motors own locomotion (S3-07).</summary>
    public bool SuppressChassisForce { get; set; }

    /// <summary>S7-02 thin degrade multiplier (1 = intact, &lt;1 = damaged drive).</summary>
    public float DrivePowerScale { get; set; } = 1f;

    public void SetCommand(PhysicsTestDriveCommand next)
    {
        command = next;
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        disableFlag = GetComponent<PhysicsTestDisableFlag>();
    }

    void FixedUpdate()
    {
        if (disableFlag == null)
            disableFlag = GetComponent<PhysicsTestDisableFlag>();
        if (disableFlag != null && disableFlag.Disabled)
            return;

        if (command.Brake)
        {
            var linear = body.linearVelocity;
            var angular = body.angularVelocity;
            if (linear.sqrMagnitude > 0.0001f)
                body.AddForce(-linear * brakeLinearDamping, ForceMode.Acceleration);
            if (angular.sqrMagnitude > 0.0001f)
                body.AddTorque(-angular * brakeAngularDamping, ForceMode.Acceleration);
            return;
        }

        if (SuppressChassisForce)
            return;

        var scale = Mathf.Clamp01(DrivePowerScale);
        if (Mathf.Abs(command.Move) > 0.01f)
            body.AddForce(transform.forward * (command.Move * driveAcceleration * scale), ForceMode.Acceleration);

        if (Mathf.Abs(command.Turn) > 0.01f)
            body.AddTorque(transform.up * (command.Turn * turnAcceleration * scale), ForceMode.Acceleration);
    }
}
