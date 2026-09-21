using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Thin actuator driver: BurstMotor/BurstPiston Fire (S7-05/06), ServoMotor/ServoPiston Analog (S7-07/08),
    /// SmartZone→Fire (S7-09), Steering hubs Analog (S7-10), BurstMotor electric draw (S7-11),
    /// Air tank recharge via AirMaxInOutRate (S7-12), electric recharge via ElectricMaxInOutRate (S7-13).
    /// SpinMotor continuous CW stays on <see cref="RobotMotorDrive"/>.
    /// Host/local authority: reads <see cref="PhysicsTestDrive.CurrentCommand"/> only.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(55)]
    public sealed class RobotActuatorDrive : MonoBehaviour
    {
        const float BurstMotorArcSeconds = 0.18f;
        const float BurstMotorTargetDegPerSec = 720f;
        const float BurstMotorForce = 220f;
        const float BurstMotorElecCost = 70f;
        const float BurstPistonImpulse = 18f;
        const float BurstPistonAirCost = 80f;
        const float RetractSpring = 140f;

        const float ServoMotorMaxDeg = 90f;
        const float ServoMotorSlowDegPerSec = 75f;
        const float ServoMotorDriveForce = 90f;
        const float ServoMotorLockForce = 320f;
        const float ServoDeadzone = 0.05f;

        const float SteerMaxDeg = 35f;
        const float SteerSlowDegPerSec = 120f;
        const float SteerDriveForce = 110f;
        const float SteerLockForce = 280f;

        const float ServoPistonTravel = 0.45f;
        const float ServoPistonAirPerSec = 55f;
        const float ServoPistonSpring = 900f;
        const float ServoPistonDamper = 50f;
        const float ServoPistonMaxForce = 600f;

        PhysicsTestDrive commandSource;
        PhysicsTestDisableFlag disableFlag;
        RobotBlueprint blueprint;
        readonly Dictionary<string, HingeJoint> burstMotors = new Dictionary<string, HingeJoint>(4);
        readonly Dictionary<string, HingeJoint> servoMotors = new Dictionary<string, HingeJoint>(4);
        readonly Dictionary<string, HingeJoint> steeringHubs = new Dictionary<string, HingeJoint>(4);
        readonly Dictionary<string, ConfigurableJoint> pistons = new Dictionary<string, ConfigurableJoint>(4);
        readonly Dictionary<string, Rigidbody> pistonBodies = new Dictionary<string, Rigidbody>(4);
        readonly Dictionary<string, Vector3> pistonRestLocal = new Dictionary<string, Vector3>(4);
        readonly Dictionary<string, ConfigurableJoint> servoPistons = new Dictionary<string, ConfigurableJoint>(4);
        readonly Dictionary<string, Rigidbody> servoPistonBodies = new Dictionary<string, Rigidbody>(4);
        readonly Dictionary<string, Vector3> servoPistonRestLocal = new Dictionary<string, Vector3>(4);
        readonly Dictionary<string, RobotSmartZoneSensor> smartZones = new Dictionary<string, RobotSmartZoneSensor>(4);
        readonly Dictionary<string, float> burstMotorUntil = new Dictionary<string, float>(4);
        readonly Dictionary<string, float> efforts = new Dictionary<string, float>(8);
        readonly List<string> fireTargets = new List<string>(4);
        readonly List<string> zoneFireTargets = new List<string>(4);

        float prevFire;
        float airRemaining;
        float electricRemaining;

        public float AirRemaining => airRemaining;
        public float ElectricRemaining => electricRemaining;
        public int BurstMotorCount => burstMotors.Count;
        public int ServoMotorCount => servoMotors.Count;
        public int SteeringCount => steeringHubs.Count;
        public int PistonCount => pistons.Count;
        public int ServoPistonCount => servoPistons.Count;
        public int SmartZoneCount => smartZones.Count;
        public int LastFireCount { get; private set; }
        public int LastZoneFireCount { get; private set; }
        public int LastAirDenied { get; private set; }
        public int LastElecDenied { get; private set; }
        public int LastServoLocked { get; private set; }
        public int LastSteerLocked { get; private set; }

        public void Bind(RobotBlueprint source, Dictionary<string, GameObject> parts, PhysicsTestDrive drive, int robotId = 0)
        {
            blueprint = source;
            commandSource = drive;
            burstMotors.Clear();
            servoMotors.Clear();
            steeringHubs.Clear();
            pistons.Clear();
            pistonBodies.Clear();
            pistonRestLocal.Clear();
            servoPistons.Clear();
            servoPistonBodies.Clear();
            servoPistonRestLocal.Clear();
            smartZones.Clear();
            burstMotorUntil.Clear();
            airRemaining = source != null ? Mathf.Max(0f, source.Power.AirTotal) : 0f;
            electricRemaining = source != null ? Mathf.Max(0f, source.Power.ElectricTotal) : 0f;
            prevFire = 0f;
            LastFireCount = 0;
            LastZoneFireCount = 0;
            LastAirDenied = 0;
            LastElecDenied = 0;
            LastServoLocked = 0;
            LastSteerLocked = 0;

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
                else if (baseKind == RobotComponentBase.ServoMotor)
                {
                    var hinge = go.GetComponent<HingeJoint>();
                    if (hinge != null)
                        servoMotors[def.Id] = hinge;
                }
                else if (baseKind == RobotComponentBase.Steering)
                {
                    var hinge = go.GetComponent<HingeJoint>();
                    if (hinge != null)
                        steeringHubs[def.Id] = hinge;
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
                else if (baseKind == RobotComponentBase.ServoPiston)
                {
                    var slide = go.GetComponent<ConfigurableJoint>();
                    var body = go.GetComponent<Rigidbody>();
                    if (slide != null && body != null)
                    {
                        servoPistons[def.Id] = slide;
                        servoPistonBodies[def.Id] = body;
                        servoPistonRestLocal[def.Id] = go.transform.localPosition;
                        ApplyServoPistonDrive(slide, 0f);
                    }
                }
                else if (baseKind == RobotComponentBase.SmartZone)
                {
                    var sensor = go.GetComponent<RobotSmartZoneSensor>();
                    if (sensor == null)
                        sensor = go.AddComponent<RobotSmartZoneSensor>();
                    sensor.Bind(def.Id, robotId);
                    smartZones[def.Id] = sensor;
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
                IdleServos();
                IdleSteering();
                prevFire = 0f;
                return;
            }

            TickAirRecharge();
            TickElectricRecharge();

            if (commandSource == null)
                commandSource = GetComponent<PhysicsTestDrive>();
            var cmd = commandSource != null ? commandSource.CurrentCommand : default;
            if (cmd.Brake)
            {
                IdleBurstMotors();
                IdleServos();
                IdleSteering();
                prevFire = cmd.Fire;
                return;
            }

            var fire = Mathf.Clamp01(cmd.Fire);
            var rising = fire >= 0.5f && prevFire < 0.5f;
            prevFire = fire;

            RobotWiringDriveResolver.ResolveFireTargets(blueprint, cmd, rising, fireTargets);
            for (var i = 0; i < fireTargets.Count; i++)
                TriggerFire(fireTargets[i]);

            RobotWiringDriveResolver.ResolveSmartZoneFireTargets(
                blueprint,
                id => smartZones.TryGetValue(id, out var z) && z != null && z.ContactRisingEdge,
                zoneFireTargets);
            for (var i = 0; i < zoneFireTargets.Count; i++)
            {
                var before = LastFireCount;
                TriggerFire(zoneFireTargets[i]);
                if (LastFireCount > before)
                    LastZoneFireCount++;
            }

            TickBurstMotors();
            TickPistonRetract();

            RobotWiringDriveResolver.ResolveMotorEfforts(blueprint, cmd, efforts);
            TickServoMotors();
            TickSteering();
            TickServoPistons();
        }

        void TickAirRecharge()
        {
            if (blueprint == null)
                return;

            var cap = Mathf.Max(0f, blueprint.Power.AirTotal);
            if (cap <= 1e-3f || airRemaining >= cap - 1e-3f)
                return;

            var busRate = Mathf.Max(0f, blueprint.Power.AirMaxInOutRate);
            if (busRate <= 1e-3f)
                return;

            // Thin: positive AirTank component rates cap the bus refill (generators/tanks).
            var tankRate = 0f;
            var hasTank = false;
            if (blueprint.Components != null)
            {
                for (var i = 0; i < blueprint.Components.Length; i++)
                {
                    var c = blueprint.Components[i];
                    if (c.ResolvedBase() != RobotComponentBase.AirTank)
                        continue;
                    hasTank = true;
                    if (c.AirMaxInOutRate > 0f)
                        tankRate += c.AirMaxInOutRate;
                }
            }

            var rate = hasTank ? Mathf.Min(busRate, tankRate) : busRate;
            if (rate <= 1e-3f)
                return;

            airRemaining = Mathf.Min(cap, airRemaining + rate * Time.fixedDeltaTime);
        }

        void TickElectricRecharge()
        {
            if (blueprint == null)
                return;

            var cap = Mathf.Max(0f, blueprint.Power.ElectricTotal);
            if (cap <= 1e-3f || electricRemaining >= cap - 1e-3f)
                return;

            var busRate = Mathf.Max(0f, blueprint.Power.ElectricMaxInOutRate);
            if (busRate <= 1e-3f)
                return;

            // Thin: positive Battery component rates cap the bus refill.
            var batteryRate = 0f;
            var hasBattery = false;
            if (blueprint.Components != null)
            {
                for (var i = 0; i < blueprint.Components.Length; i++)
                {
                    var c = blueprint.Components[i];
                    if (c.ResolvedBase() != RobotComponentBase.Battery)
                        continue;
                    hasBattery = true;
                    if (c.ElecMaxInOutRate > 0f)
                        batteryRate += c.ElecMaxInOutRate;
                }
            }

            var rate = hasBattery ? Mathf.Min(busRate, batteryRate) : busRate;
            if (rate <= 1e-3f)
                return;

            electricRemaining = Mathf.Min(cap, electricRemaining + rate * Time.fixedDeltaTime);
        }

        void TriggerFire(string componentId)
        {
            if (burstMotors.TryGetValue(componentId, out var hinge) && hinge != null)
            {
                if (electricRemaining + 1e-3f < BurstMotorElecCost)
                {
                    LastElecDenied++;
                    return;
                }

                electricRemaining = Mathf.Max(0f, electricRemaining - BurstMotorElecCost);
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

        void TickServoMotors()
        {
            LastServoLocked = 0;
            foreach (var kv in servoMotors)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                efforts.TryGetValue(kv.Key, out var effort);
                effort = Mathf.Clamp(effort, -1f, 1f);

                if (Mathf.Abs(effort) < ServoDeadzone)
                {
                    var lockMotor = hinge.motor;
                    lockMotor.targetVelocity = 0f;
                    lockMotor.force = ServoMotorLockForce;
                    lockMotor.freeSpin = false;
                    hinge.motor = lockMotor;
                    hinge.useMotor = true;
                    LastServoLocked++;
                    continue;
                }

                var targetAngle = effort * ServoMotorMaxDeg;
                var angle = hinge.angle;
                if (float.IsNaN(angle) || float.IsInfinity(angle))
                    angle = 0f;
                var error = targetAngle - angle;
                var speed = Mathf.Clamp(error * 4f, -ServoMotorSlowDegPerSec, ServoMotorSlowDegPerSec);
                var motor = hinge.motor;
                motor.targetVelocity = speed;
                motor.force = ServoMotorDriveForce;
                motor.freeSpin = false;
                hinge.motor = motor;
                hinge.useMotor = true;
            }
        }

        void TickSteering()
        {
            LastSteerLocked = 0;
            foreach (var kv in steeringHubs)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                efforts.TryGetValue(kv.Key, out var effort);
                effort = Mathf.Clamp(effort, -1f, 1f);

                if (Mathf.Abs(effort) < ServoDeadzone)
                {
                    var lockMotor = hinge.motor;
                    lockMotor.targetVelocity = 0f;
                    lockMotor.force = SteerLockForce;
                    lockMotor.freeSpin = false;
                    hinge.motor = lockMotor;
                    hinge.useMotor = true;
                    LastSteerLocked++;
                    continue;
                }

                var targetAngle = effort * SteerMaxDeg;
                var angle = hinge.angle;
                if (float.IsNaN(angle) || float.IsInfinity(angle))
                    angle = 0f;
                var error = targetAngle - angle;
                var speed = Mathf.Clamp(error * 6f, -SteerSlowDegPerSec, SteerSlowDegPerSec);
                var motor = hinge.motor;
                motor.targetVelocity = speed;
                motor.force = SteerDriveForce;
                motor.freeSpin = false;
                hinge.motor = motor;
                hinge.useMotor = true;
            }
        }

        void TickServoPistons()
        {
            var dt = Time.fixedDeltaTime;
            foreach (var kv in servoPistons)
            {
                var slide = kv.Value;
                if (slide == null || !servoPistonBodies.TryGetValue(kv.Key, out var body) || body == null)
                    continue;

                efforts.TryGetValue(kv.Key, out var effort);
                effort = Mathf.Clamp(effort, -1f, 1f);

                if (Mathf.Abs(effort) >= ServoDeadzone)
                {
                    if (airRemaining <= 1e-3f)
                    {
                        LastAirDenied++;
                        // Hold last commanded position (lock mid-stroke) when air empty.
                        continue;
                    }

                    airRemaining = Mathf.Max(0f, airRemaining - ServoPistonAirPerSec * dt);
                }

                var targetExt = Mathf.Clamp01((effort + 1f) * 0.5f) * ServoPistonTravel;
                if (Mathf.Abs(effort) < ServoDeadzone)
                {
                    // Lock mid-stroke: hold current extension via joint drive.
                    if (servoPistonRestLocal.TryGetValue(kv.Key, out var rest))
                    {
                        var axis = slide.axis.sqrMagnitude > 1e-6f ? slide.axis.normalized : Vector3.forward;
                        var cur = Vector3.Dot(body.transform.localPosition - rest, axis);
                        targetExt = Mathf.Clamp(cur, 0f, ServoPistonTravel);
                    }
                }

                ApplyServoPistonDrive(slide, targetExt);
            }
        }

        static void ApplyServoPistonDrive(ConfigurableJoint slide, float targetExt)
        {
            var drive = slide.xDrive;
            drive.positionSpring = ServoPistonSpring;
            drive.positionDamper = ServoPistonDamper;
            drive.maximumForce = ServoPistonMaxForce;
            slide.xDrive = drive;
            // Invert joint target X so +Extend matches +Dot(local, axis).
            slide.targetPosition = new Vector3(-targetExt, 0f, 0f);
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

        void IdleServos()
        {
            foreach (var kv in servoMotors)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                var motor = hinge.motor;
                motor.targetVelocity = 0f;
                motor.force = ServoMotorLockForce;
                hinge.motor = motor;
                hinge.useMotor = true;
            }

            foreach (var kv in servoPistons)
            {
                if (kv.Value == null || !servoPistonBodies.TryGetValue(kv.Key, out var body) || body == null)
                    continue;
                if (!servoPistonRestLocal.TryGetValue(kv.Key, out var rest))
                    continue;
                var axis = kv.Value.axis.sqrMagnitude > 1e-6f ? kv.Value.axis.normalized : Vector3.forward;
                var cur = Vector3.Dot(body.transform.localPosition - rest, axis);
                ApplyServoPistonDrive(kv.Value, Mathf.Clamp(cur, 0f, ServoPistonTravel));
            }
        }

        void IdleSteering()
        {
            foreach (var kv in steeringHubs)
            {
                var hinge = kv.Value;
                if (hinge == null)
                    continue;
                var motor = hinge.motor;
                motor.targetVelocity = 0f;
                motor.force = SteerLockForce;
                hinge.motor = motor;
                hinge.useMotor = true;
            }
        }
    }
}
