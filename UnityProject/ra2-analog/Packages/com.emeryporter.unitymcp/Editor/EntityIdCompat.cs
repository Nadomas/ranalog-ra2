using UnityEditor;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity 6.5 compatibility: InstanceID APIs are obsolete-as-error.
    /// EntityId.ToULong / FromULong are static (EntityId.ToULong(id)), not instance methods.
    /// </summary>
    internal static class EntityIdCompat
    {
        public static ulong IdOf(Object obj) =>
            obj != null ? EntityId.ToULong(obj.GetEntityId()) : 0UL;

        public static EntityId FromObjectId(ulong value) => EntityId.FromULong(value);

        public static EntityId FromObjectId(long value) => EntityId.FromULong(unchecked((ulong)value));

        public static EntityId FromObjectId(int value) => EntityId.FromULong(unchecked((ulong)value));

        public static Object ToObject(ulong value) => EditorUtility.EntityIdToObject(FromObjectId(value));

        public static Object ToObject(long value) => EditorUtility.EntityIdToObject(FromObjectId(value));

        public static Object ToObject(int value) => EditorUtility.EntityIdToObject(FromObjectId(value));
    }
}
