using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MuseumModerna
{
    /// <summary>Ficha única no canto, área segura, rolagem e preferências persistentes.</summary>
    public sealed class MuseumGuidePanel : MonoBehaviour
    {
        private const string Pref = "MuseumGuide_";
        private RectTransform safe, panel, settings, toggle, backdrop;
        private CanvasGroup group;
        private Image background;
        private Text title, author, technique, notice, description, relation, source;
        private Text pinLabel, enabledLabel, sizeLabel, summaryLabel, contrastLabel, lingerLabel, motionLabel, speedLabel, controlLabel, tapWalkLabel;
        private ScrollRect scroll;
        private Font font;
        private Sprite rounded;
        private Texture2D roundedTexture;
        private PaintingInfo current;
        private GazeDwellInteraction gaze;
        private bool visible, pinned, summary, contrast, linger, reducedMotion, panelsEnabled;
        private int textStep;
        private bool settingsPausedVisitor;
        private Rect lastSafe;
        private Vector2 lastSize;
        public bool IsVisible => visible;
        public bool SettingsOpen => settings != null && settings.gameObject.activeSelf;
        public bool PanelsEnabled => panelsEnabled;
        public bool Summary => summary;
        public bool HighContrast => contrast;
        public bool LongerReading => linger;
        public bool ReducedMotion => reducedMotion;
        public int TextStep => textStep;
        public Sprite RoundedSurface => rounded;
        public void CycleTextSize() { textStep = (textStep + 1) % 3; ApplyPreferences(); }
        public void ToggleSummary() { summary = !summary; ApplyPreferences(); }
        public void ToggleContrast() { contrast = !contrast; ApplyPreferences(); }
        public void TogglePanels() { panelsEnabled = !panelsEnabled; ApplyPreferences(); }
        public void ToggleLinger() { linger = !linger; ApplyPreferences(); }
        public void ToggleMotion() { reducedMotion = !reducedMotion; ApplyPreferences(); }
        public CanvasGroup Group => group;
        public RectTransform Panel => panel;
        public void Build(Transform parent)
        {
            font = Resources.GetBuiltinResource<Font>(MuseumGuideTheme.FontResource);
            gaze = FindAnyObjectByType<GazeDwellInteraction>();
            panelsEnabled = PlayerPrefs.GetInt(Pref + "enabled", 1) != 0;
            summary = PlayerPrefs.GetInt(Pref + "summary", 0) != 0;
            contrast = PlayerPrefs.GetInt(Pref + "contrast", 0) != 0;
            linger = PlayerPrefs.GetInt(Pref + "linger", 0) != 0;
            reducedMotion = PlayerPrefs.GetInt(Pref + "motion", 0) != 0;
            textStep = Mathf.Clamp(PlayerPrefs.GetInt(Pref + "size", 0), 0, 2);
            rounded = CreateRoundedSprite();
            safe = Rect("GuideSafeArea", parent);
            Stretch(safe);
            panel = Rect("FichaDaObra", safe);
            background = Surface(panel);
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = MuseumGuideTheme.Accent;
            border.effectDistance = new Vector2(1, -1);
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            var layout = Column(panel, MuseumGuideTheme.Padding);
            layout.childForceExpandHeight = false;
            var eyebrow = Label(panel, "ACERVO / MEDIAÇÃO DIGITAL", MuseumGuideTheme.Label);
            eyebrow.color = MuseumGuideTheme.Accent;

            var viewport = Rect("LeituraComRolagem", panel);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpLayout = viewport.gameObject.AddComponent<LayoutElement>();
            vpLayout.flexibleHeight = 1;
            vpLayout.minHeight = 40;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            var content = Rect("Conteudo", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            Column(content, 2);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            title = Label(content, "", MuseumGuideTheme.Title, FontStyle.Bold);
            author = Label(content, "", MuseumGuideTheme.Body, FontStyle.Bold);
            technique = Label(content, "", MuseumGuideTheme.Label);
            notice = Label(content, "", MuseumGuideTheme.Label);
            notice.color = MuseumGuideTheme.Accent;
            description = Label(content, "", MuseumGuideTheme.Body);
            relation = Label(content, "", MuseumGuideTheme.Body);
            source = Label(content, "", MuseumGuideTheme.Label);
            var readHint = Label(panel, "Role para ler • F fixa/libera • Esc fecha", MuseumGuideTheme.Label);
            readHint.gameObject.name = "InstrucoesDaFicha";
            var actions = Rect("Acoes", panel);
            var actionSize = actions.gameObject.AddComponent<LayoutElement>();
            actionSize.minHeight = actionSize.preferredHeight = MuseumGuideTheme.ButtonHeight;
            actionSize.flexibleHeight = 0;
            var row = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = MuseumGuideTheme.Gap; row.childForceExpandWidth = true; row.childForceExpandHeight = false;
            pinLabel = Button(actions, "Fixar", () => gaze?.TogglePin());
            Button(actions, "Fechar", () => gaze?.Close());

            toggle = Rect("OpcoesDaVisita", safe);
            toggle.anchorMin = toggle.anchorMax = toggle.pivot = new Vector2(0, 1);
            toggle.anchoredPosition = new Vector2(MuseumGuideTheme.Padding, -MuseumGuideTheme.Padding);
            toggle.sizeDelta = new Vector2(210, MuseumGuideTheme.ButtonHeight);
            var trigger = Button(toggle, "Opções da visita", ToggleSettings);
            Stretch((RectTransform)trigger.transform.parent);
            MuseumWalkButton.Build(safe,rounded);

            // Camada que captura o toque fora do painel de opções para fechá-lo.
            backdrop = Rect("VeuDeFundo", safe);
            Stretch(backdrop);
            var veil = backdrop.gameObject.AddComponent<Image>();
            veil.color = MuseumGuideTheme.Scrim;
            var backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = veil;
            backdropButton.onClick.AddListener(ToggleSettings);
            backdrop.gameObject.SetActive(false);

            settings = Rect("PreferenciasDeLeitura", safe);
            Surface(settings);
            // ScrollRect permite acessar todas as opções mesmo em telas baixas.
            settings.gameObject.AddComponent<RectMask2D>();
            var optionsScroll = settings.gameObject.AddComponent<ScrollRect>();
            optionsScroll.horizontal = false; optionsScroll.movementType = ScrollRect.MovementType.Clamped;
            optionsScroll.viewport = settings;
            var options = Rect("Opcoes", settings);
            options.anchorMin = new Vector2(0, 1); options.anchorMax = Vector2.one;
            options.pivot = new Vector2(.5f, 1); options.sizeDelta = Vector2.zero;
            Column(options, MuseumGuideTheme.Padding);
            options.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            optionsScroll.content = options;
            enabledLabel = Button(options, "", () => { panelsEnabled = !panelsEnabled; ApplyPreferences(); });
            sizeLabel = Button(options, "", () => { textStep = (textStep + 1) % 3; ApplyPreferences(); });
            summaryLabel = Button(options, "", () => { summary = !summary; ApplyPreferences(); });
            contrastLabel = Button(options, "", () => { contrast = !contrast; ApplyPreferences(); });
            lingerLabel = Button(options, "", () => { linger = !linger; ApplyPreferences(); });
            motionLabel = Button(options, "", () => { reducedMotion = !reducedMotion; ApplyPreferences(); });
            var visitor = FindAnyObjectByType<PlayerController>();
            speedLabel=Button(options,"Velocidade: "+HeadGazeMovement.SpeedLabel,()=>
            {
                HeadGazeMovement.CycleSpeed();
                speedLabel.text="Velocidade: "+HeadGazeMovement.SpeedLabel;
            });
            Button(options, "Calibrar câmera", () => visitor?.Calibrate());
            controlLabel=Button(options,"",()=>{});
            controlLabel.text="Caminhada: "+(HeadGazeMovement.TiltWalking?"inclinação":"botão");
            controlLabel.GetComponentInParent<Button>().onClick.AddListener(()=>
            {
                HeadGazeMovement.SetTiltWalking(!HeadGazeMovement.TiltWalking);
                controlLabel.text="Caminhada: "+(HeadGazeMovement.TiltWalking?"inclinação":"botão");
            });
            tapWalkLabel=Button(options,"",()=>
            {
                HeadGazeMovement.SetTapToWalk(!HeadGazeMovement.TapToWalk);
                RefreshWalkingLabels();
            });
            Button(options, "Voltar ao menu", () => MobileVrMode.Instance?.ShowMainMenu());
#if UNITY_ANDROID && !UNITY_EDITOR
            Button(options, "Preparar visor VR", () => MobileVrMode.Instance?.ShowPreparation());
#endif
            Button(options, "Fechar opções", ToggleSettings);
            settings.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            ApplyPreferences(false);
            UpdateSafeArea();
        }
        private void ToggleSettings()
        {
            bool open = !settings.gameObject.activeSelf;
            var visitor=FindAnyObjectByType<PlayerController>();
            if(open)
            {
                settingsPausedVisitor=visitor!=null&&visitor.State!=PlayerState.Paused;
                if(settingsPausedVisitor)visitor.SetPaused(true);
            }
            else if(settingsPausedVisitor)
            {
                settingsPausedVisitor=false;
                visitor?.SetPaused(false);
            }
            if(open) speedLabel.text="Velocidade: "+HeadGazeMovement.SpeedLabel;
            if(open) RefreshWalkingLabels();
            settings.gameObject.SetActive(open);
            backdrop.gameObject.SetActive(open);
            if (open && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(enabledLabel.transform.parent.gameObject);
            else if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
        /// <summary>Abre o painel de opções (ex.: chamado pela tela de abertura). Não faz nada se já estiver aberto.</summary>
        public void OpenSettings()
        {
            if (!settings.gameObject.activeSelf) ToggleSettings();
        }
        public void CloseSettings()
        {
            if (settings != null && settings.gameObject.activeSelf) ToggleSettings();
        }
        public void Show(PaintingInfo info)
        {
            if (info == null || !panelsEnabled) return;
            current = info; visible = true;
            panel.gameObject.SetActive(true);
            RefreshText();
            scroll.verticalNormalizedPosition = 1;
            group.interactable = true; group.blocksRaycasts = true;
        }
        public void Hide()
        {
            visible = false;
            if (group == null) return;
            group.interactable = false; group.blocksRaycasts = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel))
                EventSystem.current.SetSelectedGameObject(null);
        }
        public void SetPinned(bool value)
        {
            pinned = value;
            if (pinLabel != null) pinLabel.text = pinned ? "Liberar (F)" : "Fixar (F)";
        }
        private void RefreshText()
        {
            if (current == null) return;
            title.text = current.title;
            author.text = current.artist + " — " + current.Periodo;
            technique.text = current.tecnica;
            technique.gameObject.SetActive(!string.IsNullOrWhiteSpace(current.tecnica));
            notice.text = current.obra_historica_ou_reinterpretacao;
            notice.gameObject.SetActive(!string.IsNullOrWhiteSpace(notice.text));
            description.text = summary && !string.IsNullOrWhiteSpace(current.resumo) ? current.resumo : current.description;
            relation.text = "RELAÇÃO COM 1922\n" + current.relacao_com_a_semana_de_1922;
            source.text = "FONTES DA FICHA\n" + current.fontes;
            source.gameObject.SetActive(!summary && !string.IsNullOrWhiteSpace(current.fontes));
        }
        private void ApplyPreferences(bool save = true)
        {
            enabledLabel.text = "Painéis: " + (panelsEnabled ? "ativados" : "desativados");
            sizeLabel.text = "Texto: " + new[] { "padrão", "grande", "maior" }[textStep];
            summaryLabel.text = "Versão resumida: " + (summary ? "sim" : "não");
            contrastLabel.text = "Alto contraste: " + (contrast ? "sim" : "não");
            lingerLabel.text = "Tempo após olhar: " + (linger ? "5 segundos" : "breve");
            motionLabel.text = "Reduzir animação: " + (reducedMotion ? "sim" : "não");
            // Estado ligado/desligado também aparece na cor de fundo do botão, não só no texto.
            SetToggleVisual(enabledLabel, panelsEnabled);
            SetToggleVisual(sizeLabel, textStep > 0);
            SetToggleVisual(summaryLabel, summary);
            SetToggleVisual(contrastLabel, contrast);
            SetToggleVisual(lingerLabel, linger);
            SetToggleVisual(motionLabel, reducedMotion);
            RefreshWalkingLabels();
            background.color = contrast ? Color.black : MuseumGuideTheme.Surface;
            foreach (var label in new[] { title, author, technique, notice, description, relation, source })
            {
                int size = label == title ? MuseumGuideTheme.Title :
                    label == technique || label == notice || label == source ? MuseumGuideTheme.Label : MuseumGuideTheme.Body;
                label.fontSize = size + textStep * 3;
                label.color = contrast ? Color.white : label == notice ? MuseumGuideTheme.Accent : MuseumGuideTheme.Text;
            }
            if (gaze != null)
            {
                gaze.LongerReading = linger;
                gaze.SetPanelsEnabled(panelsEnabled);
            }
            if (!panelsEnabled) Hide();
            RefreshText();
            foreach (var button in safe.GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                colors.fadeDuration = reducedMotion ? 0 : MuseumGuideTheme.Fade;
                button.colors = colors;
            }
            if (!save) return;
            PlayerPrefs.SetInt(Pref + "enabled", panelsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "size", textStep);
            PlayerPrefs.SetInt(Pref + "summary", summary ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "contrast", contrast ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "linger", linger ? 1 : 0);
            PlayerPrefs.SetInt(Pref + "motion", reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
        }
        private void RefreshWalkingLabels()
        {
            if(controlLabel!=null) controlLabel.text="Caminhada: "+(HeadGazeMovement.TiltWalking?"inclinação":"botão");
            if(tapWalkLabel!=null)
            {
                tapWalkLabel.text="Botão: "+(HeadGazeMovement.TapToWalk?"toque para iniciar/parar":"segurar para caminhar");
                SetToggleVisual(tapWalkLabel,HeadGazeMovement.TapToWalk);
            }
        }
        private void Update()
        {
            if (group == null) return;
            UpdateSafeArea();
            group.alpha = reducedMotion ? (visible ? 1 : 0) :
                Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime / MuseumGuideTheme.Fade);
            if (!visible && group.alpha == 0 && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
            if (visible && Input.GetKey(KeyCode.PageDown)) scroll.verticalNormalizedPosition -= Time.unscaledDeltaTime;
            if (visible && Input.GetKey(KeyCode.PageUp)) scroll.verticalNormalizedPosition += Time.unscaledDeltaTime;
        }
        private void UpdateSafeArea()
        {
            var size = new Vector2(Screen.width, Screen.height);
            if (lastSafe == Screen.safeArea && lastSize == size) return;
            lastSafe = Screen.safeArea; lastSize = size;
            safe.anchorMin = new Vector2(lastSafe.xMin / size.x, lastSafe.yMin / size.y);
            safe.anchorMax = new Vector2(lastSafe.xMax / size.x, lastSafe.yMax / size.y);
            ConfigureScale(size.x, size.y);
            Canvas.ForceUpdateCanvases();
            float width = safe.rect.width, height = safe.rect.height;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);
            panel.anchoredPosition = new Vector2(-MuseumGuideTheme.Padding, MuseumGuideTheme.Padding);
            panel.sizeDelta = new Vector2(Mathf.Min(MuseumGuideTheme.Width, width > height ? width * .42f : width - 40), height * (width > height ? .70f : .46f));

            // Nunca deixa o botão de opções maior que a área segura (telas muito estreitas).
            toggle.sizeDelta = new Vector2(Mathf.Min(210, width - MuseumGuideTheme.Padding * 2), MuseumGuideTheme.ButtonHeight);

            // Painel de opções: nunca sobrepõe o botão que o abre, largura acompanha a tela
            // (com mínimo/máximo legíveis) e altura é limitada pelo espaço restante abaixo do botão.
            float belowToggle = MuseumGuideTheme.Padding + toggle.sizeDelta.y + MuseumGuideTheme.Gap;
            settings.anchorMin = settings.anchorMax = settings.pivot = new Vector2(0, 1);
            settings.anchoredPosition = new Vector2(MuseumGuideTheme.Padding, -belowToggle);
            float settingsWidth = Mathf.Clamp(width * .88f, MuseumGuideTheme.SettingsMinWidth,
                Mathf.Min(MuseumGuideTheme.SettingsMaxWidth, width - MuseumGuideTheme.Padding * 2));
            float settingsHeight = Mathf.Min(MuseumGuideTheme.SettingsMaxHeight, height - belowToggle - MuseumGuideTheme.Padding);
            settings.sizeDelta = new Vector2(settingsWidth, Mathf.Max(160, settingsHeight));
        }
        public void ConfigureScale(float width, float height)
        {
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.referenceResolution = height > width ? new Vector2(390, 844) :
                height < 600 ? new Vector2(844, 390) : new Vector2(1280, 800);
        }
        private RectTransform Rect(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        /// <summary>Realça o fundo do botão de opção quando ela está ativa (feedback visual além do texto).</summary>
        private static void SetToggleVisual(Text label, bool active)
        {
            var image = label.transform.parent.GetComponent<Image>();
            if (image != null) image.color = active ? MuseumGuideTheme.ActiveControl : MuseumGuideTheme.Control;
        }
        private Image Surface(RectTransform rt)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = rounded; image.type = Image.Type.Sliced;
            image.color = MuseumGuideTheme.Surface;
            return image;
        }
        private static VerticalLayoutGroup Column(RectTransform rt, int padding)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = MuseumGuideTheme.Gap;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return layout;
        }
        private Text Label(Transform parent, string value, int size, FontStyle style = FontStyle.Normal)
        {
            var label = Rect("Texto", parent).gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = size; label.fontStyle = style;
            label.color = MuseumGuideTheme.Text; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.16f; label.raycastTarget = false;
            return label;
        }
        private Text Button(Transform parent, string value, UnityEngine.Events.UnityAction action)
        {
            var rt = Rect("Botao_" + value, parent);
            var image = Surface(rt); image.color = MuseumGuideTheme.Control;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = colors.selectedColor = new Color(1.7f, 1.7f, 1.7f);
            colors.pressedColor = MuseumGuideTheme.Accent;
            colors.fadeDuration = MuseumGuideTheme.Fade;
            button.colors = colors;
            button.onClick.AddListener(action);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = MuseumGuideTheme.ButtonHeight;
            le.flexibleWidth = 1; le.flexibleHeight = 0;
            var text = Label(rt, value, MuseumGuideTheme.Label);
            text.alignment = TextAnchor.MiddleCenter; Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8, 2); text.rectTransform.offsetMax = new Vector2(-8, -2);
            return text;
        }
        private Sprite CreateRoundedSprite()
        {
            const int size = 32;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(MuseumGuideTheme.Radius - x, x - (size - 1 - MuseumGuideTheme.Radius));
                float dy = Mathf.Max(MuseumGuideTheme.Radius - y, y - (size - 1 - MuseumGuideTheme.Radius));
                float distance = new Vector2(Mathf.Max(0, dx), Mathf.Max(0, dy)).magnitude;
                roundedTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(MuseumGuideTheme.Radius - distance)));
            }
            roundedTexture.Apply();
            return Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        }
        private void OnDestroy()
        {
            if (rounded != null) Destroy(rounded);
            if (roundedTexture != null) Destroy(roundedTexture);
        }
    }
}
