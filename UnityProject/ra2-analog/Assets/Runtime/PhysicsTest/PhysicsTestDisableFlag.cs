using UnityEngine;

/// <summary>
/// Thin out-of-commission flag (S2-07). Host sets; drive refuses commands when disabled.
/// Not a full damage model.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestDisableFlag : MonoBehaviour
{
    [SerializeField] bool disabled;

    public bool Disabled => disabled;

    public void SetDisabled(bool value) => disabled = value;
}
