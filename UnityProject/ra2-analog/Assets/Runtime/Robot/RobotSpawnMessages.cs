using System;

namespace Ra2.Robot
{
    /// <summary>
    /// Thin net spawn payloads for loopback (S3-03). Blueprint travels as provisional JSON
    /// (<c>ra2.robot_blueprint.v0</c>). Not a production net package.
    /// </summary>
    [Serializable]
    public struct RobotSpawnRequest
    {
        public int RobotId;
        public int SourceId;
        public string Schema;
        public string BlueprintJson;
        public bool Despawn;

        public static RobotSpawnRequest CreateSpawn(int robotId, int sourceId, RobotBlueprint blueprint)
        {
            var schema = blueprint != null && blueprint.HasV1Fields
                ? RobotBlueprintSerializer.SchemaIdV1
                : RobotBlueprintSerializer.SchemaIdV0;
            return new RobotSpawnRequest
            {
                RobotId = robotId,
                SourceId = sourceId,
                Schema = schema,
                BlueprintJson = RobotBlueprintSerializer.ToJson(blueprint, pretty: false, schema),
                Despawn = false
            };
        }

        public static RobotSpawnRequest CreateDespawn(int robotId, int sourceId)
        {
            return new RobotSpawnRequest
            {
                RobotId = robotId,
                SourceId = sourceId,
                Schema = RobotBlueprintSerializer.SchemaId,
                BlueprintJson = string.Empty,
                Despawn = true
            };
        }
    }

    [Serializable]
    public struct RobotSpawnEvent
    {
        public int RobotId;
        public int SourceId;
        public string Schema;
        public string BlueprintJson;
        public bool Despawned;
        public bool Accepted;
        public string RejectReason;

        public static RobotSpawnEvent AcceptedSpawn(int robotId, int sourceId, string schema, string blueprintJson)
        {
            return new RobotSpawnEvent
            {
                RobotId = robotId,
                SourceId = sourceId,
                Schema = schema,
                BlueprintJson = blueprintJson ?? string.Empty,
                Despawned = false,
                Accepted = true,
                RejectReason = string.Empty
            };
        }

        public static RobotSpawnEvent AcceptedDespawn(int robotId, int sourceId)
        {
            return new RobotSpawnEvent
            {
                RobotId = robotId,
                SourceId = sourceId,
                Schema = RobotBlueprintSerializer.SchemaId,
                BlueprintJson = string.Empty,
                Despawned = true,
                Accepted = true,
                RejectReason = string.Empty
            };
        }

        public static RobotSpawnEvent Rejected(int robotId, int sourceId, string reason)
        {
            return new RobotSpawnEvent
            {
                RobotId = robotId,
                SourceId = sourceId,
                Schema = RobotBlueprintSerializer.SchemaId,
                BlueprintJson = string.Empty,
                Despawned = false,
                Accepted = false,
                RejectReason = reason ?? "rejected"
            };
        }
    }
}
