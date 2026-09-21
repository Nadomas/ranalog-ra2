using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input adapter for S2-01/S2-02. Reads a key scheme and submits envelopes.
/// S2-01: direct <see cref="PhysicsTestLocalAuthority"/>.
/// S2-02: <see cref="PhysicsTestTransportClient"/> (loopback) preferred when set.
/// Does not call <see cref="PhysicsTestDrive.SetCommand"/> (authority is sole writer).
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestCommandSource : MonoBehaviour
{
    public enum KeyScheme
    {
        WasdSpace = 0,
        ArrowsShift = 1
    }

    [SerializeField] int robotId;
    [SerializeField] int sourceId;
    [SerializeField] KeyScheme scheme = KeyScheme.WasdSpace;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestTransportClient transportClient;

    public int RobotId => robotId;
    public int SourceId => sourceId;

    public void Configure(int robot, int source, KeyScheme keys, PhysicsTestLocalAuthority auth)
    {
        Configure(robot, source, keys, auth, null);
    }

    public void Configure(
        int robot,
        int source,
        KeyScheme keys,
        PhysicsTestLocalAuthority auth,
        PhysicsTestTransportClient client)
    {
        robotId = robot;
        sourceId = source;
        scheme = keys;
        authority = auth;
        transportClient = client;
    }

    void Update()
    {
        if (transportClient == null && authority == null)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Submit(default);
            return;
        }

        float move = 0f;
        float turn = 0f;
        bool brake = false;

        if (scheme == KeyScheme.WasdSpace)
        {
            if (keyboard.wKey.isPressed) move += 1f;
            if (keyboard.sKey.isPressed) move -= 1f;
            if (keyboard.aKey.isPressed) turn -= 1f;
            if (keyboard.dKey.isPressed) turn += 1f;
            brake = keyboard.spaceKey.isPressed;
        }
        else
        {
            if (keyboard.upArrowKey.isPressed) move += 1f;
            if (keyboard.downArrowKey.isPressed) move -= 1f;
            if (keyboard.leftArrowKey.isPressed) turn -= 1f;
            if (keyboard.rightArrowKey.isPressed) turn += 1f;
            brake = keyboard.rightShiftKey.isPressed;
        }

        Submit(new PhysicsTestDriveCommand
        {
            Move = Mathf.Clamp(move, -1f, 1f),
            Turn = Mathf.Clamp(turn, -1f, 1f),
            Brake = brake
        });
    }

    void Submit(PhysicsTestDriveCommand command)
    {
        var envelope = new PhysicsTestCommandEnvelope(robotId, sourceId, command);
        // Prefer transport so listen-host local input shares the same path as a remote peer.
        if (transportClient != null)
            transportClient.Send(envelope);
        else
            authority.Submit(envelope);
    }
}
