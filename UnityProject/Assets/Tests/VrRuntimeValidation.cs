#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;

namespace MuseumModerna
{
    public sealed class VrRuntimeValidation : MonoBehaviour
    {
        private MobileVrMode mode;
        private VrWorldGuide guide;
        private Camera eye;
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
            Debug.Log($"[VrValidation] PASS: {checks} verificações de interação e recuperação VR (simulação no Editor; sem lentes físicas)");
            UnityEditor.EditorApplication.Exit(0);
        }
        private IEnumerator Tests()
        {
            mode = MobileVrMode.Instance;
            Require(mode != null, "Bootstrap VR");
            guide = mode.WorldGuide;
            eye = FindAnyObjectByType<GazeDwellInteraction>().GetComponent<Camera>();
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled = false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled = false;
            foreach (var locked in FindObjectsByType<LockPosition>(FindObjectsSortMode.None)) locked.enabled = false;
            var player = FindAnyObjectByType<PlayerController>();
            var gaze = eye.GetComponent<GazeDwellInteraction>();
            var original = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None).First(e => e.PaintingData.uniqueId == "homem_amarelo");
            player.transform.position += original.transform.position + original.transform.forward * 3 - eye.transform.position;
            eye.transform.LookAt(original.transform.position);
            gaze.SetPanelsEnabled(true);
            mode.EnterEditorPreview();
            Require(mode.IsActive && !guide.MenuOpen && !FindAnyObjectByType<UIManager>().GetComponent<Canvas>().enabled, "VR inicia a visita sem menu central");
            Require(guide.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "Ficha em espaço3D para ambos os olhos");
            eye.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1, .6f));
            yield return new WaitForSecondsRealtime(1.5f);
            Require(guide.MenuOpen, "Olhar para cima abre opções sem controle");
            Require(Button("Sair do VR").interactable, "Sair do VR fica na primeira página de opções");
            var initialPosition = guide.Board.position;
            PointAt(Button("Voltar à visita").transform);
            yield return new WaitForSecondsRealtime(.5f);
            Require(guide.MenuOpen, "Permanência parcial não aciona menu");
            Require(Vector3.Distance(initialPosition, guide.Board.position) < .001f, "Painel não foge ao girar cabeça");
            yield return new WaitForSecondsRealtime(.7f);
            Require(!guide.MenuOpen, "Voltar acionado apenas pelo olhar");
            eye.transform.LookAt(original.transform.position);
            yield return new WaitForSecondsRealtime(.5f);
            Require(gaze.Focus.Current == original.PaintingData && guide.GetComponent<CanvasGroup>().alpha > .9f, "Obra abre ficha VR");
            Require(guide.PageCount > 1, "Ficha longa paginada sem toque");
            PointAt(Button("Próxima").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(guide.PageIndex == 1 && gaze.Focus.Current == original.PaintingData, "Ler controles mantém ficha e avança página");
            yield return new WaitForSecondsRealtime(1.5f);
            Require(guide.PageIndex == 1, "Página não repete ao manter olhar");
            PointAt(Button("Fixar").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(gaze.Focus.Pinned, "Fixação pelo olhar");
            eye.transform.LookAt(original.transform.position);
            yield return new WaitForSecondsRealtime(.2f);
            Capture("museum-vr-guide-editor.png");
            PointAt(Button("Liberar").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(!gaze.Focus.Pinned, "Liberar pelo olhar");
            PointAt(Button("Fechar").transform);
            yield return new WaitForSecondsRealtime(1.6f);
            Require(gaze.Focus.Current == null && guide.GetComponent<CanvasGroup>().alpha == 0, "Fechar pelo olhar");
            eye.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1, .6f));
            yield return new WaitForSecondsRealtime(1.5f);
            Require(guide.MenuOpen, "Olhar para cima abre opções sem controle");
            PointAt(Button("Sair do VR").transform); yield return new WaitForSecondsRealtime(1.3f);
            var ui = FindAnyObjectByType<UIManager>();
            Require(!mode.IsActive && ui.GetComponent<Canvas>().enabled &&
                ui.GetComponent<WelcomeScreen>().IsVisible && player.State == PlayerState.Paused,
                "Sair pelo olhar restaura a home e interrompe caminhada");

            // Recuperação conjunta: Android pode notificar foco e pausa em ordens diferentes.
            eye.transform.localRotation = Quaternion.Euler(0, 35, 0);
            mode.EnterEditorPreview();
            Require(mode.IsActive && !guide.MenuOpen && player.State != PlayerState.Paused,
                "Entrar novamente depois de sair libera a visita");
            guide.OpenMenu();
            PointAt(Button("Voltar à visita").transform);
            yield return new WaitForSecondsRealtime(.4f);
            Require(guide.DwellProgress > 0 && guide.DwellProgress < 1, "Dwell parcial antes de suspensão");
            Invoke(mode, "OnApplicationPause", true);
            Invoke(mode, "OnApplicationFocus", false);
            Require(player.State == PlayerState.Paused && guide.DwellProgress == 0,
                "Suspender VR interrompe movimento e cancela dwell parcial");
            Invoke(mode, "OnApplicationPause", false);
            yield return null;
            Require(player.State == PlayerState.Paused, "Fim da pausa sem foco não retoma o VR");
            Invoke(mode, "OnApplicationFocus", true);
            Require(player.State == PlayerState.Paused, "Retomar espera pose antes de liberar caminhada");
            for (int i = 0; i < 4; i++) yield return null;
            Require(mode.IsActive && player.State != PlayerState.Paused && !guide.MenuOpen,
                "Foco recuperado reabre visita e fecha opções antigas");
            var eyeGyro = eye.GetComponent<GyroscopeController>();
            Require(!eyeGyro.IsLookingDown && !eyeGyro.IsWalkingGestureArmed,
                "Retomada exige um novo gesto de caminhada");
            Vector3 eyePosition = eye.transform.localPosition;
            mode.Recenter();
            Require(player.State == PlayerState.Paused, "Centralizar suspende caminhada enquanto aguarda pose");
            for (int i = 0; i < 4; i++) yield return null;
            Require(player.State != PlayerState.Paused && eye.transform.localPosition == eyePosition,
                "Centralizar mantém a altura do visitante e recupera controles");

            // Reproduz o OnDisable real do SDK restaurando uma pose antiga na saída.
            var driver = eye.gameObject.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.originPose = new Pose(eyePosition + Vector3.up * 1.5f, Quaternion.identity);
            typeof(MobileVrMode).GetField("driver", Private).SetValue(mode, driver);
            Quaternion exitRotation = Quaternion.Euler(12, 65, 0);
            eye.transform.localRotation = exitRotation;
            mode.ExitVR();
            Require(eye.transform.localPosition == eyePosition &&
                Quaternion.Angle(eye.transform.localRotation, exitRotation) < .01f,
                "Desativar TrackedPoseDriver não restaura altura ou direção antigas");
            Require(!driver.enabled && !mode.HasHeadTracking && ui.GetComponent<WelcomeScreen>().IsVisible,
                "Saída devolve a câmera ao modo normal e abre a home");
            mode.EnterNormalMode();
            Require(player.State != PlayerState.Paused && !ui.GetComponent<WelcomeScreen>().IsVisible,
                "Modo normal continua acessível depois de sair do VR");

            // Sem subsistema físico no Editor, o driver não pode entregar rotação.
            mode.EnterEditorPreview();
            driver.enabled = true;
            mode.Recenter();
            yield return new WaitForSecondsRealtime(3.4f);
            Require(!mode.IsActive && mode.IsPreparationVisible && player.State == PlayerState.Paused,
                "Perda persistente de pose devolve preparação com movimento interrompido");
            Require(ui.GetComponent<Canvas>().enabled && !driver.enabled,
                "Falha de tracking restaura câmera e interface 2D");
            mode.ShowMainMenu();
            mode.EnterNormalMode();
            Require(!mode.IsPreparationVisible && player.State != PlayerState.Paused,
                "Falha de tracking permite voltar à visita normal");
        }
        private Button Button(string label) => guide.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>().text == label);
        private void PointAt(Transform target)
        {
            Canvas.ForceUpdateCanvases();
            eye.transform.LookAt(target.position);
        }
        private void Capture(string filename)
        {
            var target = new RenderTexture(1440, 900, 24);
            eye.targetTexture = target; Canvas.ForceUpdateCanvases(); eye.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); texture.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/bugfix-2026-09-21"));
            Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, filename), texture.EncodeToPNG());
            eye.targetTexture = null; RenderTexture.active = previous;
            Destroy(texture); target.Release(); Destroy(target);
            Debug.Log("[VrValidation] Captura mono da interface worldspace: " + filename);
        }
        private void Require(bool ok, string message)
        {
            if (!ok) throw new Exception("[VrValidation] " + message);
            checks++; Debug.Log("[VrValidation] PASS " + message);
        }
        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);
    }
}
#endif
