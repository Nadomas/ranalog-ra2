using System.Diagnostics;

namespace Ra2.Robot
{
    public enum WorkshopMode : byte
    {
        Design = 0,
        Configure = 1,
        Test = 2
    }

    /// <summary>
    /// In-memory Design ↔ Configure ↔ Test session (S6-01). Retains one working blueprint across modes.
    /// Test Room chrome is local-only; blueprint remains MP-admit compatible.
    /// </summary>
    public sealed class RobotWorkshopSession
    {
        public WorkshopMode Mode { get; private set; } = WorkshopMode.Design;
        public RobotBlueprint WorkingBlueprint { get; private set; }
        public RobotSpawnedInstance TestInstance { get; private set; }
        /// <summary>Last combat-admit clone from <see cref="TryPrepareCombatAdmit"/> (S6-04).</summary>
        public RobotBlueprint LastAdmitBlueprint { get; private set; }
        public string LastAdmitJson { get; private set; }
        public double LastSwitchMs { get; private set; }
        public double LastResetMs { get; private set; }
        public double LastAdmitTestMs { get; private set; }
        public int SwitchCount { get; private set; }

        public void SetWorkingBlueprint(RobotBlueprint blueprint)
        {
            WorkingBlueprint = blueprint;
        }

        public bool TrySwitchMode(
            WorkshopMode next,
            UnityEngine.Transform spawnParent,
            UnityEngine.PhysicsMaterial slide,
            UnityEngine.Color bodyColor,
            out string error)
        {
            error = null;
            var sw = Stopwatch.StartNew();

            if (next == Mode)
            {
                LastSwitchMs = 0;
                return true;
            }

            if (WorkingBlueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            // Leaving Test: despawn physics instance, keep blueprint.
            if (Mode == WorkshopMode.Test && next != WorkshopMode.Test)
            {
                RobotSpawnService.Despawn(TestInstance);
                TestInstance = null;
            }

            if (next == WorkshopMode.Test)
            {
                if (!RobotSpawnService.TryValidate(WorkingBlueprint, out error))
                    return false;

                if (TestInstance != null)
                {
                    RobotSpawnService.Despawn(TestInstance);
                    TestInstance = null;
                }

                TestInstance = RobotSpawnService.Spawn(
                    WorkingBlueprint, 0, 0, spawnParent, slide, bodyColor);
            }

            Mode = next;
            sw.Stop();
            LastSwitchMs = sw.Elapsed.TotalMilliseconds;
            SwitchCount++;
            return true;
        }

        public bool TryResetTest(
            UnityEngine.Transform spawnParent,
            UnityEngine.PhysicsMaterial slide,
            UnityEngine.Color bodyColor,
            out string error)
        {
            error = null;
            if (Mode != WorkshopMode.Test)
            {
                error = "not_in_test";
                return false;
            }

            if (WorkingBlueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            var sw = Stopwatch.StartNew();
            if (TestInstance != null)
            {
                RobotSpawnService.Despawn(TestInstance);
                TestInstance = null;
            }

            if (!RobotSpawnService.TryValidate(WorkingBlueprint, out error))
                return false;

            TestInstance = RobotSpawnService.Spawn(
                WorkingBlueprint, 0, 0, spawnParent, slide, bodyColor);
            sw.Stop();
            LastResetMs = sw.Elapsed.TotalMilliseconds;
            return true;
        }

        /// <summary>
        /// S11 glue: leave Test if needed, validate working blueprint, return MP-admit clone + JSON.
        /// Does not invent a divergent battle format — same schema as UDP spawn.
        /// </summary>
        public bool TryPrepareCombatAdmit(
            out RobotBlueprint admitBlueprint,
            out string blueprintJson,
            out string error)
        {
            admitBlueprint = null;
            blueprintJson = null;
            error = null;

            if (WorkingBlueprint == null)
            {
                error = "no_blueprint";
                return false;
            }

            if (Mode == WorkshopMode.Test)
            {
                RobotSpawnService.Despawn(TestInstance);
                TestInstance = null;
                Mode = WorkshopMode.Configure;
            }

            if (!RobotSpawnService.TryValidate(WorkingBlueprint, out error))
                return false;

            try
            {
                blueprintJson = RobotBlueprintSerializer.ToJson(WorkingBlueprint, pretty: false);
                admitBlueprint = RobotBlueprintSerializer.FromJson(blueprintJson);
            }
            catch (System.Exception)
            {
                error = "serialize_failed";
                return false;
            }

            if (!RobotSpawnService.TryValidate(admitBlueprint, out error))
                return false;

            LastAdmitBlueprint = admitBlueprint;
            LastAdmitJson = blueprintJson;
            return true;
        }

        /// <summary>
        /// S6-04: enter Test using the last combat-admit JSON clone (not raw Design edits).
        /// Rejects if Prepare Admit has not succeeded yet.
        /// </summary>
        public bool TryEnterTestFromAdmit(
            UnityEngine.Transform spawnParent,
            UnityEngine.PhysicsMaterial slide,
            UnityEngine.Color bodyColor,
            out string error)
        {
            error = null;
            if (LastAdmitBlueprint == null || string.IsNullOrEmpty(LastAdmitJson))
            {
                error = "no_admit";
                return false;
            }

            var sw = Stopwatch.StartNew();

            RobotBlueprint admitClone;
            try
            {
                admitClone = RobotBlueprintSerializer.FromJson(LastAdmitJson);
            }
            catch (System.Exception)
            {
                error = "admit_clone_failed";
                return false;
            }

            if (!RobotSpawnService.TryValidate(admitClone, out error))
                return false;

            if (Mode == WorkshopMode.Test && TestInstance != null)
            {
                RobotSpawnService.Despawn(TestInstance);
                TestInstance = null;
            }

            WorkingBlueprint = admitClone;
            LastAdmitBlueprint = admitClone;
            TestInstance = RobotSpawnService.Spawn(admitClone, 0, 0, spawnParent, slide, bodyColor);
            Mode = WorkshopMode.Test;
            sw.Stop();
            LastAdmitTestMs = sw.Elapsed.TotalMilliseconds;
            LastSwitchMs = LastAdmitTestMs;
            SwitchCount++;
            return true;
        }
    }
}
