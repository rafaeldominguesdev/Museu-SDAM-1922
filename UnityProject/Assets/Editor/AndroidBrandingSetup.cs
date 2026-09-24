using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Instala a arte original do museu nos ícones Android, incluindo adaptive e round.</summary>
    public static class AndroidBrandingSetup
    {
        public const string IconPath = "Assets/Branding/MuseumModernIcon.png";
        private const string EmptyPath = "Assets/Branding/AdaptiveTransparent.png";

        public static void Apply()
        {
            if (!File.Exists(IconPath)) throw new InvalidOperationException("Ícone do museu ausente: " + IconPath);
            if (!File.Exists(EmptyPath))
            {
                var empty = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                empty.SetPixels(new[] { Color.clear, Color.clear, Color.clear, Color.clear });
                empty.Apply();
                File.WriteAllBytes(EmptyPath, empty.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(empty);
            }
            AssetDatabase.ImportAsset(EmptyPath);
            Configure(IconPath); Configure(EmptyPath);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var transparent = AssetDatabase.LoadAssetAtPath<Texture2D>(EmptyPath);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            foreach (var kind in new[] { AndroidPlatformIconKind.Legacy, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Adaptive })
            {
                var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
                foreach (var slot in icons)
                {
                    // Arte opaca no fundo; frente transparente deixa o Android aplicar a máscara do launcher.
                    if (kind == AndroidPlatformIconKind.Adaptive) slot.SetTextures(new[] { icon, transparent });
                    else slot.SetTexture(icon);
                }
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, icons);
            }
            PlayerSettings.bundleVersion = "1.1.0";
            PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode, 2);
            AssetDatabase.SaveAssets();
            Debug.Log("[AndroidBranding] Ícones legacy, round e adaptive configurados; versão 1.1.0.");
        }

        private static void Configure(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
