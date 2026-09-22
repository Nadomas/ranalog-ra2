using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// S12-03 presentation material kit. Loads Resources/Mvp/Materials/* or builds shared runtime fallbacks.
    /// Does not affect physics materials / authority.
    /// </summary>
    public static class RobotMvpMaterialKit
    {
        const string ResourceRoot = "Mvp/Materials/";

        static Material floor;
        static Material apron;
        static Material hazard;
        static Material metal;
        static Material rubber;
        static Material accent;
        static Material board;
        static Material weapon;
        static Material battery;
        static Material spin;

        public static Material Floor => floor ??= LoadOrCreate("MvpFloor", new Color(0.22f, 0.24f, 0.28f), 0.05f, 0.35f);
        public static Material Apron => apron ??= LoadOrCreate("MvpApron", new Color(0.08f, 0.09f, 0.11f), 0.0f, 0.2f);
        public static Material Hazard => hazard ??= LoadOrCreate("MvpHazard", new Color(0.9f, 0.55f, 0.08f), 0.1f, 0.4f);
        public static Material Metal => metal ??= LoadOrCreate("MvpMetal", new Color(0.55f, 0.58f, 0.62f), 0.65f, 0.55f);
        public static Material Rubber => rubber ??= LoadOrCreate("MvpRubber", new Color(0.12f, 0.12f, 0.13f), 0.0f, 0.25f);
        public static Material Accent => accent ??= LoadOrCreate("MvpAccent", new Color(0.75f, 0.22f, 0.12f), 0.2f, 0.45f);
        public static Material Board => board ??= LoadOrCreate("MvpBoard", new Color(0.2f, 0.85f, 0.35f), 0.15f, 0.4f);
        public static Material Weapon => weapon ??= LoadOrCreate("MvpWeapon", new Color(0.85f, 0.85f, 0.9f), 0.4f, 0.5f);
        public static Material Battery => battery ??= LoadOrCreate("MvpBattery", new Color(0.95f, 0.78f, 0.12f), 0.35f, 0.45f);
        public static Material Spin => spin ??= LoadOrCreate("MvpSpin", new Color(0.35f, 0.55f, 0.95f), 0.55f, 0.5f);

        public static Material ForTeam(Color teamTint)
        {
            var m = new Material(Metal) { name = "MvpTeamTint" };
            if (m.HasProperty("_BaseColor"))
            {
                var baseC = m.GetColor("_BaseColor");
                m.SetColor("_BaseColor", Color.Lerp(baseC, teamTint, 0.55f));
            }
            else
                m.color = Color.Lerp(m.color, teamTint, 0.55f);
            return m;
        }

        public static void Apply(Renderer rend, Material mat)
        {
            if (rend == null || mat == null)
                return;
            rend.sharedMaterial = mat;
        }

        static Material LoadOrCreate(string name, Color color, float metallic, float smoothness)
        {
            var fromRes = Resources.Load<Material>(ResourceRoot + name);
            if (fromRes != null)
                return fromRes;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = name + "_Runtime" };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
    }
}
