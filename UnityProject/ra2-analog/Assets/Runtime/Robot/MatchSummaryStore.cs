using System;
using System.IO;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S10-02 local match summary persist (file stub). Not a career/history product.
    /// </summary>
    public static class MatchSummaryStore
    {
        [Serializable]
        public sealed class Dto
        {
            public bool finished;
            public int winnerRobotId;
            public int loserRobotId;
            public string reason;
            public float matchDurationSeconds;
            public float immobileSecondsLoser;
            public float immobileSecondsWinner;
            public bool loserWasDisabled;
            public string sessionId;
            public string savedUtc;
        }

        public static string DefaultDirectory =>
            Path.Combine(Application.persistentDataPath, "ra2-match-results");

        public static string DefaultPathFor(string sessionId)
        {
            var safe = string.IsNullOrEmpty(sessionId) ? "unknown" : sessionId;
            foreach (var c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            return Path.Combine(DefaultDirectory, $"match-{safe}.json");
        }

        public static Dto ToDto(MatchSummary summary)
        {
            var o = summary.Outcome;
            return new Dto
            {
                finished = o.Finished,
                winnerRobotId = o.WinnerRobotId,
                loserRobotId = o.LoserRobotId,
                reason = o.Reason.ToString(),
                matchDurationSeconds = summary.MatchDurationSeconds,
                immobileSecondsLoser = summary.ImmobileSecondsLoser,
                immobileSecondsWinner = summary.ImmobileSecondsWinner,
                loserWasDisabled = summary.LoserWasDisabled,
                sessionId = summary.SessionId ?? string.Empty,
                savedUtc = DateTime.UtcNow.ToString("o")
            };
        }

        public static bool TrySave(MatchSummary summary, out string path, out string error)
        {
            path = null;
            error = null;
            try
            {
                Directory.CreateDirectory(DefaultDirectory);
                path = DefaultPathFor(summary.SessionId);
                var json = JsonUtility.ToJson(ToDto(summary), prettyPrint: true);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryLoad(string path, out Dto dto, out string error)
        {
            dto = null;
            error = null;
            try
            {
                if (!File.Exists(path))
                {
                    error = "missing";
                    return false;
                }

                dto = JsonUtility.FromJson<Dto>(File.ReadAllText(path));
                if (dto == null)
                {
                    error = "parse";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
