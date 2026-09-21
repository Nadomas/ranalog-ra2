using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S4-02: chassis polygon edit smoke — nudge/add/reject &gt;16, validator + admit still OK.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotChassisPolygonVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
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

        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f);
        var chrome = gameObject.AddComponent<RobotChassisPolygonChrome>();
        chrome.Bind(bp);
        yield return null;

        var before = RobotChassisPolygonEditor.PointCount(bp);
        var beforePts = RobotChassisPolygonEditor.GetPoints(bp);
        var before0 = beforePts[0];

        var okNudge = chrome.TryNudgeSelected(new Vector2(0.12f, -0.05f), out var eNudge);
        var afterPts = RobotChassisPolygonEditor.GetPoints(bp);
        var nudged = okNudge && Vector2.Distance(before0, afterPts[0]) > 0.05f;

        // Fill toward 16, then reject 17th.
        var addOk = true;
        string addErr = null;
        while (RobotChassisPolygonEditor.PointCount(bp) < RobotChassisPolygonEditor.MaxPoints)
        {
            if (!RobotChassisPolygonEditor.TryAddPoint(bp, new Vector2(0.2f, 0.2f), out addErr))
            {
                addOk = false;
                break;
            }
        }

        var atMax = RobotChassisPolygonEditor.PointCount(bp) == RobotChassisPolygonEditor.MaxPoints;
        var reject17 = !RobotChassisPolygonEditor.TryAddPoint(bp, new Vector2(1f, 1f), out var e17) &&
                       e17 != null && e17.Contains(">16");

        // Reject <3 via service (before restore — no post-spawn mutation).
        var reject2 = !RobotChassisPolygonEditor.TrySetPoints(bp, new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f)
        }, out var e2) && e2 != null && e2.Contains("<3");

        // Restore a legal 4-pt rectangle then validate admit.
        var restoreOk = RobotChassisPolygonEditor.TrySetPoints(bp, new[]
        {
            new Vector2(-0.8f, -1.1f),
            new Vector2(0.8f, -1.1f),
            new Vector2(0.8f, 1.1f),
            new Vector2(-0.8f, 1.1f)
        }, out var eRestore);

        var validation = RobotBlueprintValidator.Validate(bp);
        string admitErr = null;
        var admitOk = restoreOk && validation.Ok && RobotSpawnService.TryValidate(bp, out admitErr);

        RobotSpawnedInstance spawned = null;
        var spawnOk = false;
        if (admitOk)
        {
            spawned = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.4f, 0.7f, 0.9f));
            spawnOk = spawned?.Assembly?.Root != null;
            yield return new WaitForFixedUpdate();
        }

        var pass = nudged && addOk && atMax && reject17 && restoreOk && admitOk && spawnOk && reject2 &&
                   before >= 3;

        Debug.Log(
            $"[S4-02] VERIFIER_DONE pass={pass} before={before} nudged={nudged}/{eNudge} " +
            $"at_max={atMax} reject17={reject17}/{e17} restore={restoreOk}/{eRestore} " +
            $"valid={validation.Ok} admit={admitOk}/{admitErr ?? "ok"} spawn={spawnOk} " +
            $"reject2={reject2}/{e2} status={chrome.Status}");

        if (spawned != null)
            RobotSpawnService.Despawn(spawned);

        yield return null;
    }
}
