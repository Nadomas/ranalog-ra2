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

            var chassisGo = CreatePartVisual(rootDef, bodyColor, slideMaterial, useBoxCollider: true);
            chassisGo.name = "Chassis";
            chassisGo.transform.SetParent(root.transform, false);
            result.Parts[rootDef.Id + "_mesh"] = chassisGo;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.01f, rootDef.Mass);
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
                    part = CreatePartVisual(def, Color.white, null, useBoxCollider: false);
                    part.name = "Nose";
                }
                else if (baseKind == RobotComponentBase.ControlBoard)
                {
                    part = CreatePartVisual(def, new Color(0.2f, 0.85f, 0.35f), slideMaterial, useBoxCollider: false);
                    part.name = "control_board";
                }
                else if (baseKind == RobotComponentBase.Battery)
                {
                    part = CreatePartVisual(def, new Color(0.95f, 0.85f, 0.1f), slideMaterial, useBoxCollider: false);
                    part.name = "battery";
                }
                else if (baseKind == RobotComponentBase.AirTank)
                {
                    part = CreatePartVisual(def, new Color(0.55f, 0.55f, 0.65f), slideMaterial, useBoxCollider: false);
                    part.name = def.Id;
                }
                else if (baseKind == RobotComponentBase.SpinMotor || baseKind == RobotComponentBase.BurstMotor ||
                         baseKind == RobotComponentBase.ServoMotor || baseKind == RobotComponentBase.BurstPiston ||
                         baseKind == RobotComponentBase.ServoPiston)
                {
                    // Dynamic actuators need a collider so joints / impulses have a body; visual only when no RB.
                    part = CreatePartVisual(def, new Color(0.75f, 0.25f, 0.2f), slideMaterial, useBoxCollider: def.HasRigidbody);
                    part.name = def.Id;
                }
                else if (baseKind == RobotComponentBase.Weapon)
                {
                    part = CreatePartVisual(def, new Color(0.85f, 0.85f, 0.9f), slideMaterial, useBoxCollider: false);
                    part.name = def.Id;
                }
                else
                    part = CreatePartVisual(def, bodyColor, slideMaterial, useBoxCollider: true);

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
                    hinge.connectedBody = rb;
                    hinge.anchor = Vector3.zero;
                    hinge.axis = conn.HingeAxis.sqrMagnitude > 1e-6f ? conn.HingeAxis.normalized : Vector3.up;
                    hinge.autoConfigureConnectedAnchor = true;
                    hinge.useSpring = false;
                    hinge.useMotor = false;
                    hinge.enableCollision = false;

                    // BurstMotor Fire arc: limited hinge (<180°). SpinMotor free.
                    if (childDef.ResolvedBase() == RobotComponentBase.BurstMotor)
                    {
                        hinge.useLimits = true;
                        hinge.limits = new JointLimits { min = 0f, max = 120f, bounciness = 0f, bounceMinVelocity = 0.1f };
                    }
                    else
                        hinge.useLimits = false;
                }
                else if (conn.Joint == RobotJointKind.Slider)
                {
                    EnsureDynamicBody(child, childDef);
                    var axis = conn.HingeAxis.sqrMagnitude > 1e-6f ? conn.HingeAxis.normalized : Vector3.forward;
                    var slide = child.AddComponent<ConfigurableJoint>();
                    slide.connectedBody = rb;
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

        static void EnsureDynamicBody(GameObject child, RobotComponentDef childDef)
        {
            var body = child.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = child.AddComponent<Rigidbody>();
                body.mass = Mathf.Max(0.01f, childDef.Mass);
                body.linearDamping = 0.05f;
                body.angularDamping = 0.25f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        static PhysicsMaterial wheelGrip;

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
            bool useBoxCollider)
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
                rend.sharedMaterial = CreateRuntimeColorMaterial(def.Id, color);
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
            if (rend != null)
                rend.sharedMaterial = CreateRuntimeColorMaterial(def.Id, new Color(0.12f, 0.12f, 0.12f));
            go.name = def.Id;
            return go;
        }

        static Material CreateRuntimeColorMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = "Runtime_" + name };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            return mat;
        }
    }
}
