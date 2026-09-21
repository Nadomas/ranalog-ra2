using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-05: BurstPiston Fire consumes AirTank budget and applies impulse (ConfigurableJoint slider).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBurstPistonFireVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2BurstPistonFireSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-05] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.45f, 0.55f, 0.75f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-05] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.PistonCount < 1)
        {
            Debug.Log($"[S7-05] VERIFIER_DONE pass=False reason=no_actuator pistons={act?.PistonCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("burst_piston", out var pistonGo) || pistonGo == null)
        {
            Debug.Log("[S7-05] VERIFIER_DONE pass=False reason=missing_piston");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var pistonRb = pistonGo.GetComponent<Rigidbody>();
        var slide = pistonGo.GetComponent<ConfigurableJoint>();
        var airStart = act.AirRemaining;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var rest = pistonGo.transform.localPosition;

        // Rising edge Fire
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var airAfter = act.AirRemaining;
        var delta = pistonGo.transform.localPosition - rest;
        var axis = slide != null && slide.axis.sqrMagnitude > 1e-6f ? slide.axis.normalized : Vector3.forward;
        var extension = Vector3.Dot(delta, axis);
        var speedAlong = pistonRb != null ? Mathf.Abs(Vector3.Dot(pistonRb.linearVelocity, pistonGo.transform.TransformDirection(axis))) : 0f;
        var fired = act.LastFireCount >= 1;
        var airSpent = airAfter < airStart - 1f;
        var moved = extension > 0.05f || speedAlong > 0.5f;
        var nan = pistonRb != null && IsBad(pistonRb.linearVelocity);

        // Drain air then Fire must deny
        while (act.AirRemaining >= 80f)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
            yield return new WaitForFixedUpdate();
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
            yield return new WaitForFixedUpdate();
            if (act.LastAirDenied > 0)
                break;
            // safety
            if (Time.frameCount % 200 == 0 && act.AirRemaining > 700f)
                break;
        }

        // Force empty and deny
        var deniedBefore = act.LastAirDenied;
        // Consume remaining via repeated fires
        var guard = 0;
        while (act.AirRemaining >= 80f && guard++ < 20)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
            yield return new WaitForFixedUpdate();
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
            yield return new WaitForFixedUpdate();
        }

        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
        yield return new WaitForFixedUpdate();

        var denied = act.LastAirDenied > deniedBefore || act.AirRemaining < 80f;
        // If still had air somehow, last fire after drain should increment deny when empty
        if (act.AirRemaining < 80f)
        {
            var d0 = act.LastAirDenied;
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
            yield return new WaitForFixedUpdate();
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
            yield return new WaitForFixedUpdate();
            denied = act.LastAirDenied > d0;
        }

        var pass = fired && airSpent && moved && denied && !nan && slide != null;
        Debug.Log(
            $"[S7-05] VERIFIER_DONE pass={pass} fired={fired} air_start={F3(airStart)} air_after={F3(airAfter)} " +
            $"air_now={F3(act.AirRemaining)} air_spent={airSpent} moved={moved} ext={F3(extension)} " +
            $"speed={F3(speedAlong)} denied={denied} denies={act.LastAirDenied} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
