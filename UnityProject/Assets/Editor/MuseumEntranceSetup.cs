using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    public static class MuseumEntranceSetup
    {
        public static void Configure(Transform architecture)
        {
            var entrance = architecture.Find(PlayerSpawnAnchor.EntranceAnchorName);
            if (entrance == null)
            {
                entrance = new GameObject(PlayerSpawnAnchor.EntranceAnchorName).transform;
                entrance.SetParent(architecture, false);
            }
            // Um Transform vazio: não produz bloco visível nem colisão.
            entrance.localPosition = new Vector3(0, 0, -13);
            entrance.localRotation = Quaternion.identity;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null) throw new InvalidOperationException("Visitante ausente.");
            player.transform.SetPositionAndRotation(entrance.position, entrance.rotation);

            // O pedestal central legado já estava desativado; mantenha-o arquivado.
            var legacy = architecture.gameObject.scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Pedestal");
            if (legacy != null) legacy.SetActive(false);

            // Resíduo do antigo setup de física: um cubo de 1 m foi adicionado
            // ao manager do teto, sem nenhuma malha, exatamente no centro do Hall.
            // Desativa apenas esse componente; os colisores do teto ficam intactos.
            foreach (var builder in UnityEngine.Object.FindObjectsByType<MuseumCeilingBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var block = builder.GetComponent<BoxCollider>();
                if (builder.gameObject.scene != architecture.gameObject.scene || block == null ||
                    builder.GetComponent<MeshFilter>() != null || builder.GetComponent<Renderer>() != null ||
                    block.center != Vector3.zero || block.size != Vector3.one) continue;
                block.enabled = false;
                EditorUtility.SetDirty(block);
            }
        }

        [MenuItem("Museum/Visita/Configurar entrada sem bloco de spawn")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            var architecture = GameObject.Find("Museum_LouvreStyle");
            if (architecture == null) throw new InvalidOperationException("Arquitetura do museu ausente.");
            Configure(architecture.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar entrada.");
            Debug.Log("[MuseumEntrance] Entrada invisível salva no foyer, voltada para o Hall.");
        }

        public static void ApplyAndValidate()
        {
            Apply();
            PhysicsValidation.Run();
        }
    }
}
