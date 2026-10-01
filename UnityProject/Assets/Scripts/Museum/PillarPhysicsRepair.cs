using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Restaura os volumes das pilastras cujas malhas foram agrupadas na arquitetura.</summary>
    [DisallowMultipleComponent]
    public sealed class PillarPhysicsRepair : MonoBehaviour
    {
        private void Awake()
        {
            var architecture = GameObject.Find("Museum_LouvreStyle");
            if (architecture == null) return;
            Repair(architecture.transform);
        }

        public static int Repair(Transform architecture)
        {
            // A planta temática já salva todos os colisores nas posições dos seis
            // portais. A reparação legada assumia apenas quatro acessos centrais.
            if (architecture.Find("Planta_CincoSalas") != null) return 0;
            int added = 0;
            foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
            {
                added += EnsureColumn(architecture, "Central_" + x + "_" + z,
                    new Vector3(x * 8.7f, 0, z * 8.7f), 6.18f, .43f);
            }

            // Cada portal tem duas faces: colunas do salão e da galeria anexa.
            // As cenas antigas tinham as paredes, mas não estes volumes salientes.
            for (int side = 0; side < 4; side++)
            {
                Quaternion rotation = Quaternion.Euler(0, side * 90f, 0);
                foreach (int sign in new[] { -1, 1 })
                foreach (int face in new[] { -1, 1 })
                    added += EnsureColumn(architecture, "Portal_" + side + "_" + sign + "_" + face,
                        rotation * new Vector3(sign * 2.53f, 0, 10f + face * .31f), 2.75f, .32f);
            }
            return added;
        }

        private static int EnsureColumn(Transform architecture, string id, Vector3 foot, float height, float radius)
        {
            Vector3 center = foot + Vector3.up * (height * .5f);
            Vector3 worldCenter = architecture.TransformPoint(center);
            // O gerador novo usa nomes numerados; reconheça também esses volumes
            // pela posição para não sobrepor um segundo collider ao existente.
            foreach (var existing in architecture.GetComponentsInChildren<CapsuleCollider>())
            {
                if (Vector3.Distance(existing.transform.TransformPoint(existing.center), worldCenter) > .03f) continue;
                if (existing.direction != 1 || Mathf.Abs(existing.height - height) > .05f) continue;
                existing.enabled = true;
                existing.isTrigger = false;
                return 0;
            }
            var pillar = new GameObject("Colisao_Pilastra_" + id);
            pillar.transform.SetParent(architecture, false);
            pillar.transform.localPosition = center;
            var collider = pillar.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = radius;
            collider.height = height;
            return 1;
        }
    }
}
