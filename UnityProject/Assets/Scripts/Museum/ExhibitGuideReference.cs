using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Um estudo ou vitrine complementar abre a ficha curatorial da peça de origem.</summary>
    public sealed class ExhibitGuideReference : MonoBehaviour
    {
        [SerializeField] private PaintingExhibit source;
        public PaintingExhibit Source => source;
        public void SetSource(PaintingExhibit exhibit) => source = exhibit;
    }
}
