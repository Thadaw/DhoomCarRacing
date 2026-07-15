using UnityEngine;
using UnityEditor;
using System.IO;

public class OptimizeUITextures
{
    [MenuItem("Tools/Optimize UI Textures")]
    public static void OptimizeAll()
    {
        string imagePath = "Assets/Image";
        if (!Directory.Exists(imagePath))
        {
            Debug.LogError("Assets/Image folder not found!");
            return;
        }

        string[] pngFiles = Directory.GetFiles(imagePath, "*.png", SearchOption.AllDirectories);
        int updated = 0;

        var buttonPatterns = new[] { "button", "btn", "icon", "arrow", "indicator", "logo", "spinner", "play", "option", "home", "quit", "back", "close", "join", "ready", "restart", "leave", "select", "drive", "start", "lock", "host", "helmet", "nitro", "speed", "rank", "badge" };
        var backgroundPatterns = new[] { "background", "main_background", "garage_background", "stats_background", "multiplayer_background", "results_background", "car_selection_background" };
        var panelPatterns = new[] { "panel", "slot", "row", "table", "list", "frame", "bar", "resource", "input_field", "header", "title", "avatar" };

        foreach (string filePath in pngFiles)
        {
            if (filePath.EndsWith(".meta")) continue;

            string relativePath = filePath.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(relativePath).ToLower();

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer == null) continue;

            int maxSize;

            if (IsMatch(fileName, backgroundPatterns))
            {
                maxSize = 1024;
            }
            else if (IsMatch(fileName, panelPatterns))
            {
                maxSize = 512;
            }
            else if (IsMatch(fileName, buttonPatterns))
            {
                maxSize = 256;
            }
            else
            {
                maxSize = 512;
            }

            bool changed = false;

            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Compressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }

            string[] platforms = new[] { "Standalone", "Android", "WebGL" };
            foreach (string platform in platforms)
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                if (settings == null || !settings.overridden)
                {
                    settings = new TextureImporterPlatformSettings();
                    settings.name = platform;
                    settings.overridden = true;
                }

                bool platformChanged = false;

                if (settings.maxTextureSize != maxSize)
                {
                    settings.maxTextureSize = maxSize;
                    platformChanged = true;
                }

                if (settings.textureCompression != TextureImporterCompression.Compressed)
                {
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    platformChanged = true;
                }

                if (!settings.crunchedCompression)
                {
                    settings.crunchedCompression = true;
                    platformChanged = true;
                }

                if (settings.compressionQuality != 75)
                {
                    settings.compressionQuality = 75;
                    platformChanged = true;
                }

                if (platformChanged)
                {
                    importer.SetPlatformTextureSettings(settings);
                    changed = true;
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
                updated++;
                Debug.Log($"Optimized: {relativePath} -> MaxSize: {maxSize}");
            }
        }

        Debug.Log($"Done! Optimized {updated} textures out of {pngFiles.Length} total PNG files.");
        EditorUtility.DisplayDialog("Texture Optimization Complete",
            $"Optimized {updated} textures.\n\n" +
            "Backgrounds: 1024 max\n" +
            "Panels: 512 max\n" +
            "Buttons/Icons: 256 max\n\n" +
            "Unity will reimport with new settings.",
            "OK");
    }

    private static bool IsMatch(string fileName, string[] patterns)
    {
        foreach (string pattern in patterns)
        {
            if (fileName.Contains(pattern))
                return true;
        }
        return false;
    }
}
