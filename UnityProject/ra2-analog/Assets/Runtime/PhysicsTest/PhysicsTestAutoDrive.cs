using UnityEngine;

/// <summary>
/// Local-only PhysicsTest helper. Applies an initial Rigidbody velocity in Start
/// because editor-assigned linearVelocity does not serialize into the scene file.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PhysicsTestAutoDrive : MonoBehaviour
{
    [SerializeField] Vector3 initialLinearVelocity;

    public Vector3 InitialLinearVelocity
    {
        get => initialLinearVelocity;
        set => initialLinearVelocity = value;
    }

    void Start()
    {
        var body = GetComponent<Rigidbody>();
        body.WakeUp();
        body.linearVelocity = initialLinearVelocity;
    }
}