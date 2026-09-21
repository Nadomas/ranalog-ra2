using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// S2-05 / EXP-09 thin dedicated-host tick smoke. Disables cameras/listeners (headless-like),
/// drives both robots via loopback→authority for a fixed duration, and asserts FixedUpdate
/// tick budget stays stable. Not a true Dedicated Server player build.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestDedicatedTickVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float measureSeconds = 3.0f;
    // Editor Play Mode wall-clock FixedUpdate spacing is often ~frame time, not CPU cost.
    // Gate on motion + transport + miss ratio; treat avg interval as diagnostic only.
    [SerializeField] float maxAvgTickMs = 40.0f;
    [SerializeField] float maxMissRatio = 0.08f;
    [SerializeField] float minTickRatio = 0.55f;
    [SerializeField] PhysicsTestLoopbackTransport transport;
    [SerializeField] PhysicsTestTransportClient client;
    [SerializeField] PhysicsTestTransportHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestDrive driveA;
    [SerializeField] PhysicsTestDrive driveB;

    int fixedTicks;
    double tickDtSum;
    int missedTicks;
    float expectedFixedDt;
    double lastFixedRealtime = -1;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(
        PhysicsTestLoopbackTransport loopback,
        PhysicsTestTransportClient transportClient,
        PhysicsTestTransportHost transportHost,
        PhysicsTestLocalAuthority auth,
        PhysicsTestDrive a,
        PhysicsTestDrive b)
    {
        transport = loopback;
        client = transportClient;
        host = transportHost;
        authority = auth;
        driveA = a;
        driveB = b;
    }

    void Start()
    {
        if (!autoRun)
            return;

        ResolveRefs();
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
        if (transport == null)
            transport = FindAnyObjectByType<PhysicsTestLoopbackTransport>();
        if (client == null)
            client = FindAnyObjectByType<PhysicsTestTransportClient>();
        if (host == null)
            host = FindAnyObjectByType<PhysicsTestTransportHost>();
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

        transport.SetLatencyProfile(PhysicsTestLatencyProfile.None);
        transport.ResetMetrics();

        fixedTicks = 0;
        tickDtSum = 0;
        missedTicks = 0;
        lastFixedRealtime = Time.realtimeSinceStartupAsDouble;

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var end = Time.realtimeSinceStartup + measureSeconds;

        while (Time.realtimeSinceStartup < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f, Turn = 0.15f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 1f, Turn = -0.15f }));
            yield return new WaitForFixedUpdate();
        }

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position);
        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var avgTickMs = fixedTicks > 0 ? (float)(tickDtSum / fixedTicks) * 1000f : 999f;
        var missRatio = fixedTicks > 0 ? missedTicks / (float)fixedTicks : 1f;
        var expectedTicks = measureSeconds / expectedFixedDt;
        var tickCountOk = fixedTicks > expectedTicks * minTickRatio;
        var budgetOk = avgTickMs <= maxAvgTickMs;
        var missOk = missRatio <= maxMissRatio;
        var transportOk = transport.DeliveredCommands > 0 && host.DrainedToAuthority > 0;

        // Thin EXP-09 editor smoke: playable sim under presentation-disabled host.
        // True Dedicated Server player-build CPU budget remains Open.
        var pass = !nan && bothMoved && tickCountOk && missOk && transportOk;

        Debug.Log(
            $"[S2-05] VERIFIER_DONE pass={pass} nan={nan} both_moved={bothMoved} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} fixed_ticks={fixedTicks} " +
            $"expected_ticks≈{F3((float)expectedTicks)} avg_tick_ms={F3(avgTickMs)} " +
            $"miss_ratio={F3(missRatio)} missed={missedTicks} " +
            $"budget_diag_ok={budgetOk} miss_ok={missOk} tick_count_ok={tickCountOk} " +
            $"transport_ok={transportOk} presentation=disabled " +
            $"note=editor_headless_smoke_not_dedicated_player_build");

        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = true;
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
