using System.Collections;
using System.Globalization;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-02 verifier. Round-trips sample blueprints via JSON file, diffs critical fields,
/// assembles from deserialized data, and runs a short PhysicsTestDrive smoke.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBlueprintSerializeVerifier : MonoBehaviour
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
        var originalA = RobotBlueprint.CreatePhysicsTestSampleA(new Vector3(-4f, 0.75f, 0f), 90f);
        var originalB = RobotBlueprint.CreatePhysicsTestSampleB(new Vector3(4f, 0.75f, 0f), -90f);

        var pathA = Path.Combine(Application.temporaryCachePath, "ra2_s3_02_sample_a.json");
        var pathB = Path.Combine(Application.temporaryCachePath, "ra2_s3_02_sample_b.json");

        double serMsA, deserMsA, serMsB, deserMsB;
        var bytesA = RobotBlueprintSerializer.MeasureRoundTripBytes(originalA, out serMsA, out deserMsA);
        var bytesB = RobotBlueprintSerializer.MeasureRoundTripBytes(originalB, out serMsB, out deserMsB);

        RobotBlueprint roundA;
        RobotBlueprint roundB;
        try
        {
            RobotBlueprintSerializer.WriteFile(originalA, pathA, pretty: true);
            RobotBlueprintSerializer.WriteFile(originalB, pathB, pretty: true);
            roundA = RobotBlueprintSerializer.ReadFile(pathA);
            roundB = RobotBlueprintSerializer.ReadFile(pathB);
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S3-02] VERIFIER_DONE pass=False reason=io_or_parse error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var diffsA = RobotBlueprintSerializer.DiffCritical(originalA, roundA);
        var diffsB = RobotBlueprintSerializer.DiffCritical(originalB, roundB);
        var mismatchCount = diffsA.Count + diffsB.Count;
        if (mismatchCount > 0)
        {
            Debug.Log(
                $"[S3-02] VERIFIER_DONE pass=False reason=field_mismatch mismatches={mismatchCount} " +
                $"A={string.Join(";", diffsA)} B={string.Join(";", diffsB)}");
            yield break;
        }

        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var assembledA = RobotAssembler.Assemble(roundA, null, slideMaterial, new Color(0.2f, 0.55f, 1f));
        var assembledB = RobotAssembler.Assemble(roundB, null, slideMaterial, new Color(1f, 0.35f, 0.2f));
        var driveA = assembledA.Root.AddComponent<PhysicsTestDrive>();
        var driveB = assembledB.Root.AddComponent<PhysicsTestDrive>();

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            driveA.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = 0.2f });
            driveB.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = -0.2f });
            yield return new WaitForFixedUpdate();
        }

        driveA.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        driveB.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.25f);

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var rbA = assembledA.RootBody;
        var rbB = assembledB.RootBody;
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(rbA.linearVelocity) || IsBad(rbB.linearVelocity);
        var hingeOk = driveA.transform.Find("wheel_fl") != null &&
                      driveA.transform.Find("wheel_fl").GetComponent<HingeJoint>() != null;
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var pass = !nan && bothMoved && hingeOk && mismatchCount == 0;

        Debug.Log(
            $"[S3-02] VERIFIER_DONE pass={pass} nan={nan} both_moved={bothMoved} hinge_ok={hingeOk} " +
            $"mismatches={mismatchCount} bytes_a={bytesA} bytes_b={bytesB} " +
            $"ser_ms_a={F3((float)serMsA)} deser_ms_a={F3((float)deserMsA)} " +
            $"ser_ms_b={F3((float)serMsB)} deser_ms_b={F3((float)deserMsB)} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} " +
            $"path_a={pathA} schema=ra2.robot_blueprint.v0");
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
