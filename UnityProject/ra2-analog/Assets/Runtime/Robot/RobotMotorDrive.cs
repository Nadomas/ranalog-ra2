using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Applies wiring-resolved effort as HingeJoint motors (S3-07).
    /// Reads <see cref="PhysicsTestDrive.CurrentCommand"/> so Stage 2 authority stays the command writer.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class RobotMotorDrive : MonoBehaviour
    {
        [SerializeField] float maxSpeedDegrees = 720f;
        [SerializeField] float motorForce = 180f;
        [SerializeField] float brakeForce = 280f;

        PhysicsTestDrive commandSource;
        PhysicsTestDisableFlag disableFlag;
        RobotBlueprint blueprint;
        readonly Dictionary<string, HingeJoint> motors = new Dictionary<string, HingeJoint>(8);
        readonly Dictionary<string, float> efforts = new Dictionary<string, float>(8);

        public int MotorCount => motors.Count;
        public int LastPoweredMotors { get; private set; }

        /// <summary>S7-02 thin degrade multiplier applied to motor force/speed.</summary>
        public float DrivePowerScale { get; set; } = 1f;

        public void Bind(RobotBlueprint source, Dictionary<string, GameObject> parts, PhysicsTestDrive drive)
        {
            blueprint = source;
            commandSource = drive;
            motors.Clear();
            if (parts == null || source?.Wirings == null)
                return;

            for (var i = 0; i < source.Wirings.Length; i++)
            {
                var id = source.Wirings[i].ComponentId;
                if (string.IsNullOrEmpty(id) || motors.ContainsKey(id))
                    continue;
                if (!parts.TryGetValue(id, out var go) || go == null)
                    continue;
                var hinge = go.GetComponent<HingeJoint>();
                if (hinge == null)
                    continue;
                motors[id] = hinge;
            }
        }

        void Awake()
        {
            if (commandSource == null)
                commandSource = GetComponent<PhysicsTestDrive>();
            disableFlag = GetComponent<PhysicsTestDisableFlag>();
        }

        void FixedUpdate()
        {
            if (disableFlag == null)
                disableFlag = GetComponent<PhysicsTestDisableFlag>();
            if (disableFlag != null && disableFlag.Disabled)
            {
                ApplyIdle(lockWheels: true);
                return;
            }

            if (commandSource == null)
                commandSource = GetComponent<PhysicsTestDrive>();
            var cmd = commandSource != null ? commandSource.CurrentCommand : default;
            if (cmd.Brake)
            {
                ApplyIdle(lockWheels: true);
                return;
            }

            RobotWiringDriveResolver.ResolveMotorEfforts(blueprint, cmd, efforts);
            LastPoweredMotors = 0;
            var scale = Mathf.Clamp01(DrivePowerScale);
            foreach (var kv in motors)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                efforts.TryGetValue(kv.Key, out var effort);
                if (Mathf.Abs(effort) < 0.02f)
                {
                    var idle = hinge.motor;
                    idle.force = 8f;
                    idle.targetVelocity = 0f;
                    hinge.motor = idle;
                    hinge.useMotor = true;
                    continue;
                }

                var motor = hinge.motor;
                motor.targetVelocity = effort * maxSpeedDegrees * scale;
                motor.force = motorForce * scale;
                motor.freeSpin = false;
                hinge.motor = motor;
                hinge.useMotor = true;
                LastPoweredMotors++;
            }
        }

        void ApplyIdle(bool lockWheels)
        {
            LastPoweredMotors = 0;
            foreach (var kv in motors)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                var motor = hinge.motor;
                motor.targetVelocity = 0f;
                motor.force = lockWheels ? brakeForce : 8f;
                hinge.motor = motor;
                hinge.useMotor = true;
            }
        }
    }
}
