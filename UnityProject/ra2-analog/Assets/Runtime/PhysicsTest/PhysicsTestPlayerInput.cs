using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Local-only S1-02 input adapter. Reads keyboard intent and writes a drive command.
/// Does not apply physics.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PhysicsTestDrive))]
public sealed class PhysicsTestPlayerInput : MonoBehaviour
{
    PhysicsTestDrive drive;

    void Awake()
    {
        drive = GetComponent<PhysicsTestDrive>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            drive.SetCommand(default);
            return;
        }

        float move = 0f;
        if (keyboard.wKey.isPressed)
            move += 1f;
        if (keyboard.sKey.isPressed)
            move -= 1f;

        float turn = 0f;
        if (keyboard.aKey.isPressed)
            turn -= 1f;
        if (keyboard.dKey.isPressed)
            turn += 1f;

        drive.SetCommand(new PhysicsTestDriveCommand
        {
            Move = Mathf.Clamp(move, -1f, 1f),
            Turn = Mathf.Clamp(turn, -1f, 1f),
            Brake = keyboard.spaceKey.isPressed
        });
    }
}
