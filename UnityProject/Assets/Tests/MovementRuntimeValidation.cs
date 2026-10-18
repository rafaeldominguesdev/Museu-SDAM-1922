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
            bool savedTilt=HeadGazeMovement.TiltWalking;
            HeadGazeMovement.SetTiltWalking(true);
            yield return null; yield return null;
            var routine = Tests();
            while (true)
            {
                object value;
                try { if (!routine.MoveNext()) break; value = routine.Current; }
                catch (Exception e) { HeadGazeMovement.SetTiltWalking(savedTilt); Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); yield break; }
                yield return value;
            }
            HeadGazeMovement.SetTiltWalking(savedTilt);
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

            HeadGazeMovement.SetTiltWalking(false);
            Pose(0,4);Pose(-40,2);Invoke(movement,"Update");
            Require(movement.CurrentSpeed==0,"Modo por botão não caminha ao observar o chão");
            eye.localRotation=Quaternion.identity;
            movement.RequestManualWalk();Invoke(movement,"Update");
            Require(movement.ManualWalkingRequested&&movement.CurrentSpeed>0,"Comando explícito inicia caminhada normal");
            movement.CancelManualWalk();
            Require(!movement.ManualWalkingRequested&&movement.CurrentSpeed==0,"Soltar botão interrompe imediatamente");
            movement.RequestManualWalk();player.SetPaused(true);player.SetPaused(false);
            Require(!movement.ManualWalkingRequested,"Pausa cancela intenção manual sem retomada automática");
            HeadGazeMovement.SetTiltWalking(true);

            Pose(0,4);Pose(-40,2);
            movement.RequestManualWalk();Invoke(movement,"Update");
            movement.CancelManualWalk();
            Pose(-40,6);Invoke(movement,"Update");
            Require(movement.CurrentSpeed==0&&!gyro.IsLookingDown,
                "Parar pelo botão não retoma caminhada por inclinação no frame seguinte");
            movement.ResetMovement();

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

            // Mede a cadeia inteira: pose de entrada, suavização, gesto e deslocamento.
            foreach(int fps in new[]{20,30,60})
            {
                Time.captureDeltaTime=1f/fps;
                yield return null;
                eye.localRotation=Quaternion.identity;
                gyro.ResumeFromCurrentPose();
                Pose(0,4);
                Set(gyro,"_targetRotation",Quaternion.Euler(50f,0,0));
                Set(movement,"_currentSpeed",0f);
                float elapsed=0f, started=-1f;
                for(int frame=0;frame<fps;frame++)
                {
                    Invoke(gyro,"ApplyLookRotation",1f/fps); Invoke(movement,"Update");
                    elapsed+=1f/fps;
                    if(started<0 && movement.CurrentSpeed>.05f) started=elapsed;
                    yield return null;
                }
                Require(started>0 && started<=.35f && movement.CurrentSpeed>1.2f && movement.CurrentSpeed<=1.251f,
                    $"Modo normal a {fps} fps inicia em {started:F3}s e limita a 1,25 m/s");
                Set(gyro,"_targetRotation",Quaternion.identity);
                for(int frame=0;frame<fps/2;frame++)
                { Invoke(gyro,"ApplyLookRotation",1f/fps); Invoke(movement,"Update"); yield return null; }
                Require(movement.CurrentSpeed<.02f,"Modo normal para em até 0,5 s ao levantar o olhar");
            }
            Time.captureDeltaTime=1f/60f;
            yield return null;

            int savedSpeed=HeadGazeMovement.SpeedSetting;
            for(int i=0;i<3;i++)
            {
                rig.transform.position=new Vector3(200,0,200);
                eye.localRotation=Quaternion.identity;gyro.ResumeFromCurrentPose();
                Pose(0,4);Pose(-40,2);eye.localRotation=Quaternion.Euler(40,0,0);
                Set(movement,"_currentSpeed",0f);
                for(int frame=0;frame<60;frame++){Invoke(movement,"Update");yield return null;}
                Require(Mathf.Abs(movement.CurrentSpeed-HeadGazeMovement.SelectedSpeed(false))<.02f,
                    "Caminhada atinge velocidade selecionada: "+HeadGazeMovement.SpeedLabel);
                HeadGazeMovement.CycleSpeed();
            }
            Require(HeadGazeMovement.SpeedSetting==savedSpeed,"Preferência preservada após ciclo de velocidades");

            // A proteção da obra não pode desarmar o gesto: virar deve liberar a saída.
            Pose(0, 4); Pose(-40, 2);
            eye.localRotation = Quaternion.Euler(40, 0, 0);
            var exhibit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exhibit.name = "MovementTestExhibit";
            exhibit.transform.position = rig.transform.position + new Vector3(0, 1, .85f);
            exhibit.transform.localScale = new Vector3(2, 2, .1f);
            exhibit.AddComponent<PaintingExhibit>();
            Physics.SyncTransforms();
            before = rig.transform.position;
            for (int frame = 0; frame < 30; frame++)
            {
                Pose(-40, 1);
                Invoke(movement, "Update");
                yield return null;
            }
            Require(movement.CurrentSpeed == 0 && HorizontalDistance(before, rig.transform.position) < .01f,
                "Proteção mantém distância da obra enquanto o visitante insiste em avançar");
            Require(gyro.IsLookingDown && gyro.IsWalkingGestureArmed,
                "Proteção da obra preserva intenção de caminhada");
            eye.localRotation = Quaternion.Euler(40, 180, 0);
            for (int frame = 0; frame < 30; frame++)
            {
                Pose(-40, 1);
                Invoke(movement, "Update");
                yield return null;
            }
            Require(before.z - rig.transform.position.z > .35f,
                "Virar para sair da obra retoma caminhada sem levantar a cabeça");
            Destroy(exhibit);
            yield return null;

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

            // Uma ficha é uma suspensão temporária, não uma pausa de segurança.
            Pose(0, 4); Pose(-40, 2);
            var guide = mode.WorldGuide;
            guide.Show(FindAnyObjectByType<PaintingExhibit>().PaintingData);
            var guideEye = (Camera)guide.GetType().GetField("eye", Private).GetValue(guide);
            var eyeRotation = guideEye.transform.rotation;
            guideEye.transform.LookAt(guide.Board.position);
            Invoke(guide, "Update");
            Require(guide.ConsumesGaze, "Olhar sobre a ficha VR é detectado");
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed == 0 && gyro.IsWalkingGestureArmed,
                "Ler a ficha suspende avanço sem desarmar caminhada");
            guide.Hide();
            guideEye.transform.rotation = eyeRotation;
            eye.localRotation = Quaternion.Euler(40, 180, 0);
            before = rig.transform.position;
            Invoke(movement, "Update");
            Require(movement.CurrentSpeed > .05f && HorizontalDistance(before, rig.transform.position) > .001f,
                "Sair da ficha retoma no primeiro frame, sem cooldown ou novo gesto");

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
