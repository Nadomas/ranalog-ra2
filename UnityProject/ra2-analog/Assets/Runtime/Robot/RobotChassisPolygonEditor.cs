using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Plain chassis polygon mutator (S4-02). Enforces ≤16 / ≥3 point budgets before write.
    /// Does not touch Rigidbody transforms — data only until spawn/admit.
    /// </summary>
    public static class RobotChassisPolygonEditor
    {
        public const int MaxPoints = 16;
        public const int MinPoints = 3;

        public static Vector2[] GetPoints(RobotBlueprint blueprint)
        {
            if (blueprint.Chassis.BaseplatePoints == null)
                return Array.Empty<Vector2>();
            return (Vector2[])blueprint.Chassis.BaseplatePoints.Clone();
        }

        public static int PointCount(RobotBlueprint blueprint) =>
            blueprint?.Chassis.BaseplatePoints?.Length ?? 0;

        public static bool TrySetPoints(RobotBlueprint blueprint, Vector2[] points, out string error)
        {
            error = null;
            if (blueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            if (points == null)
            {
                error = "null_points";
                return false;
            }

            if (points.Length > MaxPoints)
            {
                error = $"chassis_points:{points.Length}>{MaxPoints}";
                return false;
            }

            if (points.Length > 0 && points.Length < MinPoints)
            {
                error = $"chassis_points:{points.Length}<{MinPoints}";
                return false;
            }

            var chassis = blueprint.Chassis;
            chassis.BaseplatePoints = (Vector2[])points.Clone();
            blueprint.Chassis = chassis;
            return true;
        }

        public static bool TryAddPoint(RobotBlueprint blueprint, Vector2 point, out string error)
        {
            var current = GetPoints(blueprint);
            if (current.Length >= MaxPoints)
            {
                error = $"chassis_points:{current.Length + 1}>{MaxPoints}";
                return false;
            }

            var next = new Vector2[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = point;
            return TrySetPoints(blueprint, next, out error);
        }

        public static bool TrySetPoint(RobotBlueprint blueprint, int index, Vector2 point, out string error)
        {
            error = null;
            var current = GetPoints(blueprint);
            if (index < 0 || index >= current.Length)
            {
                error = $"index:{index}";
                return false;
            }

            current[index] = point;
            return TrySetPoints(blueprint, current, out error);
        }

        public static bool TryRemovePoint(RobotBlueprint blueprint, int index, out string error)
        {
            error = null;
            var current = GetPoints(blueprint);
            if (index < 0 || index >= current.Length)
            {
                error = $"index:{index}";
                return false;
            }

            if (current.Length <= MinPoints)
            {
                error = $"chassis_points:{current.Length - 1}<{MinPoints}";
                return false;
            }

            var next = new List<Vector2>(current.Length - 1);
            for (var i = 0; i < current.Length; i++)
            {
                if (i == index)
                    continue;
                next.Add(current[i]);
            }

            return TrySetPoints(blueprint, next.ToArray(), out error);
        }

        /// <summary>Nudge one point by delta (thin edit smoke).</summary>
        public static bool TryNudgePoint(RobotBlueprint blueprint, int index, Vector2 delta, out string error)
        {
            var current = GetPoints(blueprint);
            if (index < 0 || index >= current.Length)
            {
                error = $"index:{index}";
                return false;
            }

            return TrySetPoint(blueprint, index, current[index] + delta, out error);
        }
    }
}
