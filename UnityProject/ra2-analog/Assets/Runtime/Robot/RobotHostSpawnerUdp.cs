using System.Collections.Generic;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S2-09 host spawn authority over UDP. Same validate→assemble→rebind path as
/// <see cref="RobotHostSpawner"/>, but bound to <see cref="PhysicsTestUdpTransport"/>
/// and <see cref="PhysicsTestUdpHost"/> (cross-process, not loopback).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-160)]
public sealed class RobotHostSpawnerUdp : MonoBehaviour
{
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestUdpHost udpHost;
    [SerializeField] PhysicsTestCombatAuthority combat;
    [SerializeField] PhysicsMaterial slideMaterial;
    [SerializeField] Transform spawnParent;

    readonly List<RobotSpawnRequest> requestBuffer = new List<RobotSpawnRequest>(8);
    readonly List<RobotSpawnedInstance> live = new List<RobotSpawnedInstance>(4);

    int acceptedSpawns;
    int acceptedDespawns;
    int rejectedSpawns;
    double lastAssembleMs;
    double maxAssembleMs;

    public int AcceptedSpawns => acceptedSpawns;
    public int AcceptedDespawns => acceptedDespawns;
    public int RejectedSpawns => rejectedSpawns;
    public int LiveCount => live.Count;
    public double LastAssembleMs => lastAssembleMs;
    public double MaxAssembleMs => maxAssembleMs;
    public IReadOnlyList<RobotSpawnedInstance> Live => live;

    public void Configure(
        PhysicsTestUdpTransport udp,
        PhysicsTestLocalAuthority auth,
        PhysicsTestUdpHost host,
        PhysicsTestCombatAuthority combatAuth,
        PhysicsMaterial slide,
        Transform parent = null)
    {
        transport = udp;
        authority = auth;
        udpHost = host;
        combat = combatAuth;
        slideMaterial = slide;
        spawnParent = parent;
    }

    void FixedUpdate()
    {
        if (transport == null)
            return;

        requestBuffer.Clear();
        if (transport.DrainSpawnRequests(requestBuffer) <= 0)
            return;

        for (var i = 0; i < requestBuffer.Count; i++)
            HandleRequest(requestBuffer[i]);
    }

    void HandleRequest(RobotSpawnRequest request)
    {
        if (request.Despawn)
        {
            HandleDespawn(request);
            return;
        }

        if (!RobotBlueprintSerializer.IsSupportedSchema(request.Schema))
        {
            Reject(request, "unsupported_schema");
            return;
        }

        RobotBlueprint blueprint;
        try
        {
            blueprint = RobotBlueprintSerializer.FromJson(request.BlueprintJson);
        }
        catch (System.Exception)
        {
            Reject(request, "parse_failed");
            return;
        }

        if (!RobotSpawnService.TryValidate(blueprint, out var error))
        {
            Reject(request, error);
            return;
        }

        if (FindLiveIndex(request.RobotId) >= 0)
        {
            Reject(request, "robot_id_in_use");
            return;
        }

        var color = request.RobotId == 0
            ? new Color(0.2f, 0.55f, 1f)
            : new Color(1f, 0.35f, 0.2f);

        RobotSpawnedInstance spawned;
        try
        {
            spawned = RobotSpawnService.Spawn(
                blueprint,
                request.RobotId,
                request.SourceId,
                spawnParent,
                slideMaterial,
                color);
        }
        catch (System.Exception)
        {
            Reject(request, "assemble_failed");
            return;
        }

        live.Add(spawned);
        lastAssembleMs = spawned.AssembleMs;
        if (spawned.AssembleMs > maxAssembleMs)
            maxAssembleMs = spawned.AssembleMs;
        acceptedSpawns++;
        RebindAuthority();

        transport.PublishSpawnEvent(RobotSpawnEvent.AcceptedSpawn(
            request.RobotId,
            request.SourceId,
            string.IsNullOrEmpty(request.Schema) ? RobotBlueprintSerializer.SchemaIdV0 : request.Schema,
            request.BlueprintJson));
    }

    void HandleDespawn(RobotSpawnRequest request)
    {
        var idx = FindLiveIndex(request.RobotId);
        if (idx < 0)
        {
            Reject(request, "not_spawned");
            return;
        }

        var instance = live[idx];
        live.RemoveAt(idx);
        RobotSpawnService.Despawn(instance);
        acceptedDespawns++;
        RebindAuthority();
        transport.PublishSpawnEvent(RobotSpawnEvent.AcceptedDespawn(request.RobotId, request.SourceId));
    }

    void Reject(RobotSpawnRequest request, string reason)
    {
        rejectedSpawns++;
        transport.PublishSpawnEvent(RobotSpawnEvent.Rejected(request.RobotId, request.SourceId, reason));
    }

    int FindLiveIndex(int robotId)
    {
        for (var i = 0; i < live.Count; i++)
        {
            if (live[i].RobotId == robotId)
                return i;
        }

        return -1;
    }

    void RebindAuthority()
    {
        var bindings = new PhysicsTestLocalAuthority.RobotBinding[live.Count];
        var tracked = new PhysicsTestUdpHost.TrackedRobot[live.Count];
        var combatTracked = new PhysicsTestCombatAuthority.TrackedRobot[live.Count];
        for (var i = 0; i < live.Count; i++)
        {
            var inst = live[i];
            bindings[i] = new PhysicsTestLocalAuthority.RobotBinding
            {
                RobotId = inst.RobotId,
                AllowedSourceId = inst.AllowedSourceId,
                Drive = inst.Drive
            };
            tracked[i] = new PhysicsTestUdpHost.TrackedRobot
            {
                RobotId = inst.RobotId,
                Drive = inst.Drive
            };
            combatTracked[i] = new PhysicsTestCombatAuthority.TrackedRobot
            {
                RobotId = inst.RobotId,
                AllowedSourceId = inst.AllowedSourceId,
                Drive = inst.Drive,
                DisableFlag = inst.DisableFlag
            };
        }

        if (authority != null)
            authority.ConfigureBindings(bindings);
        if (udpHost != null)
            udpHost.Configure(transport, authority, tracked);
        if (combat != null)
            combat.Configure(transport, combatTracked, 1);
    }
}
