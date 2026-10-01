#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Exercises the saved geometry with the actual visitor controller, not only raycasts.</summary>
    public sealed class PhysicsRuntimeValidation : MonoBehaviour
    {
        private int checks;
        private CharacterController controller;
        private PlayerController player;
        private PhysicsCollisionProbe probe;

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 8; frame++) yield return null;
            try
            {
                RunChecks();
                Debug.Log($"[PhysicsValidation] PASS: {checks} verificações de chão, spawn e colisão real.");
                UnityEditor.EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEditor.EditorApplication.Exit(1);
            }
        }

        private void RunChecks()
        {
            player = FindAnyObjectByType<PlayerController>();
            Require(player != null, "Visitante existe na cena salva");
            controller = player.GetComponent<CharacterController>();
            Require(controller != null && controller.enabled, "CharacterController está ativo");
            var movement = player.GetComponent<HeadGazeMovement>();
            var eye = player.GetComponentInChildren<Camera>();
            Require(Mathf.Abs(controller.bounds.min.y) < .07f && eye.transform.position.y > 1.5f &&
                eye.transform.position.y < 1.8f, "Spawn coloca pés no piso e olhos na altura humana");
            var entrance = GameObject.Find(PlayerSpawnAnchor.EntranceAnchorName);
            Require(entrance != null && Vector2.Distance(
                new Vector2(player.transform.position.x, player.transform.position.z),
                new Vector2(entrance.transform.position.x, entrance.transform.position.z)) < .05f,
                "Inicialização real começa na âncora do foyer");
            Require(entrance.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                entrance.GetComponentsInChildren<Collider>(true).Length == 0,
                "Spawn é invisível e não possui bloco nem colisor");
            Require(Vector3.Dot(player.transform.forward, entrance.transform.forward) > .99f,
                "Visitante inicia orientado para o Hall");
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var gaze in FindObjectsByType<GazeDwellInteraction>(FindObjectsSortMode.None)) gaze.enabled = false;
            movement.enabled = false;
            player.enabled = false;
            probe = player.gameObject.AddComponent<PhysicsCollisionProbe>();
            Physics.SyncTransforms();

            // Simula a posição legada que SaveSystem restaura antes da âncora.
            Position(new Vector3(0, .02f, 0));
            var spawn = FindAnyObjectByType<PlayerSpawnAnchor>();
            var restart = (IEnumerator)typeof(PlayerSpawnAnchor).GetMethod("Start",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawn, null);
            while (restart.MoveNext()) { }
            Require(Vector3.Distance(player.transform.position, entrance.transform.position) < .07f,
                "Posição antiga no centro é substituída pela entrada segura");

            var architecture = GameObject.Find("Museum_LouvreStyle").transform;
            Require(Vector3.Distance(architecture.InverseTransformPoint(entrance.transform.position),
                new Vector3(0, 0, -13)) < .01f, "Âncora está dentro do foyer");
            var centerObstacles = Physics.OverlapCapsule(
                architecture.TransformPoint(new Vector3(0, .4f, 0)),
                architecture.TransformPoint(new Vector3(0, 1.4f, 0)), .3f,
                ~0, QueryTriggerInteraction.Ignore);
            Require(centerObstacles.Length == 0,
                "Centro do Hall livre de bloco sólido na altura do visitante; encontrados: " +
                string.Join(", ", centerObstacles.Select(c => c.name + " em " + c.bounds.center + " tamanho " + c.bounds.size)));
            Require(GameObject.Find("Pedestal") == null, "Pedestal central legado permanece desativado");
            var columns = architecture.GetComponentsInChildren<CapsuleCollider>()
                .Where(collider => collider.name.StartsWith("Colisao_Pilastra_")).ToArray();
            Require(columns.Length == 20, "Quatro pilastras centrais e dezesseis colunas de portais são sólidas");
            int before = architecture.GetComponentsInChildren<Collider>().Length;
            PillarPhysicsRepair.Repair(architecture);
            Require(architecture.GetComponentsInChildren<Collider>().Length == before,
                "Reparação repetida não duplica colisores");
            foreach (var column in columns)
            {
                // Aproxime pelo vão livre. As duas faces do portal ficam alinhadas:
                // vir pela frente da outra face começaria dentro do seu collider.
                // As pilastras centrais ficam nos cantos; aproxime pelo salão.
                // O ponto fixo ao sul podia começar dentro da parede externa.
                Vector3 direction = Vector3.ProjectOnPlane(column.bounds.center - architecture.position, Vector3.up).normalized;
                if (column.name.Contains("Portal_"))
                {
                    var point = architecture.InverseTransformPoint(column.bounds.center);
                    direction = Mathf.Abs(point.x) < Mathf.Abs(point.z)
                        ? new Vector3(Mathf.Sign(point.x), 0, 0)
                        : new Vector3(0, 0, Mathf.Sign(point.z));
                    direction = architecture.TransformDirection(direction);
                }
                CheckObstacle(column, direction);
            }

            for (int index = 1; index <= 3; index++)
                CheckNamedObstacle("Soclo_circular_" + index, Vector3.forward);
            var showcase = GameObject.Find("VITRINE_DOCUMENTOS_CENOGRAFICOS");
            Require(showcase != null, "Vitrine documental está na cena salva");
            CheckObstacle(showcase.transform.Find("Mesa").GetComponent<BoxCollider>(), showcase.transform.forward);
            foreach (string name in new[] { "Banco_curvo_-5_-5", "Banco_curvo_-5_5", "Banco_curvo_5_-5", "Banco_curvo_5_5" })
            {
                var bench = GameObject.Find(name);
                Require(bench != null, "Banco baixo está na cena: " + name);
                CheckObstacle(bench.GetComponentsInChildren<BoxCollider>()[2], bench.transform.forward);
            }

            // The south wall has an opening in the middle; x=5 tests its solid wing.
            Position(new Vector3(5, .02f, -8));
            probe.Hits.Clear();
            for (int step = 0; step < 180; step++) controller.Move(new Vector3(0, -.033f, -.033f));
            Require(player.transform.position.z > -10f && probe.Hits.Count > 0, "Parede impede atravessar a sala");

            foreach (var route in new[]
            {
                new[] { new Vector3(0, .02f, -13), new Vector3(0, .02f, -7) },
                new[] { new Vector3(0, .02f, -2), new Vector3(0, .02f, 2) },
                new[] { new Vector3(0, .02f, 7), new Vector3(0, .02f, 12) },
                new[] { new Vector3(7, .02f, 0), new Vector3(12, .02f, 0) },
                new[] { new Vector3(-7, .02f, 0), new Vector3(-12, .02f, 0) },
                new[] { new Vector3(0, .02f, -7), new Vector3(0, .02f, -12) }
            })
            {
                Position(route[0]);
                Vector3 step = (route[1] - route[0]) / 150f;
                step.y = -.033f;
                for (int frame = 0; frame < 150; frame++) controller.Move(step);
                Require(Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z),
                    new Vector2(route[1].x, route[1].z)) < .08f && controller.isGrounded,
                    "Portal livre e chão contínuo até " + route[1]);
            }

            // A roof, furniture and a trigger must not turn into the spawn floor.
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.position = new Vector3(0, 5, 0);
            roof.transform.localScale = new Vector3(3, .2f, 3);
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.transform.position = new Vector3(0, 1, 0);
            trigger.GetComponent<Collider>().isTrigger = true;
            var furniture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            furniture.transform.position = new Vector3(0, 1.3f, 0);
            furniture.transform.localScale = new Vector3(2, .1f, 2);
            Position(new Vector3(0, .02f, 0));
            Physics.SyncTransforms();
            var anchor = FindAnyObjectByType<PlayerSpawnAnchor>();
            object[] floorArgs = { Vector3.zero, Vector3.zero };
            var floorMethod = typeof(PlayerSpawnAnchor).GetMethod("TryFindRoomFloor",
                BindingFlags.Instance | BindingFlags.NonPublic);
            bool found = (bool)floorMethod.Invoke(anchor, floorArgs);
            Require(found && Mathf.Abs(((Vector3)floorArgs[1]).y) < .03f,
                "Spawn usa o piso sob móveis e ignora teto, trigger e cápsula do visitante");
            object[] missingFloorArgs = { new Vector3(2000, 0, 2000), Vector3.zero };
            Require(!(bool)floorMethod.Invoke(anchor, missingFloorArgs),
                "Ausência de piso físico não usa bounds de objetos distantes como spawn");
            Destroy(roof); Destroy(trigger); Destroy(furniture);
        }

        private void CheckNamedObstacle(string name, Vector3 direction)
        {
            var obstacle = GameObject.Find(name);
            Require(obstacle != null, "Objeto de teste existe: " + name);
            CheckObstacle(obstacle.GetComponent<Collider>(), direction);
        }

        private void CheckObstacle(Collider obstacle, Vector3 direction)
        {
            Require(obstacle != null && obstacle.enabled && !obstacle.isTrigger, "Objeto possui colisão sólida");
            direction.y = 0;
            direction.Normalize();
            var bounds = obstacle.bounds;
            float extent = Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.z) * bounds.extents.z;
            Vector3 start = bounds.center - direction * (extent + .8f);
            start.y = .02f;
            Position(start);
            probe.Hits.Clear();
            float minSeparation = float.PositiveInfinity;
            for (int frame = 0; frame < 180; frame++)
            {
                controller.Move(direction * .025f - Vector3.up * .033f);
                var horizontal = controller.bounds.center - bounds.center;
                horizontal.y = 0;
                minSeparation = Mathf.Min(minSeparation, horizontal.magnitude);
            }
            float progress = Vector3.Dot(player.transform.position - start, direction);
            // Uma pilastra redonda pode desviar a cápsula para o lado e permitir
            // seguir adiante. Meça a separação durante todo o trajeto, não apenas
            // o avanço final, que confundia contornar com atravessar a coluna.
            bool blocked = obstacle is CapsuleCollider column
                ? minSeparation >= column.radius * Mathf.Max(column.transform.lossyScale.x, column.transform.lossyScale.z) +
                    controller.radius - controller.skinWidth - .025f
                : progress < extent + .85f;
            Require(probe.Hits.Contains(obstacle) && progress > .1f && blocked,
                "Cápsula do visitante é bloqueada por " + obstacle.name + " (avanço " + progress.ToString("F2") + " m)");
        }

        private void Position(Vector3 position)
        {
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private void Require(bool condition, string description)
        {
            if (!condition) throw new Exception("[PhysicsValidation] " + description);
            checks++;
            Debug.Log("[PhysicsValidation] PASS " + description);
        }
    }

    public sealed class PhysicsCollisionProbe : MonoBehaviour
    {
        public readonly HashSet<Collider> Hits = new HashSet<Collider>();
        private void OnControllerColliderHit(ControllerColliderHit hit) => Hits.Add(hit.collider);
    }
}
#endif
