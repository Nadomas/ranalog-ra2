using System.Diagnostics;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Shared spawn path for local test and net host (S3-03).
    /// Assembles via <see cref="RobotAssembler"/> and attaches <see cref="PhysicsTestDrive"/>.
    /// </summary>
    public sealed class RobotSpawnedInstance
    {
        public int RobotId;
        public int AllowedSourceId;
        public RobotBlueprint Blueprint;
        public RobotAssemblyResult Assembly;
        public PhysicsTestDrive Drive;
        public RobotMotorDrive MotorDrive;
        public PhysicsTestDisableFlag DisableFlag;
        public double AssembleMs;
    }

    public static class RobotSpawnService
    {
        public static bool TryValidate(RobotBlueprint blueprint, out string error)
        {
            error = null;
            var result = RobotBlueprintValidator.Validate(blueprint);
            if (result.Ok)
                return true;

            error = result.Errors.Count > 0 ? result.Errors[0] : "validation_failed";
            return false;
        }

        public static RobotSpawnedInstance Spawn(
            RobotBlueprint blueprint,
            int robotId,
            int allowedSourceId,
            Transform parent,
            PhysicsMaterial slideMaterial,
            Color bodyColor)
        {
            if (!TryValidate(blueprint, out var error))
                throw new System.InvalidOperationException("Invalid blueprint: " + error);

            var sw = Stopwatch.StartNew();
            var assembly = RobotAssembler.Assemble(blueprint, parent, slideMaterial, bodyColor);
            sw.Stop();

            var mass = RobotMassProperties.Compute(blueprint);
            RobotMassProperties.ApplyToRootBody(assembly.RootBody, mass);

            var disable = assembly.Root.GetComponent<PhysicsTestDisableFlag>();
            if (disable == null)
                disable = assembly.Root.AddComponent<PhysicsTestDisableFlag>();
            var drive = assembly.Root.AddComponent<PhysicsTestDrive>();
            RobotMotorDrive motor = null;
            if (blueprint.Wirings != null && blueprint.Wirings.Length > 0)
            {
                motor = assembly.Root.AddComponent<RobotMotorDrive>();
                motor.Bind(blueprint, assembly.Parts, drive);
                if (motor.MotorCount > 0)
                    drive.SuppressChassisForce = true;
            }

            var instance = new RobotSpawnedInstance
            {
                RobotId = robotId,
                AllowedSourceId = allowedSourceId,
                Blueprint = blueprint,
                Assembly = assembly,
                Drive = drive,
                MotorDrive = motor,
                DisableFlag = disable,
                AssembleMs = sw.Elapsed.TotalMilliseconds
            };

            // S7-03: tag root so contact probes can resolve victims without client trust.
            var tag = assembly.Root.GetComponent<RobotInstanceTag>();
            if (tag == null)
                tag = assembly.Root.AddComponent<RobotInstanceTag>();
            tag.Bind(instance);

            return instance;
        }

        public static void SetDisabled(RobotSpawnedInstance instance, bool disabled)
        {
            if (instance?.DisableFlag == null)
                return;
            instance.DisableFlag.SetDisabled(disabled);
        }

        /// <summary>
        /// Functional detach: drop hinge, unparent, keep/add dynamic rigidbody. No teleport.
        /// </summary>
        public static bool TryDetach(RobotSpawnedInstance instance, string partId, out string error)
        {
            error = null;
            if (instance?.Assembly?.Parts == null)
            {
                error = "no_assembly";
                return false;
            }

            if (string.IsNullOrEmpty(partId) || !instance.Assembly.Parts.TryGetValue(partId, out var part) || part == null)
            {
                error = "unknown_part";
                return false;
            }

            if (instance.Assembly.Root != null && part.transform == instance.Assembly.Root.transform)
            {
                error = "cannot_detach_root";
                return false;
            }

            var hinge = part.GetComponent<HingeJoint>();
            if (hinge != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(hinge);
                else
                    Object.DestroyImmediate(hinge);
            }

            // worldPositionStays: keep pose without a physics teleport write.
            part.transform.SetParent(null, true);

            var rb = part.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = part.AddComponent<Rigidbody>();
                rb.mass = 1.2f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            rb.isKinematic = false;
            return true;
        }

        public static void Despawn(RobotSpawnedInstance instance)
        {
            if (instance == null || instance.Assembly == null || instance.Assembly.Root == null)
                return;

            var root = instance.Assembly.Root;
            if (Application.isPlaying)
                Object.Destroy(root);
            else
                Object.DestroyImmediate(root);

            instance.Assembly = null;
            instance.Drive = null;
            instance.MotorDrive = null;
            instance.DisableFlag = null;
        }
    }
}
