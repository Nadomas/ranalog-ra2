using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Local-only S1-02 Play Mode helper. Injects drive commands (same boundary as input)
/// and logs velocity samples to the Console — does not write Assets/ reports.
/// Enable <see cref="autoRun"/> for automated MCP verification.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsTestDrive))]
public sealed class PhysicsTestDriveVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float phaseSeconds = 1f;

    PhysicsTestDrive drive;
    PhysicsTestPlayerInput playerInput;
    Rigidbody body;
    Rigidbody otherBody;
    Vector3 otherStart;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    void Awake()
    {
        drive = GetComponent<PhysicsTestDrive>();
        playerInput = GetComponent<PhysicsTestPlayerInput>();
        body = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (!autoRun)
            return;

        var other = GameObject.Find("Robot_B");
        if (other != null)
        {
            otherBody = other.GetComponent<Rigidbody>();
            otherStart = otherBody.position;
        }

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (playerInput != null)
            playerInput.enabled = false;

        yield return RunPhase("forward", new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);
        ZeroMotion();
        yield return new WaitForFixedUpdate();

        yield return RunPhase("reverse", new PhysicsTestDriveCommand { Move = -1f }, phaseSeconds);
        ZeroMotion();
        yield return new WaitForFixedUpdate();

        yield return RunPhase("turn_left", new PhysicsTestDriveCommand { Turn = -1f }, phaseSeconds);
        ZeroMotion();
        yield return new WaitForFixedUpdate();

        yield return RunPhase("turn_right", new PhysicsTestDriveCommand { Turn = 1f }, phaseSeconds);
        ZeroMotion();
        yield return new WaitForFixedUpdate();

        yield return RunPhase("pre_brake_accel", new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);
        var speedBeforeBrake = body.linearVelocity.magnitude;
        yield return RunPhase("brake", new PhysicsTestDriveCommand { Brake = true }, phaseSeconds);
        var speedAfterBrake = body.linearVelocity.magnitude;

        drive.SetCommand(default);

        var a = body.position;
        var b = otherBody != null ? otherBody.position : Vector3.zero;
        var bDelta = otherBody != null ? Vector3.Distance(otherStart, b) : 0f;
        var nan =
            float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z) ||
            float.IsInfinity(a.x) || float.IsInfinity(a.y) || float.IsInfinity(a.z);

        Debug.Log(
            $"[S1-02] VERIFIER_DONE nan={nan} A_pos=({F3(a.x)},{F3(a.y)},{F3(a.z)}) " +
            $"A_y={F3(a.y)} B_pos=({F3(b.x)},{F3(b.y)},{F3(b.z)}) B_delta={F3(bDelta)} " +
            $"speed_before_brake={F3(speedBeforeBrake)} speed_after_brake={F3(speedAfterBrake)} " +
            $"brake_ok={speedAfterBrake < speedBeforeBrake * 0.7f} " +
            $"floor_ok={a.y > 0.2f && a.y < 2f} push_ok={bDelta > 0.05f}");

        if (playerInput != null)
            playerInput.enabled = true;
    }

    void ZeroMotion()
    {
        drive.SetCommand(default);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    IEnumerator RunPhase(string name, PhysicsTestDriveCommand cmd, float seconds)
    {
        drive.SetCommand(cmd);
        var end = Time.time + seconds;
        var peak = 0f;
        var peakYawRate = 0f;
        while (Time.time < end)
        {
            peak = Mathf.Max(peak, body.linearVelocity.magnitude);
            peakYawRate = Mathf.Max(peakYawRate, Mathf.Abs(body.angularVelocity.y));
            yield return new WaitForFixedUpdate();
        }

        var p = body.position;
        var v = body.linearVelocity;
        Debug.Log(
            $"[S1-02] PHASE {name} peakSpeed={F3(peak)} peakYawRate={F3(peakYawRate)} " +
            $"pos=({F3(p.x)},{F3(p.y)},{F3(p.z)}) vel=({F3(v.x)},{F3(v.y)},{F3(v.z)})");
    }

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
