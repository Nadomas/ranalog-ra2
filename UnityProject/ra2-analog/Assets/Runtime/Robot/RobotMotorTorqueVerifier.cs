using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-07: tank-steer locomotion from HingeJoint motors driven by wiring, not chassis AddForce.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMotorTorqueVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.4f;
    [SerializeField] PhysicsMaterial floorMaterial;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsMaterial floor)
    {
        floorMaterial = floor;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        var bp = RobotBlueprint.CreateRa2TankSteerSample(new Vector3(0f, 0.85f, 0f), 0f);
        RobotSpawnedInstance inst;
        try
        {
            inst = RobotSpawnService.Spawn(bp, 0, 0, null, floorMaterial, new Color(0.15f, 0.65f, 0.9f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S3-07] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var drive = inst.Drive;
        var motors = inst.MotorDrive;
        var hingeCount = CountHinges(inst);
        var suppress = drive != null && drive.SuppressChassisForce;
        var motorBindOk = motors != null && motors.MotorCount >= 4 && hingeCount >= 4 && suppress;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var start = drive.transform.position;
        var startYaw = drive.transform.eulerAngles.y;
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = 0f });
            yield return new WaitForFixedUpdate();
        }

        var forwardDelta = Vector3.Distance(start, drive.transform.position);
        var powered = motors != null && motors.LastPoweredMotors >= 4;
        var nan = IsBad(drive.transform.position);
        var rb = drive.GetComponent<Rigidbody>();
        if (rb != null)
            nan = nan || IsBad(rb.linearVelocity);

        drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.25f);

        var yawStart = drive.transform.eulerAngles.y;
        end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            drive.SetCommand(new PhysicsTestDriveCommand { Move = 0.35f, Turn = 1f });
            yield return new WaitForFixedUpdate();
        }

        var yawDelta = Mathf.Abs(Mathf.DeltaAngle(yawStart, drive.transform.eulerAngles.y));
        var turned = yawDelta > 8f;
        var moved = forwardDelta > 0.4f;

        var pass = motorBindOk && moved && powered && turned && !nan;

        Debug.Log(
            $"[S3-07] VERIFIER_DONE pass={pass} motor_bind_ok={motorBindOk} hinges={hingeCount} " +
            $"motors={motors?.MotorCount ?? 0} suppress_chassis={suppress} powered={powered} " +
            $"moved={moved} forward_delta={F3(forwardDelta)} turned={turned} yaw_delta={F3(yawDelta)} " +
            $"nan={nan} start_yaw={F3(startYaw)}");
    }

    static int CountHinges(RobotSpawnedInstance inst)
    {
        if (inst?.Assembly?.Parts == null)
            return 0;
        var n = 0;
        foreach (var kv in inst.Assembly.Parts)
        {
            if (kv.Value != null && kv.Value.GetComponent<HingeJoint>() != null)
                n++;
        }

        return n;
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
