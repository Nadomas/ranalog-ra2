using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Parses <c>-ra2-role</c>/<c>-ra2-host</c>/<c>-ra2-port</c>/<c>-ra2-log</c> and configures
/// UDP host or remote client for S2-06/07/08 spikes. Dedicated disables presentation.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class PhysicsTestNetRoleBootstrap : MonoBehaviour
{
    [SerializeField] PhysicsTestNetRole defaultRole = PhysicsTestNetRole.Host;
    [SerializeField] string defaultHost = "127.0.0.1";
    [SerializeField] int defaultPort = 7777;
    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestUdpHost udpHost;
    [SerializeField] PhysicsTestUdpClient udpClient;
    [SerializeField] PhysicsTestCombatAuthority combatAuthority;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] RobotHostSpawnerUdp modularSpawner;
    [SerializeField] bool stripClientSimBodies = true;

    PhysicsTestNetRole resolvedRole;
    string logPath = "";

    public PhysicsTestNetRole ResolvedRole => resolvedRole;
    public string LogPath => logPath;

    public void ConfigureDefaults(PhysicsTestNetRole role, string host, int port)
    {
        defaultRole = role;
        defaultHost = host;
        defaultPort = port;
    }

    void Awake()
    {
        resolvedRole = ParseRole(defaultRole);
        var host = ParseArg("ra2-host", defaultHost);
        var port = ParseIntArg("ra2-port", defaultPort);
        logPath = ParseArg("ra2-log", DefaultLogPath(resolvedRole));

        if (transport == null)
            transport = GetComponent<PhysicsTestUdpTransport>();
        if (udpHost == null)
            udpHost = GetComponent<PhysicsTestUdpHost>();
        if (authority == null)
            authority = GetComponent<PhysicsTestLocalAuthority>();
        if (combatAuthority == null)
            combatAuthority = GetComponent<PhysicsTestCombatAuthority>();
        if (modularSpawner == null)
            modularSpawner = GetComponent<RobotHostSpawnerUdp>();
        if (udpClient == null)
            udpClient = FindAnyObjectByType<PhysicsTestUdpClient>();

        transport.Configure(resolvedRole, host, port);
        if (resolvedRole == PhysicsTestNetRole.Client)
        {
            if (udpHost != null)
                udpHost.enabled = false;
            if (authority != null)
                authority.enabled = false;
            if (combatAuthority != null)
                combatAuthority.enabled = false;
            if (modularSpawner != null)
                modularSpawner.enabled = false;
            if (stripClientSimBodies)
                StripAuthoritativeSimOnClient();
            transport.StartTransport();
            if (udpClient != null)
                udpClient.Configure(transport);
        }
        else
        {
            if (udpClient != null && resolvedRole != PhysicsTestNetRole.Client)
            {
                // Host scene still may have a local UdpClient for Editor dual-role tests; leave enabled only if present for remote process.
            }

            transport.StartTransport();
            if (resolvedRole == PhysicsTestNetRole.Dedicated)
                DisablePresentation();
        }

        Debug.Log($"[PhysicsTestNetRoleBootstrap] role={resolvedRole} host={host} port={port} log={logPath}");
        TryAppendLog($"BOOT role={resolvedRole} host={host} port={port}");
    }

    public void TryAppendLog(string line)
    {
        if (string.IsNullOrEmpty(logPath))
            return;
        try
        {
            var dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PhysicsTestNetRoleBootstrap] log write failed: {ex.Message}");
        }
    }

    static void StripAuthoritativeSimOnClient()
    {
        // Remote client must not run authoritative PhysX for the duel — presentation/proxy only.
        foreach (var drive in FindObjectsByType<PhysicsTestDrive>(FindObjectsSortMode.None))
            drive.enabled = false;
        foreach (var rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (var hinge in FindObjectsByType<HingeJoint>(FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(hinge);
    }

    static void DisablePresentation()
    {
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            cam.enabled = false;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            listener.enabled = false;
        foreach (var rend in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            rend.enabled = false;
    }

    static PhysicsTestNetRole ParseRole(PhysicsTestNetRole fallback)
    {
        var raw = ParseArg("ra2-role", null);
        if (string.IsNullOrEmpty(raw))
            return fallback;
        if (Enum.TryParse(raw, true, out PhysicsTestNetRole role))
            return role;
        return fallback;
    }

    static string ParseArg(string key, string fallback)
    {
        var prefix = "-" + key + "=";
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg.Substring(prefix.Length);
        }

        return fallback;
    }

    static int ParseIntArg(string key, int fallback)
    {
        var raw = ParseArg(key, null);
        if (string.IsNullOrEmpty(raw))
            return fallback;
        return int.TryParse(raw, out var v) ? v : fallback;
    }

    static string DefaultLogPath(PhysicsTestNetRole role)
    {
        // Assets -> ra2-analog -> UnityProject
        var unityProject = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        if (!Directory.Exists(unityProject))
            unityProject = Application.persistentDataPath;
        return Path.Combine(unityProject, $"_s2_net_{role.ToString().ToLowerInvariant()}.txt");
    }
}