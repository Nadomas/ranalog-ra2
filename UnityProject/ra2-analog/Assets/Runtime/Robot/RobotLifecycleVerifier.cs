using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S3-06 verifier. Modular lifecycle: drive, disable (drive ignored), detach hinged wheel, despawn.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotLifecycleVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 0.8f;
    [SerializeField] PhysicsMaterial slideMaterial;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsMaterial slide)
    {
        slideMaterial = slide;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var bp = RobotBlueprint.CreateRa2TankSteerSample(new Vector3(0f, 0.75f, 0f), 0f);
        RobotSpawnedInstance inst;
        try
        {
            inst = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.2f, 0.7f, 0.4f));
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[S3-06] VERIFIER_DONE pass=False reason=spawn error={ex.GetType().Name}:{ex.Message}");
            yield break;
        }

        var drive = inst.Drive;
        var start = drive.transform.position;
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = 0.15f });
            yield return new WaitForFixedUpdate();
        }

        var movedEnabled = Vector3.Distance(start, drive.transform.position) > 0.05f;

        drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.35f);
        var posAfterBrake = drive.transform.position;

        RobotSpawnService.SetDisabled(inst, true);
        end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = 0.15f });
            yield return new WaitForFixedUpdate();
        }

        var stillWhileDisabled = Vector3.Distance(posAfterBrake, drive.transform.position) < 0.25f;
        var disableFlagOn = inst.DisableFlag != null && inst.DisableFlag.Disabled;

        RobotSpawnService.SetDisabled(inst, false);
        var detachOk = RobotSpawnService.TryDetach(inst, "wheel_fl", out var detachErr);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        GameObject wheelGo = null;
        if (inst.Assembly != null && inst.Assembly.Parts != null)
            inst.Assembly.Parts.TryGetValue("wheel_fl", out wheelGo);

        var unparented = wheelGo != null && wheelGo.transform.parent == null;
        var hasRb = wheelGo != null && wheelGo.GetComponent<Rigidbody>() != null;
        var noHinge = wheelGo != null && wheelGo.GetComponent<HingeJoint>() == null;
        var chassisAlive = drive != null && drive.GetComponent<Rigidbody>() != null;

        RobotSpawnService.Despawn(inst);
        yield return null;
        var destroyed = inst.Assembly == null || inst.Assembly.Root == null;

        var pass = movedEnabled && stillWhileDisabled && disableFlagOn && detachOk &&
                   unparented && hasRb && noHinge && chassisAlive && destroyed;

        Debug.Log(
            $"[S3-06] VERIFIER_DONE pass={pass} moved_enabled={movedEnabled} " +
            $"still_while_disabled={stillWhileDisabled} disable_flag={disableFlagOn} " +
            $"detach_ok={detachOk} detach_err={detachErr} unparented={unparented} " +
            $"has_rb={hasRb} no_hinge={noHinge} chassis_alive={chassisAlive} destroyed={destroyed}");
    }
}
