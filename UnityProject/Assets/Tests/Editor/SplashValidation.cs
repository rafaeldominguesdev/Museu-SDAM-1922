using System;
using UnityEditor;
using UnityEngine;

namespace MuseumModerna
{
    public static class SplashValidation
    {
        public static void Run()
        {
            try
            {
                AndroidBrandingSetup.Apply();
                var logos = PlayerSettings.SplashScreen.logos;
                if (!PlayerSettings.SplashScreen.show || !PlayerSettings.SplashScreen.showUnityLogo ||
                    PlayerSettings.SplashScreen.drawMode != PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow)
                    throw new Exception("O splash deve mostrar a autoria junto da marca Unity.");
                if (logos.Length != 1 || logos[0].logo == null || logos[0].duration < 4f ||
                    AssetDatabase.GetAssetPath(logos[0].logo) != AndroidBrandingSetup.SplashPath)
                    throw new Exception("Créditos ausentes ou duração de leitura insuficiente.");
                if (logos[0].logo.rect.width != 1600 || logos[0].logo.rect.height != 420 ||
                    logos[0].logo.vertices.Length != 4)
                    throw new Exception("A imagem inteira dos créditos deve ser usada sem recorte.");
                var importer = (TextureImporter)AssetImporter.GetAtPath(AndroidBrandingSetup.SplashPath);
                if (importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency)
                    throw new Exception("A importação deve preservar a transparência e legibilidade do texto.");
                Debug.Log("[SplashValidation] PASS: autoria importada, imagem inteira, transparência, duração e marca Unity.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
