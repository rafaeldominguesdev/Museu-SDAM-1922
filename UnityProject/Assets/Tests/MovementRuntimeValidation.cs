#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Regressões de entrada VR, pausa, intenção de caminhada e colisão sem sensores físicos.</summary>
    public sealed class MovementRuntimeValidation : MonoBehaviour
    {
        private GyroscopeController gyro;
        private HeadGazeMovement movement;
        private PlayerController player;
        private Transform eye;
        private int checks;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private IEnumerator Start()
        {
            yield return null; yield return null;
            var routine = Tests();
            while (true)
            {
                object value;
                try { if (!routine.MoveNext()) break; value = routine.Current; }
                catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); yield break; }
                yield return value;
            }
            Debug.Log($"[MovementValidation] PASS: {checks} verificações de movimento (poses simuladas; sem aparelho físico).");
            UnityEditor.EditorApplication.Exit(0);
        }

        private IEnumerator Tests()
        {
            foreach (var existing in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) existing.enabled = false;
            foreach (var existing in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) existing.enabled = false;
            foreach (var existing in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) existing.enabled = false;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "MovementTestFloor";
            floor.transform.position = new Vector3(200, -.1f, 200);
            floor.transform.localScale = new Vector3(20, .2f, 20);
            var rig = new GameObject("TestMovementRig");
            rig.SetActive(false);
            rig.transform.position = new Vector3(200, 0, 200);
            var camera = new GameObject("TestEye", typeof(Camera));
            camera.GetComponent<Camera>().enabled = false;
            eye = camera.transform;
            eye.SetParent(rig.transform, false);
            eye.localPosition = Vector3.up * 1.65f;
            gyro = camera.AddComponent<GyroscopeController>();
            movement = rig.AddComponent<HeadGazeMovement>();
            player = rig.AddComponent<PlayerController>();
            Set(movement, "gyroController", gyro);
            Set(movement, "cameraTransform", eye);
            rig.SetActive(true);
            gyro.enabled = movement.enabled = false;
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            Invoke(movement, "OnApplicationFocus", true);
            Physics.SyncTransforms();
            for (int frame = 0; frame < 10; frame++) Invoke(movement, "Update");

            Pose(-55, 12);
            Require(!gyro.IsLookingDown && !gyro.IsWalkingGestureArmed, "Entrar já inclinado não inicia caminhada");
            Pose(0, 1); Pose(-55, 1); Pose(0, 3);
            Require(!gyro.IsWalkingGestureArmed, "Olhar neutro precisa ser contínuo");
            Pose(0, 1);
            Require(gyro.IsWalkingGestureArmed, "Olhar à frente por 0,35 s arma caminhada");
            Pose(-40, 1);
            Require(!gyro.IsLookingDown, "Pico curto de inclinação não anda");
            Pose(-40, 1);
            Require(gyro.IsLookingDown, "Inclinação sustentada inicia caminhada");
            Pose(-25, 1);
            Require(gyro.IsLookingDown, "Histerese evita liga/desliga no limiar");
            Pose(-20, 1);
            Require(!gyro.IsLookingDown, "Levantar a cabeça encerra gesto");

            Set(movement, "_currentSpeed", 1.1f);
            player.SetPaused(true);
            Require(movement.CurrentSpeed == 0 && !gyro.IsWalkingGestureArmed, "Pausa zera inércia e gesto imediatamente");
            player.SetPaused(false);
            var before = rig.transform.position;
            Pose(-55, 12);
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed == 0 && HorizontalDistance(rig.transform.position, before) < .001f,
                "Retomar olhando para baixo não reutiliza velocidade nem intenção anterior");

            Pose(0, 4); Pose(-40, 2);
            Set(movement, "_currentSpeed", 1.1f);
            Invoke(movement, "OnApplicationPause", true);
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed == 0 && !gyro.IsWalkingGestureArmed && rig.transform.position == before,
                "Android em segundo plano fica parado");
            Invoke(movement, "OnApplicationPause", false);
            Set(movement, "_currentSpeed", 1.1f);
            Invoke(movement, "OnApplicationFocus", false);
            Require(movement.CurrentSpeed == 0 && !gyro.IsWalkingGestureArmed, "Perda de foco cancela caminhada");
            Invoke(movement, "OnApplicationFocus", true);

            eye.localRotation = Quaternion.Euler(40, 35, 0);
            var heading = (Vector3)Invoke(movement, "GetFlatLookDirection");
            eye.localRotation = Quaternion.Euler(89, 35, 70);
            var verticalHeading = (Vector3)Invoke(movement, "GetFlatLookDirection");
            Require(Vector3.Angle(heading, verticalHeading) < .01f, "Roll perto da vertical mantém rumo da caminhada");

            rig.transform.rotation = Quaternion.Euler(0, 60, 0);
            eye.localRotation = Quaternion.Euler(15, 25, 0);
            var rotation = eye.localRotation;
            gyro.ResumeFromCurrentPose();
            Invoke(gyro, "Update");
            Require(Quaternion.Angle(rotation, eye.localRotation) < .05f &&
                Vector3.Angle(gyro.CurrentLookDirection, eye.forward) < .01f,
                "Saída de XR preserva pose e publica direção no mundo");
            rig.transform.rotation = Quaternion.identity;

            var mode = MobileVrMode.Instance;
            Require(mode != null, "Modo VR disponível para integração");
            mode.EnterEditorPreview();
            mode.WorldGuide.CloseMenu();
            yield return null;
            Pose(0, 4); Pose(-40, 2);
            // A primeira atualização detecta a transição do bloqueio da interface.
            Invoke(movement, "Update");
            Pose(0, 4); Pose(-40, 2);
            eye.localRotation = Quaternion.Euler(40, 0, 0);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "MovementTestWall";
            wall.transform.position = rig.transform.position + new Vector3(0, 1, 1.2f);
            wall.transform.localScale = new Vector3(3, 2, .2f);
            Physics.SyncTransforms();
            before = rig.transform.position;
            for (int frame = 0; frame < 100; frame++)
            {
                Pose(-40, 1);
                Invoke(movement, "Update");
                yield return null;
            }
            float walked = rig.transform.position.z - before.z;
            Require(walked > .3f && walked < 1f && Mathf.Abs(rig.transform.position.y - before.y) < .025f,
                "CharacterController caminha, colide com parede e mantém altura");
            Require(movement.CurrentSpeed <= 1.101f, "VR respeita limite de caminhada de 1,1 m/s");
            Pose(0, 1);
            before = rig.transform.position;
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed == 0 && HorizontalDistance(rig.transform.position, before) < .001f,
                "Levantar cabeça em VR para sem deslizamento");

            Pose(-40, 2); Set(movement, "_currentSpeed", 1.1f);
            mode.WorldGuide.OpenMenu();
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed == 0 && !gyro.IsWalkingGestureArmed, "Menu VR cancela velocidade e gesto");
            mode.WorldGuide.CloseMenu();
            Invoke(movement, "Update");
            Pose(-40, 10);
            Require(!gyro.IsLookingDown, "Fechar menu inclinado exige olhar à frente novamente");

            var controller = rig.GetComponent<CharacterController>();
            controller.enabled = false;
            Set(movement, "_currentSpeed", 1.1f);
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed == 0, "Controller desativado em teleporte não recebe Move");
            controller.enabled = true;
            // Pausing walking must still settle the feet after a spawn/teleport.
            controller.enabled = false;
            rig.transform.position = new Vector3(200, 1.2f, 200);
            controller.enabled = true;
            movement.SyncLockedY();
            player.SetPaused(true);
            for (int frame = 0; frame < 120; frame++) Invoke(movement, "Update");
            Require(controller.isGrounded && Mathf.Abs(rig.transform.position.y) < .06f,
                "Mesmo em pausa, gravidade apoia os pés no chão após teleporte");
            controller.enabled = false;
            rig.transform.position += Vector3.up;
            controller.enabled = true;
            Set(movement, "gyroController", null);
            player.SetPaused(false);
            for (int frame = 0; frame < 120; frame++) Invoke(movement, "Update");
            Require(controller.isGrounded && Mathf.Abs(rig.transform.position.y) < .06f,
                "Ausência de sensor não suspende a gravidade");
            Destroy(wall); Destroy(rig); Destroy(floor);
        }

        private void Pose(float pitch, int frames)
        {
            for (int i = 0; i < frames; i++) Invoke(gyro, "UpdateLookDownState", pitch, .1f);
        }
        private static float HorizontalDistance(Vector3 a, Vector3 b) =>
            Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        private void Require(bool value, string description)
        {
            if (!value) throw new Exception("[MovementValidation] " + description);
            checks++; Debug.Log("[MovementValidation] PASS " + description);
        }
        private static object Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);
    }
}
#endif
