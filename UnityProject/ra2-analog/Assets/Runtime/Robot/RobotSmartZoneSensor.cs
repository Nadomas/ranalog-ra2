using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S7-09 thin SmartZone contact sensor. Uses overlap query (host/local) — not client-authoritative.
    /// Trigger callbacks were unreliable with assembled compound bodies; OverlapBox is the thin proof path.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(40)]
    public sealed class RobotSmartZoneSensor : MonoBehaviour
    {
        Collider zoneCollider;
        readonly Collider[] overlapBuffer = new Collider[16];
        bool prevContacted;

        public string ComponentId { get; private set; }
        public int OwnerRobotId { get; private set; } = -1;
        public bool HasForeignContact { get; private set; }
        /// <summary>True for one FixedUpdate after contact rises.</summary>
        public bool ContactRisingEdge { get; private set; }
        public int ContactEnterCount { get; private set; }

        public void Bind(string componentId, int ownerRobotId)
        {
            ComponentId = componentId;
            OwnerRobotId = ownerRobotId;
            HasForeignContact = false;
            prevContacted = false;
            ContactRisingEdge = false;
            ContactEnterCount = 0;
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
                zoneCollider.isTrigger = true;
        }

        void FixedUpdate()
        {
            var now = ProbeForeignContact();
            ContactRisingEdge = now && !prevContacted;
            if (ContactRisingEdge)
                ContactEnterCount++;
            HasForeignContact = now;
            prevContacted = now;
        }

        bool ProbeForeignContact()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<Collider>();
            if (zoneCollider == null)
                return false;

            var box = zoneCollider as BoxCollider;
            int count;
            if (box != null)
            {
                var t = box.transform;
                var worldCenter = t.TransformPoint(box.center);
                var half = Vector3.Scale(box.size * 0.5f, Abs(t.lossyScale));
                count = Physics.OverlapBoxNonAlloc(
                    worldCenter, half, overlapBuffer, t.rotation, ~0, QueryTriggerInteraction.Collide);
            }
            else
            {
                var b = zoneCollider.bounds;
                count = Physics.OverlapBoxNonAlloc(
                    b.center, b.extents, overlapBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            }

            for (var i = 0; i < count; i++)
            {
                var other = overlapBuffer[i];
                if (other == null || other == zoneCollider)
                    continue;
                if (other.transform.IsChildOf(transform.root))
                    continue;
                if (!IsForeign(other))
                    continue;
                return true;
            }

            return false;
        }

        bool IsForeign(Collider other)
        {
            if (other == null)
                return false;
            var tag = other.GetComponentInParent<RobotInstanceTag>();
            if (tag == null)
                return false;
            if (OwnerRobotId >= 0 && tag.RobotId == OwnerRobotId)
                return false;
            return true;
        }

        static Vector3 Abs(Vector3 v) =>
            new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
