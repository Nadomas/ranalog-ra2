using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot builder for Assets/Scenes/PhysicsTest.unity (STAGE-1 smoke / drive / wheel joint + S2-01..08 + S3).
/// Menu: Tools/RA2/Build PhysicsTest Scene (S1-03 Wheel Joint, default)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S1-02 Drive)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S1-01 Collision Smoke)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-01 Dual Authority)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-02 Loopback Transport)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-03 Illegal Client Force)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-04 Latency Harness)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-05 Dedicated Tick Smoke)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-06 Cross-Process UDP)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-08 Dedicated Build Smoke)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S2-09 Modular Cross-Process UDP)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-01 Modular Assembly)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-02 Blueprint Serialize)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-03 Net Spawn)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-04 Blueprint V1 RA2)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-05 Net Spawn V1)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-06 Lifecycle)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S3-07 Motor Torque)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S4-01 Construction Validation)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S5-01 Configure Wiring)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S6-01 Seamless Loop)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S8-01 Combat Immobility)
/// Menu: Tools/RA2/Build PhysicsTest Scene (S7-02 Weapon Hit)
/// </summary>
public static class PhysicsTestSceneBuilder
{
    const string ScenePath = "Assets/Scenes/PhysicsTest.unity";

    enum Scenario
    {
        Drive,
        CollisionSmoke,
        WheelJoint,
        DualAuthority,
        LoopbackTransport,
        IllegalClientForce,
        LatencyHarness,
        DedicatedTickSmoke,
        CrossProcessUdp,
        DedicatedBuildSmoke,
        ModularCrossProcessUdp,
        ModularAssembly,
        BlueprintSerialize,
        BlueprintV1Ra2,
        NetSpawn,
        NetSpawnV1,
        Lifecycle,
        MotorTorque,
        ConstructionValidation,
        ConfigureWiring,
        SeamlessLoop,
        CombatImmobility,
        WeaponHit,
        MatchUdpLobby,
        MvpLoopGlue,
        DisconnectPolicy,
        ResultsPersist,
        WorkshopChrome,
        ReadyLobby
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene")]
    public static void BuildFromMenu()
    {
        Build(Scenario.WheelJoint);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S1-03 Wheel Joint)")]
    public static void BuildWheelJointFromMenu()
    {
        Build(Scenario.WheelJoint);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S1-02 Drive)")]
    public static void BuildDriveFromMenu()
    {
        Build(Scenario.Drive);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S1-01 Collision Smoke)")]
    public static void BuildCollisionSmokeFromMenu()
    {
        Build(Scenario.CollisionSmoke);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-01 Dual Authority)")]
    public static void BuildDualAuthorityFromMenu()
    {
        Build(Scenario.DualAuthority);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-02 Loopback Transport)")]
    public static void BuildLoopbackTransportFromMenu()
    {
        Build(Scenario.LoopbackTransport);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-03 Illegal Client Force)")]
    public static void BuildIllegalClientForceFromMenu()
    {
        Build(Scenario.IllegalClientForce);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-04 Latency Harness)")]
    public static void BuildLatencyHarnessFromMenu()
    {
        Build(Scenario.LatencyHarness);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-05 Dedicated Tick Smoke)")]
    public static void BuildDedicatedTickSmokeFromMenu()
    {
        Build(Scenario.DedicatedTickSmoke);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-06 Cross-Process UDP)")]
    public static void BuildCrossProcessUdpFromMenu()
    {
        Build(Scenario.CrossProcessUdp);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-08 Dedicated Build Smoke)")]
    public static void BuildDedicatedBuildSmokeFromMenu()
    {
        Build(Scenario.DedicatedBuildSmoke);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S2-09 Modular Cross-Process UDP)")]
    public static void BuildModularCrossProcessUdpFromMenu()
    {
        Build(Scenario.ModularCrossProcessUdp);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-01 Modular Assembly)")]
    public static void BuildModularAssemblyFromMenu()
    {
        Build(Scenario.ModularAssembly);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-02 Blueprint Serialize)")]
    public static void BuildBlueprintSerializeFromMenu()
    {
        Build(Scenario.BlueprintSerialize);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-03 Net Spawn)")]
    public static void BuildNetSpawnFromMenu()
    {
        Build(Scenario.NetSpawn);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-04 Blueprint V1 RA2)")]
    public static void BuildBlueprintV1FromMenu()
    {
        Build(Scenario.BlueprintV1Ra2);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-05 Net Spawn V1)")]
    public static void BuildNetSpawnV1FromMenu()
    {
        Build(Scenario.NetSpawnV1);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-06 Lifecycle)")]
    public static void BuildLifecycleFromMenu()
    {
        Build(Scenario.Lifecycle);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S3-07 Motor Torque)")]
    public static void BuildMotorTorqueFromMenu()
    {
        Build(Scenario.MotorTorque);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S4-01 Construction Validation)")]
    public static void BuildConstructionValidationFromMenu()
    {
        Build(Scenario.ConstructionValidation);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S5-01 Configure Wiring)")]
    public static void BuildConfigureWiringFromMenu()
    {
        Build(Scenario.ConfigureWiring);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S6-01 Seamless Loop)")]
    public static void BuildSeamlessLoopFromMenu()
    {
        Build(Scenario.SeamlessLoop);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S8-01 Combat Immobility)")]
    public static void BuildCombatImmobilityFromMenu()
    {
        Build(Scenario.CombatImmobility);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S7-02 Weapon Hit)")]
    public static void BuildWeaponHitFromMenu()
    {
        Build(Scenario.WeaponHit);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S9-01 Match UDP Lobby)")]
    public static void BuildMatchUdpLobbyFromMenu()
    {
        Build(Scenario.MatchUdpLobby);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S10-01 Results Stub)")]
    public static void BuildResultsStubFromMenu()
    {
        // Results are verified inside S9-01 MatchOutcome delivery path.
        Build(Scenario.MatchUdpLobby);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S11 MVP Loop Glue)")]
    public static void BuildMvpLoopGlueFromMenu()
    {
        Build(Scenario.MvpLoopGlue);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S9-02 Disconnect Policy)")]
    public static void BuildDisconnectPolicyFromMenu()
    {
        Build(Scenario.DisconnectPolicy);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S10-02 Results Persist)")]
    public static void BuildResultsPersistFromMenu()
    {
        Build(Scenario.ResultsPersist);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S11 Workshop Chrome)")]
    public static void BuildWorkshopChromeFromMenu()
    {
        Build(Scenario.WorkshopChrome);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Scene (S9-03 Ready Lobby)")]
    public static void BuildReadyLobbyFromMenu()
    {
        Build(Scenario.ReadyLobby);
    }

    [MenuItem("Tools/RA2/Force Script Compile")]
    public static void ForceScriptCompile()
    {
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestDrive.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestLatencyVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestDedicatedTickVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestLoopbackTransport.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestUdpTransport.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestCombatAuthority.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestCrossProcessHostVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/PhysicsTest/PhysicsTestDedicatedBuildVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotAssembler.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotAssemblyVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotBlueprintSerializer.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotBlueprintSerializeVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotSpawnService.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotHostSpawner.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotHostSpawnerUdp.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotNetSpawnVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotModularUdpHostVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotModularUdpClientVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotBlueprintV1Verifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotNetSpawnV1Verifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotLifecycleVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotMotorDrive.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotMotorTorqueVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotMassProperties.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotBlueprintValidator.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotConstructionValidatorVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotControlConfigurer.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotConfigureVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotWorkshopSession.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotSeamlessLoopVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotDamageService.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/ImmobilityWinEvaluator.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotCombatImmobilityVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotWeaponHitVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/MatchSummary.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/MatchResultsStub.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotMatchUdpVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotMvpLoopVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/MatchReadyLobbyFlow.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotReadyLobbyChrome.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(
            "Assets/Runtime/Robot/RobotReadyLobbyVerifier.cs",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        Debug.Log("[PhysicsTestSceneBuilder] Requested script compilation.");
    }

    public static void Build()
    {
        Build(Scenario.WheelJoint);
    }

    static void Build(Scenario scenario)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera();
        CreateDirectionalLight();

        var arena = new GameObject("Arena");
        var slide = GetOrCreateSlideMaterial();
        var floorMat = UsesGripFloor(scenario) ? GetOrCreateGripFloorMaterial() : slide;
        CreateFloor(arena.transform, floorMat);
        CreateWalls(arena.transform, slide);

        if (scenario == Scenario.ModularAssembly)
        {
            BuildModularAssemblyScenario(slide);
        }
        else if (scenario == Scenario.ModularCrossProcessUdp)
        {
            BuildModularCrossProcessUdpScenario(slide);
        }
        else if (scenario == Scenario.BlueprintSerialize)
        {
            BuildBlueprintSerializeScenario(slide);
        }
        else if (scenario == Scenario.BlueprintV1Ra2)
        {
            BuildBlueprintV1Scenario(floorMat);
        }
        else if (scenario == Scenario.NetSpawn)
        {
            BuildNetSpawnScenario(slide);
        }
        else if (scenario == Scenario.NetSpawnV1)
        {
            BuildNetSpawnV1Scenario(floorMat);
        }
        else if (scenario == Scenario.Lifecycle)
        {
            BuildLifecycleScenario(floorMat);
        }
        else if (scenario == Scenario.MotorTorque)
        {
            BuildMotorTorqueScenario(floorMat);
        }
        else if (scenario == Scenario.ConstructionValidation)
        {
            BuildConstructionValidationScenario(floorMat);
        }
        else if (scenario == Scenario.ConfigureWiring)
        {
            BuildConfigureWiringScenario(floorMat);
        }
        else if (scenario == Scenario.SeamlessLoop)
        {
            BuildSeamlessLoopScenario(floorMat);
        }
        else if (scenario == Scenario.CombatImmobility)
        {
            BuildCombatImmobilityScenario(floorMat);
        }
        else if (scenario == Scenario.WeaponHit)
        {
            BuildWeaponHitScenario(floorMat);
        }
        else if (scenario == Scenario.MatchUdpLobby)
        {
            BuildMatchUdpLobbyScenario(floorMat);
        }
        else if (scenario == Scenario.MvpLoopGlue)
        {
            BuildMvpLoopGlueScenario(floorMat);
        }
        else if (scenario == Scenario.DisconnectPolicy)
        {
            BuildDisconnectPolicyScenario(floorMat);
        }
        else if (scenario == Scenario.ResultsPersist)
        {
            BuildResultsPersistScenario();
        }
        else if (scenario == Scenario.WorkshopChrome)
        {
            BuildWorkshopChromeScenario(floorMat);
        }
        else if (scenario == Scenario.ReadyLobby)
        {
            BuildReadyLobbyScenario();
        }
        else if (scenario == Scenario.CollisionSmoke)
        {
            // Nose is local +Z; yaw so they face each other along world ±X and drive head-on.
            CreateRobot(
                "Robot_A",
                new Vector3(-4f, 0.75f, 0f),
                90f,
                new Color(0.2f, 0.55f, 1f),
                slide,
                Scenario.CollisionSmoke,
                new Vector3(8f, 0f, 0f));
            CreateRobot(
                "Robot_B",
                new Vector3(4f, 0.75f, 0f),
                -90f,
                new Color(1f, 0.35f, 0.2f),
                slide,
                Scenario.CollisionSmoke,
                new Vector3(-8f, 0f, 0f));
        }
        else if (scenario == Scenario.DualAuthority || scenario == Scenario.LoopbackTransport ||
                 scenario == Scenario.IllegalClientForce || scenario == Scenario.LatencyHarness ||
                 scenario == Scenario.DedicatedTickSmoke || scenario == Scenario.CrossProcessUdp ||
                 scenario == Scenario.DedicatedBuildSmoke)
        {
            // S2-01..08: both robots driven only via host authority (S1-03 hinge on A).
            var robotA = CreateRobot(
                "Robot_A",
                new Vector3(-4f, 0.75f, 0f),
                90f,
                new Color(0.2f, 0.55f, 1f),
                slide,
                scenario,
                Vector3.zero);
            var robotB = CreateRobot(
                "Robot_B",
                new Vector3(4f, 0.75f, 0f),
                -90f,
                new Color(1f, 0.35f, 0.2f),
                slide,
                scenario,
                Vector3.zero);

            if (scenario == Scenario.CrossProcessUdp || scenario == Scenario.DedicatedBuildSmoke)
            {
                BuildCrossProcessOrDedicated(scenario, robotA, robotB);
            }
            else
            {
            var useLoopback = scenario == Scenario.LoopbackTransport ||
                              scenario == Scenario.IllegalClientForce ||
                              scenario == Scenario.LatencyHarness ||
                              scenario == Scenario.DedicatedTickSmoke;
            var hostGo = new GameObject(useLoopback ? "ListenHost" : "LocalAuthority");
            var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
            var driveA = robotA.GetComponent<PhysicsTestDrive>();
            var driveB = robotB.GetComponent<PhysicsTestDrive>();
            authority.ConfigureBindings(new[]
            {
                new PhysicsTestLocalAuthority.RobotBinding { RobotId = 0, AllowedSourceId = 0, Drive = driveA },
                new PhysicsTestLocalAuthority.RobotBinding { RobotId = 1, AllowedSourceId = 1, Drive = driveB }
            });

            if (useLoopback)
            {
                // S2-02..05: commands cross loopback transport before host authority applies them.
                var transport = hostGo.AddComponent<PhysicsTestLoopbackTransport>();
                var transportHost = hostGo.AddComponent<PhysicsTestTransportHost>();
                transportHost.Configure(
                    transport,
                    authority,
                    new[]
                    {
                        new PhysicsTestTransportHost.TrackedRobot { RobotId = 0, Drive = driveA },
                        new PhysicsTestTransportHost.TrackedRobot { RobotId = 1, Drive = driveB }
                    });

                var clientGo = new GameObject("LoopbackClient");
                var transportClient = clientGo.AddComponent<PhysicsTestTransportClient>();
                transportClient.Configure(transport);

                var srcA = robotA.GetComponent<PhysicsTestCommandSource>();
                srcA.Configure(0, 0, PhysicsTestCommandSource.KeyScheme.WasdSpace, authority, transportClient);
                var srcB = robotB.GetComponent<PhysicsTestCommandSource>();
                srcB.Configure(1, 1, PhysicsTestCommandSource.KeyScheme.ArrowsShift, authority, transportClient);

                if (scenario == Scenario.IllegalClientForce)
                {
                    var stateAuth = hostGo.AddComponent<PhysicsTestHostStateAuthority>();
                    stateAuth.Configure(new[]
                    {
                        new PhysicsTestHostStateAuthority.TrackedRobot { RobotId = 0, Drive = driveA },
                        new PhysicsTestHostStateAuthority.TrackedRobot { RobotId = 1, Drive = driveB }
                    });

                    var verifier = hostGo.AddComponent<PhysicsTestIllegalForceVerifier>();
                    verifier.Configure(
                        transport, transportClient, transportHost, authority, stateAuth, driveA, driveB);
                    verifier.AutoRun = true;
                }
                else if (scenario == Scenario.LatencyHarness)
                {
                    var verifier = hostGo.AddComponent<PhysicsTestLatencyVerifier>();
                    verifier.Configure(transport, transportClient, transportHost, authority, driveA, driveB);
                    verifier.AutoRun = true;
                }
                else if (scenario == Scenario.DedicatedTickSmoke)
                {
                    var verifier = hostGo.AddComponent<PhysicsTestDedicatedTickVerifier>();
                    verifier.Configure(transport, transportClient, transportHost, authority, driveA, driveB);
                    verifier.AutoRun = true;
                }
                else
                {
                    var verifier = hostGo.AddComponent<PhysicsTestTransportVerifier>();
                    verifier.Configure(transport, transportClient, transportHost, authority, driveA, driveB);
                    verifier.AutoRun = true; // experiment spike default; disable for manual drive
                }
            }
            else
            {
                var srcA = robotA.GetComponent<PhysicsTestCommandSource>();
                srcA.Configure(0, 0, PhysicsTestCommandSource.KeyScheme.WasdSpace, authority);
                var srcB = robotB.GetComponent<PhysicsTestCommandSource>();
                srcB.Configure(1, 1, PhysicsTestCommandSource.KeyScheme.ArrowsShift, authority);

                var verifier = hostGo.AddComponent<PhysicsTestDualAuthorityVerifier>();
                verifier.Configure(authority, driveA, driveB);
            }
            } // end non-cross-process
        }
        else
        {
            // S1-02 / S1-03: Robot_A player-driven; Robot_B passive obstacle.
            CreateRobot(
                "Robot_A",
                new Vector3(-4f, 0.75f, 0f),
                90f,
                new Color(0.2f, 0.55f, 1f),
                slide,
                scenario,
                Vector3.zero);
            CreateRobot(
                "Robot_B",
                new Vector3(4f, 0.75f, 0f),
                -90f,
                new Color(1f, 0.35f, 0.2f),
                slide,
                Scenario.Drive,
                Vector3.zero);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PhysicsTestSceneBuilder] Saved {ScenePath} scenario={scenario}");
    }

    static void BuildCrossProcessOrDedicated(Scenario scenario, GameObject robotA, GameObject robotB)
    {
        var driveA = robotA.GetComponent<PhysicsTestDrive>();
        var driveB = robotB.GetComponent<PhysicsTestDrive>();
        var flagA = robotA.GetComponent<PhysicsTestDisableFlag>();
        var flagB = robotB.GetComponent<PhysicsTestDisableFlag>();
        if (flagA == null) flagA = robotA.AddComponent<PhysicsTestDisableFlag>();
        if (flagB == null) flagB = robotB.AddComponent<PhysicsTestDisableFlag>();

        var hostGo = new GameObject("ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(new[]
        {
            new PhysicsTestLocalAuthority.RobotBinding { RobotId = 0, AllowedSourceId = 0, Drive = driveA },
            new PhysicsTestLocalAuthority.RobotBinding { RobotId = 1, AllowedSourceId = 1, Drive = driveB }
        });

        var udp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        var defaultRole = scenario == Scenario.DedicatedBuildSmoke
            ? PhysicsTestNetRole.Dedicated
            : PhysicsTestNetRole.Host;
        udp.Configure(defaultRole, "127.0.0.1", 7777);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(
            udp,
            authority,
            new[]
            {
                new PhysicsTestUdpHost.TrackedRobot { RobotId = 0, Drive = driveA },
                new PhysicsTestUdpHost.TrackedRobot { RobotId = 1, Drive = driveB }
            });

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(
            udp,
            new[]
            {
                new PhysicsTestCombatAuthority.TrackedRobot
                {
                    RobotId = 0, AllowedSourceId = 0, Drive = driveA, DisableFlag = flagA
                },
                new PhysicsTestCombatAuthority.TrackedRobot
                {
                    RobotId = 1, AllowedSourceId = 1, Drive = driveB, DisableFlag = flagB
                }
            },
            1);

        var bootstrap = hostGo.AddComponent<PhysicsTestNetRoleBootstrap>();
        bootstrap.ConfigureDefaults(defaultRole, "127.0.0.1", 7777);

        var clientGo = new GameObject("UdpClient");
        var udpClient = clientGo.AddComponent<PhysicsTestUdpClient>();
        udpClient.Configure(udp);

        // Keyboard still available for manual host-side play; auto verifiers disable sources.
        var srcA = robotA.GetComponent<PhysicsTestCommandSource>();
        srcA.Configure(0, 0, PhysicsTestCommandSource.KeyScheme.WasdSpace, authority);
        var srcB = robotB.GetComponent<PhysicsTestCommandSource>();
        srcB.Configure(1, 1, PhysicsTestCommandSource.KeyScheme.ArrowsShift, authority);

        if (scenario == Scenario.DedicatedBuildSmoke)
        {
            var verifier = hostGo.AddComponent<PhysicsTestDedicatedBuildVerifier>();
            verifier.Configure(udp, udpHost, authority, bootstrap, driveA, driveB);
            verifier.AutoRun = true;
        }
        else
        {
            var hostVerifier = hostGo.AddComponent<PhysicsTestCrossProcessHostVerifier>();
            hostVerifier.Configure(udp, udpHost, authority, combat, bootstrap, driveA, driveB, flagB);
            hostVerifier.AutoRun = true;

            var clientVerifier = clientGo.AddComponent<PhysicsTestCrossProcessClientVerifier>();
            clientVerifier.Configure(udp, udpClient, bootstrap);
            clientVerifier.AutoRun = true;
        }

        Debug.Log($"[PhysicsTestSceneBuilder] S2 cross-process/dedicated scaffold role={defaultRole}");
    }

    static void BuildModularCrossProcessUdpScenario(PhysicsMaterial slide)
    {
        // Empty arena: robots appear only after host validates blueprint spawn requests over UDP (S2-09).
        var hostGo = new GameObject("ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());

        var udp = hostGo.AddComponent<PhysicsTestUdpTransport>();
        udp.Configure(PhysicsTestNetRole.Host, "127.0.0.1", 7777);

        var udpHost = hostGo.AddComponent<PhysicsTestUdpHost>();
        udpHost.Configure(udp, authority, System.Array.Empty<PhysicsTestUdpHost.TrackedRobot>());

        var combat = hostGo.AddComponent<PhysicsTestCombatAuthority>();
        combat.Configure(udp, System.Array.Empty<PhysicsTestCombatAuthority.TrackedRobot>(), 1);

        var spawner = hostGo.AddComponent<RobotHostSpawnerUdp>();
        spawner.Configure(udp, authority, udpHost, combat, slide);

        var bootstrap = hostGo.AddComponent<PhysicsTestNetRoleBootstrap>();
        bootstrap.ConfigureDefaults(PhysicsTestNetRole.Host, "127.0.0.1", 7777);

        var clientGo = new GameObject("UdpClient");
        var udpClient = clientGo.AddComponent<PhysicsTestUdpClient>();
        udpClient.Configure(udp);

        var hostVerifier = hostGo.AddComponent<RobotModularUdpHostVerifier>();
        hostVerifier.Configure(udp, udpHost, authority, combat, spawner, bootstrap);
        hostVerifier.AutoRun = true;

        var clientVerifier = clientGo.AddComponent<RobotModularUdpClientVerifier>();
        clientVerifier.Configure(udp, udpClient, bootstrap);
        clientVerifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S2-09 modular cross-process UDP scaffold (empty arena → UDP blueprint admit).");
    }

    static void BuildModularAssemblyScenario(PhysicsMaterial slide)
    {
        var bpA = Ra2.Robot.RobotBlueprint.CreatePhysicsTestSampleA(new Vector3(-4f, 0.75f, 0f), 90f);
        var bpB = Ra2.Robot.RobotBlueprint.CreatePhysicsTestSampleB(new Vector3(4f, 0.75f, 0f), -90f);
        var assembledA = Ra2.Robot.RobotAssembler.Assemble(bpA, null, slide, new Color(0.2f, 0.55f, 1f));
        var assembledB = Ra2.Robot.RobotAssembler.Assemble(bpB, null, slide, new Color(1f, 0.35f, 0.2f));

        var driveA = assembledA.Root.AddComponent<PhysicsTestDrive>();
        var driveB = assembledB.Root.AddComponent<PhysicsTestDrive>();

        var hostGo = new GameObject("AssemblyHost");
        var verifier = hostGo.AddComponent<RobotAssemblyVerifier>();
        verifier.Configure(driveA, driveB);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-01 assembled PhysicsTestSample_A/B from blueprint data.");
    }

    static void BuildBlueprintSerializeScenario(PhysicsMaterial slide)
    {
        // Robots are assembled at Play from JSON round-trip (U-SER thin).
        var hostGo = new GameObject("SerializeHost");
        var verifier = hostGo.AddComponent<RobotBlueprintSerializeVerifier>();
        verifier.Configure(slide);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-02 serialize host ready (assemble after JSON round-trip in Play).");
    }

    static void BuildBlueprintV1Scenario(PhysicsMaterial slide)
    {
        var hostGo = new GameObject("BlueprintV1Host");
        var verifier = hostGo.AddComponent<RobotBlueprintV1Verifier>();
        verifier.Configure(slide);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-04 RA2 v1 blueprint host ready (JSON + wiring smoke in Play).");
    }

    static void BuildNetSpawnScenario(PhysicsMaterial slide)
    {
        // Empty arena: robots appear only after host validates blueprint spawn requests (S3-03).
        var hostGo = new GameObject("ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());
        var transport = hostGo.AddComponent<PhysicsTestLoopbackTransport>();
        var transportHost = hostGo.AddComponent<PhysicsTestTransportHost>();
        transportHost.Configure(transport, authority, System.Array.Empty<PhysicsTestTransportHost.TrackedRobot>());
        var spawner = hostGo.AddComponent<RobotHostSpawner>();
        spawner.Configure(transport, authority, transportHost, slide);

        var clientGo = new GameObject("LoopbackClient");
        var transportClient = clientGo.AddComponent<PhysicsTestTransportClient>();
        transportClient.Configure(transport);

        var verifier = hostGo.AddComponent<RobotNetSpawnVerifier>();
        verifier.Configure(transport, transportClient, transportHost, authority, spawner, slide);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-03 net-spawn host ready (assemble from loopback blueprint admit).");
    }

    static void BuildNetSpawnV1Scenario(PhysicsMaterial slide)
    {
        var hostGo = new GameObject("ListenHost");
        var authority = hostGo.AddComponent<PhysicsTestLocalAuthority>();
        authority.ConfigureBindings(System.Array.Empty<PhysicsTestLocalAuthority.RobotBinding>());
        var transport = hostGo.AddComponent<PhysicsTestLoopbackTransport>();
        var transportHost = hostGo.AddComponent<PhysicsTestTransportHost>();
        transportHost.Configure(transport, authority, System.Array.Empty<PhysicsTestTransportHost.TrackedRobot>());
        var spawner = hostGo.AddComponent<RobotHostSpawner>();
        spawner.Configure(transport, authority, transportHost, slide);

        var clientGo = new GameObject("LoopbackClient");
        var transportClient = clientGo.AddComponent<PhysicsTestTransportClient>();
        transportClient.Configure(transport);

        var verifier = hostGo.AddComponent<RobotNetSpawnV1Verifier>();
        verifier.Configure(transport, transportClient, transportHost, authority, spawner, slide);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-05 v1 net-spawn host ready (valid admit + invalid reject).");
    }

    static void BuildLifecycleScenario(PhysicsMaterial slide)
    {
        var hostGo = new GameObject("LifecycleHost");
        var verifier = hostGo.AddComponent<RobotLifecycleVerifier>();
        verifier.Configure(slide);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-06 lifecycle host ready (disable/detach/despawn in Play).");
    }

    static void BuildMotorTorqueScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("MotorTorqueHost");
        var verifier = hostGo.AddComponent<RobotMotorTorqueVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S3-07 motor-torque host ready (hinge motors from wiring in Play).");
    }

    static void BuildConstructionValidationScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("ConstructionValidationHost");
        var verifier = hostGo.AddComponent<RobotConstructionValidatorVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S4-01 construction validation host ready (admit + CoM + dual spawn).");
    }

    static void BuildConfigureWiringScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("ConfigureWiringHost");
        var verifier = hostGo.AddComponent<RobotConfigureVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S5-01 configure wiring host ready (preset rebind + persist).");
    }

    static void BuildSeamlessLoopScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("SeamlessLoopHost");
        var verifier = hostGo.AddComponent<RobotSeamlessLoopVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S6-01 seamless Design↔Configure↔Test host ready.");
    }

    static void BuildCombatImmobilityScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("CombatImmobilityHost");
        var verifier = hostGo.AddComponent<RobotCombatImmobilityVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S8-01 combat immobility host ready (disable → Immobilized win).");
    }

    static void BuildWeaponHitScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("WeaponHitHost");
        var verifier = hostGo.AddComponent<RobotWeaponHitVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S7-02 weapon hit host ready (concussion/piercing → degrade/disable).");
    }

    static void BuildMatchUdpLobbyScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("MatchUdpLobbyHost");
        var verifier = hostGo.AddComponent<RobotMatchUdpVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S9-01/S10-01 match UDP lobby host ready (lobby→admit→fight→MatchOutcome).");
    }

    static void BuildMvpLoopGlueScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("MvpLoopGlueHost");
        var verifier = hostGo.AddComponent<RobotMvpLoopVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S11 MVP glue host ready (workshop→local+MP combat admit).");
    }

    static void BuildDisconnectPolicyScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("DisconnectPolicyHost");
        var verifier = hostGo.AddComponent<RobotDisconnectPolicyVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S9-02 disconnect policy host ready (goodbye→DisconnectForfeit).");
    }

    static void BuildResultsPersistScenario()
    {
        var hostGo = new GameObject("ResultsPersistHost");
        var verifier = hostGo.AddComponent<RobotResultsPersistVerifier>();
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S10-02 results persist host ready (JSON + thin view).");
    }

    static void BuildWorkshopChromeScenario(PhysicsMaterial floor)
    {
        var hostGo = new GameObject("WorkshopChromeHost");
        var verifier = hostGo.AddComponent<RobotWorkshopChromeVerifier>();
        verifier.Configure(floor);
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S11 workshop chrome host ready (Design/Configure/Test IMGUI).");
    }

    static void BuildReadyLobbyScenario()
    {
        var hostGo = new GameObject("ReadyLobbyHost");
        var verifier = hostGo.AddComponent<RobotReadyLobbyVerifier>();
        verifier.AutoRun = true;

        Debug.Log("[PhysicsTestSceneBuilder] S9-03 ready/lobby stub host ready (UDP peers → ready → start).");
    }

    static bool UsesGripFloor(Scenario scenario)
    {
        return scenario == Scenario.BlueprintV1Ra2
            || scenario == Scenario.NetSpawnV1
            || scenario == Scenario.Lifecycle
            || scenario == Scenario.MotorTorque
            || scenario == Scenario.ConstructionValidation
            || scenario == Scenario.ConfigureWiring
            || scenario == Scenario.SeamlessLoop
            || scenario == Scenario.CombatImmobility
            || scenario == Scenario.WeaponHit
            || scenario == Scenario.MatchUdpLobby
            || scenario == Scenario.MvpLoopGlue
            || scenario == Scenario.DisconnectPolicy
            || scenario == Scenario.WorkshopChrome;
    }

    static void CreateMainCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.transform.position = new Vector3(0f, 12f, -14f);
        cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.Skybox;
        go.AddComponent<AudioListener>();
    }

    static void CreateDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateFloor(Transform parent, PhysicsMaterial slide)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent, false);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f); // 25x25 world units
        Object.DestroyImmediate(floor.GetComponent<MeshCollider>());
        var box = floor.AddComponent<BoxCollider>();
        box.size = new Vector3(10f, 0.05f, 10f);
        box.center = new Vector3(0f, -0.025f, 0f);
        box.sharedMaterial = slide;
    }

    static void CreateWalls(Transform parent, PhysicsMaterial slide)
    {
        const float half = 12f;
        const float height = 2f;
        const float thickness = 0.5f;
        const float length = 24.5f;

        CreateWall(parent, "Wall_North", new Vector3(0f, height * 0.5f, half), new Vector3(length, height, thickness), slide);
        CreateWall(parent, "Wall_South", new Vector3(0f, height * 0.5f, -half), new Vector3(length, height, thickness), slide);
        CreateWall(parent, "Wall_East", new Vector3(half, height * 0.5f, 0f), new Vector3(thickness, height, length), slide);
        CreateWall(parent, "Wall_West", new Vector3(-half, height * 0.5f, 0f), new Vector3(thickness, height, length), slide);
    }

    static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, PhysicsMaterial slide)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Object.DestroyImmediate(wall.GetComponent<Collider>());
        var col = wall.AddComponent<BoxCollider>();
        col.sharedMaterial = slide;
    }

    static GameObject CreateRobot(
        string name,
        Vector3 position,
        float yawDegrees,
        Color color,
        PhysicsMaterial slide,
        Scenario scenario,
        Vector3 initialVelocity)
    {
        var root = new GameObject(name);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Chassis";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.6f, 0.7f, 2.2f);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var bodyCol = body.AddComponent<BoxCollider>();
        bodyCol.sharedMaterial = slide;
        var bodyRend = body.GetComponent<MeshRenderer>();
        bodyRend.sharedMaterial = CreateColorMaterial($"{name}_Body", color);

        // Primitive wheels: compound colliders on chassis RB unless S1-03 hinges Wheel_FL.
        CreateWheel(root.transform, "Wheel_FL", new Vector3(-0.85f, -0.35f, 0.7f), slide);
        CreateWheel(root.transform, "Wheel_FR", new Vector3(0.85f, -0.35f, 0.7f), slide);
        CreateWheel(root.transform, "Wheel_RL", new Vector3(-0.85f, -0.35f, -0.7f), slide);
        CreateWheel(root.transform, "Wheel_RR", new Vector3(0.85f, -0.35f, -0.7f), slide);

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 12f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.4f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Keep upright for STAGE-1; full free rotation comes later with real suspension.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if ((scenario == Scenario.WheelJoint || scenario == Scenario.DualAuthority ||
             scenario == Scenario.LoopbackTransport || scenario == Scenario.IllegalClientForce ||
             scenario == Scenario.LatencyHarness || scenario == Scenario.DedicatedTickSmoke ||
             scenario == Scenario.CrossProcessUdp || scenario == Scenario.DedicatedBuildSmoke) &&
            name == "Robot_A")
            AttachHingedWheelFl(root.transform, rb);

        if (scenario == Scenario.CollisionSmoke)
        {
            var drive = root.AddComponent<PhysicsTestAutoDrive>();
            drive.InitialLinearVelocity = initialVelocity;
            root.AddComponent<PhysicsTestSmokeProbe>();
        }
        else if (scenario == Scenario.DualAuthority || scenario == Scenario.LoopbackTransport ||
                 scenario == Scenario.IllegalClientForce || scenario == Scenario.LatencyHarness ||
                 scenario == Scenario.DedicatedTickSmoke || scenario == Scenario.CrossProcessUdp ||
                 scenario == Scenario.DedicatedBuildSmoke)
        {
            root.AddComponent<PhysicsTestDrive>();
            root.AddComponent<PhysicsTestCommandSource>();
            if (scenario == Scenario.CrossProcessUdp || scenario == Scenario.DedicatedBuildSmoke)
                root.AddComponent<PhysicsTestDisableFlag>();
        }
        else if (name == "Robot_A")
        {
            root.AddComponent<PhysicsTestDrive>();
            root.AddComponent<PhysicsTestPlayerInput>();
            if (scenario == Scenario.WheelJoint)
                root.AddComponent<PhysicsTestJointVerifier>();
            else
                root.AddComponent<PhysicsTestDriveVerifier>();
        }

        // Nose marker so facing is obvious in Play Mode.
        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(root.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.15f, 1.25f);
        nose.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{name}_Nose", Color.white);

        return root;
    }

    /// <summary>
    /// S1-03: one hinged wheel on Robot_A. Cylinder axle after Euler(0,0,90) is local +Y.
    /// </summary>
    static void AttachHingedWheelFl(Transform robotRoot, Rigidbody chassisRb)
    {
        var wheelTf = robotRoot.Find("Wheel_FL");
        if (wheelTf == null)
        {
            Debug.LogError("[PhysicsTestSceneBuilder] Wheel_FL missing; cannot attach hinge.");
            return;
        }

        var wheelGo = wheelTf.gameObject;
        var wheelRb = wheelGo.AddComponent<Rigidbody>();
        // ~10% of chassis mass — enough contact mass without anchoring the chassis.
        wheelRb.mass = 1.2f;
        wheelRb.linearDamping = 0.05f;
        wheelRb.angularDamping = 0.25f;
        wheelRb.interpolation = RigidbodyInterpolation.Interpolate;
        wheelRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var hinge = wheelGo.AddComponent<HingeJoint>();
        hinge.connectedBody = chassisRb;
        hinge.anchor = Vector3.zero;
        hinge.axis = Vector3.up;
        hinge.autoConfigureConnectedAnchor = true;
        hinge.useSpring = false;
        hinge.useMotor = false;
        hinge.useLimits = false;
        hinge.enableCollision = false;

        Debug.Log("[PhysicsTestSceneBuilder] Attached HingeJoint on Robot_A/Wheel_FL → chassis.");
    }

    static void CreateWheel(Transform parent, string name, Vector3 localPos, PhysicsMaterial slide)
    {
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
        Object.DestroyImmediate(wheel.GetComponent<Collider>());
        var col = wheel.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        col.sharedMaterial = slide;
        wheel.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial(name, new Color(0.12f, 0.12f, 0.12f));
    }

    static PhysicsMaterial GetOrCreateSlideMaterial()
    {
        const string dir = "Assets/Scenes/PhysicsTestMaterials";
        const string path = dir + "/PhysicsTestSlide.physicMaterial";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Scenes", "PhysicsTestMaterials");

        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (existing != null)
            return existing;

        var mat = new PhysicsMaterial("PhysicsTestSlide")
        {
            dynamicFriction = 0.05f,
            staticFriction = 0.05f,
            bounciness = 0.15f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Average
        };
        AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
    }

    static PhysicsMaterial GetOrCreateGripFloorMaterial()
    {
        const string dir = "Assets/Scenes/PhysicsTestMaterials";
        const string path = dir + "/PhysicsTestGrip.physicMaterial";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Scenes", "PhysicsTestMaterials");

        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (existing != null)
            return existing;

        var mat = new PhysicsMaterial("PhysicsTestGrip")
        {
            dynamicFriction = 0.85f,
            staticFriction = 0.95f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
    }

    static Material CreateColorMaterial(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;

        var dir = "Assets/Scenes/PhysicsTestMaterials";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Scenes", "PhysicsTestMaterials");
        var path = $"{dir}/{name}.mat";
        AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
