using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-10: Steering hub Analog (Turn) ±35° + lock at center — hinge motors, no Transform writes.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotSteeringHubVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2SteeringHubSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-10] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.35f, 0.55f, 0.85f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-10] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.SteeringCount < 1)
        {
            Debug.Log($"[S7-10] VERIFIER_DONE pass=False reason=no_steer count={act?.SteeringCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("steer_hub", out var hubGo) || hubGo == null)
        {
            Debug.Log("[S7-10] VERIFIER_DONE pass=False reason=missing_hub");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var hinge = hubGo.GetComponent<HingeJoint>();
        var rb = hubGo.GetComponent<Rigidbody>();
        var limitsOk = hinge != null && hinge.useLimits &&
                       hinge.limits.max <= 35.5f && hinge.limits.min >= -35.5f;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var driveEnd = Time.time + 0.7f;
        while (Time.time < driveEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Turn = 1f });
            yield return new WaitForFixedUpdate();
        }

        var angle = hinge != null ? hinge.angle : 0f;
        if (float.IsNaN(angle) || float.IsInfinity(angle))
            angle = 0f;
        var sped = rb != null ? rb.angularVelocity.magnitude : 0f;
        var steered = Mathf.Abs(angle) > 8f || sped > 0.5f;

        var lockEnd = Time.time + 0.5f;
        while (Time.time < lockEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Turn = 0f });
            yield return new WaitForFixedUpdate();
        }

        var lockW = rb != null ? rb.angularVelocity.magnitude : 99f;
        var locked = lockW < 1.5f && act.LastSteerLocked >= 1;
        var held = Mathf.Abs(hinge != null ? hinge.angle : angle) > 4f;
        var nan = rb != null && IsBad(rb.angularVelocity);

        var pass = limitsOk && steered && locked && held && !nan;
        Debug.Log(
            $"[S7-10] VERIFIER_DONE pass={pass} limits_ok={limitsOk} steered={steered} locked={locked} held={held} " +
            $"angle={F3(angle)} lock_w={F3(lockW)} steer_locked={act.LastSteerLocked} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
