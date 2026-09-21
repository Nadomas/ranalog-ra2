using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Local-only S1-03 Play Mode helper. Injects drive commands, checks hinge stability,
/// and logs body/joint counts + NaN/detach flags to the Console.
/// Enable <see cref="autoRun"/> for automated MCP verification.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsTestDrive))]
public sealed class PhysicsTestJointVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float phaseSeconds = 1.25f;

    PhysicsTestDrive drive;
    PhysicsTestPlayerInput playerInput;
    Rigidbody chassisBody;
    Rigidbody wheelBody;
    HingeJoint hinge;
    Rigidbody otherBody;
    Vector3 otherStart;
    Transform wheelTransform;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    void Awake()
    {
        drive = GetComponent<PhysicsTestDrive>();
        playerInput = GetComponent<PhysicsTestPlayerInput>();
        chassisBody = GetComponent<Rigidbody>();
        wheelTransform = transform.Find("Wheel_FL");
        if (wheelTransform != null)
        {
            wheelBody = wheelTransform.GetComponent<Rigidbody>();
            hinge = wheelTransform.GetComponent<HingeJoint>();
        }
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

        var rbOnA = GetComponentsInChildren<Rigidbody>(true).Length;
        var jointsOnA = GetComponentsInChildren<Joint>(true).Length;
        var hingeOk = hinge != null && hinge.connectedBody == chassisBody;
        var wheelRbOk = wheelBody != null && !wheelBody.isKinematic;

        Debug.Log(
            $"[S1-03] SETUP rb_on_A={rbOnA} joints_on_A={jointsOnA} " +
            $"hinge_ok={hingeOk} wheel_rb_ok={wheelRbOk} " +
            $"wheel_mass={(wheelBody != null ? F3(wheelBody.mass) : "n/a")}");

        yield return RunPhase("drive_forward", new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);
        yield return RunPhase("drive_reverse", new PhysicsTestDriveCommand { Move = -1f }, phaseSeconds * 0.8f);
        yield return RunPhase("turn_left", new PhysicsTestDriveCommand { Turn = -1f, Move = 0.4f }, phaseSeconds);
        yield return RunPhase("turn_right", new PhysicsTestDriveCommand { Turn = 1f, Move = 0.4f }, phaseSeconds);

        // Aim roughly toward +X wall / Robot_B (Robot_A starts facing +X at yaw 90).
        yield return RunPhase("wall_approach", new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds * 2f);
        yield return RunPhase("push_B", new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds * 1.5f);

        drive.SetCommand(default);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        LogDone(rbOnA, jointsOnA);

        if (playerInput != null)
            playerInput.enabled = true;
    }

    void LogDone(int rbOnA, int jointsOnA)
    {
        var a = chassisBody.position;
        var w = wheelBody != null ? wheelBody.position : Vector3.zero;
        var b = otherBody != null ? otherBody.position : Vector3.zero;
        var bDelta = otherBody != null ? Vector3.Distance(otherStart, b) : 0f;

        var nan =
            IsBad(a) || IsBad(chassisBody.linearVelocity) || IsBad(chassisBody.angularVelocity) ||
            (wheelBody != null && (IsBad(w) || IsBad(wheelBody.linearVelocity) || IsBad(wheelBody.angularVelocity)));

        var hingeAlive = hinge != null && hinge.connectedBody == chassisBody;
        var attachDist = wheelTransform != null
            ? Vector3.Distance(wheelTransform.position, transform.TransformPoint(new Vector3(-0.85f, -0.35f, 0.7f)))
            : 999f;
        // Allow some travel along hinge arc; hard detach would be large.
        var attached = hingeAlive && attachDist < 1.5f;

        var floorOk = a.y > 0.15f && a.y < 2.5f;
        var speed = chassisBody.linearVelocity.magnitude;
        var explosion = speed > 80f ||
                        (wheelBody != null && wheelBody.linearVelocity.magnitude > 120f);

        Debug.Log(
            $"[S1-03] VERIFIER_DONE nan={nan} explosion={explosion} attached={attached} " +
            $"hinge_alive={hingeAlive} attach_dist={F3(attachDist)} " +
            $"rb_on_A={rbOnA} joints_on_A={jointsOnA} " +
            $"A_pos=({F3(a.x)},{F3(a.y)},{F3(a.z)}) W_pos=({F3(w.x)},{F3(w.y)},{F3(w.z)}) " +
            $"B_delta={F3(bDelta)} floor_ok={floorOk} push_ok={bDelta > 0.02f} " +
            $"A_speed={F3(speed)} counts_ok={rbOnA == 2 && jointsOnA == 1}");
    }

    IEnumerator RunPhase(string name, PhysicsTestDriveCommand cmd, float seconds)
    {
        drive.SetCommand(cmd);
        var end = Time.time + seconds;
        var peak = 0f;
        var peakWheel = 0f;
        while (Time.time < end)
        {
            peak = Mathf.Max(peak, chassisBody.linearVelocity.magnitude);
            if (wheelBody != null)
                peakWheel = Mathf.Max(peakWheel, wheelBody.linearVelocity.magnitude);
            if (IsBad(chassisBody.position) || (wheelBody != null && IsBad(wheelBody.position)))
            {
                Debug.Log($"[S1-03] PHASE {name} EARLY_NAN");
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }

        var p = chassisBody.position;
        var hingeAlive = hinge != null && hinge.connectedBody == chassisBody;
        Debug.Log(
            $"[S1-03] PHASE {name} peakSpeed={F3(peak)} peakWheel={F3(peakWheel)} " +
            $"pos=({F3(p.x)},{F3(p.y)},{F3(p.z)}) hinge_alive={hingeAlive}");
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
