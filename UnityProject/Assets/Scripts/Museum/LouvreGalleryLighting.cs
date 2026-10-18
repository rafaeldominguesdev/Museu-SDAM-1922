using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Luz difusa de galeria sem depender dos probes da arquitetura anterior.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class LouvreGalleryLighting : MonoBehaviour
    {
        private void Awake()
        {
            if (transform.Find("Planta_CincoSalas") != null) ApplyGallery();
            else ApplyDaylight();
        }

        public static void ApplyGallery()
        {
            SetAmbient(new Color(.35f, .355f, .365f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.17f, .20f, .25f);
            RenderSettings.fogDensity = .005f;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 18f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.pixelLightCount = 4;
            QualitySettings.antiAliasing = 2;
        }

        public static void ApplyDaylight()
        {
            SetAmbient(new Color(.72f, .72f, .68f));
            RenderSettings.fog = false;
            QualitySettings.antiAliasing = 2;
        }

        public static void SetAmbient(Color color)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = color;
            RenderSettings.ambientIntensity = 1;
            var diffuse = new SphericalHarmonicsL2();
            diffuse.AddAmbientLight(color);
            RenderSettings.ambientProbe = diffuse;
        }
    }
}
