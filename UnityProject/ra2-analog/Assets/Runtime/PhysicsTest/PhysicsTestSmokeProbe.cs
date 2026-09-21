using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Local-only S1-01 helper. Records positions and robot-robot contacts.
/// Writes a live sample file during Play and a final summary on exit.
/// Does not drive motion.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PhysicsTestSmokeProbe : MonoBehaviour
{
    const string FinalRelativePath = "Scenes/PhysicsTest_S1_01_smoke.txt";
    const string LiveRelativePath = "Scenes/PhysicsTest_S1_01_live.txt";

    static readonly object Gate = new object();
    static StringBuilder report;
    static int liveProbes;
    static int robotContacts;
    static bool sawNaN;
    static string livePath;
    static string finalPath;

    Rigidbody body;
    float nextSampleTime;
    bool flushed;

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);

    static string ResolvePath(string relative) => Path.Combine(Application.dataPath, relative);

    void OnEnable()
    {
        body = GetComponent<Rigidbody>();
        flushed = false;
        lock (Gate)
        {
            finalPath = ResolvePath(FinalRelativePath);
            livePath = ResolvePath(LiveRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? Application.dataPath);

            if (report == null)
            {
                report = new StringBuilder(2048);
                robotContacts = 0;
                sawNaN = false;
                report.AppendLine($"S1-01 PhysicsTest smoke @ {System.DateTime.Now:o}");
                File.WriteAllText(livePath, $"LIVE {System.DateTime.Now:o}\n");
            }

            liveProbes++;
            var p = body.position;
            var enableLine = $"ENABLE {name} pos=({F3(p.x)},{F3(p.y)},{F3(p.z)})";
            report.AppendLine(enableLine);
            File.AppendAllText(livePath, enableLine + "\n");
        }

        nextSampleTime = Time.time + 0.25f;
    }

    void FixedUpdate()
    {
        var p = body.position;
        if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
            float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z))
        {
            lock (Gate)
            {
                sawNaN = true;
                var nanLine =
                    $"NAN {name} pos=({p.x},{p.y},{p.z}) t={Time.time.ToString("F2", CultureInfo.InvariantCulture)}";
                report?.AppendLine(nanLine);
                if (livePath != null)
                    File.AppendAllText(livePath, nanLine + "\n");
            }
        }

        if (Time.time < nextSampleTime)
            return;

        nextSampleTime = Time.time + 0.25f;
        var v = body.linearVelocity;
        var sample =
            $"SAMPLE t={Time.time.ToString("F2", CultureInfo.InvariantCulture)} {name} pos=({F3(p.x)},{F3(p.y)},{F3(p.z)}) vel=({F3(v.x)},{F3(v.y)},{F3(v.z)})";
        lock (Gate)
        {
            report?.AppendLine(sample);
            if (livePath != null)
                File.AppendAllText(livePath, sample + "\n");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        var otherRoot = collision.collider.transform.root;
        if (otherRoot == null || otherRoot == transform.root)
            return;
        if (!otherRoot.name.StartsWith("Robot_"))
            return;

        if (string.CompareOrdinal(name, otherRoot.name) > 0)
            return;

        lock (Gate)
        {
            robotContacts++;
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : body.position;
            var contact =
                $"CONTACT t={Time.time.ToString("F2", CultureInfo.InvariantCulture)} {name} <-> {otherRoot.name} point=({F3(point.x)},{F3(point.y)},{F3(point.z)})";
            report?.AppendLine(contact);
            if (livePath != null)
                File.AppendAllText(livePath, contact + "\n");
        }
    }

    void OnDisable()
    {
        TryFlush();
    }

    void OnDestroy()
    {
        TryFlush();
    }

    void TryFlush()
    {
        if (flushed)
            return;
        flushed = true;

        lock (Gate)
        {
            if (report == null)
                return;

            liveProbes = Mathf.Max(0, liveProbes - 1);
            if (liveProbes > 0)
                return;

            report.AppendLine($"CONTACTS={robotContacts}");
            report.AppendLine($"NAN={sawNaN}");
            File.WriteAllText(finalPath, report.ToString());
            report = null;
        }
    }
}
