using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Ciclo lento e discreto de luz externa, seguro para a iluminação do museu.</summary>
    [DisallowMultipleComponent]
    public sealed class MuseumDayNightCycle : MonoBehaviour
    {
        [SerializeField, Min(60f)] private float cycleDurationSeconds = 420f;
        [SerializeField, Range(0f, 1f)] private float initialTimeOfDay = .38f;
        [SerializeField] private bool runCycle = true;

        private Light sun;
        private float timeOfDay;

        private void Awake()
        {
            timeOfDay = initialTimeOfDay;
            var gallerySun = transform.Find("Luz_diurna");
            sun = gallerySun != null ? gallerySun.GetComponent<Light>() : null;
            if (sun == null && RenderSettings.sun != null && RenderSettings.sun.isActiveAndEnabled)
                sun = RenderSettings.sun;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (sun == null)
                foreach (var light in lights)
                    if (light.type == LightType.Directional && light.isActiveAndEnabled)
                    { sun = light; break; }
            if (sun == null)
            {
                var go = new GameObject("LuzDoCicloDiaNoite");
                go.transform.SetParent(transform, false);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            // O museu mantém uma única luz direcional: FindAnyObjectByType podia
            // escolher um spot ou uma luz desativada e criar outro sol a cada cena.
            foreach (var light in lights)
                if (light != sun && light.type == LightType.Directional) light.enabled = false;
            sun.enabled = true;
            sun.renderMode = LightRenderMode.ForcePixel;
            sun.shadows = LightShadows.Soft;
            sun.shadowCustomResolution = 2048;
            sun.shadowStrength = .65f;
            sun.shadowBias = .025f;
            sun.shadowNormalBias = .10f;
            sun.shadowNearPlane = .10f;
            RenderSettings.sun = sun;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 8f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowCascade2Split = .3f;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.pixelLightCount = Mathf.Max(2, QualitySettings.pixelLightCount);
            ApplyLighting();
        }

        private void Update()
        {
            if (!runCycle) return;
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / cycleDurationSeconds, 1f);
            ApplyLighting();
        }

        private void ApplyLighting()
        {
            float daylight = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI * 2f) * .5f + .5f);
            Color night = new Color(.30f, .33f, .39f);
            Color day = new Color(.66f, .64f, .58f);
            LouvreGalleryLighting.SetAmbient(Color.Lerp(night, day, daylight));
            sun.color = Color.Lerp(new Color(.42f, .52f, .78f), new Color(1f, .86f, .66f), daylight);
            sun.intensity = Mathf.Lerp(.18f, .60f, daylight);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(32f, 58f, daylight), timeOfDay * 360f - 110f, 0f);
        }
    }
}
