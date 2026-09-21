using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S7-03 local/host collision probe. On robot–robot contact, applies concussion/piercing
    /// via <see cref="RobotContactWeaponHit"/> (not client-trusted). No Transform writes.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(40)]
    public sealed class RobotContactWeaponProbe : MonoBehaviour
    {
        [SerializeField] float concussion = 0.9f;
        [SerializeField] float piercing = 0.4f;
        [SerializeField] float cooldownSeconds = 0.2f;
        [SerializeField] float armorAbsorb;
        [SerializeField] bool hostAuthority = true;

        RobotSpawnedInstance owner;
        float nextReadyTime;
        float accumulatedSeverity;

        public bool HostAuthority
        {
            get => hostAuthority;
            set => hostAuthority = value;
        }

        public float AccumulatedSeverity => accumulatedSeverity;
        public int HitCount { get; private set; }
        public RobotWeaponHitOutcome LastOutcome { get; private set; }
        public float LastRelativeSpeed { get; private set; }
        public float LastImpact { get; private set; }
        public string LastError { get; private set; }

        public void Bind(RobotSpawnedInstance ownerInstance)
        {
            owner = ownerInstance;
            accumulatedSeverity = 0f;
            HitCount = 0;
            LastOutcome = RobotWeaponHitOutcome.None;
            LastError = null;
        }

        public void ConfigureMix(float concussionMix, float piercingMix, float armor = 0f)
        {
            concussion = concussionMix;
            piercing = piercingMix;
            armorAbsorb = armor;
        }

        void OnCollisionEnter(Collision collision)
        {
            TryHandleContact(collision);
        }

        void TryHandleContact(Collision collision)
        {
            if (!hostAuthority || owner == null || collision == null)
                return;
            if (Time.time < nextReadyTime)
                return;

            var otherTag = collision.collider != null
                ? collision.collider.GetComponentInParent<RobotInstanceTag>()
                : null;
            if (otherTag == null || otherTag.Instance == null || otherTag.Instance == owner)
                return;

            var speed = collision.relativeVelocity.magnitude;
            LastRelativeSpeed = speed;
            if (!RobotContactWeaponHit.TryApplyFromContact(
                    otherTag.Instance,
                    concussion,
                    piercing,
                    speed,
                    ref accumulatedSeverity,
                    out var outcome,
                    out _,
                    out var impact,
                    out var err,
                    armorAbsorb))
            {
                LastError = err;
                return;
            }

            LastImpact = impact;
            LastOutcome = outcome;
            LastError = null;
            if (impact > 0f)
            {
                HitCount++;
                nextReadyTime = Time.time + Mathf.Max(0.05f, cooldownSeconds);
            }
        }
    }
}
