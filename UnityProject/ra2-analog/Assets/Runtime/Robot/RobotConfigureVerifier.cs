using System.Collections;
using System.Globalization;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S5-01 verifier. Rebind wiring/presets on same hardware → different motion; map persists in JSON.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotConfigureVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.0f;
    [SerializeField] PhysicsMaterial slideMaterial;

    float lastDelta;
    bool lastNan;
    Vector3 lastTravel;

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

        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f);
        RobotControlConfigurer.SetSlotBinding(bp, "forward_back", "W/S");
        RobotControlConfigurer.SetSlotBinding(bp, "left_right", "A/D");
        RobotControlConfigurer.ApplyDrivePreset(bp, RobotControlConfigurer.DrivePreset.TankSteer);

        var v0 = RobotBlueprintValidator.Validate(bp);
        if (!v0.Ok)
        {
            Debug.Log($"[S5-01] VERIFIER_DONE pass=False reason=base_invalid {string.Join(";", v0.Errors)}");
            yield break;
        }

        yield return MeasureDrive(bp, forward: 1f, turn: 0f);
        var deltaTank = lastDelta;
        var travelTank = lastTravel;
        if (lastNan)
        {
            Debug.Log("[S5-01] VERIFIER_DONE pass=False reason=nan_tank");
            yield break;
        }

        RobotControlConfigurer.ApplyDrivePreset(bp, RobotControlConfigurer.DrivePreset.ReversedDrive);
        RobotControlConfigurer.SetSlotBinding(bp, "forward_back", "S/W");
        var vRev = RobotBlueprintValidator.Validate(bp);
        if (!vRev.Ok)
        {
            Debug.Log($"[S5-01] VERIFIER_DONE pass=False reason=reverse_invalid {string.Join(";", vRev.Errors)}");
            yield break;
        }

        yield return MeasureDrive(bp, forward: 1f, turn: 0f);
        var deltaRev = lastDelta;
        var travelRev = lastTravel;
        if (lastNan)
        {
            Debug.Log("[S5-01] VERIFIER_DONE pass=False reason=nan_reverse");
            yield break;
        }

        var path = Path.Combine(Application.temporaryCachePath, "ra2_s5_01_configure.json");
        RobotBlueprintSerializer.WriteFile(bp, path, pretty: true, schema: RobotBlueprintSerializer.SchemaIdV1);
        var loaded = RobotBlueprintSerializer.ReadFile(path);
        var bindingOk = RobotControlConfigurer.GetSlotBinding(loaded, "forward_back") == "S/W";
        var wiresOk = loaded.Wirings != null && loaded.Wirings.Length >= 4;
        var conflicts = RobotControlConfigurer.FindWiringConflicts(loaded);

        RobotControlConfigurer.ApplyDrivePreset(loaded, RobotControlConfigurer.DrivePreset.TurnOnly);
        yield return MeasureDrive(loaded, forward: 1f, turn: 0f);
        var deltaTurnOnly = lastDelta;
        if (lastNan)
        {
            Debug.Log("[S5-01] VERIFIER_DONE pass=False reason=nan_turn_only");
            yield break;
        }

        // Same ForwardBack=+1: reverse wiring should move opposite along robot forward (±Z world when yaw=0).
        var tankForward = travelTank.z;
        var revForward = travelRev.z;
        var reverseOpposite = tankForward * revForward < 0f ||
                              (Mathf.Abs(tankForward) > 0.15f && Mathf.Abs(revForward) < Mathf.Abs(tankForward) * 0.35f);
        var turnOnlyWeak = deltaTurnOnly < deltaTank * 0.35f;
        var pass = deltaTank > 0.2f && reverseOpposite && turnOnlyWeak && bindingOk && wiresOk && conflicts.Count == 0;

        Debug.Log(
            $"[S5-01] VERIFIER_DONE pass={pass} tank_delta={F3(deltaTank)} tank_z={F3(tankForward)} " +
            $"rev_delta={F3(deltaRev)} rev_z={F3(revForward)} turn_only_delta={F3(deltaTurnOnly)} " +
            $"reverse_opp={reverseOpposite} turn_only_weak={turnOnlyWeak} " +
            $"binding_ok={bindingOk} wires_ok={wiresOk} conflicts={conflicts.Count} path={path}");
    }

    IEnumerator MeasureDrive(RobotBlueprint blueprint, float forward, float turn)
    {
        // Fresh spawn each measurement so CoM/pose reset without transform cheats on live bodies.
        var spawned = RobotSpawnService.Spawn(
            blueprint, 0, 0, null, slideMaterial, new Color(0.25f, 0.65f, 0.4f));
        var drive = spawned.Drive;
        // Re-bind motors to current wiring (spawn already bound once).
        if (spawned.MotorDrive != null)
            spawned.MotorDrive.Bind(blueprint, spawned.Assembly.Parts, drive);

        var start = drive.transform.position;
        var end = Time.time + driveSeconds;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = forward, LeftRight = turn };
        while (Time.time < end)
        {
            drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(blueprint, cmd));
            yield return new WaitForFixedUpdate();
        }

        drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.15f);

        lastTravel = drive.transform.position - start;
        lastDelta = lastTravel.magnitude;
        lastNan = IsBad(drive.transform.position) || IsBad(drive.GetComponent<Rigidbody>().linearVelocity);
        RobotSpawnService.Despawn(spawned);
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float v) => v.ToString("F3", CultureInfo.InvariantCulture);
}
