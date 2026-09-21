using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-04 verifier. RA2 v1 blueprint round-trip, validation, wiring→drive smoke.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBlueprintV1Verifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.2f;
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
        var original = RobotBlueprint.CreateRa2TankSteerSample(new Vector3(-4f, 0.75f, 0f), 90f);
        var validation = RobotBlueprintValidator.Validate(original);
        if (!validation.Ok)
        {
            Debug.Log(
                $"[S3-04] VERIFIER_DONE pass=False reason=validation errors={string.Join(";", validation.Errors)}");
            yield break;
        }

        var path = Path.Combine(Application.temporaryCachePath, "ra2_s3_04_tank_v1.json");
        RobotBlueprint round;
        try
        {
            RobotBlueprintSerializer.WriteFile(original, path, pretty: true, schema: RobotBlueprintSerializer.SchemaIdV1);
            round = RobotBlueprintSerializer.ReadFile(path);
        }
        catch (Exception ex)
        {
            Debug.Log($"[S3-04] VERIFIER_DONE pass=False reason=io error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var diffs = RobotBlueprintSerializer.DiffCritical(original, round);
        if (diffs.Count > 0)
        {
            Debug.Log($"[S3-04] VERIFIER_DONE pass=False reason=diff mismatches={diffs.Count} {string.Join(";", diffs)}");
            yield break;
        }

        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        // Use the shared spawn path so wiring drives per-wheel hinge motors (RobotMotorDrive),
        // not the root-body cheat.
        var spawned = RobotSpawnService.Spawn(round, 0, 0, null, slideMaterial, new Color(0.15f, 0.7f, 0.35f));
        var drive = spawned.Drive;
        var start = drive.transform.position;
        var end = Time.time + driveSeconds;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0.25f };

        while (Time.time < end)
        {
            drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(round, cmd));
            yield return new WaitForFixedUpdate();
        }

        drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.25f);

        var delta = Vector3.Distance(start, drive.transform.position);
        var nan = float.IsNaN(drive.transform.position.x);
        var controlBoard = spawned.Assembly != null &&
                            spawned.Assembly.Root != null &&
                            spawned.Assembly.Root.transform.Find("control_board") != null;
        var wiringOk = round.Wirings.Length >= 8 && round.ControlSlots.Length >= 2;
        RobotSpawnService.Despawn(spawned);
        var pass = !nan && delta > 0.05f && controlBoard && wiringOk && diffs.Count == 0;

        Debug.Log(
            $"[S3-04] VERIFIER_DONE pass={pass} nan={nan} moved={delta > 0.05f} control_board={controlBoard} " +
            $"wiring_ok={wiringOk} mismatches={diffs.Count} schema={RobotBlueprintSerializer.SchemaIdV1} " +
            $"slots={round.ControlSlots.Length} wires={round.Wirings.Length} delta={F3(delta)} path={path}");
    }

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
