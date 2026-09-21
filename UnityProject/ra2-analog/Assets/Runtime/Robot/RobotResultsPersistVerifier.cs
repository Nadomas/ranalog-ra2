using System.Collections;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S10-02: persist MatchSummary to local JSON + show thin MatchResultsView.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotResultsPersistVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        var view = gameObject.AddComponent<MatchResultsView>();

        var outcome = new MatchOutcome(true, 1, 0, MatchWinReason.Immobilized);
        var summary = new MatchSummary(
            outcome,
            matchDurationSeconds: 12.5f,
            immobileSecondsLoser: 1.5f,
            immobileSecondsWinner: 0.1f,
            loserWasDisabled: true,
            sessionId: "s10-persist-thin");

        var presented = MatchResultsStub.Present(summary, "local", persist: true, view: view);
        yield return null;

        var path = MatchSummaryStore.DefaultPathFor(summary.SessionId);
        var loaded = MatchSummaryStore.TryLoad(path, out var dto, out var loadErr);
        var fileOk = loaded &&
                     dto.finished &&
                     dto.winnerRobotId == 1 &&
                     dto.loserRobotId == 0 &&
                     dto.reason == nameof(MatchWinReason.Immobilized) &&
                     dto.sessionId == summary.SessionId &&
                     dto.loserWasDisabled &&
                     File.Exists(path);

        var viewOk = view.Visible &&
                     !string.IsNullOrEmpty(view.Body) &&
                     view.Body.Contains("Immobilized") &&
                     (view.Body.Contains("winner=1") || view.Body.Contains("Winner: 1"));

        var pass = fileOk && viewOk && !string.IsNullOrEmpty(presented);

        Debug.Log(
            $"[S10-02] VERIFIER_DONE pass={pass} file_ok={fileOk} view_ok={viewOk} " +
            $"path={path} load_err={loadErr ?? "none"} view_visible={view.Visible}");

        // Keep view visible briefly for Play Mode smoke; exit is owned by MCP/agent.
        yield return new WaitForSecondsRealtime(0.5f);
    }
}
