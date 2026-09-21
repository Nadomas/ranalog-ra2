using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-14: Ackermann front steers — wheel admit under SpinMotor on Steering; opposite Turn angles.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotAckermannSteerVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2AckermannSteerSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-14] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.3f, 0.65f, 0.55f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-14] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.SteeringCount < 2)
        {
            Debug.Log($"[S7-14] VERIFIER_DONE pass=False reason=steer_count count={act?.SteeringCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("steer_fl", out var flGo) ||
            !bot.Assembly.Parts.TryGetValue("steer_fr", out var frGo) ||
            !bot.Assembly.Parts.TryGetValue("wheel_fl", out var wFl))
        {
            Debug.Log("[S7-14] VERIFIER_DONE pass=False reason=missing_parts");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var hingeFl = flGo.GetComponent<HingeJoint>();
        var hingeFr = frGo.GetComponent<HingeJoint>();
        var wheelHinge = wFl.GetComponent<HingeJoint>();
        var wheelOnSteer = wheelHinge != null && hingeFl != null &&
                           wheelHinge.connectedBody == flGo.GetComponent<Rigidbody>();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var driveEnd = Time.time + 0.7f;
        while (Time.time < driveEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Turn = 1f });
            yield return new WaitForFixedUpdate();
        }

        var aFl = hingeFl != null ? hingeFl.angle : 0f;
        var aFr = hingeFr != null ? hingeFr.angle : 0f;
        if (float.IsNaN(aFl) || float.IsInfinity(aFl)) aFl = 0f;
        if (float.IsNaN(aFr) || float.IsInfinity(aFr)) aFr = 0f;
        var opposite = Mathf.Abs(aFl) > 6f && Mathf.Abs(aFr) > 6f && (aFl * aFr) < 0f;

        var start = bot.Drive.transform.position;
        var moveEnd = Time.time + 0.4f;
        while (Time.time < moveEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = 0.35f });
            yield return new WaitForFixedUpdate();
        }

        var moved = Vector3.Distance(start, bot.Drive.transform.position) > 0.04f;
        var nan = IsBad(bot.Drive.transform.position);
        var pass = opposite && wheelOnSteer && moved && !nan;

        Debug.Log(
            $"[S7-14] VERIFIER_DONE pass={pass} admit=True opposite={opposite} wheel_on_steer={wheelOnSteer} " +
            $"moved={moved} a_fl={F3(aFl)} a_fr={F3(aFr)} steers={act.SteeringCount} nan={nan}");

        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
