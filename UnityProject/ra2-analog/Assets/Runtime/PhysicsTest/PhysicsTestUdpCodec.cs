using System;
using System.IO;
using System.Net;
using System.Text;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Compact binary codec for S2-06 UDP payloads. Provisional — not a production protocol.
/// </summary>
public static class PhysicsTestUdpCodec
{
    public const byte MsgHello = 1;
    public const byte MsgCommand = 2;
    public const byte MsgPoses = 3;
    public const byte MsgCombatRequest = 4;
    public const byte MsgCombatEvent = 5;
    public const byte MsgGoodbye = 6;
    public const byte MsgSpawnRequest = 7;
    public const byte MsgSpawnEvent = 8;
    public const byte MsgMatchOutcome = 9;
    /// <summary>S9-04 keepalive; any inbound packet also refreshes receive clock.</summary>
    public const byte MsgHeartbeat = 10;

    public const int ProtocolVersion = 1;
    /// <summary>Localhost UDP can carry large datagrams; still cap so a bad JSON cannot flood the socket.</summary>
    public const int MaxSpawnPayloadBytes = 48 * 1024;

    public static byte[] WriteHello(PhysicsTestNetRole role, int peerId)
    {
        using var ms = new MemoryStream(16);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgHello);
        w.Write(ProtocolVersion);
        w.Write((int)role);
        w.Write(peerId);
        return ms.ToArray();
    }

    public static bool TryReadHello(byte[] data, int length, out PhysicsTestNetRole role, out int peerId, out int version)
    {
        role = default;
        peerId = 0;
        version = 0;
        if (length < 13 || data[0] != MsgHello)
            return false;
        using var ms = new MemoryStream(data, 0, length, false);
        using var r = new BinaryReader(ms, Encoding.UTF8, true);
        r.ReadByte();
        version = r.ReadInt32();
        role = (PhysicsTestNetRole)r.ReadInt32();
        peerId = r.ReadInt32();
        return version == ProtocolVersion;
    }

    public static byte[] WriteCommand(PhysicsTestCommandEnvelope envelope)
    {
        using var ms = new MemoryStream(32);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgCommand);
        w.Write(envelope.RobotId);
        w.Write(envelope.SourceId);
        w.Write(envelope.Command.Move);
        w.Write(envelope.Command.Turn);
        w.Write(envelope.Command.Brake);
        return ms.ToArray();
    }

    public static bool TryReadCommand(byte[] data, int length, out PhysicsTestCommandEnvelope envelope)
    {
        envelope = default;
        if (length < 18 || data[0] != MsgCommand)
            return false;
        using var ms = new MemoryStream(data, 0, length, false);
        using var r = new BinaryReader(ms, Encoding.UTF8, true);
        r.ReadByte();
        var robotId = r.ReadInt32();
        var sourceId = r.ReadInt32();
        var move = r.ReadSingle();
        var turn = r.ReadSingle();
        var brake = r.ReadBoolean();
        envelope = new PhysicsTestCommandEnvelope(
            robotId,
            sourceId,
            new PhysicsTestDriveCommand { Move = move, Turn = turn, Brake = brake });
        return true;
    }

    public static byte[] WritePoses(PhysicsTestPoseSnapshot[] poses)
    {
        poses ??= Array.Empty<PhysicsTestPoseSnapshot>();
        using var ms = new MemoryStream(8 + poses.Length * 48);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgPoses);
        w.Write(poses.Length);
        for (var i = 0; i < poses.Length; i++)
        {
            var p = poses[i];
            w.Write(p.RobotId);
            WriteVec3(w, p.Position);
            WriteQuat(w, p.Rotation);
            WriteVec3(w, p.LinearVelocity);
        }

        return ms.ToArray();
    }

    public static bool TryReadPoses(byte[] data, int length, out PhysicsTestPoseSnapshot[] poses)
    {
        poses = Array.Empty<PhysicsTestPoseSnapshot>();
        if (length < 5 || data[0] != MsgPoses)
            return false;
        using var ms = new MemoryStream(data, 0, length, false);
        using var r = new BinaryReader(ms, Encoding.UTF8, true);
        r.ReadByte();
        var count = r.ReadInt32();
        if (count < 0 || count > 64)
            return false;
        poses = new PhysicsTestPoseSnapshot[count];
        for (var i = 0; i < count; i++)
        {
            poses[i] = new PhysicsTestPoseSnapshot
            {
                RobotId = r.ReadInt32(),
                Position = ReadVec3(r),
                Rotation = ReadQuat(r),
                LinearVelocity = ReadVec3(r)
            };
        }

        return true;
    }

    public static byte[] WriteCombatRequest(PhysicsTestCombatRequest request)
    {
        using var ms = new MemoryStream(40);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgCombatRequest);
        w.Write(request.AttackerRobotId);
        w.Write(request.SourceId);
        w.Write(request.TargetRobotId);
        WriteVec3(w, request.ImpulseWorld);
        return ms.ToArray();
    }

    public static bool TryReadCombatRequest(byte[] data, int length, out PhysicsTestCombatRequest request)
    {
        request = default;
        if (length < 25 || data[0] != MsgCombatRequest)
            return false;
        using var ms = new MemoryStream(data, 0, length, false);
        using var r = new BinaryReader(ms, Encoding.UTF8, true);
        r.ReadByte();
        request = new PhysicsTestCombatRequest
        {
            AttackerRobotId = r.ReadInt32(),
            SourceId = r.ReadInt32(),
            TargetRobotId = r.ReadInt32(),
            ImpulseWorld = ReadVec3(r)
        };
        return true;
    }

    public static byte[] WriteCombatEvent(PhysicsTestCombatEvent evt)
    {
        using var ms = new MemoryStream(48);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgCombatEvent);
        w.Write(evt.Sequence);
        w.Write(evt.AttackerRobotId);
        w.Write(evt.TargetRobotId);
        WriteVec3(w, evt.ImpulseWorld);
        w.Write(evt.TargetDisabled);
        w.Write(evt.TargetHitCount);
        return ms.ToArray();
    }

    public static bool TryReadCombatEvent(byte[] data, int length, out PhysicsTestCombatEvent evt)
    {
        evt = default;
        if (length < 30 || data[0] != MsgCombatEvent)
            return false;
        using var ms = new MemoryStream(data, 0, length, false);
        using var r = new BinaryReader(ms, Encoding.UTF8, true);
        r.ReadByte();
        evt = new PhysicsTestCombatEvent
        {
            Sequence = r.ReadInt32(),
            AttackerRobotId = r.ReadInt32(),
            TargetRobotId = r.ReadInt32(),
            ImpulseWorld = ReadVec3(r),
            TargetDisabled = r.ReadBoolean(),
            TargetHitCount = r.ReadInt32()
        };
        return true;
    }

    public static byte[] WriteGoodbye() => new[] { MsgGoodbye };

    public static byte[] WriteHeartbeat() => new[] { MsgHeartbeat };

    public static bool TryReadHeartbeat(byte[] data, int length)
    {
        return length >= 1 && data != null && data[0] == MsgHeartbeat;
    }

    public static byte[] WriteMatchOutcome(MatchSummary summary)
    {
        using var ms = new MemoryStream(64);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgMatchOutcome);
        var o = summary.Outcome;
        w.Write(o.Finished);
        w.Write(o.WinnerRobotId);
        w.Write(o.LoserRobotId);
        w.Write((byte)o.Reason);
        w.Write(summary.MatchDurationSeconds);
        w.Write(summary.ImmobileSecondsLoser);
        w.Write(summary.ImmobileSecondsWinner);
        w.Write(summary.LoserWasDisabled);
        WriteUtf8(w, summary.SessionId ?? string.Empty);
        return ms.ToArray();
    }

    public static bool TryReadMatchOutcome(byte[] data, int length, out MatchSummary summary)
    {
        summary = MatchSummary.None;
        if (length < 24 || data[0] != MsgMatchOutcome)
            return false;
        try
        {
            using var ms = new MemoryStream(data, 0, length, false);
            using var r = new BinaryReader(ms, Encoding.UTF8, true);
            r.ReadByte();
            var finished = r.ReadBoolean();
            var winner = r.ReadInt32();
            var loser = r.ReadInt32();
            var reason = (MatchWinReason)r.ReadByte();
            var duration = r.ReadSingle();
            var immobileLoser = r.ReadSingle();
            var immobileWinner = r.ReadSingle();
            var loserDisabled = r.ReadBoolean();
            var sessionId = ReadUtf8(r);
            summary = new MatchSummary(
                new MatchOutcome(finished, winner, loser, reason),
                duration,
                immobileLoser,
                immobileWinner,
                loserDisabled,
                sessionId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] WriteSpawnRequest(RobotSpawnRequest request)
    {
        using var ms = new MemoryStream(256);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgSpawnRequest);
        w.Write(request.RobotId);
        w.Write(request.SourceId);
        w.Write(request.Despawn);
        WriteUtf8(w, request.Schema ?? string.Empty);
        WriteUtf8(w, request.BlueprintJson ?? string.Empty);
        var payload = ms.ToArray();
        return payload.Length > MaxSpawnPayloadBytes ? null : payload;
    }

    public static bool TryReadSpawnRequest(byte[] data, int length, out RobotSpawnRequest request)
    {
        request = default;
        if (length < 14 || data[0] != MsgSpawnRequest || length > MaxSpawnPayloadBytes)
            return false;
        try
        {
            using var ms = new MemoryStream(data, 0, length, false);
            using var r = new BinaryReader(ms, Encoding.UTF8, true);
            r.ReadByte();
            request = new RobotSpawnRequest
            {
                RobotId = r.ReadInt32(),
                SourceId = r.ReadInt32(),
                Despawn = r.ReadBoolean(),
                Schema = ReadUtf8(r),
                BlueprintJson = ReadUtf8(r)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] WriteSpawnEvent(RobotSpawnEvent evt)
    {
        using var ms = new MemoryStream(256);
        using var w = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write(MsgSpawnEvent);
        w.Write(evt.RobotId);
        w.Write(evt.SourceId);
        w.Write(evt.Despawned);
        w.Write(evt.Accepted);
        WriteUtf8(w, evt.Schema ?? string.Empty);
        WriteUtf8(w, evt.RejectReason ?? string.Empty);
        WriteUtf8(w, evt.BlueprintJson ?? string.Empty);
        var payload = ms.ToArray();
        return payload.Length > MaxSpawnPayloadBytes ? null : payload;
    }

    public static bool TryReadSpawnEvent(byte[] data, int length, out RobotSpawnEvent evt)
    {
        evt = default;
        if (length < 15 || data[0] != MsgSpawnEvent || length > MaxSpawnPayloadBytes)
            return false;
        try
        {
            using var ms = new MemoryStream(data, 0, length, false);
            using var r = new BinaryReader(ms, Encoding.UTF8, true);
            r.ReadByte();
            evt = new RobotSpawnEvent
            {
                RobotId = r.ReadInt32(),
                SourceId = r.ReadInt32(),
                Despawned = r.ReadBoolean(),
                Accepted = r.ReadBoolean(),
                Schema = ReadUtf8(r),
                RejectReason = ReadUtf8(r),
                BlueprintJson = ReadUtf8(r)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    static void WriteUtf8(BinaryWriter w, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        w.Write(bytes.Length);
        w.Write(bytes);
    }

    static string ReadUtf8(BinaryReader r)
    {
        var len = r.ReadInt32();
        if (len < 0 || len > MaxSpawnPayloadBytes)
            throw new InvalidOperationException("spawn_string_too_large");
        var bytes = r.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }

    static void WriteVec3(BinaryWriter w, Vector3 v)
    {
        w.Write(v.x);
        w.Write(v.y);
        w.Write(v.z);
    }

    static Vector3 ReadVec3(BinaryReader r) =>
        new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

    static void WriteQuat(BinaryWriter w, Quaternion q)
    {
        w.Write(q.x);
        w.Write(q.y);
        w.Write(q.z);
        w.Write(q.w);
    }

    static Quaternion ReadQuat(BinaryReader r) =>
        new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

    public static string EndpointKey(IPEndPoint ep) => ep == null ? "" : $"{ep.Address}:{ep.Port}";
}
