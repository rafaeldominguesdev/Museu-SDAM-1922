using System.Collections.Generic;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Legendas 3D respeitam paredes, móveis e o atlas dinâmico da fonte.</summary>
    [DefaultExecutionOrder(-400)]
    public sealed class MuseumWorldText : MonoBehaviour
    {
        private readonly Dictionary<Font, Material> materials = new Dictionary<Font, Material>();
        private void Awake()
        {
            var shader = Shader.Find("Museum/World Text Depth");
            if (shader == null) return;
            foreach (var text in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (text.font == null) continue;
                if (!materials.TryGetValue(text.font, out var material))
                {
                    material = new Material(shader) { mainTexture = text.font.material.mainTexture };
                    materials.Add(text.font, material);
                }
                text.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            Font.textureRebuilt += RefreshAtlas;
        }
        private void RefreshAtlas(Font font)
        {
            if (materials.TryGetValue(font, out var material)) material.mainTexture = font.material.mainTexture;
        }
        private void OnDestroy()
        {
            Font.textureRebuilt -= RefreshAtlas;
            foreach (var material in materials.Values) Destroy(material);
        }
    }
}
