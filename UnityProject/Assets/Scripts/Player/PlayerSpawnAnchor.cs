using System.Collections;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Garante que o player sempre nasça no centro seguro da sala, mesmo que a
    /// sala esteja posicionada em qualquer altura no mundo.
    ///
    /// Estratégia:
    ///   1. Busca o piso físico próximo da origem arquitetônica da sala.
    ///   2. Ignora o visitante, triggers e superfícies acima do piso.
    ///   3. Posiciona os pés, preservando a altura local da câmera.
    ///
    /// Como usar:
    ///   Adicione ao mesmo GameObject do PlayerController (ou qualquer objeto ativo).
    ///   Executa automaticamente no Start.
    /// </summary>
    [DefaultExecutionOrder(200)] // roda depois do SaveSystem (order 0) e HeadGazeMovement
    public class PlayerSpawnAnchor : MonoBehaviour
    {
        [Header("Ajustes de Spawn")]
        [Tooltip("Pequena folga dos pés acima do chão, resolvida pela gravidade no primeiro frame")]
        [SerializeField, Range(0f, .05f)] private float floorClearance = .02f;

        [Tooltip("Sempre reposiciona o visitante no centro seguro da sala ao abrir o aplicativo.")]
        [SerializeField] private bool resetToRoomCenterOnLaunch = true;

        [Tooltip("Layers consideradas chão (deixe 'Everything' para pegar qualquer coisa)")]
        [SerializeField] private LayerMask floorLayers = ~0;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private IEnumerator Start()
        {
            // Espera 1 frame para SaveSystem carregar a posição salva primeiro
            yield return null;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null) yield break;

            Physics.SyncTransforms();
            Vector3 spawnBase = FindRoomCenter();
            if (!TryFindRoomFloor(spawnBase, out Vector3 floorPosition))
            {
                Debug.LogWarning("[MuseumModerna] Spawn mantido: não há piso físico abaixo da entrada.");
                yield break;
            }
            if (!resetToRoomCenterOnLaunch && player.transform.position.y >= floorPosition.y - 3f) yield break;

            // Saves continuam existindo para outros dados, mas nunca definem a
            // posição inicial: cada abertura começa no centro da sala.
            var controller = player.GetComponent<CharacterController>();
            // O pivot fica nos pés na cena atual. A câmera já tem os seus 1,6 m
            // locais: acrescentar 1 m à raiz suspendia todo o corpo sobre móveis.
            float footOffset = controller != null
                ? player.transform.TransformVector(controller.center - Vector3.up * (controller.height * .5f)).y
                : 0f;
            Vector3 spawnPos = floorPosition + Vector3.up * (floorClearance - footOffset);
            TeleportPlayer(player, spawnPos);
            Debug.Log($"[MuseumModerna] Spawn reiniciado no centro seguro: {spawnPos}.");
        }

        // ─── Encontrar o Chão ─────────────────────────────────────────────────

        private bool TryFindRoomFloor(Vector3 playerPos, out Vector3 floorPosition)
        {
            // Começa dentro da sala: um raio vindo do céu pode selecionar o teto.
            // Ignora também o próprio visitante e volumes de interação.
            Vector3 rayOrigin = playerPos + Vector3.up * 2.5f;
            var hits = Physics.RaycastAll(rayOrigin, Vector3.down, 8f, floorLayers,
                QueryTriggerInteraction.Ignore);
            float floorOffset = float.PositiveInfinity;
            RaycastHit floor = default;
            foreach (var hit in hits)
            {
                if (hit.normal.y < .7f || hit.collider is CharacterController ||
                    hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                // A cota da origem arquitetônica é o piso da entrada. Um móvel
                // entre a origem do raio e o chão não deve elevar o visitante.
                float offset = Mathf.Abs(hit.point.y - playerPos.y);
                if (offset >= floorOffset) continue;
                floorOffset = offset;
                floor = hit;
            }
            floorPosition = floor.point;
            if (floor.collider != null)
            {
                Debug.Log($"[MuseumModerna] Chão encontrado via raycast em Y={floor.point.y:F2}");
                return true;
            }
            return false;
        }

        private Vector3 FindRoomCenter()
        {
            var architecture = GameObject.Find("Museum_LouvreStyle");
            // O gerador constrói o piso da galeria central na origem local.
            // Bounds globais também incluem jardins e objetos sob o piso.
            if (architecture != null) return architecture.transform.position;
            return Vector3.zero;
        }

        // ─── Teleportar ───────────────────────────────────────────────────────

        private static void TeleportPlayer(PlayerController player, Vector3 position)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            player.transform.position = position;
            if (cc != null) cc.enabled = wasEnabled;
            Physics.SyncTransforms();

            // O teleporte descarta a velocidade antiga; o chão resolve a altura.
            player.GetComponent<HeadGazeMovement>()?.SyncLockedY();
        }
    }
}
