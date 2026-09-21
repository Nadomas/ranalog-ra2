using System;
using System.IO;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S11-18 local blueprint persist (workshop save slot). Not cloud/career inventory.
    /// </summary>
    public static class RobotBlueprintStore
    {
        public const string DefaultFileName = "workshop-bot.json";

        public static string DefaultDirectory =>
            Path.Combine(Application.persistentDataPath, "ra2-blueprints");

        public static string DefaultPath => Path.Combine(DefaultDirectory, DefaultFileName);

        public static bool TrySave(RobotBlueprint blueprint, out string path, out string error)
        {
            path = null;
            error = null;
            if (blueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            try
            {
                path = DefaultPath;
                RobotBlueprintSerializer.WriteFile(blueprint, path, pretty: true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryLoad(out RobotBlueprint blueprint, out string path, out string error)
        {
            blueprint = null;
            path = DefaultPath;
            error = null;
            try
            {
                if (!File.Exists(path))
                {
                    error = "missing";
                    return false;
                }

                blueprint = RobotBlueprintSerializer.ReadFile(path);
                if (blueprint == null)
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
