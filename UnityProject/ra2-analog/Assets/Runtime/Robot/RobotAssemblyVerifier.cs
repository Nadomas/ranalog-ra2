using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-01 verifier. Assembled robots must accept PhysicsTestDrive commands and move without NaN.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotAssemblyVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.5f;
    [SerializeField] PhysicsTestDrive driveA;
    [SerializeField] PhysicsTestDrive driveB;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsTestDrive a, PhysicsTestDrive b)
    {
        driveA = a;
        driveB = b;
    }

    void Start()
    {
        if (!autoRun)
            return;
        ResolveRefs();
        StartCoroutine(RunSequence());
    }

    void ResolveRefs()
    {
        if (driveA == null)
        {
            var go = GameObject.Find("PhysicsTestSample_A");
            if (go != null) driveA = go.GetComponent<PhysicsTestDrive>();
        }

        if (driveB == null)
        {
            var go = GameObject.Find("PhysicsTestSample_B");
            if (go != null) driveB = go.GetComponent<PhysicsTestDrive>();
        }
    }

    IEnumerator RunSequence()
    {
        if (driveA == null || driveB == null)
        {
            Debug.Log("[S3-01] VERIFIER_DONE pass=False reason=missing_drives");
            yield break;
        }

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
        yield return new WaitForSeconds(0.3f);

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var rbA = driveA.GetComponent<Rigidbody>();
        var rbB = driveB.GetComponent<Rigidbody>();
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(rbA.linearVelocity) || IsBad(rbB.linearVelocity);
        var hingeOk = driveA.transform.Find("wheel_fl") != null &&
                      driveA.transform.Find("wheel_fl").GetComponent<HingeJoint>() != null;
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var pass = !nan && bothMoved && hingeOk;

        Debug.Log(
            $"[S3-01] VERIFIER_DONE pass={pass} nan={nan} both_moved={bothMoved} hinge_ok={hingeOk} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} assembled_from=blueprint");
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
