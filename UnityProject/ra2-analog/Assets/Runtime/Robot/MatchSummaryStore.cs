using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S10-02 local match summary persist + S11-15 thin history list (file stub).
    /// Not a career/cloud product.
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

        /// <summary>S11-15: newest-first local match files (cap). Display only — no recompute.</summary>
        public static int TryListRecent(List<Dto> into, int maxCount = 12)
        {
            if (into == null)
                return 0;
            into.Clear();
            if (maxCount <= 0)
                return 0;

            try
            {
                if (!Directory.Exists(DefaultDirectory))
                    return 0;

                var files = Directory.GetFiles(DefaultDirectory, "match-*.json");
                Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                var n = Math.Min(maxCount, files.Length);
                for (var i = 0; i < n; i++)
                {
                    if (TryLoad(files[i], out var dto, out _) && dto != null)
                        into.Add(dto);
                }

                return into.Count;
            }
            catch
            {
                return into.Count;
            }
        }

        public static string FormatHistoryLine(Dto dto)
        {
            if (dto == null)
                return "(empty)";
            var session = string.IsNullOrEmpty(dto.sessionId) ? "?" : dto.sessionId;
            var reason = string.IsNullOrEmpty(dto.reason) ? "?" : dto.reason;
            return $"{session}  W{dto.winnerRobotId}  {reason}  {dto.matchDurationSeconds:0.0}s";
        }

        public static string FormatHistoryDetail(Dto dto)
        {
            if (dto == null)
                return "";
            return
                $"Session: {dto.sessionId}\n" +
                $"Finished: {dto.finished}\n" +
                $"Reason: {dto.reason}\n" +
                $"Winner: {dto.winnerRobotId}\n" +
                $"Loser: {dto.loserRobotId}\n" +
                $"Duration: {dto.matchDurationSeconds:0.00} s\n" +
                $"Immobile loser: {dto.immobileSecondsLoser:0.00} s\n" +
                $"Immobile winner: {dto.immobileSecondsWinner:0.00} s\n" +
                $"Loser disabled: {dto.loserWasDisabled}\n" +
                $"Saved UTC: {dto.savedUtc}";
        }
    }
}
