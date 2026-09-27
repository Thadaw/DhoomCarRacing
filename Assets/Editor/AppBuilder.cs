using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AppBuilder
{
    const string ProductName = "Doom Car Racing";
    const string CompanyName = "AAR";
    const string AppVersion = "1.0.0";
    const string IconSource = "Assets/Image/mainmenu/Game_Logo_DHOOM.png";
    const string IconFolder = "Assets/AppIcon";
    const string BuildFolder = "Builds/Windows";
    const string BuildPath = "Builds/Windows/Doom Car Racing.exe";

    static readonly int[] IconSizes = { 8, 16, 32, 48, 64, 128, 256, 512 };

    [MenuItem("Build/Build Windows App")]
    public static void BuildWindowsApp()
    {
        try
        {
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();

            var target = BuildTarget.StandaloneWindows64;
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.Log("[AppBuilder] Switching build target to StandaloneWindows64 (was " + EditorUserBuildSettings.activeBuildTarget + ")");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target))
                    throw new Exception("Could not switch build target to StandaloneWindows64.");
            }

            Directory.CreateDirectory(Path.Combine(ProjectRoot, BuildFolder));

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Debug.Log("[AppBuilder] Scenes in build: " + scenes.Length);
            if (scenes.Length == 0)
                throw new Exception("No enabled scenes in Build Settings.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log("[AppBuilder] RESULT status=" + summary.result
                      + " totalSize=" + (summary.totalSize / (1024f * 1024f)).ToString("F1") + "MB"
                      + " output=" + summary.outputPath
                      + " errors=" + summary.totalErrors
                      + " warnings=" + summary.totalWarnings);

            if (summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed: " + summary.result + " with " + summary.totalErrors + " errors.");
        }
        catch (Exception e)
        {
            Debug.LogError("[AppBuilder] FAILED: " + e);
            throw;
        }
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = AppVersion;

        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.runInBackground = true;
        PlayerSettings.visibleInBackground = false;
        PlayerSettings.allowFullscreenSwitch = true;
        PlayerSettings.forceSingleInstance = false;
        PlayerSettings.useFlipModelSwapchain = true;

        var standalone = NamedBuildTarget.Standalone;
        PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetArchitecture(standalone, 1);
        PlayerSettings.SetApiCompatibilityLevel(standalone, ApiCompatibilityLevel.NET_Standard_2_0);

        var icons = BuildIcons();
        SetIcons(standalone, icons);
        Debug.Log("[AppBuilder] Icons assigned: " + icons.Count(i => i != null) + "/" + IconSizes.Length);

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
    }

    static void SetIcons(NamedBuildTarget target, Texture2D[] icons)
    {
        var overloads = typeof(PlayerSettings).GetMethods()
            .Where(m => m.Name == "SetIcons" && m.GetParameters().Length == 3)
            .ToArray();

        if (overloads.Length == 0)
            throw new Exception("PlayerSettings.SetIcons(target, icons, kind) overload not found.");

        var method = overloads[0];
        var kindType = method.GetParameters()[2].ParameterType;
        object kind;

        if (kindType.IsEnum)
        {
            var names = Enum.GetNames(kindType);
            var chosen = names.FirstOrDefault(n => n == "All")
                         ?? names.FirstOrDefault(n => n == "Any")
                         ?? names.First();
            kind = Enum.Parse(kindType, chosen);
            Debug.Log("[AppBuilder] SetIcons kind = " + kindType.Name + "." + chosen + " (available: " + string.Join(", ", names) + ")");
        }
        else
        {
            kind = Activator.CreateInstance(kindType);
            Debug.Log("[AppBuilder] SetIcons kind is not an enum (" + kindType.FullName + "), using default instance");
        }

        method.Invoke(null, new[] { (object)target, icons, kind });
    }

    static Texture2D[] BuildIcons()
    {
        var source = Path.Combine(ProjectRoot, IconSource);
        if (!File.Exists(source))
            throw new Exception("Icon source not found: " + source);

        var bytes = File.ReadAllBytes(source);
        var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(decoded, bytes))
            throw new Exception("Could not decode icon image: " + source);
        Debug.Log("[AppBuilder] Icon source is " + decoded.width + "x" + decoded.height);

        Directory.CreateDirectory(Path.Combine(ProjectRoot, IconFolder));

        var result = new Texture2D[IconSizes.Length];
        for (var i = 0; i < IconSizes.Length; i++)
        {
            var size = IconSizes[i];
            var scaled = Resize(decoded, size);
            var path = IconFolder + "/icon_" + size + ".png";
            File.WriteAllBytes(Path.Combine(ProjectRoot, path), ImageConversion.EncodeToPNG(scaled));
            UnityEngine.Object.DestroyImmediate(scaled);
            result[i] = null;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for (var i = 0; i < IconSizes.Length; i++)
        {
            var path = IconFolder + "/icon_" + IconSizes[i] + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            result[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (result[i] == null)
                Debug.LogWarning("[AppBuilder] Icon missing after import: " + path);
        }

        UnityEngine.Object.DestroyImmediate(decoded);
        return result;
    }

    static Texture2D Resize(Texture2D source, int size)
    {
        var rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        var readable = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        readable.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        return readable;
    }

    static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
}
