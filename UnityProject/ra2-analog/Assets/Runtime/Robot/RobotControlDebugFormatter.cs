using System.Globalization;
using System.Text;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S11-19 local-only control debug text (presentation). Reads command + bindings; no physics writes.
    /// </summary>
    public static class RobotControlDebugFormatter
    {
        public static string Format(RobotBlueprint blueprint, PhysicsTestDrive drive)
        {
            var sb = new StringBuilder(128);
            var cmd = drive != null ? drive.CurrentCommand : default;
            sb.Append("cmd M=").Append(F2(cmd.Move));
            sb.Append(" T=").Append(F2(cmd.Turn));
            sb.Append(" F=").Append(F2(cmd.Fire));
            if (cmd.Brake)
                sb.Append(" BRK");

            var driveBind = RobotControlConfigurer.GetGroupBinding(
                blueprint, RobotControlConfigurer.BindingGroupId.Drive) ?? "—";
            var turnBind = RobotControlConfigurer.GetGroupBinding(
                blueprint, RobotControlConfigurer.BindingGroupId.Turn) ?? "—";
            sb.Append('\n');
            sb.Append("Drive=").Append(driveBind);
            sb.Append("  Turn=").Append(turnBind);
            sb.Append("  wires=").Append(blueprint?.Wirings?.Length ?? 0);

            if (blueprint?.ControlSlots != null && blueprint.ControlSlots.Length > 0)
            {
                sb.Append('\n');
                for (var i = 0; i < blueprint.ControlSlots.Length && i < 4; i++)
                {
                    var s = blueprint.ControlSlots[i];
                    if (i > 0)
                        sb.Append(" · ");
                    sb.Append(string.IsNullOrEmpty(s.DisplayName) ? s.Id : s.DisplayName);
                    sb.Append('[').Append(s.Kind).Append(']');
                }
            }

            return sb.ToString();
        }

        static string F2(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
