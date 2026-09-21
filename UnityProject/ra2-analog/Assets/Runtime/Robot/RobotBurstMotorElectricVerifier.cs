using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-11: BurstMotor Fire draws electric budget; insufficient power denies fire.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBurstMotorElectricVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2BurstMotorElectricSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-11] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.9f, 0.55f, 0.2f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-11] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.BurstMotorCount < 1)
        {
            Debug.Log($"[S7-11] VERIFIER_DONE pass=False reason=no_burst count={act?.BurstMotorCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var startElec = act.ElectricRemaining;
        yield return new WaitForFixedUpdate();

        // Rising edge Fire #1 — should spend budget.
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var afterFirst = act.ElectricRemaining;
        var spent = afterFirst < startElec - 1f;
        var firedOnce = act.LastFireCount >= 1;

        // Hold then rising edge #2 — should deny (budget 100, cost 70 → only one shot).
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        var firesBefore = act.LastFireCount;
        var deniesBefore = act.LastElecDenied;
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var denied = act.LastElecDenied > deniesBefore;
        var noSecondFire = act.LastFireCount == firesBefore;
        var nan = false;
        if (bot.Assembly.Parts.TryGetValue("burst_motor", out var go) && go != null)
        {
            var rb = go.GetComponent<Rigidbody>();
            nan = rb != null && IsBad(rb.angularVelocity);
        }

        var pass = spent && firedOnce && denied && noSecondFire && !nan;
        Debug.Log(
            $"[S7-11] VERIFIER_DONE pass={pass} spent={spent} fired={firedOnce} denied={denied} " +
            $"no_retrigger={noSecondFire} elec_start={F3(startElec)} elec_after={F3(afterFirst)} " +
            $"fires={act.LastFireCount} elec_denies={act.LastElecDenied} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
