using System.Collections.Generic;
using UnityEngine;

namespace Ra2.Robot
{
    public sealed class RobotAssemblyResult
    {
        public GameObject Root;
        public Rigidbody RootBody;
        public Dictionary<string, GameObject> Parts = new Dictionary<string, GameObject>();
    }

    /// <summary>
    /// Plain assembly service (S3-01). Builds Unity physics objects from <see cref="RobotBlueprint"/>.
    /// Presentation materials optional; no gameplay authority here.
    /// </summary>
    public static class RobotAssembler
    {
        public static RobotAssemblyResult Assemble(
            RobotBlueprint blueprint,
            Transform parent,
            PhysicsMaterial slideMaterial,
            Color bodyColor)
        {
            if (blueprint == null)
                throw new System.ArgumentNullException(nameof(blueprint));

            var result = new RobotAssemblyResult();
            var byId = new Dictionary<string, RobotComponentDef>();
            for (var i = 0; i < blueprint.Components.Length; i++)
                byId[blueprint.Components[i].Id] = blueprint.Components[i];

            RobotComponentDef rootDef = default;
            var foundRoot = false;
            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                if (!blueprint.Components[i].IsRoot)
                    continue;
                rootDef = blueprint.Components[i];
                foundRoot = true;
                break;
            }

            if (!foundRoot)
                throw new System.InvalidOperationException("Blueprint missing root component.");

            var root = new GameObject(string.IsNullOrEmpty(blueprint.Name) ? "AssembledRobot" : blueprint.Name);
            if (parent != null)
                root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(
                blueprint.RootPosition,
                Quaternion.Euler(0f, blueprint.RootYawDegrees, 0f));
            result.Root = root;
            result.Parts[rootDef.Id] = root;

            var chassisGo = CreatePartVisual(rootDef, bodyColor, GetChassisGripMaterial(), useBoxCollider: true, null);
            chassisGo.name = "Chassis";
            chassisGo.transform.SetParent(root.transform, false);
            result.Parts[rootDef.Id + "_mesh"] = chassisGo;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.01f, rootDef.Mass);
            rb.useGravity = true;
            rb.isKinematic = false;
            // Let pitch/roll respond to hits so wheels can plant again (no hover / freeze-upright hack).
            rb.constraints = RigidbodyConstraints.None;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.85f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            result.RootBody = rb;

            for (var i = 0; i < blueprint.Components.Length; i++)
            {
                var def = blueprint.Components[i];
                if (def.IsRoot)
                    continue;

                GameObject part;
                var baseKind = def.ResolvedBase();
                if (def.Kind == RobotComponentKind.Wheel || baseKind == RobotComponentBase.Wheel)
                    part = CreateWheelPart(def, slideMaterial);
                else if (def.Kind == RobotComponentKind.NoseMarker)
                {
                    part = CreatePartVisual(def, Color.white, null, useBoxCollider: false, RobotMvpMaterialKit.Accent);
                    part.name = "Nose";
                }
                else if (baseKind == RobotComponentBase.ControlBoard)
                {
                    part = CreatePartVisual(def, Color.white, slideMaterial, useBoxCollider: false, RobotMvpMaterialKit.Board);
                    part.name = "control_board";
                }
                else if (baseKind == RobotComponentBase.Battery)
                {
                    part = CreatePartVisual(def, Color.white, slideMaterial, useBoxCollider: false, RobotMvpMaterialKit.Battery);
                    part.name = "battery";
                }
                else if (baseKind == RobotComponentBase.AirTank)
                {
                    part = CreatePartVisual(def, Color.white, slideMaterial, useBoxCollider: false, RobotMvpMaterialKit.Metal);
                    part.name = def.Id;
                }
                else if (baseKind == RobotComponentBase.SpinMotor || baseKind == RobotComponentBase.BurstMotor ||
                         baseKind == RobotComponentBase.ServoMotor || baseKind == RobotComponentBase.BurstPiston ||
                         baseKind == RobotComponentBase.ServoPiston || baseKind == RobotComponentBase.Steering)
                {
                    // Dynamic actuators need a collider so joints / impulses have a body; visual only when no RB.
                    Material motorMat;
                    if (baseKind == RobotComponentBase.Steering)
                        motorMat = RobotMvpMaterialKit.Metal;
                    else if (baseKind == RobotComponentBase.SpinMotor)
                        motorMat = RobotMvpMaterialKit.Spin;
                    else
                        motorMat = RobotMvpMaterialKit.Accent;
                    part = CreatePartVisual(def, Color.white, slideMaterial, useBoxCollider: def.HasRigidbody, motorMat);
                    part.name = def.Id;
                }
                else if (baseKind == RobotComponentBase.SmartZone)
                {
                    part = CreatePartVisual(def, Color.white, slideMaterial, useBoxCollider: true, RobotMvpMaterialKit.Board);
                    part.name = def.Id;
                    var col = part.GetComponent<Collider>();
                    if (col != null)
                        col.isTrigger = true;
                    // Dedicated kinematic body so OnTrigger* reliably fires on the zone sensor.
                    var zoneRb = part.AddComponent<Rigidbody>();
                    zoneRb.isKinematic = true;
                    zoneRb.useGravity = false;
                    zoneRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
                else if (baseKind == RobotComponentBase.Weapon)
                {
                    part = CreatePartVisual(def, Color.white, slideMaterial, useBoxCollider: false, RobotMvpMaterialKit.Weapon);
                    part.name = def.Id;
                }
                else
                    part = CreatePartVisual(def, bodyColor, slideMaterial, useBoxCollider: true, null);

                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = def.LocalPosition;
                part.transform.localRotation = Quaternion.Euler(def.LocalEuler);
                part.transform.localScale = def.Scale;
                result.Parts[def.Id] = part;
            }

            // Hierarchy-fixed children (non-physics): parent under part so they follow hinges/sliders.
            for (var i = 0; i < blueprint.Connections.Length; i++)
            {
                var conn = blueprint.Connections[i];
                if (conn.Joint != RobotJointKind.FixedHierarchy)
                    continue;
                if (!result.Parts.TryGetValue(conn.ChildId, out var child) || child == null)
                    continue;
                if (!result.Parts.TryGetValue(conn.ParentId, out var parentGo) || parentGo == null)
                    continue;
                if (child.transform.parent == parentGo.transform)
                    continue;
                var localPos = child.transform.localPosition;
                var localRot = child.transform.localRotation;
                var localScale = child.transform.localScale;
                // If child was authored in root space and parent is not root, keep world pose.
                if (parentGo != root && child.transform.parent == root.transform)
                    child.transform.SetParent(parentGo.transform, true);
                else
                {
                    child.transform.SetParent(parentGo.transform, false);
                    child.transform.localPosition = localPos;
                    child.transform.localRotation = localRot;
                    child.transform.localScale = localScale;
                }
            }

            // Ensure dynamic bodies exist before joints so parent-chain ResolveConnectedBody
            // finds Steering hubs (S7-14) even if their hinge is declared later in Connections.
            foreach (var kv in result.Parts)
            {
                if (!byId.TryGetValue(kv.Key, out var def) || !def.HasRigidbody || kv.Value == null)
                    continue;
                EnsureDynamicBody(kv.Value, def);
            }

            for (var i = 0; i < blueprint.Connections.Length; i++)
            {
                var conn = blueprint.Connections[i];
                if (!result.Parts.TryGetValue(conn.ChildId, out var child))
                    continue;
                if (!byId.TryGetValue(conn.ChildId, out var childDef) || !childDef.HasRigidbody)
                    continue;

                if (conn.Joint == RobotJointKind.Hinge)
                {
                    EnsureDynamicBody(child, childDef);
                    var hinge = child.AddComponent<HingeJoint>();
                    hinge.connectedBody = ResolveConnectedBody(conn.ParentId, result, rb);
                    hinge.anchor = Vector3.zero;
                    hinge.axis = conn.HingeAxis.sqrMagnitude > 1e-6f ? conn.HingeAxis.normalized : Vector3.up;
                    hinge.autoConfigureConnectedAnchor = true;
                    hinge.useSpring = false;
                    hinge.useMotor = false;
                    hinge.enableCollision = false;

                    // BurstMotor Fire arc: limited hinge (<180°). ServoMotor ±90°. Steering hub ±35°. SpinMotor free.
                    if (childDef.ResolvedBase() == RobotComponentBase.BurstMotor)
                    {
                        hinge.useLimits = true;
                        hinge.limits = new JointLimits { min = 0f, max = 120f, bounciness = 0f, bounceMinVelocity = 0.1f };
                    }
                    else if (childDef.ResolvedBase() == RobotComponentBase.ServoMotor)
                    {
                        hinge.useLimits = true;
                        hinge.limits = new JointLimits { min = -90f, max = 90f, bounciness = 0f, bounceMinVelocity = 0.1f };
                    }
                    else if (childDef.ResolvedBase() == RobotComponentBase.Steering)
                    {
                        hinge.useLimits = true;
                        hinge.limits = new JointLimits { min = -35f, max = 35f, bounciness = 0f, bounceMinVelocity = 0.1f };
                    }
                    else
                        hinge.useLimits = false;
                }
                else if (conn.Joint == RobotJointKind.Slider)
                {
                    EnsureDynamicBody(child, childDef);
                    var axis = conn.HingeAxis.sqrMagnitude > 1e-6f ? conn.HingeAxis.normalized : Vector3.forward;
                    var slide = child.AddComponent<ConfigurableJoint>();
                    slide.connectedBody = ResolveConnectedBody(conn.ParentId, result, rb);
                    slide.anchor = Vector3.zero;
                    slide.axis = axis;
                    slide.autoConfigureConnectedAnchor = true;
                    slide.xMotion = ConfigurableJointMotion.Limited;
                    slide.yMotion = ConfigurableJointMotion.Locked;
                    slide.zMotion = ConfigurableJointMotion.Locked;
                    slide.angularXMotion = ConfigurableJointMotion.Locked;
                    slide.angularYMotion = ConfigurableJointMotion.Locked;
                    slide.angularZMotion = ConfigurableJointMotion.Locked;
                    slide.linearLimit = new SoftJointLimit { limit = 0.45f, bounciness = 0f };
                    slide.linearLimitSpring = new SoftJointLimitSpring { spring = 0f, damper = 0f };
                    slide.enableCollision = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Hinge/slider attach to nearest Rigidbody on the connection parent chain (S7-14 Ackermann).
        /// Falls back to chassis root — preserves tank wheel→motor(no RB)→chassis behavior.
        /// </summary>
        static Rigidbody ResolveConnectedBody(
            string parentId,
            RobotAssemblyResult result,
            Rigidbody rootBody)
        {
            if (string.IsNullOrEmpty(parentId) || result?.Parts == null)
                return rootBody;
            if (!result.Parts.TryGetValue(parentId, out var parentGo) || parentGo == null)
                return rootBody;

            var t = parentGo.transform;
            while (t != null)
            {
                var body = t.GetComponent<Rigidbody>();
                if (body != null)
                    return body;
                t = t.parent;
            }

            return rootBody;
        }

        static void EnsureDynamicBody(GameObject child, RobotComponentDef childDef)
        {
            var body = child.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = child.AddComponent<Rigidbody>();
                body.mass = Mathf.Max(0.01f, childDef.Mass);
                body.linearDamping = 0.05f;
                body.angularDamping = 0.35f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            body.useGravity = true;
            body.isKinematic = false;
        }

        static PhysicsMaterial wheelGrip;
        static PhysicsMaterial chassisGrip;

        static PhysicsMaterial GetChassisGripMaterial()
        {
            if (chassisGrip != null)
                return chassisGrip;
            chassisGrip = new PhysicsMaterial("RuntimeChassisGrip")
            {
                dynamicFriction = 0.55f,
                staticFriction = 0.65f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return chassisGrip;
        }

        static PhysicsMaterial GetWheelGripMaterial()
        {
            if (wheelGrip != null)
                return wheelGrip;
            wheelGrip = new PhysicsMaterial("RuntimeWheelGrip")
            {
                dynamicFriction = 0.9f,
                staticFriction = 1.05f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return wheelGrip;
        }

        static void StripPrimitiveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(col);
            else
                Object.DestroyImmediate(col);
        }

        static GameObject CreatePartVisual(
            RobotComponentDef def,
            Color color,
            PhysicsMaterial slide,
            bool useBoxCollider,
            Material materialOverride)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripPrimitiveCollider(go);
            if (useBoxCollider)
            {
                var box = go.AddComponent<BoxCollider>();
                if (slide != null)
                    box.sharedMaterial = slide;
            }

            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                if (materialOverride != null)
                    RobotMvpMaterialKit.Apply(rend, materialOverride);
                else
                    rend.sharedMaterial = RobotMvpMaterialKit.ForTeam(color);
            }
            return go;
        }

        static GameObject CreateWheelPart(RobotComponentDef def, PhysicsMaterial slide)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripPrimitiveCollider(go);
            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            if (slide != null)
                sphere.sharedMaterial = slide;
            if (def.HasRigidbody)
            {
                var grip = GetWheelGripMaterial();
                if (grip != null)
                    sphere.sharedMaterial = grip;
            }
            var rend = go.GetComponent<MeshRenderer>();
            RobotMvpMaterialKit.Apply(rend, RobotMvpMaterialKit.Rubber);
            go.name = def.Id;
            return go;
        }

    }
}
