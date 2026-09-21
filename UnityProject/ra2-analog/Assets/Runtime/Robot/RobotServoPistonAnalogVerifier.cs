using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-08: ServoPiston Analog Extend/Retract with air draw + mid-stroke lock (ConfigurableJoint drive).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotServoPistonAnalogVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2ServoPistonAnalogSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-08] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.35f, 0.65f, 0.55f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-08] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.ServoPistonCount < 1)
        {
            Debug.Log($"[S7-08] VERIFIER_DONE pass=False reason=no_servo_piston count={act?.ServoPistonCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("servo_piston", out var pistonGo) || pistonGo == null)
        {
            Debug.Log("[S7-08] VERIFIER_DONE pass=False reason=missing_piston");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var slide = pistonGo.GetComponent<ConfigurableJoint>();
        var rb = pistonGo.GetComponent<Rigidbody>();
        var airStart = act.AirRemaining;
        var rest = pistonGo.transform.localPosition;
        var axis = slide != null && slide.axis.sqrMagnitude > 1e-6f ? slide.axis.normalized : Vector3.forward;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Full extend (Analog +1 → Move)
        var extEnd = Time.time + 0.9f;
        while (Time.time < extEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f });
            yield return new WaitForFixedUpdate();
        }

        var airAfterExt = act.AirRemaining;
        var ext = Extension(pistonGo, rest, axis);
        var extended = ext > 0.12f;
        var airSpent = airAfterExt < airStart - 5f;

        // Mid-stroke lock (Analog 0)
        var lockEnd = Time.time + 0.5f;
        while (Time.time < lockEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 0f });
            yield return new WaitForFixedUpdate();
        }

        var mid = Extension(pistonGo, rest, axis);
        var midHeld = mid > 0.08f;

        // Retract (Analog -1)
        var retEnd = Time.time + 0.9f;
        while (Time.time < retEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = -1f });
            yield return new WaitForFixedUpdate();
        }

        var ret = Extension(pistonGo, rest, axis);
        var retracted = ret < mid - 0.05f || ret < 0.12f;
        var nan = rb != null && IsBad(rb.linearVelocity);

        var pass = slide != null && extended && airSpent && midHeld && retracted && !nan;
        Debug.Log(
            $"[S7-08] VERIFIER_DONE pass={pass} extended={extended} mid_held={midHeld} retracted={retracted} " +
            $"air_start={F3(airStart)} air_after={F3(airAfterExt)} air_now={F3(act.AirRemaining)} air_spent={airSpent} " +
            $"ext={F3(ext)} mid={F3(mid)} ret={F3(ret)} denies={act.LastAirDenied} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static float Extension(GameObject go, Vector3 rest, Vector3 axis) =>
        Vector3.Dot(go.transform.localPosition - rest, axis);

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
