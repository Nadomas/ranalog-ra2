using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S10-03: readable multiline results chrome from authoritative MatchSummary.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotResultsReadableVerifier : MonoBehaviour
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
            matchDurationSeconds: 18.25f,
            immobileSecondsLoser: 2.0f,
            immobileSecondsWinner: 0.05f,
            loserWasDisabled: true,
            sessionId: "s10-readable-thin");

        var presented = MatchResultsStub.Present(summary, "local", persist: false, view: view);
        yield return null;

        var body = view.Body ?? string.Empty;
        var multiline = body.IndexOf('\n') >= 0;
        var hasReason = body.Contains("Immobilized") || body.Contains("Reason");
        var hasWinner = body.Contains("Winner") && body.Contains("1");
        var hasLoser = body.Contains("Loser") && body.Contains("0");
        var hasDuration = body.Contains("Duration") || body.Contains("18.25") || body.Contains("18,25");
        var hasSession = body.Contains("s10-readable-thin");
        var readable = MatchResultsStub.FormatReadable(summary, "local");
        var formatOk = readable.IndexOf('\n') >= 0 && readable.Contains("Immobilized");

        var pass = view.Visible && multiline && hasReason && hasWinner && hasLoser &&
                   hasDuration && hasSession && formatOk && !string.IsNullOrEmpty(presented);

        Debug.Log(
            $"[S10-03] VERIFIER_DONE pass={pass} visible={view.Visible} multiline={multiline} " +
            $"reason={hasReason} winner={hasWinner} loser={hasLoser} duration={hasDuration} " +
            $"session={hasSession} format_ok={formatOk}");

        yield return new WaitForSecondsRealtime(0.4f);
    }
}
