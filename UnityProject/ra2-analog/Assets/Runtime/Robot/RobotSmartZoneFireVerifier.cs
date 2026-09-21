using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-09: SmartZone foreign contact rising edge optionally triggers BurstMotor Fire (no player Fire).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotSmartZoneFireVerifier : MonoBehaviour
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

        var bp = RobotBlueprint.CreateRa2SmartZoneFireSample(new Vector3(0f, 0.85f, 0f), 0f);
        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-09] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        RobotSpawnedInstance bot;
        try
        {
            bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.7f, 0.45f, 0.25f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S7-09] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var act = bot.ActuatorDrive;
        if (act == null || act.SmartZoneCount < 1 || act.BurstMotorCount < 1)
        {
            Debug.Log(
                $"[S7-09] VERIFIER_DONE pass=False reason=missing_parts zones={act?.SmartZoneCount ?? 0} " +
                $"burst={act?.BurstMotorCount ?? 0}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        if (!bot.Assembly.Parts.TryGetValue("smart_zone", out var zoneGo) || zoneGo == null ||
            !bot.Assembly.Parts.TryGetValue("burst_motor", out var motorGo) || motorGo == null)
        {
            Debug.Log("[S7-09] VERIFIER_DONE pass=False reason=missing_zone_or_motor");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var sensor = zoneGo.GetComponent<RobotSmartZoneSensor>();
        var hinge = motorGo.GetComponent<HingeJoint>();
        var motorRb = motorGo.GetComponent<Rigidbody>();

        // Keep Fire off — only zone should trigger.
        bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var firesBefore = act.LastFireCount;
        var zoneFiresBefore = act.LastZoneFireCount;

        // Foreign probe starts overlapping SmartZone (dynamic body — gravity/forces, no Transform drive loop).
        var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        probe.name = "SmartZoneProbe";
        probe.transform.position = zoneGo.transform.position + new Vector3(0f, 0.15f, 0f);
        probe.transform.localScale = Vector3.one * 0.55f;
        var probeRb = probe.AddComponent<Rigidbody>();
        probeRb.mass = 3f;
        probeRb.linearVelocity = Vector3.zero;
        probeRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        var probeCol = probe.GetComponent<Collider>();
        if (probeCol != null)
            probeCol.isTrigger = false;
        var tag = probe.AddComponent<RobotInstanceTag>();
        tag.BindProbe(99);
        Physics.SyncTransforms();

        var waitEnd = Time.time + 1.6f;
        while (Time.time < waitEnd)
        {
            bot.Drive.SetCommand(new PhysicsTestDriveCommand { Fire = 0f });
            if (probeRb != null && zoneGo != null)
            {
                var toZone = zoneGo.transform.position - probe.transform.position;
                if (toZone.sqrMagnitude > 0.01f)
                    probeRb.AddForce(toZone.normalized * 18f, ForceMode.Acceleration);
            }
            yield return new WaitForFixedUpdate();
            if (act.LastZoneFireCount > zoneFiresBefore)
                break;
        }

        var zoneContacted = sensor != null && (sensor.HasForeignContact || sensor.ContactEnterCount > 0);
        var zoneFired = act.LastZoneFireCount > zoneFiresBefore;
        var fired = act.LastFireCount > firesBefore;
        var arced = false;
        if (motorRb != null)
            arced = motorRb.angularVelocity.magnitude > 1.5f;
        if (!arced && hinge != null)
        {
            var a = hinge.angle;
            if (!float.IsNaN(a) && !float.IsInfinity(a))
                arced = Mathf.Abs(a) > 8f;
        }

        var nan = motorRb != null && IsBad(motorRb.angularVelocity);
        var pass = zoneContacted && zoneFired && fired && arced && !nan;

        Debug.Log(
            $"[S7-09] VERIFIER_DONE pass={pass} zone_contact={zoneContacted} zone_fired={zoneFired} fired={fired} " +
            $"arced={arced} enters={sensor?.ContactEnterCount ?? 0} fires={act.LastFireCount} " +
            $"zone_fires={act.LastZoneFireCount} w={F3(motorRb != null ? motorRb.angularVelocity.magnitude : 0f)} nan={nan}");

        if (probe != null)
            Object.Destroy(probe);
        RobotSpawnService.Despawn(bot);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
