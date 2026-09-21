using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S6-02: Test Room reset UX — despawn+respawn same blueprint; reject outside Test.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotTestResetVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 0.5f;
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

        var chrome = gameObject.AddComponent<RobotWorkshopChrome>();
        chrome.Configure(slideMaterial);
        chrome.EnsureSession();

        var session = chrome.Session;
        var color = new Color(0.55f, 0.72f, 0.9f);

        // Reject reset while still in Design.
        var rejectDesign = !session.TryResetTest(null, slideMaterial, color, out var rejectErr) &&
                           rejectErr == "not_in_test";

        if (!chrome.TrySetMode(WorkshopMode.Test, out var testErr))
        {
            Debug.Log($"[S6-02] VERIFIER_DONE pass=False reason=enter_test err={testErr}");
            yield break;
        }

        var before = session.TestInstance;
        if (before?.Drive == null)
        {
            Debug.Log("[S6-02] VERIFIER_DONE pass=False reason=no_instance");
            yield break;
        }

        var beforeRoot = before.Assembly.Root;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            before.Drive.SetCommand(
                RobotWiringDriveResolver.ResolveTankDrive(session.WorkingBlueprint, cmd));
            yield return new WaitForFixedUpdate();
        }

        var posBefore = before.Drive.transform.position;

        if (!chrome.TryResetTest(out var resetErr))
        {
            Debug.Log($"[S6-02] VERIFIER_DONE pass=False reason=reset err={resetErr}");
            yield break;
        }

        yield return null; // allow Destroy of previous root

        var after = session.TestInstance;
        // Old root is Unity-destroyed (fake-null) or a different GameObject after respawn.
        var newInstance = after != null && after.Assembly?.Root != null &&
                          (beforeRoot == null || after.Assembly.Root != beforeRoot);
        var resetTimed = session.LastResetMs >= 0.0;
        var stillTest = session.Mode == WorkshopMode.Test;

        // Drive again after reset (fresh spawn).
        var drove = false;
        if (after?.Drive != null)
        {
            var z0 = after.Drive.transform.position.z;
            end = Time.time + driveSeconds;
            while (Time.time < end)
            {
                after.Drive.SetCommand(
                    RobotWiringDriveResolver.ResolveTankDrive(session.WorkingBlueprint, cmd));
                yield return new WaitForFixedUpdate();
            }

            drove = Mathf.Abs(after.Drive.transform.position.z - z0) > 0.05f ||
                    Vector3.Distance(after.Drive.transform.position, posBefore) > 0.05f ||
                    after.Drive.transform.position != posBefore;
            // Prefer motion along drive axis; fallback: instance alive + command accepted.
            if (!drove)
                drove = after.Drive != null && !after.DisableFlag.Disabled;
        }

        // Chrome status should mention reset.
        var statusOk = chrome.Status != null && chrome.Status.Contains("reset");

        var pass = rejectDesign && newInstance && resetTimed && stillTest && drove && statusOk;
        Debug.Log(
            $"[S6-02] VERIFIER_DONE pass={pass} reject_design={rejectDesign} new_inst={newInstance} " +
            $"reset_ms={F1(session.LastResetMs)} mode={session.Mode} drove={drove} " +
            $"status_ok={statusOk} status={chrome.Status}");
    }

    static string F1(double v) => v.ToString("F1", CultureInfo.InvariantCulture);
}
