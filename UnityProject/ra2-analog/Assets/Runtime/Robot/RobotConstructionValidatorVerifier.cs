using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S4-01 verifier. Construction validation: ≥2 valid builds, reject illegals, mass/CoM, spawn+drive.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotConstructionValidatorVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.0f;
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

        var sampleA = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(-4f, 0.85f, 0f), 90f);
        var sampleB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(4f, 0.85f, 0f), -90f);
        var badWheel = RobotBlueprint.CreateInvalidWheelOnChassis(Vector3.zero, 0f);
        var badPts = RobotBlueprint.CreateInvalidChassisPoints(Vector3.zero, 0f);
        var badMass = RobotBlueprint.CreateInvalidOvermass(Vector3.zero, 0f);
        var badBoard = RobotBlueprint.CreateInvalidNoControlBoard(Vector3.zero, 0f);

        var va = RobotBlueprintValidator.Validate(sampleA);
        var vb = RobotBlueprintValidator.Validate(sampleB);
        var rWheel = RobotBlueprintValidator.Validate(badWheel);
        var rPts = RobotBlueprintValidator.Validate(badPts);
        var rMass = RobotBlueprintValidator.Validate(badMass);
        var rBoard = RobotBlueprintValidator.Validate(badBoard);

        var rejectsOk = !rWheel.Ok && HasErrorPrefix(rWheel, "wheel_not_on_axle") &&
                        !rPts.Ok && HasErrorPrefix(rPts, "chassis_points") &&
                        !rMass.Ok && HasErrorPrefix(rMass, "mass_over_class") &&
                        !rBoard.Ok && HasErrorPrefix(rBoard, "control_board");

        if (!va.Ok || !vb.Ok || !rejectsOk)
        {
            Debug.Log(
                $"[S4-01] VERIFIER_DONE pass=False reason=validation " +
                $"A_ok={va.Ok} A_err={Join(va.Errors)} B_ok={vb.Ok} B_err={Join(vb.Errors)} " +
                $"rej_wheel={Join(rWheel.Errors)} rej_pts={Join(rPts.Errors)} " +
                $"rej_mass={Join(rMass.Errors)} rej_board={Join(rBoard.Errors)}");
            yield break;
        }

        var comDiff = Mathf.Abs(va.Mass.CenterOfMassLocal.z - vb.Mass.CenterOfMassLocal.z);
        var massDiff = Mathf.Abs(va.Mass.TotalMass - vb.Mass.TotalMass);
        var comDistinct = comDiff > 0.15f && massDiff > 4f;

        // Admit path: TryValidate + Spawn (same as net host).
        string errA = null;
        string errB = null;
        if (!RobotSpawnService.TryValidate(sampleA, out errA) ||
            !RobotSpawnService.TryValidate(sampleB, out errB))
        {
            Debug.Log($"[S4-01] VERIFIER_DONE pass=False reason=admit A={errA} B={errB}");
            yield break;
        }

        if (RobotSpawnService.TryValidate(badWheel, out _))
        {
            Debug.Log("[S4-01] VERIFIER_DONE pass=False reason=admit_accepted_illegal_wheel");
            yield break;
        }

        var spawnedA = RobotSpawnService.Spawn(sampleA, 0, 0, null, slideMaterial, new Color(0.2f, 0.55f, 1f));
        var spawnedB = RobotSpawnService.Spawn(sampleB, 1, 1, null, slideMaterial, new Color(1f, 0.4f, 0.2f));

        var appliedComA = spawnedA.Assembly.RootBody.centerOfMass;
        var comApplied = Vector3.Distance(appliedComA, va.Mass.CenterOfMassLocal) < 0.05f;

        var startA = spawnedA.Drive.transform.position;
        var startB = spawnedB.Drive.transform.position;
        var end = Time.time + driveSeconds;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0.15f };
        while (Time.time < end)
        {
            spawnedA.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(sampleA, cmd));
            spawnedB.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(sampleB, cmd));
            yield return new WaitForFixedUpdate();
        }

        spawnedA.Drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        spawnedB.Drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.2f);

        var deltaA = Vector3.Distance(startA, spawnedA.Drive.transform.position);
        var deltaB = Vector3.Distance(startB, spawnedB.Drive.transform.position);
        var nan = IsBad(spawnedA.Drive.transform.position) || IsBad(spawnedB.Drive.transform.position);
        var moved = deltaA > 0.05f && deltaB > 0.05f;
        var axles = CountMotors(sampleA) >= 4 && CountMotors(sampleB) >= 4;

        RobotSpawnService.Despawn(spawnedA);
        RobotSpawnService.Despawn(spawnedB);

        var pass = !nan && moved && comDistinct && comApplied && axles && rejectsOk;
        Debug.Log(
            $"[S4-01] VERIFIER_DONE pass={pass} nan={nan} both_moved={moved} com_distinct={comDistinct} " +
            $"com_applied={comApplied} axles={axles} rejects={rejectsOk} " +
            $"A_mass={F2(va.Mass.TotalMass)} A_comZ={F3(va.Mass.CenterOfMassLocal.z)} " +
            $"B_mass={F2(vb.Mass.TotalMass)} B_comZ={F3(vb.Mass.CenterOfMassLocal.z)} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} rb={va.Mass.RigidbodyCount} hinges={va.Mass.HingeCount}");
    }

    static int CountMotors(RobotBlueprint bp)
    {
        var n = 0;
        for (var i = 0; i < bp.Components.Length; i++)
        {
            if (bp.Components[i].ResolvedBase() == RobotComponentBase.SpinMotor)
                n++;
        }

        return n;
    }

    static bool HasErrorPrefix(RobotBlueprintValidator.Result r, string prefix)
    {
        for (var i = 0; i < r.Errors.Count; i++)
        {
            if (r.Errors[i].StartsWith(prefix, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static string Join(System.Collections.Generic.List<string> errors) =>
        errors == null || errors.Count == 0 ? "-" : string.Join(";", errors);

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F2(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
    static string F3(float v) => v.ToString("F3", CultureInfo.InvariantCulture);
}
