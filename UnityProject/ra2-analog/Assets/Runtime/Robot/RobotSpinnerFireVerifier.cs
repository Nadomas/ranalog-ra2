using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-04: SpinMotor continuous CW via Button Fire wiring (hinge motor, no Transform writes).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotSpinnerFireVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float spinSeconds = 0.7f;
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

        var bp = RobotBlueprint.CreateRa2SpinnerFireSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-04] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.9f, 0.35f, 0.15f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-04] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("spinner_motor", out var spinnerGo) || spinnerGo == null)
        {
            Debug.Log("[S7-04] VERIFIER_DONE pass=False reason=missing_spinner");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var hinge = spinnerGo.GetComponent<HingeJoint>();
        var spinnerRb = spinnerGo.GetComponent<Rigidbody>();
        var motorBound = bot.MotorDrive != null && bot.MotorDrive.MotorCount >= 5;
        var hingeOk = hinge != null && spinnerRb != null;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Idle: Fire off — spinner should stay near rest.
        var idleEnd = Time.time + 0.25f;
        while (Time.time < idleEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 0f, Turn = 0f, Fire = 0f });
            yield return new WaitForFixedUpdate();
        }

        var idleSpeed = spinnerRb != null ? spinnerRb.angularVelocity.magnitude : 0f;

        var spinEnd = Time.time + spinSeconds;
        while (Time.time < spinEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 0f, Turn = 0f, Fire = 1f });
            yield return new WaitForFixedUpdate();
        }

        var spunSpeed = spinnerRb != null ? spinnerRb.angularVelocity.magnitude : 0f;
        var powered = bot.MotorDrive != null && bot.MotorDrive.LastPoweredMotors >= 1;
        var spun = spunSpeed > 4f && spunSpeed > idleSpeed + 2f;
        var nan = IsBad(spinnerRb != null ? spinnerRb.angularVelocity : Vector3.zero);

        // Release Fire — speed should drop (not Transform-teleport stop).
        var coastEnd = Time.time + 0.45f;
        while (Time.time < coastEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
            yield return new WaitForFixedUpdate();
        }

        var coastSpeed = spinnerRb != null ? spinnerRb.angularVelocity.magnitude : 0f;
        var coasted = coastSpeed < spunSpeed * 0.85f;

        var pass = hingeOk && motorBound && spun && powered && coasted && !nan;
        Debug.Log(
            $"[S7-04] VERIFIER_DONE pass={pass} hinge_ok={hingeOk} motor_bound={motorBound} " +
            $"motors={bot.MotorDrive?.MotorCount ?? 0} powered={powered} spun={spun} " +
            $"idle_w={F3(idleSpeed)} spin_w={F3(spunSpeed)} coast_w={F3(coastSpeed)} coasted={coasted} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
