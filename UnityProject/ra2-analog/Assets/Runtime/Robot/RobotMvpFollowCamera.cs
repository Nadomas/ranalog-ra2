using UnityEngine;

/// <summary>
/// S11-11 local-only presentation: soft follow cam for Test / Fight (no physics writes).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMvpFollowCamera : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(0f, 10.5f, -12.5f);
    [SerializeField] float positionLerp = 6.5f;
    [SerializeField] float lookHeight = 0.6f;

    Transform primary;
    Transform secondary;

    public void SetTargets(Transform focus, Transform other = null)
    {
        primary = focus;
        secondary = other;
    }

    public void Clear()
    {
        primary = null;
        secondary = null;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || primary == null)
            return;

        var look = primary.position;
        if (secondary != null)
            look = (primary.position + secondary.position) * 0.5f;
        look.y += lookHeight;

        var desired = look + offset;
        var t = 1f - Mathf.Exp(-positionLerp * Time.deltaTime);
        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, t);

        var dir = look - cam.transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            cam.transform.rotation = Quaternion.Slerp(
                cam.transform.rotation,
                Quaternion.LookRotation(dir.normalized, Vector3.up),
                t);
    }
}
