using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Thin BurstMotor Fire arc + BurstPiston Fire (air) driver (S7-05 / S7-06).
    /// SpinMotor continuous CW/Fire stays on <see cref="RobotMotorDrive"/> via digital wiring.
    /// Host/local authority: reads <see cref="PhysicsTestDrive.CurrentCommand"/> only.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(55)]
    public sealed class RobotActuatorDrive : MonoBehaviour
    {
        const float BurstMotorArcSeconds = 0.18f;
        const float BurstMotorTargetDegPerSec = 720f;
        const float BurstMotorForce = 220f;
        const float BurstPistonImpulse = 18f;
        const float BurstPistonAirCost = 80f;
        const float RetractSpring = 140f;

        PhysicsTestDrive commandSource;
        PhysicsTestDisableFlag disableFlag;
        RobotBlueprint blueprint;
        readonly Dictionary<string, HingeJoint> burstMotors = new Dictionary<string, HingeJoint>(4);
        readonly Dictionary<string, ConfigurableJoint> pistons = new Dictionary<string, ConfigurableJoint>(4);
        readonly Dictionary<string, Rigidbody> pistonBodies = new Dictionary<string, Rigidbody>(4);
        readonly Dictionary<string, Vector3> pistonRestLocal = new Dictionary<string, Vector3>(4);
        readonly Dictionary<string, float> burstMotorUntil = new Dictionary<string, float>(4);
        readonly List<string> fireTargets = new List<string>(4);

        float prevFire;
        float airRemaining;

        public float AirRemaining => airRemaining;
        public int BurstMotorCount => burstMotors.Count;
        public int PistonCount => pistons.Count;
        public int LastFireCount { get; private set; }
        public int LastAirDenied { get; private set; }

        public void Bind(RobotBlueprint source, Dictionary<string, GameObject> parts, PhysicsTestDrive drive)
        {
            blueprint = source;
            commandSource = drive;
            burstMotors.Clear();
            pistons.Clear();
            pistonBodies.Clear();
            pistonRestLocal.Clear();
            burstMotorUntil.Clear();
            airRemaining = source != null ? Mathf.Max(0f, source.Power.AirTotal) : 0f;
            prevFire = 0f;
            LastFireCount = 0;
            LastAirDenied = 0;

            if (parts == null || source?.Components == null)
                return;

            for (var i = 0; i < source.Components.Length; i++)
            {
                var def = source.Components[i];
                var baseKind = def.ResolvedBase();
                if (!parts.TryGetValue(def.Id, out var go) || go == null)
                    continue;

                if (baseKind == RobotComponentBase.BurstMotor)
                {
                    var hinge = go.GetComponent<HingeJoint>();
                    if (hinge != null)
                        burstMotors[def.Id] = hinge;
                }
                else if (baseKind == RobotComponentBase.BurstPiston)
                {
                    var slide = go.GetComponent<ConfigurableJoint>();
                    var body = go.GetComponent<Rigidbody>();
                    if (slide != null && body != null)
                    {
                        pistons[def.Id] = slide;
                        pistonBodies[def.Id] = body;
                        pistonRestLocal[def.Id] = go.transform.localPosition;
                    }
                }
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
                IdleBurstMotors();
                prevFire = 0f;
                return;
            }

            if (commandSource == null)
                commandSource = GetComponent<PhysicsTestDrive>();
            var cmd = commandSource != null ? commandSource.CurrentCommand : default;
            if (cmd.Brake)
            {
                IdleBurstMotors();
                prevFire = cmd.Fire;
                return;
            }

            var fire = Mathf.Clamp01(cmd.Fire);
            var rising = fire >= 0.5f && prevFire < 0.5f;
            prevFire = fire;

            RobotWiringDriveResolver.ResolveFireTargets(blueprint, cmd, rising, fireTargets);
            for (var i = 0; i < fireTargets.Count; i++)
                TriggerFire(fireTargets[i]);

            TickBurstMotors();
            TickPistonRetract();
        }

        void TriggerFire(string componentId)
        {
            if (burstMotors.TryGetValue(componentId, out var hinge) && hinge != null)
            {
                burstMotorUntil[componentId] = Time.time + BurstMotorArcSeconds;
                var motor = hinge.motor;
                motor.targetVelocity = BurstMotorTargetDegPerSec;
                motor.force = BurstMotorForce;
                motor.freeSpin = false;
                hinge.motor = motor;
                hinge.useMotor = true;
                LastFireCount++;
                return;
            }

            if (pistons.TryGetValue(componentId, out var slide) &&
                pistonBodies.TryGetValue(componentId, out var body) &&
                slide != null && body != null)
            {
                if (airRemaining + 1e-3f < BurstPistonAirCost)
                {
                    LastAirDenied++;
                    return;
                }

                airRemaining = Mathf.Max(0f, airRemaining - BurstPistonAirCost);
                var axis = slide.axis.sqrMagnitude > 1e-6f ? slide.axis.normalized : Vector3.forward;
                body.AddRelativeForce(axis * BurstPistonImpulse, ForceMode.VelocityChange);
                LastFireCount++;
            }
        }

        void TickBurstMotors()
        {
            foreach (var kv in burstMotors)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                if (!burstMotorUntil.TryGetValue(kv.Key, out var until) || Time.time > until)
                {
                    var idle = hinge.motor;
                    idle.targetVelocity = -180f; // slow retract toward min limit
                    idle.force = 40f;
                    hinge.motor = idle;
                    hinge.useMotor = true;
                    continue;
                }

                var motor = hinge.motor;
                motor.targetVelocity = BurstMotorTargetDegPerSec;
                motor.force = BurstMotorForce;
                hinge.motor = motor;
                hinge.useMotor = true;
            }
        }

        void TickPistonRetract()
        {
            foreach (var kv in pistonBodies)
            {
                var body = kv.Value;
                if (body == null || !pistons.TryGetValue(kv.Key, out var slide) || slide == null)
                    continue;
                if (!pistonRestLocal.TryGetValue(kv.Key, out var rest))
                    continue;
                var axis = slide.axis.sqrMagnitude > 1e-6f ? slide.axis.normalized : Vector3.forward;
                var delta = body.transform.localPosition - rest;
                var extension = Vector3.Dot(delta, axis);
                if (extension > 0.02f)
                    body.AddRelativeForce(-axis * extension * RetractSpring, ForceMode.Acceleration);
            }
        }

        void IdleBurstMotors()
        {
            foreach (var kv in burstMotors)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                var motor = hinge.motor;
                motor.targetVelocity = 0f;
                motor.force = 20f;
                hinge.motor = motor;
                hinge.useMotor = true;
            }
        }
    }
}
