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
            bool savedTilt=HeadGazeMovement.TiltWalking;
            HeadGazeMovement.SetTiltWalking(false);
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
            foreach (var existingMovement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) existingMovement.enabled = false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled = false;
            foreach (var locked in FindObjectsByType<LockPosition>(FindObjectsSortMode.None)) locked.enabled = false;
            var player = FindAnyObjectByType<PlayerController>();
            var gaze = eye.GetComponent<GazeDwellInteraction>();
            var original = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None).First(e =>
                e.PaintingData.uniqueId == "homem_amarelo" || e.PaintingData.uniqueId == "anita_homem");
            player.transform.position += original.transform.position + original.transform.forward * 3 - eye.transform.position;
            eye.transform.LookAt(original.transform.position);
            gaze.SetPanelsEnabled(true);
            mode.EnterEditorPreview();
            Invoke(player.GetComponent<HeadGazeMovement>(),"OnApplicationFocus",true);
            Require(mode.IsActive && !guide.MenuOpen && !FindAnyObjectByType<UIManager>().GetComponent<Canvas>().enabled, "VR inicia a visita sem menu central");
            Require(guide.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "Ficha em espaço3D para ambos os olhos");
            var nearRay = new Ray(original.transform.position + original.transform.forward * 2.5f, -original.transform.forward);
            var farRay = new Ray(original.transform.position + original.transform.forward * 5f, -original.transform.forward);
            Require(gaze.RaycastExhibit(farRay) == null, "Obra distante não abre ficha VR");
            Require(gaze.RaycastExhibit(nearRay) == original, "Obra próxima continua acessível pelo olhar");
            var toolbar=(RectTransform)typeof(VrWorldGuide).GetField("toolbar",Private).GetValue(guide);
            foreach (var exhibit in FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None))
            {
                player.transform.position += exhibit.transform.position + exhibit.transform.forward * 2.5f - eye.transform.position;
                eye.transform.rotation = Quaternion.LookRotation(exhibit.transform.forward);
                Physics.SyncTransforms();
                yield return null;
                Require(!toolbar.gameObject.activeSelf, "Menu oculto perto da obra mesmo olhando para longe: " + exhibit.PaintingData.uniqueId);
                guide.OpenMenu();
                Require(!guide.MenuOpen, "Atalho de opções respeita distância da obra: " + exhibit.PaintingData.uniqueId);
            }
            player.transform.position += new Vector3(0, 1.65f, 16) - eye.transform.position;
            eye.transform.rotation = Quaternion.identity;
            Physics.SyncTransforms(); yield return null;
            Require(toolbar.gameObject.activeSelf, "Menu disponível no ponto livre central da sala ao fundo");
            player.transform.position += new Vector3(0, 1.65f, 0) - eye.transform.position;
            Physics.SyncTransforms();
            eye.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1, .6f));
            yield return new WaitForSecondsRealtime(1.5f);
            Require(!guide.MenuOpen, "Observar a lua e o céu não abre opções");
            eye.transform.rotation=Quaternion.identity;yield return null;
            Require(toolbar.gameObject.activeSelf && !VrWorldGuide.HitRect(toolbar, new Ray(eye.transform.position, eye.transform.forward)),
                "Menu fica lateral e deixa livre o centro da visita");
            Require(!toolbar.GetComponentsInChildren<Button>(true).Any(b=>b.GetComponentInChildren<Text>().text=="Caminhar"),
                "VR n?o solicita comando Caminhar");
            Capture("vr-visita-sem-painel.png");
            var movement=player.GetComponent<HeadGazeMovement>();
            var sensor=eye.GetComponent<GyroscopeController>();
            foreach(bool legacyPreference in new[]{false,true})
            {
                HeadGazeMovement.SetTiltWalking(legacyPreference);
                eye.transform.rotation=Quaternion.Euler(40,0,0);yield return null;
                for(int i=0;i<8;i++)Invoke(sensor,"UpdateLookDownState",-40f,.1f);
                Invoke(movement,"UpdateHorizontalMovement");
                Require(movement.CurrentSpeed==0,"Entrar inclinado exige postura neutra antes de caminhar");
                for(int i=0;i<4;i++)Invoke(sensor,"UpdateLookDownState",0f,.1f);
                for(int i=0;i<8;i++)Invoke(sensor,"UpdateLookDownState",-40f,.1f);
                Invoke(movement,"UpdateHorizontalMovement");
                Require(movement.CurrentSpeed>0&&!movement.ManualWalkingRequested&&!guide.MenuOpen,
                    "Olhar para baixo caminha sem bot?o com prefer?ncia antiga "+legacyPreference);
                Require(!toolbar.gameObject.activeSelf,"Menu n?o disputa o gesto de caminhada");
                Invoke(sensor,"UpdateLookDownState",0f,.1f);
                Invoke(movement,"UpdateHorizontalMovement");
                Require(movement.CurrentSpeed==0,"Levantar a cabe?a para imediatamente no VR");
            }
            int savedSpeed=HeadGazeMovement.SpeedSetting;
            foreach(var expected in new[]{.8f,1.25f,1.6f})
            {
                int setting=Array.IndexOf(new[]{.8f,1.25f,1.6f},expected);
                PlayerPrefs.SetInt("MuseumWalkSpeed",setting);
                Require(Mathf.Approximately(HeadGazeMovement.SelectedSpeed(true),expected),"Velocidade VR: "+expected);
            }
            PlayerPrefs.SetInt("MuseumWalkSpeed",savedSpeed);
            HeadGazeMovement.SetTiltWalking(false);
            var savedPosition=player.transform.position;
            var controller=player.GetComponent<CharacterController>();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(200,-.1f,200);floor.transform.localScale=new Vector3(20,.2f,20);
            controller.enabled=false;player.transform.position=new Vector3(200,0,200);controller.enabled=true;
            Physics.SyncTransforms();
            movement.ResetMovement();
            eye.transform.rotation=Quaternion.Euler(40,0,0);yield return null;
            for(int i=0;i<4;i++)Invoke(sensor,"UpdateLookDownState",0f,.1f);
            for(int i=0;i<8;i++)Invoke(sensor,"UpdateLookDownState",-40f,.1f);
            movement.enabled=true;
            var beforeMove=player.transform.position;
            yield return new WaitForSecondsRealtime(.4f);
            Require(Vector2.Distance(new Vector2(beforeMove.x,beforeMove.z),new Vector2(player.transform.position.x,player.transform.position.z))>.1f,
                "Inclina??o desloca o CharacterController em frames reais");
            eye.transform.rotation=Quaternion.identity;
            Invoke(sensor,"UpdateLookDownState",0f,.1f);
            yield return null;yield return null;
            Require(movement.CurrentSpeed==0,"Levantar cabe?a interrompe deslocamento real");
            movement.enabled=false;Destroy(floor);
            controller.enabled=false;player.transform.position=savedPosition;controller.enabled=true;
            yield return null;
            var toolbarHeading=toolbar.rotation;
            PointAt(Button("Menu").transform);
            Capture("vr-controles.png");
            yield return new WaitForSecondsRealtime(.4f);
            Require(Quaternion.Angle(toolbarHeading,toolbar.rotation)<.01f,"Menu lateral n?o foge ao mirar nele");
            yield return new WaitForSecondsRealtime(.9f);
            Require(guide.MenuOpen, "Bot?o Menu abre op??es pelo olhar");
            Require(!toolbar.gameObject.activeSelf,"Abrir op??es oculta o bot?o lateral");
            Invoke(movement,"UpdateHorizontalMovement");
            Require(movement.CurrentSpeed==0&&!sensor.IsWalkingGestureArmed,"Menu pausa caminhada e desarma o gesto");
            var menuPosition=guide.Board.position;
            eye.transform.rotation=Quaternion.Euler(0,40,0);
            guide.OpenMenu();
            Require(Vector3.Distance(menuPosition,guide.Board.position)<.001f,
                "Solicitação repetida de menu não reposiciona opções sobre o olhar");
            Require(Button("Sair do VR").interactable, "Sair do VR fica na primeira página de opções");
            int initialSpeed=HeadGazeMovement.SpeedSetting;
            for(int i=0;i<3;i++)
            {
                var speed=Button("Velocidade: "+HeadGazeMovement.SpeedLabel);
                PointAt(speed.transform);
                yield return new WaitForSecondsRealtime(1.3f);
                Require(HeadGazeMovement.SpeedSetting==(initialSpeed+i+1)%3,"Velocidade VR alterada pelo olhar e salva");
                PointAt(Button("Centralizar").transform);yield return null;
            }
            Capture("vr-velocidade.png");
            var initialPosition = guide.Board.position;
            PointAt(Button("Voltar à visita").transform);
            yield return new WaitForSecondsRealtime(.5f);
            Require(guide.MenuOpen, "Permanência parcial não aciona menu");
            Require(Vector3.Distance(initialPosition, guide.Board.position) < .001f, "Painel não foge ao girar cabeça");
            yield return new WaitForSecondsRealtime(.7f);
            Require(!guide.MenuOpen, "Voltar acionado apenas pelo olhar");
            player.transform.position += original.transform.position + original.transform.forward * 3f - eye.transform.position;
            Physics.SyncTransforms();
            eye.transform.LookAt(original.transform.position);
            yield return new WaitForSecondsRealtime(.2f);
            Require(gaze.Focus.Current==null,"Passar rapidamente pela obra não abre ficha");
            player.SetPaused(true);
            yield return new WaitForSecondsRealtime(.2f);
            player.SetPaused(false);
            yield return new WaitForSecondsRealtime(.4f);
            Require(gaze.Focus.Current==null,"Retomar visita não abre ficha usando tempo de olhar anterior à pausa");
            yield return new WaitForSecondsRealtime(.6f);
            Require(gaze.Focus.Current == original.PaintingData && guide.GetComponent<CanvasGroup>().alpha > .9f, "Obra abre ficha VR");
            Require(guide.PageCount > 1, "Ficha longa paginada sem toque");
            var openingRay = new Ray(eye.transform.position, eye.transform.forward);
            Require(Vector3.Angle(openingRay.direction, guide.Board.position - openingRay.origin) < .1f &&
                Vector3.Angle(guide.Board.forward, openingRay.direction) < .1f,
                "Ficha abre centralizada e voltada ao olhar, incluindo inclinação");
            var corners = new Vector3[4];
            guide.Board.GetWorldCorners(corners);
            Require(corners.All(c => {
                var local = Quaternion.Inverse(eye.transform.rotation) * (c - eye.transform.position);
                return local.z > 0 && Mathf.Abs(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg) < 20 &&
                    Mathf.Abs(Mathf.Atan2(local.y, local.z) * Mathf.Rad2Deg) < 20;
            }), "Ficha inteira cabe nos 40 graus centrais ao abrir");
            var preferences=FindAnyObjectByType<UIManager>().Guide;
            var bodyText=(Text)typeof(VrWorldGuide).GetField("body",Private).GetValue(guide);
            var titleText=(Text)typeof(VrWorldGuide).GetField("title",Private).GetValue(guide);
            var pageContents=(System.Collections.Generic.List<string>)typeof(VrWorldGuide).GetField("pages",Private).GetValue(guide);
            Require(guide.GetComponentsInChildren<Button>().Select(b => b.GetComponentInChildren<Text>().text)
                .OrderBy(label => label).SequenceEqual(new[] { "Anterior", "Próxima página" }),
                "Ficha oferece somente Anterior e Próxima página");
            Require(bodyText.rectTransform.rect.height > 650, "Área dos controles removidos recebe texto de leitura");
            for(int textSize=0;textSize<3;textSize++)
            {
                foreach(var exhibit in FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None))
                {
                    guide.Show(exhibit.PaintingData);Canvas.ForceUpdateCanvases();
                    Require(titleText.preferredHeight<=titleText.rectTransform.rect.height+2,"Título completo: "+exhibit.PaintingData.uniqueId);
                    bool fits=true;
                    foreach(var content in pageContents)
                    {
                        bodyText.text=content;
                        fits &= bodyText.preferredHeight<=bodyText.rectTransform.rect.height+2;
                    }
                    Require(fits,"Páginas sem corte no tamanho "+preferences.TextStep+": "+exhibit.PaintingData.uniqueId);
                    var completeText = string.Concat(pageContents.Select(WithoutWhitespace));
                    var data = exhibit.PaintingData;
                    var fields = new[] { data.artist, data.Periodo, data.tecnica,
                        preferences.Summary ? data.resumo : data.description,
                        data.relacao_com_a_semana_de_1922, data.obra_historica_ou_reinterpretacao,
                        preferences.Summary ? "" : data.fontes };
                    Require(fields.All(field => completeText.Contains(WithoutWhitespace(field ?? ""))),
                        "Paginação preserva conteúdo e fontes: " + data.uniqueId);
                }
                preferences.CycleTextSize();
            }
            guide.Show(original.PaintingData);
            Require(pageContents[0].Contains("RELAÇÃO COM 1922"), "Primeira página aproveita espaço com a seção seguinte");
            PointAt(Button("Próxima página").transform);
            yield return new WaitForSecondsRealtime(1.3f);
            Require(guide.PageIndex == 1 && gaze.Focus.Current == original.PaintingData, "Ler controles mantém ficha e avança página");
            yield return new WaitForSecondsRealtime(1.5f);
            Require(guide.PageIndex == 1, "Página não repete ao manter olhar");
            var readingPosition = player.transform.position;
            var boardPosition = guide.Board.position;
            float readingDistance = Vector3.Distance(eye.transform.position, boardPosition);
            player.transform.position += guide.Board.forward * .6f;
            yield return null; yield return null;
            Require(Vector3.Distance(boardPosition, guide.Board.position) < .001f && guide.PageIndex == 1 &&
                Vector3.Distance(eye.transform.position, guide.Board.position) < readingDistance - .5f,
                "Aproximar reduz distância de leitura sem afastar ficha ou perder página");
            boardPosition = guide.Board.position;
            var boardRotation = guide.Board.rotation;
            eye.transform.rotation *= Quaternion.Euler(0, 25, 0);
            yield return null; yield return null;
            Require(Vector3.Distance(boardPosition, guide.Board.position) < .001f && Quaternion.Angle(boardRotation, guide.Board.rotation) < .01f,
                "Girar a cabeça mantém os botões da ficha estáveis");
            player.transform.position = readingPosition;
            yield return null; yield return null;
            Button("Anterior").onClick.Invoke();
            eye.transform.LookAt(original.transform.position);
            yield return new WaitForSecondsRealtime(.2f);
            Capture("museum-vr-guide-editor.png");
            eye.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            yield return new WaitForSecondsRealtime(5.5f);
            Require(gaze.Focus.Current == null && guide.GetComponent<CanvasGroup>().alpha == 0, "Desviar o olhar fecha ficha sem botão adicional");
            eye.transform.rotation = Quaternion.LookRotation(new Vector3(0, 1, .6f));
            yield return new WaitForSecondsRealtime(1.5f);
            Require(!guide.MenuOpen, "Céu permanece livre depois de fechar ficha");
            player.transform.position += new Vector3(0, 1.65f, 0) - eye.transform.position;
            Physics.SyncTransforms();
            eye.transform.rotation=Quaternion.identity;yield return null;
            PointAt(Button("Menu").transform);yield return new WaitForSecondsRealtime(1.3f);
            Require(guide.MenuOpen,"Menu continua disponível após fechar ficha");
            PointAt(Button("Sair do VR").transform); yield return new WaitForSecondsRealtime(1.3f);
            var ui = FindAnyObjectByType<UIManager>();
            Require(!mode.IsActive && ui.GetComponent<Canvas>().enabled &&
                ui.GetComponent<WelcomeScreen>().IsVisible && player.State == PlayerState.Paused,
                "Sair pelo olhar restaura a home e interrompe caminhada");

            // Recuperação conjunta: Android pode notificar foco e pausa em ordens diferentes.
            eye.transform.localRotation = Quaternion.Euler(35, 125, 0);
            mode.EnterEditorPreview();
            Require(mode.IsActive && !guide.MenuOpen && player.State != PlayerState.Paused,
                "Entrar novamente depois de sair libera a visita");
            yield return null;
            Require(Vector3.Angle(Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up),toolbar.forward)<.01f,
                "Entrar inclinado posiciona controles na direção atual, não na visita anterior");
            guide.OpenMenu();
            mode.HandleBack();
            Require(mode.IsActive&&!guide.MenuOpen,"Voltar fecha opções VR sem sair do visor");
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
            var speedButton=ui.GetComponentsInChildren<Button>(true).First(b=>b.GetComponentInChildren<Text>(true)?.text=="Velocidade: "+HeadGazeMovement.SpeedLabel);
            for(int i=0;i<3;i++)
            {
                speedButton.onClick.Invoke();
                Require(HeadGazeMovement.SpeedSetting==(initialSpeed+i+1)%3,"Velocidade normal alterada e compartilhada com VR");
            }
            Require(!ui.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("QR")),"Interface sem opção ou solicitação de QR");
        }
        private Button Button(string label) => FindObjectsByType<Button>(FindObjectsSortMode.None).First(b =>
            (b.transform.IsChildOf(guide.transform) || b.transform.root.name=="Controles_da_visita_VR" ||
             b.GetComponentInParent<Canvas>().name=="Controles_da_visita_VR") && b.GetComponentInChildren<Text>().text == label);
        private void PointAt(Transform target)
        {
            Canvas.ForceUpdateCanvases();
            eye.transform.LookAt(target.position);
        }
        private static string WithoutWhitespace(string value) => new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        private void Capture(string filename)
        {
            var target = new RenderTexture(1440, 900, 24);
            eye.targetTexture = target; Canvas.ForceUpdateCanvases(); eye.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); texture.Apply();
            string folder = Path.GetFullPath(Environment.GetEnvironmentVariable("MUSEUM_CAPTURE_OUTPUT") ??
                Path.Combine(Application.dataPath, "../../artifacts/bugfix-2026-09-21"));
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
