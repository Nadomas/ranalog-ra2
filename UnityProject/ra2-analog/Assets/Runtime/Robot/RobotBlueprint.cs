using System;
using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>Data-only robot blueprint (S3-01). Serialization format provisional (U-SER / S3-02).</summary>
    [Serializable]
    public sealed class RobotBlueprint
    {
        public string Name;
        public float RootYawDegrees;
        public Vector3 RootPosition;
        public RobotChassisDef Chassis;
        public RobotPowerBudgetDef Power;
        public RobotComponentDef[] Components = Array.Empty<RobotComponentDef>();
        public RobotConnectionDef[] Connections = Array.Empty<RobotConnectionDef>();
        public RobotControlSlotDef[] ControlSlots = Array.Empty<RobotControlSlotDef>();
        public RobotWiringDef[] Wirings = Array.Empty<RobotWiringDef>();

        /// <summary>True when blueprint carries v1 RA2 workshop fields (control/wiring/chassis).</summary>
        public bool HasV1Fields =>
            ControlSlots.Length > 0 ||
            Wirings.Length > 0 ||
            (Chassis.BaseplatePoints != null && Chassis.BaseplatePoints.Length > 0) ||
            Power.ElectricTotal > 0f ||
            Power.AirTotal > 0f ||
            ContainsBase(RobotComponentBase.ControlBoard);

        bool ContainsBase(RobotComponentBase b)
        {
            for (var i = 0; i < Components.Length; i++)
            {
                if (Components[i].ResolvedBase() == b)
                    return true;
            }

            return false;
        }

        /// <summary>Sample matching PhysicsTest Robot_A proportions (chassis + 4 wheels + nose + FL hinge).</summary>
        public static RobotBlueprint CreatePhysicsTestSampleA(UnityEngine.Vector3 rootPosition, float yawDegrees)
        {
            return new RobotBlueprint
            {
                Name = "PhysicsTestSample_A",
                RootPosition = rootPosition,
                RootYawDegrees = yawDegrees,
                Components = new[]
                {
                    new RobotComponentDef
                    {
                        Id = "chassis",
                        Kind = RobotComponentKind.Chassis,
                        LocalPosition = UnityEngine.Vector3.zero,
                        LocalEuler = UnityEngine.Vector3.zero,
                        Scale = new UnityEngine.Vector3(1.6f, 0.7f, 2.2f),
                        Mass = 12f,
                        HasRigidbody = true,
                        IsRoot = true
                    },
                    new RobotComponentDef
                    {
                        Id = "wheel_fl",
                        Kind = RobotComponentKind.Wheel,
                        LocalPosition = new UnityEngine.Vector3(-0.85f, -0.35f, 0.7f),
                        LocalEuler = new UnityEngine.Vector3(0f, 0f, 90f),
                        Scale = new UnityEngine.Vector3(0.55f, 0.18f, 0.55f),
                        Mass = 1.2f,
                        HasRigidbody = true,
                        IsRoot = false
                    },
                    new RobotComponentDef
                    {
                        Id = "wheel_fr",
                        Kind = RobotComponentKind.Wheel,
                        LocalPosition = new UnityEngine.Vector3(0.85f, -0.35f, 0.7f),
                        LocalEuler = new UnityEngine.Vector3(0f, 0f, 90f),
                        Scale = new UnityEngine.Vector3(0.55f, 0.18f, 0.55f),
                        Mass = 0f,
                        HasRigidbody = false,
                        IsRoot = false
                    },
                    new RobotComponentDef
                    {
                        Id = "wheel_rl",
                        Kind = RobotComponentKind.Wheel,
                        LocalPosition = new UnityEngine.Vector3(-0.85f, -0.35f, -0.7f),
                        LocalEuler = new UnityEngine.Vector3(0f, 0f, 90f),
                        Scale = new UnityEngine.Vector3(0.55f, 0.18f, 0.55f),
                        Mass = 0f,
                        HasRigidbody = false,
                        IsRoot = false
                    },
                    new RobotComponentDef
                    {
                        Id = "wheel_rr",
                        Kind = RobotComponentKind.Wheel,
                        LocalPosition = new UnityEngine.Vector3(0.85f, -0.35f, -0.7f),
                        LocalEuler = new UnityEngine.Vector3(0f, 0f, 90f),
                        Scale = new UnityEngine.Vector3(0.55f, 0.18f, 0.55f),
                        Mass = 0f,
                        HasRigidbody = false,
                        IsRoot = false
                    },
                    new RobotComponentDef
                    {
                        Id = "nose",
                        Kind = RobotComponentKind.NoseMarker,
                        LocalPosition = new UnityEngine.Vector3(0f, 0.15f, 1.25f),
                        LocalEuler = UnityEngine.Vector3.zero,
                        Scale = new UnityEngine.Vector3(0.4f, 0.3f, 0.4f),
                        Mass = 0f,
                        HasRigidbody = false,
                        IsRoot = false
                    }
                },
                Connections = new[]
                {
                    new RobotConnectionDef
                    {
                        ParentId = "chassis",
                        ChildId = "wheel_fl",
                        Joint = RobotJointKind.Hinge,
                        HingeAxis = UnityEngine.Vector3.up
                    },
                    new RobotConnectionDef
                    {
                        ParentId = "chassis",
                        ChildId = "wheel_fr",
                        Joint = RobotJointKind.FixedHierarchy,
                        HingeAxis = UnityEngine.Vector3.zero
                    },
                    new RobotConnectionDef
                    {
                        ParentId = "chassis",
                        ChildId = "wheel_rl",
                        Joint = RobotJointKind.FixedHierarchy,
                        HingeAxis = UnityEngine.Vector3.zero
                    },
                    new RobotConnectionDef
                    {
                        ParentId = "chassis",
                        ChildId = "wheel_rr",
                        Joint = RobotJointKind.FixedHierarchy,
                        HingeAxis = UnityEngine.Vector3.zero
                    },
                    new RobotConnectionDef
                    {
                        ParentId = "chassis",
                        ChildId = "nose",
                        Joint = RobotJointKind.FixedHierarchy,
                        HingeAxis = UnityEngine.Vector3.zero
                    }
                }
            };
        }

        public static RobotBlueprint CreatePhysicsTestSampleB(UnityEngine.Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreatePhysicsTestSampleA(rootPosition, yawDegrees);
            bp.Name = "PhysicsTestSample_B";
            // B: no hinged wheel — all wheels hierarchy-fixed (matches S2 Robot_B).
            for (var i = 0; i < bp.Connections.Length; i++)
            {
                if (bp.Connections[i].ChildId != "wheel_fl")
                    continue;
                var c = bp.Connections[i];
                c.Joint = RobotJointKind.FixedHierarchy;
                bp.Connections[i] = c;
            }

            for (var i = 0; i < bp.Components.Length; i++)
            {
                if (bp.Components[i].Id != "wheel_fl")
                    continue;
                var c = bp.Components[i];
                c.HasRigidbody = false;
                c.Mass = 0f;
                bp.Components[i] = c;
            }

            return bp;
        }

        /// <summary>RA2-aligned tank-steer sample: Control Board + battery + spin-motor axles + 4 wheels + wiring (v1/S4).</summary>
        public static RobotBlueprint CreateRa2TankSteerSample(Vector3 rootPosition, float yawDegrees)
        {
            return CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
        }

        /// <summary>Construction sample A: balanced lightweight tank (wheels on spin-motor axles).</summary>
        public static RobotBlueprint CreateRa2ConstructionSampleA(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreatePhysicsTestSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2ConstructionSample_A";
            ApplyV1ChassisPowerControls(bp, RobotWeightClass.Lightweight, ballastMass: 0f, ballastZ: 0f);
            InsertSpinMotorAxles(bp);
            return bp;
        }

        /// <summary>Construction sample B: rear-heavy layout (different CoM) — still valid Lightweight.</summary>
        public static RobotBlueprint CreateRa2ConstructionSampleB(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreatePhysicsTestSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2ConstructionSample_B";
            ApplyV1ChassisPowerControls(bp, RobotWeightClass.Lightweight, ballastMass: 8f, ballastZ: -0.95f);
            InsertSpinMotorAxles(bp);
            // Longer wheelbase / nose forward for distinct geometry.
            for (var i = 0; i < bp.Components.Length; i++)
            {
                var c = bp.Components[i];
                if (c.Id == "nose")
                {
                    c.LocalPosition = new Vector3(0f, 0.15f, 1.45f);
                    bp.Components[i] = c;
                }
            }

            return bp;
        }

        /// <summary>v1-shaped payload missing Control Board — host must reject.</summary>
        public static RobotBlueprint CreateInvalidNoControlBoard(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2TankSteerSample(rootPosition, yawDegrees);
            bp.Name = "InvalidNoControlBoard";
            var kept = new System.Collections.Generic.List<RobotComponentDef>(bp.Components.Length);
            for (var i = 0; i < bp.Components.Length; i++)
            {
                if (bp.Components[i].ResolvedBase() == RobotComponentBase.ControlBoard)
                    continue;
                kept.Add(bp.Components[i]);
            }

            bp.Components = kept.ToArray();
            return bp;
        }

        /// <summary>Wheels parented to chassis (illegal attachment) — construction must reject.</summary>
        public static RobotBlueprint CreateInvalidWheelOnChassis(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "InvalidWheelOnChassis";
            // Remove motors; reconnect wheels directly to chassis (illegal).
            var kept = new System.Collections.Generic.List<RobotComponentDef>();
            for (var i = 0; i < bp.Components.Length; i++)
            {
                if (bp.Components[i].ResolvedBase() == RobotComponentBase.SpinMotor)
                    continue;
                kept.Add(bp.Components[i]);
            }

            bp.Components = kept.ToArray();
            var conns = new System.Collections.Generic.List<RobotConnectionDef>();
            for (var i = 0; i < bp.Connections.Length; i++)
            {
                var c = bp.Connections[i];
                if (c.ChildId != null && c.ChildId.StartsWith("motor_", StringComparison.Ordinal))
                    continue;
                if (c.ParentId != null && c.ParentId.StartsWith("motor_", StringComparison.Ordinal))
                {
                    conns.Add(new RobotConnectionDef
                    {
                        ParentId = "chassis",
                        ChildId = c.ChildId,
                        Joint = RobotJointKind.Hinge,
                        HingeAxis = Vector3.up
                    });
                    continue;
                }

                conns.Add(c);
            }

            bp.Connections = conns.ToArray();
            return bp;
        }

        /// <summary>Chassis polygon &gt;16 points — must reject.</summary>
        public static RobotBlueprint CreateInvalidChassisPoints(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "InvalidChassisPoints";
            var pts = new Vector2[17];
            for (var i = 0; i < pts.Length; i++)
            {
                var a = i * Mathf.PI * 2f / pts.Length;
                pts[i] = new Vector2(Mathf.Cos(a) * 0.9f, Mathf.Sin(a) * 1.1f);
            }

            var chassis = bp.Chassis;
            chassis.BaseplatePoints = pts;
            bp.Chassis = chassis;
            return bp;
        }

        /// <summary>Mass over Lightweight class cap — must reject.</summary>
        public static RobotBlueprint CreateInvalidOvermass(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "InvalidOvermass";
            for (var i = 0; i < bp.Components.Length; i++)
            {
                if (bp.Components[i].Id != "chassis")
                    continue;
                var c = bp.Components[i];
                c.Mass = 80f;
                bp.Components[i] = c;
                break;
            }

            return bp;
        }

        static void ApplyV1ChassisPowerControls(
            RobotBlueprint bp,
            RobotWeightClass weightClass,
            float ballastMass,
            float ballastZ)
        {
            bp.Chassis = new RobotChassisDef
            {
                BaseplatePoints = new[]
                {
                    new Vector2(-0.8f, -1.1f),
                    new Vector2(0.8f, -1.1f),
                    new Vector2(0.8f, 1.1f),
                    new Vector2(-0.8f, 1.1f)
                },
                Height = 0.7f,
                Armor = RobotArmorType.Aluminum,
                WeightClass = weightClass
            };
            bp.Power = new RobotPowerBudgetDef
            {
                ElectricTotal = 24000f,
                ElectricMaxInOutRate = 400f,
                AirTotal = 0f,
                AirMaxInOutRate = 0f
            };

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components);
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                c.Base = c.ResolvedBase();
                if (c.Id == "chassis")
                {
                    c.Base = RobotComponentBase.Chassis;
                    c.CatalogId = "chassis_alum";
                }
                else if (c.Kind == RobotComponentKind.Wheel)
                {
                    c.Base = RobotComponentBase.Wheel;
                    c.CatalogId = "wheel1";
                }

                components[i] = c;
            }

            components.Add(new RobotComponentDef
            {
                Id = "control_board",
                Kind = RobotComponentKind.Module,
                Base = RobotComponentBase.ControlBoard,
                CatalogId = "controlboard",
                LocalPosition = new Vector3(0f, -0.2f, 0f),
                LocalEuler = Vector3.zero,
                Scale = new Vector3(0.35f, 0.08f, 0.35f),
                Mass = 0.2f,
                HasRigidbody = false,
                IsRoot = false
            });
            components.Add(new RobotComponentDef
            {
                Id = "battery",
                Kind = RobotComponentKind.Module,
                Base = RobotComponentBase.Battery,
                CatalogId = "battery2",
                LocalPosition = new Vector3(0.35f, -0.25f, -0.2f),
                LocalEuler = Vector3.zero,
                Scale = new Vector3(0.25f, 0.12f, 0.35f),
                Mass = 1.5f,
                HasRigidbody = false,
                IsRoot = false,
                ElecMaxInOutRate = 400f
            });

            if (ballastMass > 0f)
            {
                components.Add(new RobotComponentDef
                {
                    Id = "ballast_rear",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Structural,
                    CatalogId = "ballast",
                    LocalPosition = new Vector3(0f, -0.1f, ballastZ),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.5f, 0.2f, 0.35f),
                    Mass = ballastMass,
                    HasRigidbody = false,
                    IsRoot = false
                });
            }

            bp.Components = components.ToArray();

            bp.ControlSlots = new[]
            {
                new RobotControlSlotDef
                {
                    Id = "forward_back",
                    DisplayName = "Forward-Back",
                    Kind = RobotControlKind.Analog,
                    InputBinding = "W/S"
                },
                new RobotControlSlotDef
                {
                    Id = "left_right",
                    DisplayName = "Left-Right",
                    Kind = RobotControlKind.Analog,
                    InputBinding = "A/D"
                }
            };

            bp.Wirings = new[]
            {
                new RobotWiringDef { ControlSlotId = "forward_back", ComponentId = "wheel_fl", Channel = "CW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "forward_back", ComponentId = "wheel_fr", Channel = "CW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "forward_back", ComponentId = "wheel_rl", Channel = "CW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "forward_back", ComponentId = "wheel_rr", Channel = "CW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_fl", Channel = "CCW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_fr", Channel = "CW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_rl", Channel = "CCW", Sign = -1f },
                new RobotWiringDef { ControlSlotId = "left_right", ComponentId = "wheel_rr", Channel = "CW", Sign = -1f }
            };
        }

        /// <summary>Insert spin-motor axles; wheels hinge off motors (construction attachment graph).</summary>
        static void InsertSpinMotorAxles(RobotBlueprint bp)
        {
            var wheelIds = new[] { "wheel_fl", "wheel_fr", "wheel_rl", "wheel_rr" };
            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components);
            var wheelPos = new System.Collections.Generic.Dictionary<string, Vector3>(StringComparer.Ordinal);

            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.Kind != RobotComponentKind.Wheel)
                    continue;
                c.HasRigidbody = true;
                c.Mass = 1.2f;
                c.Base = RobotComponentBase.Wheel;
                components[i] = c;
                wheelPos[c.Id] = c.LocalPosition;
            }

            foreach (var wid in wheelIds)
            {
                if (!wheelPos.TryGetValue(wid, out var pos))
                    continue;
                var mid = "motor_" + wid.Substring("wheel_".Length);
                components.Add(new RobotComponentDef
                {
                    Id = mid,
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.SpinMotor,
                    CatalogId = "ztek",
                    LocalPosition = pos + new Vector3(0f, 0.12f, 0f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.22f, 0.18f, 0.22f),
                    Mass = 0.8f,
                    HasRigidbody = false,
                    IsRoot = false,
                    ElecMaxInOutRate = 50f
                });
            }

            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>();
            for (var i = 0; i < bp.Connections.Length; i++)
            {
                var c = bp.Connections[i];
                if (c.ChildId != null && c.ChildId.StartsWith("wheel_", StringComparison.Ordinal))
                    continue;
                conns.Add(c);
            }

            foreach (var wid in wheelIds)
            {
                var mid = "motor_" + wid.Substring("wheel_".Length);
                conns.Add(new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = mid,
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                });
                conns.Add(new RobotConnectionDef
                {
                    ParentId = mid,
                    ChildId = wid,
                    Joint = RobotJointKind.Hinge,
                    HingeAxis = Vector3.up
                });
            }

            conns.Add(new RobotConnectionDef
            {
                ParentId = "chassis",
                ChildId = "control_board",
                Joint = RobotJointKind.FixedHierarchy,
                HingeAxis = Vector3.zero
            });
            conns.Add(new RobotConnectionDef
            {
                ParentId = "chassis",
                ChildId = "battery",
                Joint = RobotJointKind.FixedHierarchy,
                HingeAxis = Vector3.zero
            });

            for (var i = 0; i < bp.Components.Length; i++)
            {
                if (bp.Components[i].Id != "ballast_rear")
                    continue;
                conns.Add(new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "ballast_rear",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                });
                break;
            }

            bp.Connections = conns.ToArray();
        }

        /// <summary>S7-04: tank + SpinMotor spinner blade wired to Button Fire (continuous CW while held).</summary>
        public static RobotBlueprint CreateRa2SpinnerFireSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2SpinnerFireSample";

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "spinner_motor",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.SpinMotor,
                    CatalogId = "spinner_ztek",
                    LocalPosition = new Vector3(0f, 0.35f, 0.85f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.28f, 0.2f, 0.28f),
                    Mass = 1.2f,
                    HasRigidbody = true,
                    IsRoot = false,
                    ElecMaxInOutRate = 60f
                },
                new RobotComponentDef
                {
                    Id = "spinner_blade",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Weapon,
                    CatalogId = "blade_thin",
                    LocalPosition = new Vector3(0.55f, 0f, 0f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(1.1f, 0.08f, 0.18f),
                    Mass = 0.6f,
                    HasRigidbody = false,
                    IsRoot = false,
                    Concussion = 0.5f,
                    Piercing = 0.2f
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "spinner_motor",
                    Joint = RobotJointKind.Hinge,
                    HingeAxis = Vector3.up
                },
                new RobotConnectionDef
                {
                    ParentId = "spinner_motor",
                    ChildId = "spinner_blade",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                }
            };
            bp.Connections = conns.ToArray();

            AppendDigitalSlot(bp, "fire_spin", "Spin Fire", RobotControlKind.Button, "Space");
            AppendWiring(bp, "fire_spin", "spinner_motor", "CW", 1f);
            return bp;
        }

        /// <summary>S7-05: tank + AirTank + BurstPiston Fire (air budget).</summary>
        public static RobotBlueprint CreateRa2BurstPistonFireSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2BurstPistonFireSample";
            bp.Power = new RobotPowerBudgetDef
            {
                ElectricTotal = bp.Power.ElectricTotal,
                ElectricMaxInOutRate = bp.Power.ElectricMaxInOutRate,
                AirTotal = 800f,
                AirMaxInOutRate = 120f
            };

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "air_tank",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.AirTank,
                    CatalogId = "airtank",
                    LocalPosition = new Vector3(-0.35f, -0.2f, -0.15f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.3f, 0.2f, 0.4f),
                    Mass = 1.5f,
                    HasRigidbody = false,
                    IsRoot = false,
                    AirMaxInOutRate = 120f
                },
                new RobotComponentDef
                {
                    Id = "burst_piston",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.BurstPiston,
                    CatalogId = "burstpiston",
                    LocalPosition = new Vector3(0f, 0.15f, 1.15f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.22f, 0.22f, 0.55f),
                    Mass = 1.4f,
                    HasRigidbody = true,
                    IsRoot = false,
                    AirMaxInOutRate = -80f
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "air_tank",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                },
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "burst_piston",
                    Joint = RobotJointKind.Slider,
                    HingeAxis = Vector3.forward
                }
            };
            bp.Connections = conns.ToArray();

            AppendDigitalSlot(bp, "fire_piston", "Piston Fire", RobotControlKind.Button, "F");
            AppendWiring(bp, "fire_piston", "burst_piston", "Fire", 1f);
            return bp;
        }

        /// <summary>S7-06: tank + BurstMotor Fire arc (&lt;180°) on Button.</summary>
        public static RobotBlueprint CreateRa2BurstMotorFireSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2BurstMotorFireSample";

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "burst_motor",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.BurstMotor,
                    CatalogId = "burstmotor",
                    LocalPosition = new Vector3(0f, 0.25f, 0.7f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.25f, 0.2f, 0.25f),
                    Mass = 1.3f,
                    HasRigidbody = true,
                    IsRoot = false,
                    ElecMaxInOutRate = 70f
                },
                new RobotComponentDef
                {
                    Id = "flipper_pad",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Weapon,
                    CatalogId = "flipper_pad",
                    LocalPosition = new Vector3(0f, 0f, 0.55f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.7f, 0.1f, 0.35f),
                    Mass = 0.8f,
                    HasRigidbody = false,
                    IsRoot = false,
                    Concussion = 0.8f,
                    Piercing = 0.1f
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "burst_motor",
                    Joint = RobotJointKind.Hinge,
                    HingeAxis = Vector3.right
                },
                new RobotConnectionDef
                {
                    ParentId = "burst_motor",
                    ChildId = "flipper_pad",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                }
            };
            bp.Connections = conns.ToArray();

            AppendDigitalSlot(bp, "fire_burst", "Burst Fire", RobotControlKind.Button, "Mouse0");
            AppendWiring(bp, "fire_burst", "burst_motor", "Fire", 1f);
            return bp;
        }

        /// <summary>S7-07: tank + ServoMotor Analog slow rotate + lock at stop.</summary>
        public static RobotBlueprint CreateRa2ServoMotorAnalogSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2ServoMotorAnalogSample";

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "servo_motor",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.ServoMotor,
                    CatalogId = "servomotor",
                    LocalPosition = new Vector3(0f, 0.3f, 0.75f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.24f, 0.2f, 0.24f),
                    Mass = 1.1f,
                    HasRigidbody = true,
                    IsRoot = false,
                    ElecMaxInOutRate = 40f
                },
                new RobotComponentDef
                {
                    Id = "servo_arm",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Weapon,
                    CatalogId = "servo_arm",
                    LocalPosition = new Vector3(0f, 0f, 0.5f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.15f, 0.08f, 0.9f),
                    Mass = 0.5f,
                    HasRigidbody = false,
                    IsRoot = false,
                    Concussion = 0.3f,
                    Piercing = 0.1f
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "servo_motor",
                    Joint = RobotJointKind.Hinge,
                    HingeAxis = Vector3.up
                },
                new RobotConnectionDef
                {
                    ParentId = "servo_motor",
                    ChildId = "servo_arm",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                }
            };
            bp.Connections = conns.ToArray();

            AppendAnalogSlot(bp, "servo_aim", "Servo Aim", "AxisServo");
            AppendWiring(bp, "servo_aim", "servo_motor", "CW", 1f);
            return bp;
        }

        /// <summary>S7-08: tank + AirTank + ServoPiston Analog Extend/Retract (air).</summary>
        public static RobotBlueprint CreateRa2ServoPistonAnalogSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2ServoPistonAnalogSample";
            bp.Power = new RobotPowerBudgetDef
            {
                ElectricTotal = bp.Power.ElectricTotal,
                ElectricMaxInOutRate = bp.Power.ElectricMaxInOutRate,
                AirTotal = 600f,
                AirMaxInOutRate = 100f
            };

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "air_tank",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.AirTank,
                    CatalogId = "airtank",
                    LocalPosition = new Vector3(-0.35f, -0.2f, -0.15f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.3f, 0.2f, 0.4f),
                    Mass = 1.5f,
                    HasRigidbody = false,
                    IsRoot = false,
                    AirMaxInOutRate = 100f
                },
                new RobotComponentDef
                {
                    Id = "servo_piston",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.ServoPiston,
                    CatalogId = "servopiston",
                    LocalPosition = new Vector3(0f, 0.15f, 1.1f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.2f, 0.2f, 0.5f),
                    Mass = 1.2f,
                    HasRigidbody = true,
                    IsRoot = false,
                    AirMaxInOutRate = -40f
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "air_tank",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                },
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "servo_piston",
                    Joint = RobotJointKind.Slider,
                    HingeAxis = Vector3.forward
                }
            };
            bp.Connections = conns.ToArray();

            AppendAnalogSlot(bp, "piston_stroke", "Piston Stroke", "AxisPiston");
            AppendWiring(bp, "piston_stroke", "servo_piston", "Extend", 1f);
            return bp;
        }

        /// <summary>S7-09: BurstMotor Fire + SmartZone contact optional auto-Fire.</summary>
        public static RobotBlueprint CreateRa2SmartZoneFireSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2BurstMotorFireSample(rootPosition, yawDegrees);
            bp.Name = "Ra2SmartZoneFireSample";

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "smart_zone",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.SmartZone,
                    CatalogId = "smartzone",
                    LocalPosition = new Vector3(0f, 0.2f, 1.35f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(1.2f, 0.6f, 0.9f),
                    Mass = 0.01f,
                    HasRigidbody = false,
                    IsRoot = false
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "smart_zone",
                    Joint = RobotJointKind.FixedHierarchy,
                    HingeAxis = Vector3.zero
                }
            };
            bp.Connections = conns.ToArray();

            // Optional zone trigger: ControlSlotId names the SmartZone component (not a Button).
            AppendWiring(bp, "smart_zone", "burst_motor", "Fire", 1f);
            return bp;
        }

        /// <summary>S7-10: tank + Steering hub Analog (Turn / left_right) ±35° + lock at center.</summary>
        public static RobotBlueprint CreateRa2SteeringHubSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2SteeringHubSample";

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "steer_hub",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Steering,
                    CatalogId = "steering_hub",
                    LocalPosition = new Vector3(0.55f, -0.15f, 0.85f),
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.2f, 0.25f, 0.2f),
                    Mass = 0.9f,
                    HasRigidbody = true,
                    IsRoot = false,
                    ElecMaxInOutRate = 25f
                }
            };
            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>(bp.Connections)
            {
                new RobotConnectionDef
                {
                    ParentId = "chassis",
                    ChildId = "steer_hub",
                    Joint = RobotJointKind.Hinge,
                    HingeAxis = Vector3.up
                }
            };
            bp.Connections = conns.ToArray();

            AppendWiring(bp, "left_right", "steer_hub", "CW", 1f);
            return bp;
        }

        /// <summary>S7-11: BurstMotor Fire with tight electric budget (draw + deny).</summary>
        public static RobotBlueprint CreateRa2BurstMotorElectricSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2BurstMotorFireSample(rootPosition, yawDegrees);
            bp.Name = "Ra2BurstMotorElectricSample";
            bp.Power = new RobotPowerBudgetDef
            {
                ElectricTotal = 100f,
                ElectricMaxInOutRate = bp.Power.ElectricMaxInOutRate,
                AirTotal = bp.Power.AirTotal,
                AirMaxInOutRate = bp.Power.AirMaxInOutRate
            };
            return bp;
        }

        /// <summary>S7-12: BurstPiston with small air tank + fast AirMaxInOutRate refill.</summary>
        public static RobotBlueprint CreateRa2AirRechargeSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2BurstPistonFireSample(rootPosition, yawDegrees);
            bp.Name = "Ra2AirRechargeSample";
            bp.Power = new RobotPowerBudgetDef
            {
                ElectricTotal = bp.Power.ElectricTotal,
                ElectricMaxInOutRate = bp.Power.ElectricMaxInOutRate,
                AirTotal = 100f,
                AirMaxInOutRate = 250f
            };

            if (bp.Components != null)
            {
                for (var i = 0; i < bp.Components.Length; i++)
                {
                    if (bp.Components[i].ResolvedBase() != RobotComponentBase.AirTank)
                        continue;
                    var tank = bp.Components[i];
                    tank.AirMaxInOutRate = 250f;
                    bp.Components[i] = tank;
                }
            }

            return bp;
        }

        /// <summary>S7-13: BurstMotor with small electric budget + fast ElectricMaxInOutRate refill.</summary>
        public static RobotBlueprint CreateRa2ElectricRechargeSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2BurstMotorElectricSample(rootPosition, yawDegrees);
            bp.Name = "Ra2ElectricRechargeSample";
            bp.Power = new RobotPowerBudgetDef
            {
                ElectricTotal = 100f,
                ElectricMaxInOutRate = 250f,
                AirTotal = bp.Power.AirTotal,
                AirMaxInOutRate = bp.Power.AirMaxInOutRate
            };

            if (bp.Components != null)
            {
                for (var i = 0; i < bp.Components.Length; i++)
                {
                    if (bp.Components[i].ResolvedBase() != RobotComponentBase.Battery)
                        continue;
                    var bat = bp.Components[i];
                    bat.ElecMaxInOutRate = 250f;
                    bp.Components[i] = bat;
                }
            }

            return bp;
        }

        /// <summary>
        /// S7-14: front Ackermann — Steering hubs on chassis, SpinMotor+Wheel under each hub (admit OK).
        /// Turn drives opposite hub angles; Move still powers wheel hinges.
        /// </summary>
        public static RobotBlueprint CreateRa2AckermannSteerSample(Vector3 rootPosition, float yawDegrees)
        {
            var bp = CreateRa2ConstructionSampleA(rootPosition, yawDegrees);
            bp.Name = "Ra2AckermannSteerSample";

            Vector3 posFl = new Vector3(-0.85f, 0.05f, 0.95f);
            Vector3 posFr = new Vector3(0.85f, 0.05f, 0.95f);
            for (var i = 0; i < bp.Components.Length; i++)
            {
                if (bp.Components[i].Id == "motor_fl")
                    posFl = bp.Components[i].LocalPosition;
                else if (bp.Components[i].Id == "motor_fr")
                    posFr = bp.Components[i].LocalPosition;
            }

            var components = new System.Collections.Generic.List<RobotComponentDef>(bp.Components)
            {
                new RobotComponentDef
                {
                    Id = "steer_fl",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Steering,
                    CatalogId = "steering_hub",
                    LocalPosition = posFl,
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.18f, 0.2f, 0.18f),
                    Mass = 0.7f,
                    HasRigidbody = true,
                    IsRoot = false,
                    ElecMaxInOutRate = 20f
                },
                new RobotComponentDef
                {
                    Id = "steer_fr",
                    Kind = RobotComponentKind.Module,
                    Base = RobotComponentBase.Steering,
                    CatalogId = "steering_hub",
                    LocalPosition = posFr,
                    LocalEuler = Vector3.zero,
                    Scale = new Vector3(0.18f, 0.2f, 0.18f),
                    Mass = 0.7f,
                    HasRigidbody = true,
                    IsRoot = false,
                    ElecMaxInOutRate = 20f
                }
            };

            // Motors sit as FixedHierarchy children under steer hubs (local offset).
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.Id == "motor_fl" || c.Id == "motor_fr")
                {
                    c.LocalPosition = new Vector3(0f, -0.08f, 0f);
                    components[i] = c;
                }
            }

            bp.Components = components.ToArray();

            var conns = new System.Collections.Generic.List<RobotConnectionDef>();
            for (var i = 0; i < bp.Connections.Length; i++)
            {
                var c = bp.Connections[i];
                // Drop chassis→front motor; keep motor→wheel and everything else.
                if (c.ParentId == "chassis" && (c.ChildId == "motor_fl" || c.ChildId == "motor_fr"))
                    continue;
                conns.Add(c);
            }

            conns.Add(new RobotConnectionDef
            {
                ParentId = "chassis",
                ChildId = "steer_fl",
                Joint = RobotJointKind.Hinge,
                HingeAxis = Vector3.up
            });
            conns.Add(new RobotConnectionDef
            {
                ParentId = "chassis",
                ChildId = "steer_fr",
                Joint = RobotJointKind.Hinge,
                HingeAxis = Vector3.up
            });
            conns.Add(new RobotConnectionDef
            {
                ParentId = "steer_fl",
                ChildId = "motor_fl",
                Joint = RobotJointKind.FixedHierarchy,
                HingeAxis = Vector3.zero
            });
            conns.Add(new RobotConnectionDef
            {
                ParentId = "steer_fr",
                ChildId = "motor_fr",
                Joint = RobotJointKind.FixedHierarchy,
                HingeAxis = Vector3.zero
            });
            bp.Connections = conns.ToArray();

            // Turn only to steers (opposite); keep forward_back on wheels; drop wheel left_right.
            var wirings = new System.Collections.Generic.List<RobotWiringDef>();
            if (bp.Wirings != null)
            {
                for (var i = 0; i < bp.Wirings.Length; i++)
                {
                    var w = bp.Wirings[i];
                    if (string.Equals(w.ControlSlotId, "left_right", System.StringComparison.Ordinal))
                        continue;
                    wirings.Add(w);
                }
            }

            wirings.Add(new RobotWiringDef
            {
                ControlSlotId = "left_right",
                ComponentId = "steer_fl",
                Channel = "CW",
                Sign = 1f
            });
            wirings.Add(new RobotWiringDef
            {
                ControlSlotId = "left_right",
                ComponentId = "steer_fr",
                Channel = "CCW",
                Sign = 1f
            });
            bp.Wirings = wirings.ToArray();
            return bp;
        }

        static void AppendDigitalSlot(
            RobotBlueprint bp,
            string id,
            string displayName,
            RobotControlKind kind,
            string binding)
        {
            var slots = new System.Collections.Generic.List<RobotControlSlotDef>(
                bp.ControlSlots ?? System.Array.Empty<RobotControlSlotDef>())
            {
                new RobotControlSlotDef
                {
                    Id = id,
                    DisplayName = displayName,
                    Kind = kind,
                    InputBinding = binding
                }
            };
            bp.ControlSlots = slots.ToArray();
        }

        static void AppendAnalogSlot(RobotBlueprint bp, string id, string displayName, string binding)
        {
            AppendDigitalSlot(bp, id, displayName, RobotControlKind.Analog, binding);
        }

        static void AppendWiring(
            RobotBlueprint bp,
            string slotId,
            string componentId,
            string channel,
            float sign)
        {
            var wires = new System.Collections.Generic.List<RobotWiringDef>(
                bp.Wirings ?? System.Array.Empty<RobotWiringDef>())
            {
                new RobotWiringDef
                {
                    ControlSlotId = slotId,
                    ComponentId = componentId,
                    Channel = channel,
                    Sign = sign
                }
            };
            bp.Wirings = wires.ToArray();
        }
    }
}
