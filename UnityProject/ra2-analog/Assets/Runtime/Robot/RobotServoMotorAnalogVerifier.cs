using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-07: ServoMotor Analog slow rotate toward target + lock at stop (hinge motor, no Transform writes).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotServoMotorAnalogVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] PhysicsMaterial slideMaterial;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsMaterial slide)
    {
        slideMaterial = slide;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var bp = RobotBlueprint.CreateRa2ServoMotorAnalogSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-07] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.55f, 0.4f, 0.75f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-07] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.ServoMotorCount < 1)
        {
            Debug.Log($"[S7-07] VERIFIER_DONE pass=False reason=no_servo count={act?.ServoMotorCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("servo_motor", out var servoGo) || servoGo == null)
        {
            Debug.Log("[S7-07] VERIFIER_DONE pass=False reason=missing_servo");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var hinge = servoGo.GetComponent<HingeJoint>();
        var rb = servoGo.GetComponent<Rigidbody>();
        var limitsOk = hinge != null && hinge.useLimits && hinge.limits.max <= 90.5f && hinge.limits.min >= -90.5f;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Drive analog toward +max
        var driveEnd = Time.time + 0.85f;
        while (Time.time < driveEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f });
            yield return new WaitForFixedUpdate();
        }

        var angle = hinge != null ? hinge.angle : 0f;
        if (float.IsNaN(angle) || float.IsInfinity(angle))
            angle = 0f;
        var sped = rb != null ? rb.angularVelocity.magnitude : 0f;
        var moved = Mathf.Abs(angle) > 12f || sped > 0.8f;

        // Release analog → lock at stop
        var lockEnd = Time.time + 0.55f;
        while (Time.time < lockEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 0f });
            yield return new WaitForFixedUpdate();
        }

        var lockedSpeed = rb != null ? rb.angularVelocity.magnitude : 99f;
        var locked = lockedSpeed < 1.25f && act.LastServoLocked >= 1;
        var angleLocked = hinge != null ? hinge.angle : angle;
        if (float.IsNaN(angleLocked) || float.IsInfinity(angleLocked))
            angleLocked = angle;
        var held = Mathf.Abs(angleLocked) > 8f; // did not snap to zero via Transform
        var nan = rb != null && IsBad(rb.angularVelocity);

        var pass = limitsOk && moved && locked && held && !nan;
        Debug.Log(
            $"[S7-07] VERIFIER_DONE pass={pass} limits_ok={limitsOk} moved={moved} locked={locked} held={held} " +
            $"angle={F3(angle)} angle_lock={F3(angleLocked)} drive_w={F3(sped)} lock_w={F3(lockedSpeed)} " +
            $"servo_locked={act.LastServoLocked} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
