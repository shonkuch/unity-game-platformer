using System;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Builds the game's particle effects from code and wires them into the prefabs and scenes (Tools > VFX).
// Build: generates the textures, materials, effect prefabs and the portal flash profile under Assets/Effects/VFX,
// overwriting earlier output (hand tweaks to generated prefabs are lost; tweak the instances or this file instead).
// Hand-made textures in Assets/Effects/VFX/Provided replace the generated ones when present:
// Slash.png (single-row sheet of square frames, facing right) and PortalRing.png (one greyscale ring or rune).
// Wire: adds effect instances as "VFX_*" children and fills the effect fields of the Player, Projectile and
// TimedLaserBeam prefabs and of every scene under Assets/Scenes. Re-running replaces those children.
public static class VfxBuilder
{
    private const string Root = "Assets/Effects/VFX";
    private const string TexturesPath = Root + "/Textures";
    private const string MaterialsPath = Root + "/Materials";
    private const string PrefabsPath = Root + "/Prefabs";
    private const string ProvidedPath = Root + "/Provided";
    private const string FlashProfilePath = Root + "/PortalFlashProfile.asset";
    private const string ScenesPath = "Assets/Scenes";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
    private const string LaserPrefabPath = "Assets/Prefabs/TimedLaserBeam.prefab";
    private const string SortingLayer = "Player";

    // One pixel of the 32 PPU art.
    private const float Px = 1f / 32f;

    // Sorting orders on the Player layer (the player sprite is at 0).
    private const int BehindPlayer = -1;
    private const int BehindPlayerFar = -2;
    private const int Front = 10;
    private const int FrontTop = 12;

    // Palette sampled from the sprites.
    private static readonly Color Dust = Hex("5a6a70");
    private static readonly Color DustDeep = Hex("3a4a50");
    private static readonly Color DustTeal = Hex("309098");
    private static readonly Color White = Hex("f8f8f8");
    private static readonly Color Yellow = Hex("f8d800");
    private static readonly Color Peach = Hex("f8b060");
    private static readonly Color Orange = Hex("e87808");
    private static readonly Color Lavender = Hex("a090d0");
    private static readonly Color Magenta = Hex("c018e8");
    private static readonly Color Plum = Hex("902088");
    private static readonly Color Purple = Hex("402860");
    private static readonly Color HotYellow = Hex("f8f828");
    private static readonly Color Amber = Hex("f8a838");
    private static readonly Color Red = Hex("c82038");
    private static readonly Color Crimson = Hex("880840");
    private static readonly Color Lime = Hex("c8f870");
    private static readonly Color Green = Hex("88d858");
    private static readonly Color Jade = Hex("38a070");
    private static readonly Color Ice = Hex("b8f8f8");

    private static class Names
    {
        public const string Landing = "VFX_Landing";
        public const string Jump = "VFX_Jump";
        public const string Run = "VFX_Run";
        public const string WallGrab = "VFX_WallGrab";
        public const string Attack = "VFX_Attack";
        public const string Crouch = "VFX_Crouch";
        // No longer built; removed from earlier output.
        public const string CloakOn = "VFX_CloakOn";
        public const string CloakBreak = "VFX_CloakBreak";
        public const string HitSparks = "VFX_HitSparks";
        public const string HitSparksPlayer = "VFX_HitSparks_Player";
        public const string DeathEnemy = "VFX_Death_Enemy";
        public const string DeathPlayer = "VFX_Death_Player";
        public const string MuzzleFlash = "VFX_MuzzleFlash";
        public const string ProjectileTrail = "VFX_ProjectileTrail";
        public const string ProjectileImpact = "VFX_ProjectileImpact";
        public const string LaserTelegraph = "VFX_LaserTelegraph";
        public const string LaserActivate = "VFX_LaserActivate";
        public const string LaserDeactivate = "VFX_LaserDeactivate";
        // No longer built; removed from earlier output.
        public const string Alert = "VFX_Alert";
        public const string PortalIdle = "VFX_PortalIdle";
        public const string PortalEnter = "VFX_PortalEnter";
        public const string TelegraphLine = "VFX_TelegraphLine";
        public const string PortalFlash = "VFX_PortalFlash";
    }

    private struct Materials
    {
        public Material Pixel;
        public Material GlowPixel;
        public Material Glow;
        public Material Star;
        public Material Slash;
        public int SlashFrames;
        public Material Ring;
        public Material Portal;
    }

    [MenuItem("Tools/VFX/Build And Wire")]
    public static void BuildAndWire()
    {
        if (Build()) Wire();
    }

    [MenuItem("Tools/VFX/Build Effects")]
    public static bool Build()
    {
        foreach (string folder in new[] { TexturesPath, MaterialsPath, PrefabsPath, ProvidedPath })
            Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        Shader shader = Shader.Find("Custom/VfxParticle");
        if (shader == null)
        {
            Debug.LogError("VfxBuilder: shader Custom/VfxParticle not found (Assets/Effects/VfxParticle.shader).");
            return false;
        }

        Materials materials = BuildMaterials(shader);
        BuildPrefabs(materials);
        BuildFlashProfile();

        AssetDatabase.SaveAssets();
        Debug.Log($"VfxBuilder: effects built in {Root}.");
        return true;
    }

    [MenuItem("Tools/VFX/Wire Into Project")]
    public static void Wire()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (Load(Names.Landing) == null)
        {
            Debug.LogError("VfxBuilder: effect prefabs are missing; run Tools > VFX > Build Effects first.");
            return;
        }

        string activeScene = SceneManager.GetActiveScene().path;

        EditPrefab(PlayerPrefabPath, root => WirePlayer(root));
        EditPrefab(ProjectilePrefabPath, WireProjectile);
        EditPrefab(LaserPrefabPath, root => WireLaser(root.GetComponent<TimedLaser>()));

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { ScenesPath }))
        {
            Scene scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
            WireScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(activeScene)) EditorSceneManager.OpenScene(activeScene, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("VfxBuilder: effects wired into the prefabs and scenes.");
    }

    // ---------------------------------------------------------------- Textures and materials

    private static Materials BuildMaterials(Shader shader)
    {
        Texture2D pixel = WriteTexture("Pixel", 4, 4, true, (x, y) => Color.white);
        Texture2D soft = WriteTexture("SoftDot", 32, 32, false, SoftDot);
        Texture2D star = WriteTexture("Star", 64, 16, true, Star);
        Texture2D ring = WriteTexture("Ring", 32, 32, true, Ring);

        Texture2D slash = Provided("Slash.png") ?? WriteTexture("Slash", 192, 48, true, Slash);
        Texture2D portal = Provided("PortalRing.png") ?? ring;

        return new Materials
        {
            Pixel = WriteMaterial("M_Pixel", shader, pixel, false, 1f),
            GlowPixel = WriteMaterial("M_GlowPixel", shader, pixel, true, 2.5f),
            Glow = WriteMaterial("M_Glow", shader, soft, true, 2f),
            Star = WriteMaterial("M_Star", shader, star, true, 2.5f),
            Slash = WriteMaterial("M_Slash", shader, slash, true, 2f),
            SlashFrames = Mathf.Max(1, Mathf.RoundToInt(slash.width / (float)slash.height)),
            Ring = WriteMaterial("M_Ring", shader, ring, true, 2f),
            Portal = WriteMaterial("M_Portal", shader, portal, true, 2f),
        };
    }

    private static Color SoftDot(int x, int y)
    {
        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
        float alpha = Mathf.Clamp01(1f - distance);
        return new Color(1f, 1f, 1f, alpha * alpha);
    }

    // Four 16 px frames of a shrinking four-point star with short diagonals.
    private static Color Star(int x, int y)
    {
        int frame = x / 16;
        float arm = 7f - frame * 2f;
        float dx = Mathf.Abs(x % 16 - 7.5f);
        float dy = Mathf.Abs(y - 7.5f);

        bool core = dx < 1.5f && dy < 1.5f;
        bool cross = (dx < 1f && dy <= arm) || (dy < 1f && dx <= arm);
        bool diagonal = Mathf.Abs(dx - dy) < 0.6f && dx <= arm * 0.5f;
        return core || cross || diagonal ? Color.white : Color.clear;
    }

    // A hard pixel ring with four gaps, so its spin reads.
    private static Color Ring(int x, int y)
    {
        Vector2 offset = new(x + 0.5f - 16f, y + 0.5f - 16f);
        float radius = offset.magnitude;
        float angle = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg, 90f);
        bool band = radius >= 12f && radius <= 14.5f;
        bool gap = angle < 14f;
        return band && !gap ? Color.white : Color.clear;
    }

    // Four 48 px frames of a crescent swipe facing right: it sweeps from low to high, thickening then thinning.
    private static Color Slash(int x, int y)
    {
        int frame = x / 48;
        Vector2 offset = new(x % 48 + 0.5f - 14f, y + 0.5f - 24f);
        float radius = offset.magnitude;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

        float[] starts = { -75f, -75f, -45f, 0f };
        float[] ends = { -15f, 40f, 75f, 75f };
        float[] thickness = { 4f, 6f, 5f, 2f };
        if (angle < starts[frame] || angle > ends[frame]) return Color.clear;

        // Tapers toward both ends of the visible arc.
        float along = Mathf.InverseLerp(starts[frame], ends[frame], angle);
        float taper = Mathf.Sin(along * Mathf.PI);
        float inner = 22f - thickness[frame] * taper;
        return radius >= inner && radius <= 23f ? Color.white : Color.clear;
    }

    private static Texture2D WriteTexture(string name, int width, int height, bool point, Func<int, int, Color> pixel)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            texture.SetPixel(x, y, pixel(x, y));
        texture.Apply();

        string path = $"{TexturesPath}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ConfigureImporter(path, point);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D Provided(string fileName)
    {
        string path = $"{ProvidedPath}/{fileName}";
        if (!File.Exists(path)) return null;

        ConfigureImporter(path, true);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void ConfigureImporter(string path, bool point)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    private static Material WriteMaterial(string name, Shader shader, Texture texture, bool additive, float intensity)
    {
        string path = $"{MaterialsPath}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.mainTexture = texture;
        material.SetColor("_Color", new Color(intensity, intensity, intensity, 1f));
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildFlashProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FlashProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, FlashProfilePath);
        }

        if (!profile.TryGet(out ColorAdjustments adjustments))
        {
            adjustments = profile.Add<ColorAdjustments>(true);
            adjustments.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(adjustments, profile);
        }

        adjustments.postExposure.Override(2.5f);
        adjustments.colorFilter.Override(new Color(0.85f, 1f, 0.8f));
        EditorUtility.SetDirty(adjustments);
        EditorUtility.SetDirty(profile);
    }

    // ---------------------------------------------------------------- Effect prefabs

    private static void BuildPrefabs(Materials m)
    {
        AssetDatabase.DeleteAsset($"{PrefabsPath}/{Names.CloakOn}.prefab");
        AssetDatabase.DeleteAsset($"{PrefabsPath}/{Names.Alert}.prefab");

        // Movement dust: alpha-blended pixels behind the player.
        Save(Names.Landing, () =>
        {
            ParticleSystem ps = NewSystem(Names.Landing, null, m.Pixel, BehindPlayer);
            Main(ps, 0.25f, 0.45f, 1.2f, 2.6f, 2 * Px, 3 * Px, Dust, DustDeep, 0.4f);
            Burst(ps, 10);
            Cone(ps, 80f, 0.2f, new Vector3(-90f, 0f, 0f));
            Drag(ps, 4f);
            FadeOut(ps);
            Shrink(ps);
            ScaleSize(ps, 2.5f * 2.5f);
            return ps;
        });

        Save(Names.Jump, () =>
        {
            ParticleSystem ps = NewSystem(Names.Jump, null, m.Pixel, BehindPlayer);
            Main(ps, 0.2f, 0.35f, 0.8f, 1.8f, 2 * Px, 3 * Px, Dust, DustDeep, 0.3f);
            Burst(ps, 6);
            Cone(ps, 60f, 0.15f, new Vector3(-90f, 0f, 0f));
            Drag(ps, 4f);
            FadeOut(ps);
            Shrink(ps);
            ScaleSize(ps, 2.5f);
            return ps;
        });

        Save(Names.Run, () =>
        {
            ParticleSystem ps = NewSystem(Names.Run, null, m.Pixel, BehindPlayer);
            Main(ps, 0.25f, 0.4f, 0.2f, 0.6f, Px, 2 * Px, Dust, DustDeep, 0f);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverDistance = 5f;
            Cone(ps, 45f, 0.05f, new Vector3(-90f, 0f, 0f));
            FadeOut(ps);
            Shrink(ps);
            return ps;
        });

        // Chips off the wall, flying away from it (authored for the right wall).
        Save(Names.WallGrab, () =>
        {
            ParticleSystem ps = NewSystem(Names.WallGrab, null, m.Pixel, BehindPlayer);
            Main(ps, 0.25f, 0.5f, 1f, 2.5f, Px, 2 * Px, Dust, DustTeal, 1.5f);
            Burst(ps, 7);
            Cone(ps, 50f, 0.25f, new Vector3(0f, -90f, 0f));
            Drag(ps, 3f);
            FadeOut(ps);
            ScaleSize(ps, 2.5f);
            return ps;
        });

        Save(Names.Crouch, () =>
        {
            ParticleSystem ps = NewSystem(Names.Crouch, null, m.Pixel, BehindPlayer);
            Main(ps, 0.2f, 0.3f, 0.6f, 1.2f, 2 * Px, 2 * Px, Dust, DustDeep, 0f);
            Burst(ps, 4);
            Cone(ps, 85f, 0.25f, new Vector3(-90f, 0f, 0f));
            Drag(ps, 5f);
            FadeOut(ps);
            return ps;
        });

        // Attack: one additive slash sprite that follows the player (authored facing right), plus forward sparks.
        Save(Names.Attack, () =>
        {
            ParticleSystem ps = NewSystem(Names.Attack, null, m.Slash, Front);
            Main(ps, 0.18f, 0.18f, 0f, 0f, 1.6f, 1.6f, White, White, 0f);
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            Burst(ps, 1);
            Sheet(ps, m.SlashFrames);
            ColorOverLife(ps, Yellow, Orange, 1f, 0.85f);
            GetRenderer(ps).alignment = ParticleSystemRenderSpace.Local;

            ParticleSystem sparks = NewSystem("Sparks", ps.transform, m.GlowPixel, Front);
            Main(sparks, 0.1f, 0.2f, 3f, 5f, Px, 2 * Px, Yellow, White, 0f);
            Burst(sparks, 5);
            Cone(sparks, 35f, 0.1f, new Vector3(0f, 90f, 0f));
            Stretch(sparks);
            FadeOut(sparks);
            return ps;
        });

        Save(Names.CloakBreak, () =>
        {
            ParticleSystem ps = NewSystem(Names.CloakBreak, null, m.GlowPixel, Front);
            Main(ps, 0.2f, 0.4f, 1.5f, 3f, Px, 2 * Px, Magenta, Lavender, 0f);
            Burst(ps, 18);
            Circle(ps, 0.3f);
            Drag(ps, 4f);
            FadeOut(ps);
            Flash(ps.transform, m.Glow, Plum, 1.4f, 0.12f, Front);
            ScaleSize(ps, 1.5f);
            return ps;
        });

        // Combat.
        Save(Names.HitSparks, () => HitSparks(Names.HitSparks, m, Yellow, White, Yellow, Orange, White));
        Save(Names.HitSparksPlayer, () => HitSparks(Names.HitSparksPlayer, m, Red, HotYellow, Amber, Crimson, Red));
        Save(Names.DeathEnemy, () => Death(Names.DeathEnemy, m, Red, Amber, HotYellow, Amber, Amber, 2.5f, 0.5f, 0.9f));
        Save(Names.DeathPlayer, () => Death(Names.DeathPlayer, m, Plum, Purple, Lavender, Magenta, Magenta, 1.5f, 0.6f, 1.1f));

        Save(Names.MuzzleFlash, () =>
        {
            ParticleSystem ps = NewSystem(Names.MuzzleFlash, null, m.Star, Front);
            Main(ps, 0.08f, 0.08f, 0f, 0f, 0.45f, 0.45f, White, White, 0f);
            Burst(ps, 1);
            Sheet(ps, 4);
            ColorOverLife(ps, HotYellow, Red, 1f, 0f);
            GetRenderer(ps).alignment = ParticleSystemRenderSpace.Local;

            ParticleSystem sparks = NewSystem("Sparks", ps.transform, m.GlowPixel, Front);
            Main(sparks, 0.08f, 0.14f, 3f, 5f, Px, Px, Amber, Red, 0f);
            Burst(sparks, 3);
            Cone(sparks, 20f, 0f, new Vector3(0f, 90f, 0f));
            Stretch(sparks);
            FadeOut(sparks);
            return ps;
        });

        Save(Names.ProjectileTrail, () =>
        {
            ParticleSystem ps = NewSystem(Names.ProjectileTrail, null, m.GlowPixel, Front);
            Main(ps, 0.15f, 0.25f, 0f, 0f, Px, 2 * Px, Amber, Red, 0f);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverDistance = 24f;
            ColorOverLife(ps, Amber, Crimson, 1f, 0f);
            Shrink(ps);
            return ps;
        });

        // Impact: authored for a projectile flying right, so debris sprays back left.
        Save(Names.ProjectileImpact, () =>
        {
            ParticleSystem ps = NewSystem(Names.ProjectileImpact, null, m.Star, Front);
            Main(ps, 0.1f, 0.1f, 0f, 0f, 0.5f, 0.5f, White, White, 0f);
            Burst(ps, 1);
            Sheet(ps, 4);
            ColorOverLife(ps, HotYellow, Red, 1f, 0f);

            ParticleSystem sparks = NewSystem("Sparks", ps.transform, m.GlowPixel, Front);
            Main(sparks, 0.1f, 0.2f, 2f, 5f, Px, Px, Amber, Red, 0f);
            Burst(sparks, 6);
            Cone(sparks, 50f, 0f, new Vector3(0f, -90f, 0f));
            Stretch(sparks);
            Drag(sparks, 4f);
            FadeOut(sparks);

            ParticleSystem debris = NewSystem("Debris", ps.transform, m.Pixel, Front);
            Main(debris, 0.25f, 0.35f, 1f, 2f, Px, 2 * Px, Dust, DustDeep, 2f);
            Burst(debris, 4);
            Cone(debris, 60f, 0f, new Vector3(0f, -90f, 0f));
            FadeOut(debris);
            return ps;
        });

        // Hazards.
        Save(Names.LaserTelegraph, () =>
        {
            ParticleSystem ps = NewSystem(Names.LaserTelegraph, null, m.GlowPixel, Front);
            Main(ps, 0.1f, 0.25f, 0.5f, 1.5f, Px, Px, Red, Crimson, 0f);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 30f;
            Circle(ps, 0.05f);
            FadeOut(ps);
            return ps;
        });

        Save(Names.LaserActivate, () =>
        {
            ParticleSystem ps = NewSystem(Names.LaserActivate, null, m.GlowPixel, Front);
            Main(ps, 0.1f, 0.2f, 2f, 4f, Px, 2 * Px, HotYellow, Red, 0f);
            Burst(ps, 10);
            Circle(ps, 0.05f);
            Stretch(ps);
            FadeOut(ps);
            Flash(ps.transform, m.Glow, Red, 0.8f, 0.1f, Front);
            return ps;
        });

        // Power down: dim embers that drop and cool off, under a faint pop.
        Save(Names.LaserDeactivate, () =>
        {
            ParticleSystem ps = NewSystem(Names.LaserDeactivate, null, m.GlowPixel, Front);
            Main(ps, 0.25f, 0.45f, 0.5f, 1.5f, Px, 2 * Px, White, White, 1f);
            Burst(ps, 8);
            Circle(ps, 0.05f);
            Drag(ps, 3f);
            ColorOverLife(ps, Red, Crimson, 1f, 0f);
            Flash(ps.transform, m.Glow, Crimson, 0.6f, 0.12f, Front);
            return ps;
        });

        // Portal.
        Save(Names.PortalIdle, () =>
        {
            ParticleSystem ps = NewSystem(Names.PortalIdle, null, m.Portal, BehindPlayerFar);
            Main(ps, 2f, 2f, 0f, 0f, 1.6f, 1.8f, Green, Jade, 0f);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.maxParticles = 5;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 1.5f;
            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = 60f * Mathf.Deg2Rad;
            FadeInOut(ps, 0.8f);
            SizeOverLife(ps, 0.8f, 1.1f);

            ParticleSystem motes = NewSystem("Motes", ps.transform, m.GlowPixel, BehindPlayerFar);
            Main(motes, 0.6f, 1f, 0f, 0f, Px, 2 * Px, Lime, Green, 0f);
            ParticleSystem.MainModule motesMain = motes.main;
            motesMain.loop = true;
            motesMain.playOnAwake = true;
            motesMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule motesEmission = motes.emission;
            motesEmission.rateOverTime = 14f;
            Circle(motes, 0.9f);
            ParticleSystem.ShapeModule shape = motes.shape;
            shape.radiusThickness = 0f;
            ParticleSystem.VelocityOverLifetimeModule velocity = motes.velocityOverLifetime;
            velocity.enabled = true;
            velocity.radial = -1.2f;
            velocity.orbitalZ = 2f;
            FadeInOut(motes, 1f);
            return ps;
        });

        Save(Names.PortalEnter, () =>
        {
            ParticleSystem ps = NewSystem(Names.PortalEnter, null, m.GlowPixel, FrontTop);
            Main(ps, 0.3f, 0.6f, 2f, 6f, Px, 3 * Px, Lime, Ice, 0f);
            Burst(ps, 30);
            Circle(ps, 0.1f);
            Drag(ps, 3f);
            FadeOut(ps);

            ParticleSystem ring = NewSystem("Ring", ps.transform, m.Portal, FrontTop);
            Main(ring, 0.45f, 0.45f, 0f, 0f, 3f, 3f, White, White, 0f);
            Burst(ring, 1);
            ColorOverLife(ring, Ice, Green, 1f, 0f);
            SizeOverLife(ring, 0.1f, 1f);

            Flash(ps.transform, m.Glow, Lime, 2.5f, 0.3f, FrontTop);
            return ps;
        });
    }

    private static ParticleSystem HitSparks(string name, Materials m, Color a, Color b, Color from, Color to, Color flash)
    {
        ParticleSystem ps = NewSystem(name, null, m.GlowPixel, Front);
        Main(ps, 0.12f, 0.25f, 3f, 6f, Px, 2 * Px, a, b, 0f);
        Burst(ps, 9);
        Circle(ps, 0.05f);
        Stretch(ps);
        Drag(ps, 5f);
        ColorOverLife(ps, from, to, 1f, 0f);
        Flash(ps.transform, m.Glow, flash, 0.7f, 0.07f, Front);
        return ps;
    }

    private static ParticleSystem Death(string name, Materials m, Color debrisA, Color debrisB, Color sparkA,
        Color sparkB, Color flash, float gravity, float minLife, float maxLife)
    {
        ParticleSystem ps = NewSystem(name, null, m.Pixel, Front);
        Main(ps, minLife, maxLife, 2f, 5f, 2 * Px, 3 * Px, debrisA, debrisB, gravity);
        ParticleSystem.MainModule main = ps.main;
        main.duration = maxLife;
        Burst(ps, 16);
        Circle(ps, 0.2f);
        Drag(ps, 1.5f);
        FadeOut(ps, 0.6f);

        ParticleSystem sparks = NewSystem("Sparks", ps.transform, m.GlowPixel, Front);
        Main(sparks, 0.15f, 0.3f, 4f, 7f, Px, 2 * Px, sparkA, sparkB, 0f);
        Burst(sparks, 12);
        Circle(sparks, 0.1f);
        Stretch(sparks);
        Drag(sparks, 4f);
        FadeOut(sparks);

        Flash(ps.transform, m.Glow, flash, 1.8f, 0.12f, Front);
        return ps;
    }

    // A single soft additive particle that grows and fades: the bright pop under bursts.
    private static void Flash(Transform parent, Material material, Color color, float size, float lifetime, int order)
    {
        ParticleSystem ps = NewSystem("Flash", parent, material, order);
        Main(ps, lifetime, lifetime, 0f, 0f, size, size, color, color, 0f);
        Burst(ps, 1);
        FadeOut(ps);
        SizeOverLife(ps, 0.5f, 1f);
    }

    // ---------------------------------------------------------------- Particle system helpers

    private static ParticleSystem NewSystem(string name, Transform parent, Material material, int order)
    {
        var host = new GameObject(name);
        if (parent != null) host.transform.SetParent(parent, false);

        ParticleSystem ps = host.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 200;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = GetRenderer(ps);
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = order;
        return ps;
    }

    private static ParticleSystemRenderer GetRenderer(ParticleSystem ps) => ps.GetComponent<ParticleSystemRenderer>();

    private static void Main(ParticleSystem ps, float minLife, float maxLife, float minSpeed, float maxSpeed,
        float minSize, float maxSize, Color a, Color b, float gravity)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.gravityModifier = gravity;
    }

    private static void Burst(ParticleSystem ps, int count)
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    // Cones emit along local +Z; rotation turns that axis into the 2D plane.
    private static void Cone(ParticleSystem ps, float angle, float radius, Vector3 rotation)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = Mathf.Max(radius, 0.0001f);
        shape.rotation = rotation;
    }

    // Circles lie in the XY plane and emit outward.
    private static void Circle(ParticleSystem ps, float radius)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
    }

    private static void Box(ParticleSystem ps, Vector3 size)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
    }

    private static void Drag(ParticleSystem ps, float drag)
    {
        ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100f;
        limit.dampen = 0f;
        limit.drag = drag;
        limit.multiplyDragByParticleSize = false;
        limit.multiplyDragByParticleVelocity = false;
    }

    private static void Stretch(ParticleSystem ps)
    {
        ParticleSystemRenderer renderer = GetRenderer(ps);
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.03f;
        renderer.lengthScale = 1f;
    }

    private static void Sheet(ParticleSystem ps, int frames)
    {
        ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
        sheet.enabled = frames > 1;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = frames;
        sheet.numTilesY = 1;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.cycleCount = 1;
    }

    // Alpha holds until fadeStart (fraction of life), then falls to 0.
    private static void FadeOut(ParticleSystem ps, float fadeStart = 0.3f)
    {
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = MakeGradient(Color.white, Color.white, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
    }

    private static void FadeInOut(ParticleSystem ps, float peak)
    {
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = MakeGradient(Color.white, Color.white, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.3f), new GradientAlphaKey(0f, 1f) });
    }

    // Color and alpha over life; multiplies the start color, so start colors stay white for these systems.
    private static void ColorOverLife(ParticleSystem ps, Color from, Color to, float alphaFrom, float alphaTo)
    {
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = MakeGradient(from, to, new[] { new GradientAlphaKey(alphaFrom, 0f), new GradientAlphaKey(alphaTo, 1f) });
    }

    // Multiplies the start size of every system in the effect (spread and speed stay).
    private static void ScaleSize(ParticleSystem ps, float factor)
    {
        foreach (ParticleSystem system in ps.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            ParticleSystem.MinMaxCurve size = main.startSize;
            main.startSize = new ParticleSystem.MinMaxCurve(size.constantMin * factor, size.constantMax * factor);
        }
    }

    private static void Shrink(ParticleSystem ps) => SizeOverLife(ps, 1f, 0f);

    private static void SizeOverLife(ParticleSystem ps, float from, float to)
    {
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
    }

    private static Gradient MakeGradient(Color from, Color to, GradientAlphaKey[] alphas)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) }, alphas);
        return gradient;
    }

    private static void Save(string name, Func<ParticleSystem> build)
    {
        ParticleSystem ps = build();
        PrefabUtility.SaveAsPrefabAsset(ps.gameObject, $"{PrefabsPath}/{name}.prefab");
        Object.DestroyImmediate(ps.gameObject);
    }

    private static ParticleSystem Load(string name) =>
        AssetDatabase.LoadAssetAtPath<ParticleSystem>($"{PrefabsPath}/{name}.prefab");

    // ---------------------------------------------------------------- Wiring

    private static void EditPrefab(string path, Action<GameObject> wire)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            wire(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PlayerController player in root.GetComponentsInChildren<PlayerController>(true))
                if (!IsInstanceOf(player, PlayerPrefabPath)) WirePlayer(player.gameObject);

            foreach (EnemyController enemy in root.GetComponentsInChildren<EnemyController>(true))
                WireEnemy(enemy);

            foreach (TimedLaser laser in root.GetComponentsInChildren<TimedLaser>(true))
                if (!IsInstanceOf(laser, LaserPrefabPath)) WireLaser(laser);

            foreach (VisionActivatedLaser laser in root.GetComponentsInChildren<VisionActivatedLaser>(true))
                Remove(laser.transform, Names.Alert);

            foreach (Portal portal in root.GetComponentsInChildren<Portal>(true))
                WirePortal(portal);

            foreach (CinemachineCamera camera in root.GetComponentsInChildren<CinemachineCamera>(true))
                GetOrAdd<CinemachineImpulseListener>(camera.gameObject);
        }
    }

    private static void WirePlayer(GameObject root)
    {
        var body = root.GetComponent<CapsuleCollider2D>();
        Vector3 center = body != null ? (Vector3)body.offset : Vector3.zero;
        Vector3 feet = body != null ? new Vector3(body.offset.x, body.offset.y - body.size.y / 2f) : Vector3.zero;
        float wallX = body != null ? body.offset.x + body.size.x / 2f : 0.3f;

        Vector3 attackPoint = new(0.6f, center.y);
        var attackState = root.GetComponent<AttackState>();
        if (attackState != null && Get(attackState, "castPoint") is Transform castPoint)
            attackPoint = root.transform.InverseTransformPoint(castPoint.position);

        PlayerEffects effects = GetOrAdd<PlayerEffects>(root);
        Set(effects, "landing", Attach(root.transform, Names.Landing, feet));
        Set(effects, "jump", Attach(root.transform, Names.Jump, feet));
        Set(effects, "run", Attach(root.transform, Names.Run, feet));
        Set(effects, "wallGrab", Attach(root.transform, Names.WallGrab, new Vector3(wallX, center.y + 0.2f)));
        Set(effects, "attack", Attach(root.transform, Names.Attack, attackPoint));
        Set(effects, "crouch", Attach(root.transform, Names.Crouch, feet));
        Remove(root.transform, Names.CloakOn);
        Set(effects, "cloakBreak", Attach(root.transform, Names.CloakBreak, center));

        var damageable = root.GetComponent<Damageable>();
        if (damageable != null)
        {
            Set(damageable, "hitParticles", Attach(root.transform, Names.HitSparksPlayer, center));
            Set(damageable, "deathEffect", Load(Names.DeathPlayer));
            WireFeedback(root, 0.15f, 0.4f, 0.2f, 0.12f);

            DamageVignette vignette = GetOrAdd<DamageVignette>(root);
            Set(vignette, "volume", root.GetComponentInChildren<Volume>(true));
            Set(vignette, "damageable", damageable);
        }
    }

    private static void WireEnemy(EnemyController enemy)
    {
        GameObject root = enemy.gameObject;
        var damageable = root.GetComponent<Damageable>();
        if (damageable != null)
        {
            var sprite = Get(damageable, "spriteRenderer") as SpriteRenderer;
            Vector3 center = sprite != null ? root.transform.InverseTransformPoint(sprite.bounds.center) : Vector3.zero;
            Set(damageable, "hitParticles", Attach(root.transform, Names.HitSparks, center));
            Set(damageable, "deathEffect", Load(Names.DeathEnemy));
            WireFeedback(root, 0f, 0.15f, 0f, 0.05f);
        }

        var attack = root.GetComponent<EnemyAttackState>();
        if (attack != null && Get(attack, "muzzle") is Transform muzzle)
            Set(attack, "muzzleFlash", Attach(muzzle, Names.MuzzleFlash, Vector3.zero));
    }

    private static void WireFeedback(GameObject root, float hitShake, float deathShake, float deathTimeScale,
        float deathTimeDuration)
    {
        CinemachineImpulseSource source = GetOrAdd<CinemachineImpulseSource>(root);
        DamageFeedback feedback = GetOrAdd<DamageFeedback>(root);
        Set(feedback, "impulseSource", source);
        SetFloat(feedback, "hitShake", hitShake);
        SetFloat(feedback, "deathShake", deathShake);
        SetFloat(feedback, "deathTimeScale", deathTimeScale);
        SetFloat(feedback, "deathTimeDuration", deathTimeDuration);
    }

    private static void WireProjectile(GameObject root)
    {
        var projectile = root.GetComponent<Projectile>();
        Set(projectile, "trail", Attach(root.transform, Names.ProjectileTrail, Vector3.zero));
        Set(projectile, "impactEffect", Load(Names.ProjectileImpact));
    }

    // Emitter effects sit on both beam marks: the second instance is a child of the first, so one Play starts both.
    private static void WireLaser(TimedLaser laser)
    {
        if (laser == null) return;

        Transform upper = FindDeep(laser.transform, "UpperBeamMark");
        Transform lower = FindDeep(laser.transform, "LowerBeamMark");
        Transform first = upper != null ? upper : laser.transform;

        ParticleSystem telegraph = Attach(first, Names.LaserTelegraph, Vector3.zero);
        ParticleSystem activate = Attach(first, Names.LaserActivate, Vector3.zero);
        ParticleSystem deactivate = Attach(first, Names.LaserDeactivate, Vector3.zero);
        if (lower != null)
        {
            Attach(telegraph.transform, Names.LaserTelegraph, Vector3.zero).transform.position = lower.position;
            Attach(activate.transform, Names.LaserActivate, Vector3.zero).transform.position = lower.position;
            Attach(deactivate.transform, Names.LaserDeactivate, Vector3.zero).transform.position = lower.position;
        }

        Set(laser, "telegraphEffect", telegraph);
        Set(laser, "activateEffect", activate);
        Set(laser, "deactivateEffect", deactivate);
        Set(laser, "hitEffect", Load(Names.HitSparks));

        if (Get(laser, "laserBeam") is SpriteRenderer beam)
            Set(laser, "telegraphLine", TelegraphLine(beam));
    }

    // A thinner, dimmer copy of the beam sprite, flickered by TimedLaser during the telegraph.
    private static SpriteRenderer TelegraphLine(SpriteRenderer beam)
    {
        Transform parent = beam.transform.parent;
        Transform old = parent.Find(Names.TelegraphLine);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var host = new GameObject(Names.TelegraphLine);
        host.transform.SetParent(parent, false);
        host.transform.SetLocalPositionAndRotation(beam.transform.localPosition, beam.transform.localRotation);
        Vector3 scale = beam.transform.localScale;
        host.transform.localScale = new Vector3(scale.x * 0.3f, scale.y, scale.z);

        var line = host.AddComponent<SpriteRenderer>();
        line.sprite = beam.sprite;
        line.drawMode = beam.drawMode;
        line.size = beam.size;
        line.sharedMaterial = beam.sharedMaterial;
        line.sortingLayerID = beam.sortingLayerID;
        line.sortingOrder = beam.sortingOrder;
        line.color = new Color(Crimson.r, Crimson.g, Crimson.b, 0.7f);
        line.enabled = false;
        return line;
    }

    private static void WirePortal(Portal portal)
    {
        var sprite = portal.GetComponentInChildren<SpriteRenderer>();
        Vector3 center = sprite != null ? portal.transform.InverseTransformPoint(sprite.bounds.center) : Vector3.zero;

        Attach(portal.transform, Names.PortalIdle, center);
        Set(portal, "enterEffect", Attach(portal.transform, Names.PortalEnter, center));

        Transform old = portal.transform.Find(Names.PortalFlash);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var host = new GameObject(Names.PortalFlash);
        host.transform.SetParent(portal.transform, false);
        var volume = host.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.weight = 0f;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FlashProfilePath);
        Set(portal, "flashVolume", volume);
    }

    // Replaces the child named after the effect with a fresh prefab instance.
    private static ParticleSystem Attach(Transform parent, string name, Vector3 localPosition)
    {
        Remove(parent, name);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load(name).gameObject, parent);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        return instance.GetComponent<ParticleSystem>();
    }

    private static void Remove(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
    }

    private static bool IsInstanceOf(Component component, string prefabPath) =>
        PrefabUtility.IsPartOfPrefabInstance(component) &&
        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component) == prefabPath;

    private static Transform FindDeep(Transform parent, string name)
    {
        var queue = new Queue<Transform>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            if (current.name == name) return current;
            foreach (Transform child in current) queue.Enqueue(child);
        }

        return null;
    }

    private static T GetOrAdd<T>(GameObject host) where T : Component
    {
        var component = host.GetComponent<T>();
        return component != null ? component : host.AddComponent<T>();
    }

    private static Object Get(Object target, string field) =>
        new SerializedObject(target).FindProperty(field)?.objectReferenceValue;

    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"VfxBuilder: {target.GetType().Name} has no field {field}.", target);
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
