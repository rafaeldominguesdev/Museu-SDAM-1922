using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Luz difusa de galeria sem depender dos probes da arquitetura anterior.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class LouvreGalleryLighting : MonoBehaviour
    {
        private void Awake() => ApplyDaylight();

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
