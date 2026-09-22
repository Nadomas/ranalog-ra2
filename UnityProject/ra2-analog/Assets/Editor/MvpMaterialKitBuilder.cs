using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// S12-03/04/05: generate thin URP material kit + checker albedo textures under Resources/Mvp.
/// </summary>
public static class MvpMaterialKitBuilder
{
    const string TexDir = "Assets/Resources/Mvp/Textures";
    const string MatDir = "Assets/Resources/Mvp/Materials";

    [MenuItem("Tools/RA2/Build MVP Material Kit (S12-03)")]
    public static void BuildFromMenu()
    {
        EnsureFolders();
        var floorTex = WriteChecker("tex_floor", new Color(0.18f, 0.2f, 0.24f), new Color(0.28f, 0.3f, 0.34f), 8);
        var metalTex = WriteNoise("tex_metal", new Color(0.5f, 0.52f, 0.56f), new Color(0.62f, 0.64f, 0.68f));
        var rubberTex = WriteSolid("tex_rubber", new Color(0.1f, 0.1f, 0.11f));
        var hazardTex = WriteStripes("tex_hazard", new Color(0.95f, 0.75f, 0.1f), new Color(0.08f, 0.08f, 0.08f));
        var accentTex = WriteSolid("tex_accent", new Color(0.78f, 0.2f, 0.12f));
        var boardTex = WriteChecker("tex_board", new Color(0.12f, 0.55f, 0.22f), new Color(0.22f, 0.85f, 0.35f), 6);
        var weaponTex = WriteSolid("tex_weapon", new Color(0.82f, 0.84f, 0.88f));
        var apronTex = WriteSolid("tex_apron", new Color(0.07f, 0.08f, 0.1f));
        var batteryTex = WriteStripes("tex_battery", new Color(0.95f, 0.82f, 0.15f), new Color(0.15f, 0.15f, 0.12f));
        var spinTex = WriteNoise("tex_spin", new Color(0.25f, 0.4f, 0.85f), new Color(0.55f, 0.75f, 1f));

        MakeLit("MvpFloor", floorTex, 0.05f, 0.35f);
        MakeLit("MvpApron", apronTex, 0f, 0.2f);
        MakeLit("MvpHazard", hazardTex, 0.1f, 0.4f);
        MakeLit("MvpMetal", metalTex, 0.65f, 0.55f);
        MakeLit("MvpRubber", rubberTex, 0f, 0.25f);
        MakeLit("MvpAccent", accentTex, 0.2f, 0.45f);
        MakeLit("MvpBoard", boardTex, 0.15f, 0.4f);
        MakeLit("MvpWeapon", weaponTex, 0.4f, 0.5f);
        MakeLit("MvpBattery", batteryTex, 0.35f, 0.45f);
        MakeLit("MvpSpin", spinTex, 0.55f, 0.5f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[S14-01] MATERIAL_KIT_BUILT mats={MatDir} tex={TexDir} (+battery/spin)");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Mvp"))
            AssetDatabase.CreateFolder("Assets/Resources", "Mvp");
        if (!AssetDatabase.IsValidFolder(TexDir))
            AssetDatabase.CreateFolder("Assets/Resources/Mvp", "Textures");
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder("Assets/Resources/Mvp", "Materials");
    }

    static void MakeLit(string name, Texture2D albedo, float metallic, float smoothness)
    {
        var path = $"{MatDir}/{name}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
            mat.shader = shader;

        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", albedo);
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", albedo);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
    }

    static Texture2D WriteSolid(string name, Color c) =>
        WritePixels(name, 64, (x, y) => c);

    static Texture2D WriteChecker(string name, Color a, Color b, int cells) =>
        WritePixels(name, 128, (x, y) =>
        {
            var cx = (x * cells) / 128;
            var cy = (y * cells) / 128;
            return ((cx + cy) & 1) == 0 ? a : b;
        });

    static Texture2D WriteStripes(string name, Color a, Color b) =>
        WritePixels(name, 128, (x, y) => ((x / 16) & 1) == 0 ? a : b);

    static Texture2D WriteNoise(string name, Color a, Color b) =>
        WritePixels(name, 128, (x, y) =>
        {
            var n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
            return Color.Lerp(a, b, n);
        });

    static Texture2D WritePixels(string name, int size, System.Func<int, int, Color> sample)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            pixels[y * size + x] = sample(x, y);
        tex.SetPixels(pixels);
        tex.Apply(false, false);

        var path = $"{TexDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
