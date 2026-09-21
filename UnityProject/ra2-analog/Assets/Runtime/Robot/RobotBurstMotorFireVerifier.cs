using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-06: BurstMotor Fire arc on Button rising edge (limited hinge &lt;180°, motor impulse).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBurstMotorFireVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2BurstMotorFireSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-06] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.85f, 0.2f, 0.35f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-06] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.BurstMotorCount < 1)
        {
            Debug.Log($"[S7-06] VERIFIER_DONE pass=False reason=no_burst_motor count={act?.BurstMotorCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("burst_motor", out var motorGo) || motorGo == null)
        {
            Debug.Log("[S7-06] VERIFIER_DONE pass=False reason=missing_burst_motor");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var hinge = motorGo.GetComponent<HingeJoint>();
        var motorRb = motorGo.GetComponent<Rigidbody>();
        var limitsOk = hinge != null && hinge.useLimits && hinge.limits.max <= 180f && hinge.limits.max > 10f;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var startAngle = hinge != null ? hinge.angle : 0f;
        if (float.IsNaN(startAngle) || float.IsInfinity(startAngle))
            startAngle = 0f;
        var startW = motorRb != null ? motorRb.angularVelocity.magnitude : 0f;

        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });

        var end = Time.time + 0.35f;
        while (Time.time < end)
            yield return new WaitForFixedUpdate();

        var angle = hinge != null ? hinge.angle : 0f;
        if (float.IsNaN(angle) || float.IsInfinity(angle))
            angle = 0f;
        var angleDelta = Mathf.Abs(Mathf.DeltaAngle(startAngle, angle));
        var peakW = motorRb != null ? motorRb.angularVelocity.magnitude : 0f;
        var fired = act.LastFireCount >= 1;
        var arced = angleDelta > 8f || peakW > startW + 1.5f;
        var withinLimits = hinge == null || angle <= hinge.limits.max + 5f;
        var nan = motorRb != null && IsBad(motorRb.angularVelocity);

        // Held Fire should not keep re-triggering without rising edge
        var firesAfterHold = act.LastFireCount;
        var holdEnd = Time.time + 0.25f;
        while (Time.time < holdEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
            yield return new WaitForFixedUpdate();
        }

        var noRetrigger = act.LastFireCount == firesAfterHold;

        var pass = limitsOk && fired && arced && withinLimits && noRetrigger && !nan;
        Debug.Log(
            $"[S7-06] VERIFIER_DONE pass={pass} limits_ok={limitsOk} fired={fired} arced={arced} " +
            $"angle0={F3(startAngle)} angle={F3(angle)} dAngle={F3(angleDelta)} " +
            $"w0={F3(startW)} w={F3(peakW)} within_limits={withinLimits} no_retrigger={noRetrigger} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
