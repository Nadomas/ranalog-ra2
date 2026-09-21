using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// S2-08 / EXP-09 fuller dedicated/headless smoke. Runs presentation-disabled host tick,
/// records FixedUpdate cadence + Stopwatch CPU sample window, writes log for player builds.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestDedicatedBuildVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float measureSeconds = 4.0f;
    [SerializeField] float maxMissRatio = 0.12f;
    [SerializeField] float minTickRatio = 0.50f;
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestUdpHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestNetRoleBootstrap bootstrap;
    [SerializeField] PhysicsTestDrive driveA;
    [SerializeField] PhysicsTestDrive driveB;

    int fixedTicks;
    double tickDtSum;
    int missedTicks;
    float expectedFixedDt;
    double lastFixedRealtime = -1;
    readonly Stopwatch cpuWindow = new Stopwatch();

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(
        PhysicsTestUdpTransport udp,
        PhysicsTestUdpHost udpHost,
        PhysicsTestLocalAuthority auth,
        PhysicsTestNetRoleBootstrap roleBootstrap,
        PhysicsTestDrive a,
        PhysicsTestDrive b)
    {
        transport = udp;
        host = udpHost;
        authority = auth;
        bootstrap = roleBootstrap;
        driveA = a;
        driveB = b;
    }

    void Start()
    {
        if (!autoRun)
            return;

        ResolveRefs();
        if (bootstrap != null && bootstrap.ResolvedRole == PhysicsTestNetRole.Client)
            return;

        DisablePresentation();
        expectedFixedDt = Time.fixedDeltaTime;
        StartCoroutine(RunSequence());
    }

    void FixedUpdate()
    {
        if (!autoRun || lastFixedRealtime < 0)
            return;

        var now = Time.realtimeSinceStartupAsDouble;
        var dt = (float)(now - lastFixedRealtime);
        lastFixedRealtime = now;
        fixedTicks++;
        tickDtSum += dt;
        if (dt > expectedFixedDt * 2.5f)
            missedTicks++;
    }

    void ResolveRefs()
    {
        if (transport == null) transport = FindAnyObjectByType<PhysicsTestUdpTransport>();
        if (host == null) host = FindAnyObjectByType<PhysicsTestUdpHost>();
        if (authority == null) authority = FindAnyObjectByType<PhysicsTestLocalAuthority>();
        if (bootstrap == null) bootstrap = FindAnyObjectByType<PhysicsTestNetRoleBootstrap>();
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
    }

    static void DisablePresentation()
    {
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            cam.enabled = false;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            listener.enabled = false;
        foreach (var rend in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            rend.enabled = false;
    }

    IEnumerator RunSequence()
    {
        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = false;

        // Dedicated smoke can run without a remote client: inject commands via authority directly
        // while still exercising UDP host pose publish path if a peer appears.
        fixedTicks = 0;
        tickDtSum = 0;
        missedTicks = 0;
        lastFixedRealtime = Time.realtimeSinceStartupAsDouble;
        cpuWindow.Reset();
        cpuWindow.Start();

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var end = Time.realtimeSinceStartup + measureSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            authority.Submit(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f, Turn = 0.1f }));
            authority.Submit(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 1f, Turn = -0.1f }));
            yield return new WaitForFixedUpdate();
        }

        cpuWindow.Stop();
        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position);
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var avgTickMs = fixedTicks > 0 ? (float)(tickDtSum / fixedTicks) * 1000f : 999f;
        var missRatio = fixedTicks > 0 ? missedTicks / (float)fixedTicks : 1f;
        var expectedTicks = measureSeconds / expectedFixedDt;
        var tickCountOk = fixedTicks > expectedTicks * minTickRatio;
        var missOk = missRatio <= maxMissRatio;
        var cpuMsPerTick = fixedTicks > 0 ? (float)cpuWindow.Elapsed.TotalMilliseconds / fixedTicks : 999f;
        var isPlayer = !Application.isEditor;
        var isDedicatedRole = bootstrap != null &&
                              (bootstrap.ResolvedRole == PhysicsTestNetRole.Dedicated ||
                               bootstrap.ResolvedRole == PhysicsTestNetRole.Host);
        var pass = !nan && bothMoved && tickCountOk && missOk && isDedicatedRole;

        var line =
            $"[S2-08] VERIFIER_DONE pass={pass} nan={nan} both_moved={bothMoved} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} fixed_ticks={fixedTicks} " +
            $"expected_ticks≈{F3((float)expectedTicks)} avg_wall_tick_ms={F3(avgTickMs)} " +
            $"cpu_ms_per_tick≈{F3(cpuMsPerTick)} miss_ratio={F3(missRatio)} " +
            $"player_build={isPlayer} role={(bootstrap != null ? bootstrap.ResolvedRole.ToString() : "n/a")} " +
            $"headless_like=True";
        Debug.Log(line);
        AppendLog(line);

        if (isPlayer)
        {
            AppendLog("DEDICATED_QUIT");
            Application.Quit(pass ? 0 : 1);
        }
    }

    void AppendLog(string msg)
    {
        if (bootstrap != null)
        {
            bootstrap.TryAppendLog(msg);
            return;
        }

        try
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "_s2_net_dedicated.txt"));
            File.AppendAllText(path, msg + "\n");
        }
        catch
        {
            // ignore
        }
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
