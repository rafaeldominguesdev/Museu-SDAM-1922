using System.Collections;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace MuseumModerna
{
    /// <summary>Coordena menu, visita normal e Cardboard sem reaproveitar movimento entre modos.</summary>
    public sealed class MobileVrMode : MonoBehaviour
    {
        public static MobileVrMode Instance { get; private set; }
        public bool IsActive { get; private set; }
        public bool HasHeadTracking => IsActive && driver != null && driver.enabled;
        public bool IsPreparationVisible => preparation != null && preparation.activeSelf;
        public VrWorldGuide WorldGuide { get; private set; }
        private UIManager ui;
        private Camera eye;
        private PlayerController player;
        private Canvas desktopCanvas;
        private GameObject preparation;
        private RectTransform preparationSafe;
        private Text status;
        private Button enterButton, scanButton;
        private TrackedPoseDriver driver;
        private XRManagerSettings manager;
        private bool prepared, preparing, suspended;
        private bool applicationPaused, applicationUnfocused;
        private Coroutine entry;
        private float normalNearClip;
        private float missingPoseTime;

        public void Initialize(UIManager managerUI)
        {
            Instance = this;
            ui = managerUI;
            player = FindAnyObjectByType<PlayerController>();
            eye = player.GetComponentInChildren<GazeDwellInteraction>().GetComponent<Camera>();
            normalNearClip = eye.nearClipPlane;
            eye.stereoTargetEye = StereoTargetEyeMask.None;
            desktopCanvas = ui.GetComponent<Canvas>();
            WorldGuide = new GameObject("FichaVR").AddComponent<VrWorldGuide>();
            WorldGuide.Build(eye, ui.Guide, this);
            player.SetPaused(true);
#if UNITY_ANDROID && !UNITY_EDITOR
            RestoreFullScreen();
#endif
            // O menu é a única tela inicial. Preparar XR somente depois da escolha do visitante.
        }

        private IEnumerator PrepareXR()
        {
            if (prepared || preparing) yield break;
            preparing = true;
            status.text = "Preparando o visor…";
            if (!SystemInfo.supportsGyroscope)
            {
                status.text = "Este celular não tem giroscópio disponível. Volte ao menu e escolha Modo normal.";
                preparing = false;
                yield break;
            }
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null)
            {
                status.text = "VR indisponível. Volte ao menu para usar o Modo normal.";
                preparing = false;
                yield break;
            }
            yield return manager.InitializeLoader();
            prepared = manager.activeLoader is Google.XR.Cardboard.XRLoader &&
                manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>() != null &&
                manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>() != null;
            preparing = false;
            if (!prepared)
            {
                status.text = "Não foi possível preparar o visor. Volte ao menu e tente novamente.";
                if (manager.isInitializationComplete) manager.DeinitializeLoader();
                yield break;
            }
            driver = eye.GetComponent<TrackedPoseDriver>() ?? eye.gameObject.AddComponent<TrackedPoseDriver>();
            driver.enabled = false;
            driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.UseRelativeTransform = false;
            Api.UpdateScreenParams();
            UpdatePreparation();
        }
        private void UpdatePreparation()
        {
            if (status == null) return;
            bool hasProfile = prepared && Api.HasDeviceParams();
            scanButton.interactable = prepared;
            // O perfil do Cardboard melhora a correção das lentes, mas não é
            // pré-requisito para iniciar o XR. Exigi-lo aqui fazia o visitante
            // depender de sair para o leitor de QR e voltar ao app.
            enterButton.interactable = prepared;
            status.text = hasProfile
                ? "Perfil salvo. Se a imagem estiver pequena ou desalinhada, leia o QR do seu VR Box."
                : "Você já pode entrar no VR. Ler o QR do visor é opcional e apenas ajusta as lentes.";
        }
        public void ScanViewer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (prepared && !IsActive) Api.ScanDeviceParams();
#endif
        }
        public void EnterVR()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsActive || !prepared || suspended || manager == null || manager.activeLoader == null) return;
            player.SetPaused(true);
            RestoreFullScreen();
            eye.rect = new Rect(0, 0, 1, 1);
            eye.ResetAspect(); eye.ResetProjectionMatrix();
            // HasDeviceParams atualiza o contador; HasNewDeviceParams aqui perderia o novo QR.
            if (Api.HasDeviceParams()) Api.ReloadDeviceParams();
            Api.UpdateScreenParams();
            manager.StartSubsystems();
            var display = manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            var input = manager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>();
            if (display == null || !display.running || input == null || !input.running)
            {
                manager.StopSubsystems();
                status.text = "O visor não iniciou. Tente novamente ou volte ao Modo normal.";
                return;
            }
            driver.enabled = true;
            eye.stereoTargetEye = StereoTargetEyeMask.Both;
            eye.nearClipPlane = .05f;
            Api.Recenter();
            ActivateInterface(true);
#endif
        }
        private void ActivateInterface(bool waitForPose = false)
        {
            if (IsActive) return;
            player.SetPaused(true);
            player.GetComponentInChildren<GazeDwellInteraction>().Close();
            ui.Guide.CloseSettings();
            ui.GetComponent<WelcomeScreen>()?.Hide();
            IsActive = true;
            desktopCanvas.enabled = false;
            if (preparation != null) preparation.SetActive(false);
            WorldGuide.SetVrEnabled(true);
            if (waitForPose) BeginPoseRecovery();
            else CompleteEntry();
        }
        private void BeginPoseRecovery()
        {
            if (entry != null) StopCoroutine(entry);
            player.SetPaused(true);
            WorldGuide.ResetInteraction();
            missingPoseTime = 0f;
            entry = StartCoroutine(FinishEntry());
        }
        private IEnumerator FinishEntry()
        {
            int validFrames = 0;
            float deadline = Time.realtimeSinceStartup + 3f;
            // Não libera caminhada usando a orientação anterior do menu.
            while (IsActive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                if (suspended) { validFrames = 0; deadline = Time.realtimeSinceStartup + 3f; continue; }
                // Usa a mesma fonte do TrackedPoseDriver; uma feature de outro nó
                // pode existir sem que a câmera esteja recebendo uma pose utilizável.
                bool valid = HasUsableHeadPose();
#if UNITY_EDITOR
                valid = driver == null || valid; // Prévia mono sem sensor físico.
#endif
                validFrames = valid ? validFrames + 1 : 0;
                if (validFrames < 2) continue;
                entry = null;
                CompleteEntry();
                yield break;
            }
            entry = null;
            if (!IsActive) yield break;
            RestoreNormalRendering();
            ShowPreparation();
            status.text = "Não recebemos a orientação do visor. Tente novamente ou escolha Modo normal.";
        }
        private void CompleteEntry()
        {
            if (!IsActive || suspended) return;
            missingPoseTime = 0f;
            ResetWalkingGesture();
            player.SetPaused(false);
            // Entrar no visor deve começar a visita, não cobrir a imagem com o
            // menu central. O menu continua acessível olhando para cima.
            WorldGuide.CloseMenu();
        }
        public void ResetWalkingGesture()
        {
            player.GetComponent<HeadGazeMovement>()?.ResetMovement();
            player.GetComponentInChildren<GyroscopeController>()?.ResetWalkingGesture();
        }
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private bool HasUsableHeadPose()
        {
            if (driver == null || !driver.enabled) return false;
            var flags = PoseDataSource.GetDataFromSource(driver.poseSource, out var pose);
            var rotation = pose.rotation;
            return (flags & PoseDataFlags.Rotation) != 0 &&
                IsFinite(rotation.x) && IsFinite(rotation.y) && IsFinite(rotation.z) && IsFinite(rotation.w) &&
                rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w > .5f;
        }
#if UNITY_EDITOR
        public void EnterEditorPreview() => ActivateInterface();
#endif
        public void EnterNormalMode()
        {
            RestoreNormalRendering();
            if (preparation != null) preparation.SetActive(false);
            ui.GetComponent<WelcomeScreen>()?.Hide();
            ui.Guide.CloseSettings();
            player.SetPaused(false);
        }
        public void ExitVR() => ShowMainMenu();
        public void ShowMainMenu()
        {
            RestoreNormalRendering();
            if (preparation != null) preparation.SetActive(false);
            ui.Guide.CloseSettings();
            player.SetPaused(true);
            ui.GetComponent<WelcomeScreen>()?.Show();
        }
        private void RestoreNormalRendering()
        {
            if (entry != null) { StopCoroutine(entry); entry = null; }
            player.SetPaused(true);
            WorldGuide.SetVrEnabled(false);
            IsActive = false;
            missingPoseTime = 0f;
            // OnDisable do TrackedPoseDriver restaura a pose de Awake. Preserve
            // a altura e a direção antes de devolver a câmera ao giroscópio,
            // inclusive se a cena trouxe um driver antigo com tracking de posição.
            var finalPosition = eye.transform.localPosition;
            var finalRotation = eye.transform.localRotation;
            if (driver != null) driver.enabled = false;
            eye.transform.localPosition = finalPosition;
            eye.transform.localRotation = finalRotation;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (prepared) manager?.StopSubsystems();
#endif
            eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.nearClipPlane = normalNearClip;
            eye.rect = new Rect(0, 0, 1, 1);
            eye.ResetAspect(); eye.ResetProjectionMatrix();
            desktopCanvas.enabled = true;
            player.GetComponentInChildren<GyroscopeController>()?.ResumeFromCurrentPose();
            player.GetComponentInChildren<GazeDwellInteraction>().Close();
        }
        public void Recenter()
        {
            if (!IsActive || suspended) return;
            player.SetPaused(true);
#if UNITY_ANDROID && !UNITY_EDITOR
            Api.Recenter();
#endif
            BeginPoseRecovery();
        }
        private void Update()
        {
            if (IsPreparationVisible) UpdateSafeArea();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (prepared && !suspended)
            {
                Api.UpdateScreenParams();
                if (Api.HasNewDeviceParams())
                {
                    Api.ReloadDeviceParams();
                    if (!IsActive) UpdatePreparation();
                }
            }
            if (IsActive)
            {
                if (Api.IsCloseButtonPressed || Input.GetKeyDown(KeyCode.Escape)) ShowMainMenu();
                else if (Api.IsGearButtonPressed) { ShowPreparation(); ScanViewer(); }
                else if (Api.IsTriggerHeldPressed) Recenter();
                else if (!suspended && entry == null)
                {
                    missingPoseTime = HasUsableHeadPose() ? 0f : missingPoseTime + Time.unscaledDeltaTime;
                    if (missingPoseTime >= .15f) BeginPoseRecovery();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && ui.GetComponent<WelcomeScreen>()?.IsVisible != true)
                ShowMainMenu();
#endif
        }
        private static void RestoreFullScreen()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.fullScreen = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            UpdateApplicationState();
        }
        private void OnApplicationFocus(bool focused)
        {
            applicationUnfocused = !focused;
            UpdateApplicationState();
        }
        private void UpdateApplicationState()
        {
            bool wasSuspended = suspended;
            suspended = applicationPaused || applicationUnfocused;
            if (player == null || wasSuspended == suspended) return;
            WorldGuide?.ResetInteraction();
            if (suspended)
            {
                if (IsActive) player.SetPaused(true);
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            RestoreFullScreen();
            if (prepared)
            {
                Api.UpdateScreenParams();
                if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
                if (IsPreparationVisible) UpdatePreparation();
            }
#endif
            if (IsActive) Recenter();
        }
        private void CreatePreparation()
        {
            preparation = new GameObject("PrepararVisor", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
            preparation.transform.SetParent(ui.transform, false);
            var rect = (RectTransform)preparation.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            preparation.GetComponent<Image>().color = MuseumGuideTheme.Surface;
            var canvas = preparation.GetComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 60;
            var safe = new GameObject("AreaSeguraVR", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            safe.transform.SetParent(rect, false);
            preparationSafe = (RectTransform)safe.transform;
            safe.GetComponent<Image>().color = Color.clear;
            var box = new GameObject("Preparacao", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            box.transform.SetParent(safe.transform, false);
            var content = (RectTransform)box.transform;
            content.anchorMin = new Vector2(.08f, 1); content.anchorMax = new Vector2(.92f, 1);
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = box.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(MuseumGuideTheme.Padding, MuseumGuideTheme.Padding, MuseumGuideTheme.Padding, MuseumGuideTheme.Padding);
            layout.spacing = MuseumGuideTheme.Gap; layout.childForceExpandHeight = false;
            box.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = safe.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.viewport = preparationSafe; scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            VrWorldGuide.MakeText(content, "MODO VR / PREPARAR VISOR", MuseumGuideTheme.Title, 40);
            status = VrWorldGuide.MakeText(content, "Preparando o visor…", MuseumGuideTheme.Body, 54);
            enterButton = VrWorldGuide.MakeButton(content, "Entrar em VR", EnterVR);
            scanButton = VrWorldGuide.MakeButton(content, "Ajustar lentes · ler QR do visor", ScanViewer);
            enterButton.interactable = scanButton.interactable = false;
            VrWorldGuide.MakeText(content, "Encaixe o celular na horizontal. O QR é opcional: use-o apenas para calibrar as lentes. Olhe à frente; depois, abaixe a cabeça para andar e levante para parar.", MuseumGuideTheme.Label, 58);
            VrWorldGuide.MakeButton(content, "Voltar ao menu", ShowMainMenu);
            UpdateSafeArea();
        }
        private void UpdateSafeArea()
        {
            var safe = Screen.safeArea;
            preparationSafe.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            preparationSafe.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            preparationSafe.offsetMin = preparationSafe.offsetMax = Vector2.zero;
        }
        public void ShowPreparation()
        {
            if (IsActive) RestoreNormalRendering();
            ui.GetComponent<WelcomeScreen>()?.Hide();
            ui.Guide.CloseSettings();
            player.GetComponentInChildren<GazeDwellInteraction>().Close();
            player.SetPaused(true);
            if (preparation == null) CreatePreparation();
            preparation.SetActive(true);
#if UNITY_ANDROID && !UNITY_EDITOR
            RestoreFullScreen();
            if (prepared) UpdatePreparation();
            else if (!preparing) StartCoroutine(PrepareXR());
#else
            status.text = "As lentes exigem Android. No Editor, teste os controles pelo olhar.";
            enterButton.interactable = true;
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() => ActivateInterface());
#endif
        }
        public void SkipPreparation() => EnterNormalMode();
        private void OnDestroy()
        {
            if (driver != null) driver.enabled = false;
            if (WorldGuide != null) Destroy(WorldGuide.gameObject);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (manager != null && manager.isInitializationComplete)
            {
                manager.StopSubsystems(); manager.DeinitializeLoader();
            }
#endif
            if (Instance == this) Instance = null;
        }
    }
}
