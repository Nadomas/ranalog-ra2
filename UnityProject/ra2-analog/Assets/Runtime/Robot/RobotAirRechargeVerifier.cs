using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-12: AirTank refill via Power/AirTank AirMaxInOutRate after BurstPiston Fire spend.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotAirRechargeVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2AirRechargeSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-12] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.4f, 0.7f, 0.75f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-12] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.PistonCount < 1)
        {
            Debug.Log($"[S7-12] VERIFIER_DONE pass=False reason=no_piston count={act?.PistonCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var start = act.AirRemaining;
        yield return new WaitForFixedUpdate();

        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var afterFire = act.AirRemaining;
        var spent = afterFire < start - 1f && act.LastFireCount >= 1;

        // Hold fire off and wait for bus refill (~250/s → ~80 units in ~0.35s).
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        var waitEnd = Time.time + 0.5f;
        while (Time.time < waitEnd)
            yield return new WaitForFixedUpdate();

        var afterRecharge = act.AirRemaining;
        var recharged = afterRecharge > afterFire + 40f && afterRecharge >= 80f;

        var firesBefore = act.LastFireCount;
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 1f });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var secondFire = act.LastFireCount > firesBefore;
        var pass = spent && recharged && secondFire;

        Debug.Log(
            $"[S7-12] VERIFIER_DONE pass={pass} spent={spent} recharged={recharged} second_fire={secondFire} " +
            $"air_start={F3(start)} air_after_fire={F3(afterFire)} air_after_recharge={F3(afterRecharge)} " +
            $"fires={act.LastFireCount}");

        RobotSpawnService.Despawn(bot);
    }

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
