using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S16-01 local-only Practice garage obstacles (RA2 Practice.py parity).
    /// Procedural dynamic props — not catalog meshes; same PhysX world as Test/Battle floor.
    /// </summary>
    public enum PracticeObstacleKind : byte
    {
        None = 0,
        Barrels = 1,
        Blocks = 2,
        Crates = 3,
        Cones = 4
    }

    public static class RobotMvpPracticeObstacles
    {
        public const string RootName = "PracticeObstacles";

        public static PracticeObstacleKind Current { get; private set; } = PracticeObstacleKind.None;

        public static PracticeObstacleKind Cycle()
        {
            var next = (PracticeObstacleKind)(((int)Current + 1) % 5);
            Apply(next);
            return Current;
        }

        public static void Clear() => Apply(PracticeObstacleKind.None);

        public static void Apply(PracticeObstacleKind kind)
        {
            Current = kind;
            var existing = GameObject.Find(RootName);
            if (existing != null)
                Object.Destroy(existing);

            if (kind == PracticeObstacleKind.None)
                return;

            var root = new GameObject(RootName);
            switch (kind)
            {
                case PracticeObstacleKind.Barrels:
                    SpawnBarrel(root.transform, new Vector3(-1.2f, 0.45f, 3.2f));
                    SpawnBarrel(root.transform, new Vector3(0f, 0.45f, 3.4f));
                    SpawnBarrel(root.transform, new Vector3(1.2f, 0.45f, 3.2f));
                    break;
                case PracticeObstacleKind.Blocks:
                    for (var i = 0; i < 4; i++)
                        SpawnBlock(root.transform, new Vector3(-1.5f + i * 1.0f, 0.35f, 3.5f));
                    break;
                case PracticeObstacleKind.Crates:
                    SpawnCrate(root.transform, new Vector3(-0.55f, 0.4f, 3.2f));
                    SpawnCrate(root.transform, new Vector3(0.55f, 0.4f, 3.2f));
                    SpawnCrate(root.transform, new Vector3(0f, 1.15f, 3.2f));
                    break;
                case PracticeObstacleKind.Cones:
                    SpawnCone(root.transform, new Vector3(-1.4f, 0.35f, 2.8f));
                    SpawnCone(root.transform, new Vector3(0f, 0.35f, 3.5f));
                    SpawnCone(root.transform, new Vector3(1.4f, 0.35f, 2.8f));
                    break;
            }
        }

        static void SpawnBarrel(Transform parent, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "barrel";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            FinishDynamic(go, 8f, RobotMvpMaterialKit.Hazard);
        }

        static void SpawnBlock(Transform parent, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "block";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            FinishDynamic(go, 12f, RobotMvpMaterialKit.Metal);
        }

        static void SpawnCrate(Transform parent, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "crate";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.85f, 0.75f, 0.85f);
            FinishDynamic(go, 10f, RobotMvpMaterialKit.Accent);
        }

        static void SpawnCone(Transform parent, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "cone";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            FinishDynamic(go, 3f, RobotMvpMaterialKit.Weapon);
        }

        static void FinishDynamic(GameObject go, float mass, Material mat)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            RobotMvpMaterialKit.Apply(go.GetComponent<Renderer>(), mat);
        }
    }
}
