using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Local-only S2-01 verifier. Drives both robots via authority Submit (not direct SetCommand),
/// then attempts an unauthorized submit and a direct cheat SetCommand to confirm authority wins.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestDualAuthorityVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float phaseSeconds = 1.2f;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestDrive driveA;
    [SerializeField] PhysicsTestDrive driveB;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsTestLocalAuthority auth, PhysicsTestDrive a, PhysicsTestDrive b)
    {
        authority = auth;
        driveA = a;
        driveB = b;
    }

    void Start()
    {
        if (!autoRun)
            return;

        if (authority == null)
            authority = FindAnyObjectByType<PhysicsTestLocalAuthority>();
        if (driveA == null)
        {
            var go = GameObject.Find("Robot_A");
            if (go != null) driveA = go.GetComponent<PhysicsTestDrive>();
        }

        if (driveB == null)
        {
            var go = GameObject.Find("Robot_B");
            if (go != null) driveB = go.GetComponent<PhysicsTestDrive>();
        }

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // Disable live keyboard sources during auto run.
        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = false;

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var rejectedBefore = authority.RejectedUnauthorized;

        yield return RunPhase("A_forward", 0, 0, new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);
        yield return RunPhase("B_forward", 1, 1, new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);

        // Parallel intents: submit both each frame for a short window.
        var end = Time.time + phaseSeconds;
        while (Time.time < end)
        {
            authority.Submit(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 0.6f, Turn = 0.4f }));
            authority.Submit(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 0.6f, Turn = -0.4f }));
            yield return new WaitForFixedUpdate();
        }

        // Unauthorized source for robot 0 (source 99) must be rejected.
        var rejected = authority.Submit(new PhysicsTestCommandEnvelope(0, 99, new PhysicsTestDriveCommand { Move = 1f }));
        var rejectOk = !rejected && authority.RejectedUnauthorized > rejectedBefore;

        // Cheat: write drive directly, then wait FixedUpdates — authority must overwrite idle.
        driveA.SetCommand(new PhysicsTestDriveCommand { Move = 1f });
        authority.Submit(new PhysicsTestCommandEnvelope(0, 0, default));
        authority.Submit(new PhysicsTestCommandEnvelope(1, 1, default));
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var cheatOverwritten = Mathf.Abs(driveA.CurrentCommand.Move) < 0.01f;

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(driveA.GetComponent<Rigidbody>().linearVelocity) ||
                  IsBad(driveB.GetComponent<Rigidbody>().linearVelocity);

        Debug.Log(
            $"[S2-01] VERIFIER_DONE nan={nan} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} " +
            $"both_moved={deltaA > 0.05f && deltaB > 0.05f} " +
            $"reject_ok={rejectOk} cheat_overwritten={cheatOverwritten} " +
            $"rejected_total={authority.RejectedUnauthorized} applied={authority.AppliedCommands}");

        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = true;
    }

    IEnumerator RunPhase(string name, int robotId, int sourceId, PhysicsTestDriveCommand cmd, float seconds)
    {
        if (seconds <= 0f)
            yield break;

        var end = Time.time + seconds;
        while (Time.time < end)
        {
            authority.Submit(new PhysicsTestCommandEnvelope(robotId, sourceId, cmd));
            // Hold other robot idle.
            var other = robotId == 0 ? 1 : 0;
            authority.Submit(new PhysicsTestCommandEnvelope(other, other, default));
            yield return new WaitForFixedUpdate();
        }

        Debug.Log($"[S2-01] PHASE {name} done");
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
