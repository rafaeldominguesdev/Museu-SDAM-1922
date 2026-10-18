#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MuseumModerna
{
    public sealed class AndroidMenuRuntimeValidation : MonoBehaviour
    {
        private WelcomeScreen welcome;
        private UIManager ui;
        private Camera eye;
        private int checks;
        private int savedTilt, savedTap;
        private bool hadTilt, hadTap;
        private IEnumerator Start()
        {
            hadTilt=PlayerPrefs.HasKey("MuseumTiltWalking");hadTap=PlayerPrefs.HasKey("MuseumTapToWalk");
            savedTilt=PlayerPrefs.GetInt("MuseumTiltWalking",0);savedTap=PlayerPrefs.GetInt("MuseumTapToWalk",0);
            HeadGazeMovement.SetTapToWalk(false);
            yield return null; yield return null;
            var routine = Tests();
            while (true)
            {
                object next;
                try { if (!routine.MoveNext()) break; next = routine.Current; }
                catch (Exception e) { RestorePreferences();Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); yield break; }
                yield return next;
            }
            RestorePreferences();
            Debug.Log($"[AndroidMenuValidation] PASS: {checks} verificações de menu/visual; 7 capturas.");
            UnityEditor.EditorApplication.Exit(0);
        }
        private void RestorePreferences()
        {
            if(hadTilt)PlayerPrefs.SetInt("MuseumTiltWalking",savedTilt);else PlayerPrefs.DeleteKey("MuseumTiltWalking");
            if(hadTap)PlayerPrefs.SetInt("MuseumTapToWalk",savedTap);else PlayerPrefs.DeleteKey("MuseumTapToWalk");
            PlayerPrefs.Save();
        }
        private IEnumerator Tests()
        {
                ui = FindAnyObjectByType<UIManager>();
                welcome = ui.GetComponent<WelcomeScreen>();
                eye = FindAnyObjectByType<GazeDwellInteraction>().GetComponent<Camera>();
                var player = FindAnyObjectByType<PlayerController>();
                var mode = MobileVrMode.Instance;
                Require(welcome.IsVisible && player.State == PlayerState.Paused && !mode.IsPreparationVisible,
                    "Abertura única, jogador parado, preparação VR não iniciada");
                Require(welcome.Root.GetComponentsInChildren<Button>().Length == 2, "Exatamente dois modos principais");
                foreach (var size in new[] { new Vector2Int(844,390), new Vector2Int(1280,720), new Vector2Int(2400,1080), new Vector2Int(390,844), new Vector2Int(568,320), new Vector2Int(640,360), new Vector2Int(320,568) })
                    Capture(size.x, size.y);
                EventSystem.current.enabled = false;
                welcome.Show();
                yield return null;
                Click(welcome.NormalButton);
                Require(!welcome.IsVisible && !mode.IsActive && !mode.IsPreparationVisible && player.State == PlayerState.Walking,
                    "Modo normal entra sem inicializar visor e sem menu sobreposto");
                bool savedTilt=HeadGazeMovement.TiltWalking;
                HeadGazeMovement.SetTiltWalking(false);yield return null;
                var walk=FindAnyObjectByType<MuseumWalkButton>();
                Require(walk.GetComponentInChildren<Text>().text.Contains("CAMINHAR"),
                    "Botão de caminhada mostra instrução desde a primeira visita");
                var movement=player.GetComponent<HeadGazeMovement>();
                typeof(HeadGazeMovement).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(movement,new object[]{true});
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                Canvas.ForceUpdateCanvases();
                var walkRect=(RectTransform)walk.transform;
                pointer.position=RectTransformUtility.WorldToScreenPoint(null,walkRect.TransformPoint(walkRect.rect.center));
                var walkHits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,walkHits);
                Require(walkHits.Count>0&&ExecuteEvents.GetEventHandler<IPointerDownHandler>(walkHits[0].gameObject)==walk.gameObject,
                    "Toque na tela alcança o botão de caminhada sem sobreposição");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                Require(movement.ManualWalkingRequested,"Pressionar botão normal solicita caminhada");
                var beforeWalk=player.transform.position;
                yield return new WaitForSecondsRealtime(.4f);
                Require(Vector2.Distance(new Vector2(beforeWalk.x,beforeWalk.z),new Vector2(player.transform.position.x,player.transform.position.z))>.1f,
                    "Segurar o botão desloca o jogador real no Hall");
                Require(walk.GetComponent<Image>().color==MuseumGuideTheme.Focus,"Feedback de pressão permanece durante a caminhada");
                var otherFinger=new PointerEventData(EventSystem.current){pointerId=8,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(walk.gameObject,otherFinger,ExecuteEvents.pointerUpHandler);
                Require(movement.ManualWalkingRequested,"Soltar outro dedo não cancela a caminhada");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                Require(!movement.ManualWalkingRequested&&movement.CurrentSpeed==0,"Soltar botão normal para sem inércia");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                Require(!movement.ManualWalkingRequested,"Sair da área do botão também interrompe caminhada");
                HeadGazeMovement.SetTapToWalk(true);yield return null;
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                Require(movement.ManualWalkingRequested,"Modo por toque inicia caminhada sem exigir pressão contínua");
                yield return null;
                Require(walk.GetComponentInChildren<Text>().text.Contains("Toque para parar"),"Modo por toque mostra como parar");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                Require(!movement.ManualWalkingRequested,"Segundo toque interrompe caminhada");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                walk.enabled=false;
                Require(!movement.ManualWalkingRequested,"Desativar controle cancela caminhada por toque");
                walk.enabled=true;
                HeadGazeMovement.SetTapToWalk(false);
                HeadGazeMovement.SetTiltWalking(true);yield return null;
                Require(walk.GetComponent<CanvasGroup>().interactable,"Botão continua disponível quando inclinação está ativa");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                Require(movement.ManualWalkingRequested,"Botão inicia caminhada mesmo com inclinação ativa");
                ExecuteEvents.Execute(walk.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                Require(!movement.ManualWalkingRequested,"Botão interrompe caminhada auxiliar com inclinação ativa");
                HeadGazeMovement.SetTiltWalking(false);
                HeadGazeMovement.SetTiltWalking(savedTilt);
                ui.Guide.OpenSettings();
                Require(player.State==PlayerState.Paused,"Opções pausam a visita em qualquer modo de caminhada");
                mode.HandleBack();
                Require(!ui.Guide.SettingsOpen&&!welcome.IsVisible&&!mode.IsPreparationVisible,
                    "Voltar fecha somente opções sem sair da visita normal");
                Require(EventSystem.current.currentSelectedGameObject==null,
                    "Fechar opções limpa seleção de botão oculto");
                Require(player.State!=PlayerState.Paused&&!movement.ManualWalkingRequested,"Fechar opções retoma sem caminhar sozinho");
                mode.ShowMainMenu();
                yield return null;
                Require(welcome.IsVisible && player.State == PlayerState.Paused, "Retornar ao menu interrompe visita");
                Click(welcome.VrButton);
                Require(mode.IsActive && !mode.WorldGuide.MenuOpen && !welcome.IsVisible && !ui.GetComponent<Canvas>().enabled,
                    "Modo VR inicia visita diretamente e remove interface 2D");
                mode.ExitVR();
                yield return null;
                Require(!mode.IsActive && welcome.IsVisible && player.State == PlayerState.Paused && ui.GetComponent<Canvas>().enabled,
                    "Sair do visor retorna à escolha dos dois modos");
                mode.ShowPreparation();
                yield return null;
                Require(mode.IsPreparationVisible && !welcome.IsVisible && player.State == PlayerState.Paused,
                    "Preparação tem tela exclusiva e movimento bloqueado");
                var back = ui.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>()?.text == "Voltar ao menu");
                Click(back);
                Require(welcome.IsVisible && !mode.IsPreparationVisible, "Preparação permite voltar ao menu");
                yield return null;
                Click(welcome.NormalButton);
                Require(!welcome.IsVisible && player.State == PlayerState.Walking, "Home continua clicável depois de VR e preparação");
        }
        private void Capture(int width, int height)
        {
            var canvas = ui.GetComponent<Canvas>();
            var originalMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            var scaler = ui.GetComponent<CanvasScaler>();
            var originalResolution = scaler.referenceResolution;
            float originalScale = canvas.scaleFactor;
            var target = new RenderTexture(width, height, 24);
            eye.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = eye; canvas.planeDistance = .5f;
            ui.Guide.ConfigureScale(width, height);
            canvas.scaleFactor = Mathf.Sqrt(width / scaler.referenceResolution.x * height / scaler.referenceResolution.y);
            Canvas.ForceUpdateCanvases();
            typeof(WelcomeScreen).GetMethod("UpdateLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(welcome, new object[] { true });
            Canvas.ForceUpdateCanvases();
            var root = (RectTransform)welcome.Root.transform;
            var safe = (RectTransform)root.Find("AreaSegura");
            foreach (var button in new[] { welcome.NormalButton, welcome.VrButton })
            {
                var rect = (RectTransform)button.transform;
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                Require(corners.All(p => safe.rect.Contains((Vector2)safe.InverseTransformPoint(p))),
                    $"{button.name} cabe na área segura em {width}x{height}");
                Require(rect.rect.height >= MuseumGuideTheme.ButtonHeight, "Área de toque mínima preservada");
                AssertPointerTarget(button);
            }
            foreach (var label in root.GetComponentsInChildren<Text>())
            {
                float preferred = label.preferredHeight;
                Require(preferred <= label.rectTransform.rect.height + 2, $"Texto {label.name} sem corte em {width}x{height}");
            }
            // A abertura real usa overlay: a geometria não deve interceptar o Canvas da captura.
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled).ToArray();
            foreach (var renderer in renderers) renderer.enabled = false;
            eye.Render();
            foreach (var renderer in renderers) renderer.enabled = true;
            var old = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
            string folder = Path.GetFullPath(Environment.GetEnvironmentVariable("MUSEUM_CAPTURE_OUTPUT") ?? Path.Combine(Application.dataPath,"../../artifacts/bugfix-2026-09-21"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,$"menu-{width}x{height}.png"),texture.EncodeToPNG());
            RenderTexture.active = old; eye.targetTexture = null;
            canvas.renderMode = originalMode; canvas.worldCamera = originalCamera;
            scaler.referenceResolution = originalResolution;
            canvas.scaleFactor = originalScale;
            Canvas.ForceUpdateCanvases();
            typeof(WelcomeScreen).GetMethod("UpdateLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(welcome, new object[] { true });
            Canvas.ForceUpdateCanvases();
            Destroy(texture); target.Release(); Destroy(target);
        }
        private PointerEventData AssertPointerTarget(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var events = EventSystem.current;
            Require(events != null && events.currentInputModule != null && events.currentInputModule.isActiveAndEnabled,
                "EventSystem e módulo de entrada ativos");
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(events)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            events.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
                button.name + " recebe o toque real, sem bloqueio por outro painel; primeiro alvo: " +
                (hits.Count > 0 ? hits[0].gameObject.name : "nenhum"));
            return pointer;
        }
        private void Click(Button button)
        {
            var pointer = AssertPointerTarget(button);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        private void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("[AndroidMenuValidation] " + message);
            checks++;
            Debug.Log("[AndroidMenuValidation] PASS " + message);
        }
    }
}
#endif
