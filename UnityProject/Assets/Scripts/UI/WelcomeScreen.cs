using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuseumModerna
{
    /// <summary>Escolha inicial entre visita no celular e visita com visor VR.</summary>
    public sealed class WelcomeScreen : MonoBehaviour
    {
        private const string ImageResource = "UI/homem_amarelo";
        private const string Credit = "O homem amarelo · Anita Malfatti\n1915–1916";
        public const string Authorship = "Feito por: Bruno Proença Silva | Rafael Fernandes Domingues";
        private GameObject root;
        private RectTransform rootRect, safeArea, composition, artPanel, photo, content;
        private Text eyebrow, title, hint, credit, authors;
        private PlayerController player;
        private UIManager ui;
        private Sprite artworkSprite, roundedSprite;
        private Texture2D roundedTexture;
        private Rect lastSafe;
        private Vector2 lastSize, lastCanvasSize;

        public GameObject Root => root;
        public Button NormalButton { get; private set; }
        public Button VrButton { get; private set; }
        public bool IsVisible => root != null && root.activeInHierarchy;

        public void Build(UIManager managerUI)
        {
            if (root != null) return;
            ui = managerUI;
            player = FindAnyObjectByType<PlayerController>();
            roundedSprite = CreateRoundedSprite();

            rootRect = Rect("TelaDeAbertura", transform);
            root = rootRect.gameObject;
            Stretch(rootRect);
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<Image>().color = MuseumGuideTheme.WelcomeBackground;

            safeArea = Rect("AreaSegura", root.transform);
            Stretch(safeArea);
            composition = Rect("EscolhaDoModo", safeArea);
            composition.anchorMin = composition.anchorMax = composition.pivot = Vector2.one * .5f;
            artPanel = Rect("Acervo", composition);
            var artBackground = artPanel.gameObject.AddComponent<Image>();
            artBackground.color = MuseumGuideTheme.Control;
            artBackground.sprite = roundedSprite;
            artBackground.type = Image.Type.Sliced;
            artBackground.raycastTarget = false;

            var texture = Resources.Load<Texture2D>(ImageResource);
            if (texture != null)
            {
                artworkSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f);
                photo = Rect("O_Homem_Amarelo", artPanel);
                var image = photo.gameObject.AddComponent<Image>();
                image.sprite = artworkSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            credit = Label(artPanel, "CreditoDaObra", Credit, MuseumGuideTheme.WelcomeCaption);
            credit.alignment = TextAnchor.MiddleCenter;

            content = Rect("ModosDeVisita", composition);
            eyebrow = Label(content, "Identidade", "MUSEU / 1922", MuseumGuideTheme.WelcomeCaption);
            eyebrow.color = MuseumGuideTheme.Accent;
            title = Label(content, "Titulo", "Semana de\nArte Moderna", MuseumGuideTheme.WelcomeTitle);
            title.fontStyle = FontStyle.Bold;
            hint = Label(content, "Escolha", "Escolha como visitar", MuseumGuideTheme.Body);
            NormalButton = ModeButton("Modo_normal", "Modo normal", "Visite pelo celular, sem óculos", true, EnterNormal);
            VrButton = ModeButton("Modo_VR", "Modo VR", "Prepare o VR Box e entre no museu", false, PrepareVr);
            authors = Label(content, "CreditosDeAutoria", Authorship, MuseumGuideTheme.WelcomeCaption);
#if !UNITY_ANDROID && !UNITY_EDITOR
            VrButton.interactable = false;
            VrButton.transform.Find("Descricao").GetComponent<Text>().text = "Disponível no aplicativo Android";
#endif
            var normalNavigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = VrButton };
            var vrNavigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = NormalButton };
            NormalButton.navigation = normalNavigation;
            VrButton.navigation = vrNavigation;
            Show();
        }

        public void Show()
        {
            if (root == null) return;
            UIManager.EnsureEventSystem();
            ui.GetComponent<Canvas>().enabled = true;
            ui.Guide?.CloseSettings();
            rootRect.SetAsLastSibling();
            root.SetActive(true);
            NormalButton.interactable = player != null;
            VrButton.interactable = player != null && MobileVrMode.Instance != null;
#if UNITY_ANDROID && !UNITY_EDITOR
            VrButton.interactable &= SystemInfo.supportsGyroscope;
            VrButton.transform.Find("Descricao").GetComponent<Text>().text = VrButton.interactable
                ? "Prepare o VR Box e entre no museu" : "VR precisa de um celular com giroscópio";
#elif !UNITY_EDITOR
            VrButton.interactable = false;
#endif
            hint.text = player != null ? "Escolha como visitar" : "Aguarde o museu carregar";
            player?.SetPaused(true);
            ui.Guide?.Hide();
            UpdateLayout(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(NormalButton.gameObject);
        }

        public void Hide()
        {
            if (root == null) return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(root.transform))
                EventSystem.current.SetSelectedGameObject(null);
            root.SetActive(false);
        }

        private void EnterNormal()
        {
            if (!IsVisible || !NormalButton.interactable) return;
            Hide();
            if (MobileVrMode.Instance != null) MobileVrMode.Instance.EnterNormalMode();
            else player?.SetPaused(false);
        }

        private void PrepareVr()
        {
            if (!IsVisible || !VrButton.interactable || MobileVrMode.Instance == null) return;
            Hide();
#if UNITY_EDITOR
            MobileVrMode.Instance.EnterEditorPreview();
#else
            MobileVrMode.Instance.ShowPreparation(true);
#endif
        }

        private void Update()
        {
            if (IsVisible) UpdateLayout();
        }

        private void UpdateLayout(bool force = false)
        {
            var size = new Vector2(Screen.width, Screen.height);
            var canvasSize = rootRect.rect.size;
            if (!force && lastSafe == Screen.safeArea && lastSize == size && lastCanvasSize == canvasSize) return;
            if (size.x <= 0 || size.y <= 0) return;
            lastSafe = Screen.safeArea;
            lastSize = size;
            lastCanvasSize = canvasSize;
            safeArea.anchorMin = new Vector2(lastSafe.xMin / size.x, lastSafe.yMin / size.y);
            safeArea.anchorMax = new Vector2(lastSafe.xMax / size.x, lastSafe.yMax / size.y);
            Canvas.ForceUpdateCanvases();

            float padding = MuseumGuideTheme.WelcomePadding;
            float width = Mathf.Min(MuseumGuideTheme.WelcomeMaxWidth, safeArea.rect.width - padding * 2);
            float height = safeArea.rect.height - padding * 2;
            bool landscape = width > height * MuseumGuideTheme.WelcomeLandscapeAspect;
            bool compact = height < MuseumGuideTheme.WelcomeCompactHeight;
            float headingHeight = compact ? MuseumGuideTheme.WelcomeCompactHeadingHeight : MuseumGuideTheme.WelcomeHeadingHeight;
            float buttonHeight = compact ? MuseumGuideTheme.WelcomeCompactModeHeight : MuseumGuideTheme.WelcomeModeHeight;
            float contentHeight = MuseumGuideTheme.WelcomeEyebrowHeight + headingHeight +
                MuseumGuideTheme.WelcomeHintHeight + MuseumGuideTheme.Gap * 3 + buttonHeight * 2 + MuseumGuideTheme.WelcomeCreditHeight;
            composition.sizeDelta = new Vector2(width, height);

            if (landscape)
            {
                float artWidth = width * MuseumGuideTheme.WelcomeArtWidthFraction;
                Place(artPanel, 0, 0, artWidth, height);
                float contentX = artWidth + MuseumGuideTheme.WelcomePanelGap;
                Place(content, contentX, Mathf.Max(0, (height - contentHeight) * .5f), width - contentX, contentHeight);
            }
            else
            {
                float artHeight = Mathf.Min(MuseumGuideTheme.WelcomePortraitArtMaxHeight,
                    Mathf.Min(height * MuseumGuideTheme.WelcomeArtHeightFraction,
                        height - contentHeight - MuseumGuideTheme.WelcomePanelGap));
                bool showArt = artHeight >= MuseumGuideTheme.WelcomeMinArtHeight;
                artPanel.gameObject.SetActive(showArt);
                if (showArt) Place(artPanel, 0, 0, width, artHeight);
                float contentTop = showArt ? artHeight + MuseumGuideTheme.WelcomePanelGap : Mathf.Max(0, (height - contentHeight) * .5f);
                Place(content, 0, contentTop, width, contentHeight);
            }
            if (landscape) artPanel.gameObject.SetActive(true);

            title.fontSize = compact ? MuseumGuideTheme.WelcomeCompactTitle : MuseumGuideTheme.WelcomeTitle;
            float y = 0;
            Place(eyebrow.rectTransform, 0, y, content.rect.width, MuseumGuideTheme.WelcomeEyebrowHeight);
            y += MuseumGuideTheme.WelcomeEyebrowHeight;
            Place(title.rectTransform, 0, y, content.rect.width, headingHeight);
            y += headingHeight;
            Place(hint.rectTransform, 0, y, content.rect.width, MuseumGuideTheme.WelcomeHintHeight);
            y += MuseumGuideTheme.WelcomeHintHeight + MuseumGuideTheme.Gap;
            Place((RectTransform)NormalButton.transform, 0, y, content.rect.width, buttonHeight);
            y += buttonHeight + MuseumGuideTheme.Gap;
            Place((RectTransform)VrButton.transform, 0, y, content.rect.width, buttonHeight);
            y += buttonHeight + MuseumGuideTheme.Gap;
            Place(authors.rectTransform, 0, y, content.rect.width, MuseumGuideTheme.WelcomeCreditHeight);

            if (photo != null)
                Place(photo, MuseumGuideTheme.Gap, MuseumGuideTheme.Gap,
                    artPanel.rect.width - MuseumGuideTheme.Gap * 2,
                    artPanel.rect.height - MuseumGuideTheme.WelcomeCreditHeight - MuseumGuideTheme.Gap * 2);
            Place(credit.rectTransform, MuseumGuideTheme.Gap,
                artPanel.rect.height - MuseumGuideTheme.WelcomeCreditHeight - MuseumGuideTheme.Gap,
                artPanel.rect.width - MuseumGuideTheme.Gap * 2, MuseumGuideTheme.WelcomeCreditHeight);
        }

        private Button ModeButton(string objectName, string label, string description, bool primary,
            UnityEngine.Events.UnityAction onClick)
        {
            var rect = Rect(objectName, content);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            if (!primary)
            {
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = MuseumGuideTheme.Accent;
                outline.effectDistance = Vector2.one * MuseumGuideTheme.WelcomeOutlineWidth;
            }
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? MuseumGuideTheme.Accent : MuseumGuideTheme.Control;
            colors.highlightedColor = colors.selectedColor = primary ? MuseumGuideTheme.ActionHover : MuseumGuideTheme.Focus;
            colors.pressedColor = primary ? MuseumGuideTheme.ActionPressed : MuseumGuideTheme.ActiveControl;
            colors.disabledColor = MuseumGuideTheme.DisabledControl;
            // Sem animação: estados imediatos também respeitam a preferência por reduzir movimento.
            colors.fadeDuration = 0;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var labelText = Label(rect, "NomeDoModo", label, MuseumGuideTheme.WelcomeModeTitle);
            labelText.fontStyle = FontStyle.Bold;
            var descriptionText = Label(rect, "Descricao", description, MuseumGuideTheme.WelcomeCaption);
            labelText.color = descriptionText.color = primary ? MuseumGuideTheme.OnAccent : MuseumGuideTheme.Text;
            labelText.rectTransform.anchorMin = new Vector2(0, .47f);
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = new Vector2(MuseumGuideTheme.Padding, 0);
            labelText.rectTransform.offsetMax = new Vector2(-MuseumGuideTheme.Padding, -MuseumGuideTheme.Gap);
            descriptionText.rectTransform.anchorMin = Vector2.zero;
            descriptionText.rectTransform.anchorMax = new Vector2(1, .47f);
            descriptionText.rectTransform.offsetMin = new Vector2(MuseumGuideTheme.Padding, MuseumGuideTheme.Gap);
            descriptionText.rectTransform.offsetMax = new Vector2(-MuseumGuideTheme.Padding, 0);
            return button;
        }

        private static Text Label(Transform parent, string objectName, string value, int fontSize)
        {
            var text = Rect(objectName, parent).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(MuseumGuideTheme.FontResource);
            text.fontSize = fontSize;
            text.text = value;
            text.color = MuseumGuideTheme.Text;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform Rect(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private Sprite CreateRoundedSprite()
        {
            int size = MuseumGuideTheme.RoundedTextureSize;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(MuseumGuideTheme.Radius - x, x - (size - 1 - MuseumGuideTheme.Radius));
                float dy = Mathf.Max(MuseumGuideTheme.Radius - y, y - (size - 1 - MuseumGuideTheme.Radius));
                float distance = new Vector2(Mathf.Max(0, dx), Mathf.Max(0, dy)).magnitude;
                var pixel = Color.white;
                pixel.a = Mathf.Clamp01(MuseumGuideTheme.Radius - distance);
                roundedTexture.SetPixel(x, y, pixel);
            }
            roundedTexture.Apply();
            return Sprite.Create(roundedTexture, new Rect(0, 0, size, size), Vector2.one * .5f,
                MuseumGuideTheme.SpritePixelsPerUnit, 0, SpriteMeshType.FullRect,
                Vector4.one * MuseumGuideTheme.RoundedSpriteBorder);
        }

        private void OnDestroy()
        {
            if (artworkSprite != null) Destroy(artworkSprite);
            if (roundedSprite != null) Destroy(roundedSprite);
            if (roundedTexture != null) Destroy(roundedTexture);
        }
    }
}
