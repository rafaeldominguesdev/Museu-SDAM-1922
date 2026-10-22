using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MuseumModerna
{
    /// <summary>Ficha binocular ancorada no mundo: a cabeça pode alcançar os controles pelo olhar.</summary>
    [DefaultExecutionOrder(75)]
    public sealed class VrWorldGuide : MonoBehaviour
    {
        public bool IsEnabled { get; private set; }
        public bool ConsumesGaze { get; private set; }
        public bool BlocksWalking => MenuOpen || (visible && HitRect(Board, new Ray(eye.transform.position, eye.transform.forward)));
        public bool MenuOpen { get; private set; }
        public RectTransform Board { get; private set; }
        public int PageIndex => page;
        public int PageCount => pages.Count;
        public float DwellProgress => activation.Progress;
        private Camera eye;
        private MuseumGuidePanel preferences;
        private MobileVrMode mode;
        private CanvasGroup group;
        private Image background, ring;
        private RectTransform reticle;
        private Text heading, title, body, pageLabel, hint;
        private Button previous, next;
        private readonly Button[] actions = new Button[6];
        private RectTransform actionGrid, toolbar;
        private Button menuControl;
        private Quaternion toolbarRotation=Quaternion.identity;
        private Button highlighted;
        private readonly List<string> pages = new List<string>();
        private readonly GazeActivation activation = new GazeActivation();
        private PaintingInfo current;
        private GazeDwellInteraction gaze;
        private Material overlayMaterial;
        private Texture2D ringTexture;
        private Sprite ringSprite;
        private bool visible;
        private int page, menuPage;
        private const float DwellSeconds = MuseumGuideTheme.VrDwell;
        private const float Distance = MuseumGuideTheme.VrDistance, Scale = MuseumGuideTheme.VrScale;

        public void Build(Camera camera, MuseumGuidePanel settings, MobileVrMode owner)
        {
            eye = camera; preferences = settings; mode = owner;
            gaze = eye.GetComponent<GazeDwellInteraction>();
            var shader = Resources.Load<Shader>("Shaders/VrGuideUI");
            if (shader == null) shader = Shader.Find("Museum/VR Guide UI");
            overlayMaterial = new Material(shader);
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = eye; canvas.sortingOrder = 100;
            Board = gameObject.GetComponent<RectTransform>();
            Board.sizeDelta = new Vector2(MuseumGuideTheme.VrWidth, MuseumGuideTheme.VrHeight); Board.localScale = Vector3.one * Scale;
            background = gameObject.AddComponent<Image>(); background.color = MuseumGuideTheme.Surface;
            background.sprite = preferences.RoundedSurface; background.type = Image.Type.Sliced;
            var border = gameObject.AddComponent<Outline>();
            border.effectColor = MuseumGuideTheme.Accent; border.effectDistance = new Vector2(1.3f, -1.3f);
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false;
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 22); layout.spacing = 8;
            layout.childForceExpandHeight = false;
            heading = MakeText(Board, "", 22, 30);
            heading.color = MuseumGuideTheme.Accent;
            title = MakeText(Board, "", MuseumGuideTheme.VrTitle, 94); title.fontStyle = FontStyle.Bold;
            body = MakeText(Board, "", MuseumGuideTheme.VrBody, 330);
            pageLabel = MakeText(Board, "", 22, 30);
            var pagesRow = Row(Board, "Paginas");
            previous = MakeButton(pagesRow, "Anterior", () => ChangePage(-1));
            next = MakeButton(pagesRow, "Próxima página", () => ChangePage(1));
            var grid=new GameObject("Acoes",typeof(RectTransform),typeof(GridLayoutGroup),typeof(LayoutElement));
            grid.transform.SetParent(Board,false);actionGrid=(RectTransform)grid.transform;
            var gridLayout=grid.GetComponent<GridLayoutGroup>();gridLayout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;gridLayout.constraintCount=2;
            gridLayout.cellSize=new Vector2((MuseumGuideTheme.VrWidth-52-MuseumGuideTheme.VrGap)*.5f,MuseumGuideTheme.VrButtonHeight);
            gridLayout.spacing=Vector2.one*MuseumGuideTheme.VrGap;
            for (int i = 0; i < actions.Length; i++)
            {
                int index = i;
                actions[i] = MakeButton(actionGrid, "", () => InvokeAction(index));
            }
            hint = MakeText(Board, "Olhe para um botão por 1 segundo para acionar.", 22, 32);
            foreach (var button in GetComponentsInChildren<Button>())
            {
                var surface = button.GetComponent<Image>();
                surface.sprite = preferences.RoundedSurface; surface.type = Image.Type.Sliced;
            }
            foreach (var graphic in GetComponentsInChildren<Graphic>()) graphic.material = overlayMaterial;
            // A mira pertence à cabeça; o painel permanece no lugar quando a cabeça se move.
            var reticleGO = new GameObject("MiraVR", typeof(RectTransform), typeof(Canvas));
            reticle = (RectTransform)reticleGO.transform;
            reticle.SetParent(eye.transform, false); reticle.localPosition = Vector3.forward * MuseumGuideTheme.VrReticleDistance;
            reticle.localScale = Vector3.one * MuseumGuideTheme.VrReticleScale;
            reticle.sizeDelta = Vector2.one * MuseumGuideTheme.VrReticleSize;
            reticleGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            reticleGO.GetComponent<Canvas>().sortingOrder = 200;
            ringSprite = MakeRing();
            ring = reticleGO.AddComponent<Image>(); ring.material = overlayMaterial; ring.sprite = ringSprite;
            ring.color = MuseumGuideTheme.Accent; ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360;
            var dot = new GameObject("Ponto", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(reticle, false); ((RectTransform)dot.transform).sizeDelta = Vector2.one * MuseumGuideTheme.VrReticleDot;
            dot.GetComponent<Image>().color = MuseumGuideTheme.Text; dot.GetComponent<Image>().material = overlayMaterial;
            BuildToolbar();
            SetVrEnabled(false);
        }
        private void BuildToolbar()
        {
            var go=new GameObject("Controles_da_visita_VR",typeof(RectTransform),typeof(Canvas),typeof(Image),typeof(VerticalLayoutGroup));
            toolbar=(RectTransform)go.transform;toolbar.SetParent(transform.parent,false);
            toolbar.sizeDelta=new Vector2(MuseumGuideTheme.VrToolbarWidth,MuseumGuideTheme.VrToolbarHeight);toolbar.localScale=Vector3.one*Scale;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=eye;canvas.sortingOrder=110;
            go.GetComponent<Image>().color=MuseumGuideTheme.Surface;
            var column=go.GetComponent<VerticalLayoutGroup>();column.padding=new RectOffset(16,16,12,12);column.spacing=10;column.childForceExpandHeight=false;
            menuControl=MakeButton(toolbar,"Menu",OpenMenu);
            foreach(var graphic in toolbar.GetComponentsInChildren<Graphic>())graphic.material=overlayMaterial;
            UpdateToolbar();
        }
        private void UpdateToolbar()
        {
            bool lookingDown=eye.transform.forward.y<-.26f;
            bool available=IsEnabled&&!MenuOpen&&!visible&&!gaze.IsPaused&&!lookingDown&&AwayFromExhibits();
            // Refresh heading while walking; keep the lateral target still when looking up to select it.
            if(lookingDown)
            {
                var forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up);
                if(forward.sqrMagnitude>.1f)toolbarRotation=Quaternion.LookRotation(forward);
            }
            toolbar.SetPositionAndRotation(eye.transform.position+toolbarRotation*
                new Vector3(-1.2f,0,MuseumGuideTheme.VrToolbarDistance),toolbarRotation);
            toolbar.gameObject.SetActive(available);
        }
        public void SetVrEnabled(bool enabled)
        {
            IsEnabled = enabled; visible = false; MenuOpen = false; ConsumesGaze = false;
            current = null; group.alpha = 0; ResetInteraction();
            reticle.gameObject.SetActive(enabled);
            if (enabled)
            {
                var forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up);
                if(forward.sqrMagnitude>.1f)toolbarRotation=Quaternion.LookRotation(forward);
            }
            toolbar.gameObject.SetActive(false);
            gameObject.SetActive(enabled);
        }
        public void Show(PaintingInfo info)
        {
            if (!IsEnabled || info == null || !preferences.PanelsEnabled) return;
            if (MenuOpen) return;
            bool changed = current != info || !visible;
            current = info; visible = true;
            if (changed)
            {
                page = 0; SetReadingLayout(true); PlaceBoard(); RebuildPages();
            }
            RenderPage();
        }
        public void Hide()
        {
            if (MenuOpen) return;
            visible = false; ConsumesGaze = false; current = null;
        }
        public void OpenMenu()
        {
            if (!IsEnabled || MenuOpen || gaze.IsPaused || !AwayFromExhibits()) return;
            MenuOpen = true; visible = true; menuPage = 0;
            mode.ResetWalkingGesture();
            PlaceBoard(); RenderMenu();
        }
        private bool AwayFromExhibits() => !Physics.CheckSphere(eye.transform.position, 4f,
            LayerMask.GetMask("Painting"), QueryTriggerInteraction.Ignore);
        public void CloseMenu()
        {
            MenuOpen = false; ConsumesGaze = false;
            ResetInteraction();
            current = gaze.Focus.Current;
            if (current != null && preferences.PanelsEnabled)
            {
                page = 0; SetReadingLayout(true); RebuildPages(); PlaceBoard(); RenderPage();
            }
            else visible = false;
        }
        public void ResetInteraction()
        {
            activation.Reset(); ConsumesGaze = false;
            if (ring != null) ring.fillAmount = 0;
            if (highlighted != null) highlighted.GetComponent<Image>().color = MuseumGuideTheme.Control;
            highlighted = null;
        }
        private void PlaceBoard()
        {
            // Anchor once along the opening gaze: approaching must actually reduce reading distance.
            var rotation = Quaternion.LookRotation(eye.transform.forward, Vector3.up);
            Board.SetPositionAndRotation(eye.transform.position + eye.transform.forward * Distance, rotation);
            activation.Reset();
        }
        private void RebuildPages()
        {
            pages.Clear();
            body.fontSize = MuseumGuideTheme.VrBody + preferences.TextStep * 4;
            var description = preferences.Summary ? current.resumo : current.description;
            Canvas.ForceUpdateCanvases();
            var sections=new List<string>{"SOBRE A OBRA\n"+current.artist+" — "+current.Periodo+"\n"+current.tecnica+"\n\n"+description,
                "RELAÇÃO COM 1922\n"+current.relacao_com_a_semana_de_1922,
                "SOBRE ESTA REPRODUÇÃO\n"+current.obra_historica_ou_reinterpretacao};
            if(!preferences.Summary)sections.Add("FONTES\n"+current.fontes);
            foreach(var section in sections){var split=new List<string>();Paginate(section,body,split);pages.AddRange(split);}
            page = Mathf.Clamp(page, 0, pages.Count - 1);
        }
        public static void Paginate(string text, Text target, List<string> output)
        {
            output.Clear();
            var generator = new TextGenerator();
            var settings = target.GetGenerationSettings(new Vector2(target.rectTransform.rect.width, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            int start = 0;
            while (start < text.Length)
            {
                int end = start + 1, fit = end;
                while (end <= text.Length)
                {
                    float height = generator.GetPreferredHeight(text.Substring(start, end - start), settings) / target.pixelsPerUnit;
                    if (height > target.rectTransform.rect.height - 4) break;
                    fit = end; end++;
                }
                if (fit < text.Length)
                {
                    int boundary = -1;
                    // Prefer a complete sentence or paragraph when it fills most of the page.
                    int minimum=start+(fit-start)/2;
                    for(int i=fit-1;i>=minimum;i--)
                    {
                        if(text[i]=='\n'||((text[i]=='.'||text[i]=='!'||text[i]=='?')&&i+1<text.Length&&char.IsWhiteSpace(text[i+1])))
                        {boundary=i;break;}
                    }
                    if(boundary<0)boundary = text.LastIndexOf(' ', fit - 1, fit - start);
                    if (boundary > start) fit = boundary + 1;
                }
                output.Add(text.Substring(start, fit - start).Trim()); start = fit;
            }
            if (output.Count == 0) output.Add("");
        }
        private void SetReadingLayout(bool reading)
        {
            heading.gameObject.SetActive(!reading);
            hint.gameObject.SetActive(!reading);
            actionGrid.gameObject.SetActive(!reading);
            var layout = body.GetComponent<LayoutElement>();
            // Four visible rows, three gaps and the existing vertical padding.
            layout.minHeight = layout.preferredHeight = reading ? Board.sizeDelta.y - 44 - 94 - 30 - MuseumGuideTheme.VrButtonHeight - 24 : 330;
        }
        private void RenderPage()
        {
            if (current == null || MenuOpen) return;
            title.text = current.title;
            body.text = pages[page];
            pageLabel.text = "Página " + (page + 1) + " de " + pages.Count;
            previous.interactable = page > 0; next.interactable = page + 1 < pages.Count;
            ApplyContrast();
        }
        private void RenderMenu()
        {
            SetReadingLayout(false);
            heading.text = "VISITA / CONTROLES PELO OLHAR";
            title.text = "Opções do visor";
            body.fontSize = MuseumGuideTheme.VrBody;
            body.text = "Abaixe a cabeça para andar e levante para parar.\n\nAs opções ficam disponíveis no hall e em áreas afastadas das obras. Olhe por 1 segundo para Menu. A caminhada pausa durante os ajustes.";
            pageLabel.text = menuPage==0?"VISITA · 1 de 2":"LEITURA · 2 de 2";
            previous.interactable = menuPage > 0; next.interactable = menuPage < 1;
            SetActionCount(6);
            if (menuPage == 0)
            {
                SetButton(actions[0], "Voltar à visita");
                SetButton(actions[1], "Centralizar");
                SetButton(actions[2], "Velocidade: "+HeadGazeMovement.SpeedLabel);
                SetButton(actions[3], "Ajustar leitura");SetButton(actions[4], "Sair do VR");
                SetActionCount(5);
            }
            else if (menuPage == 1)
            {
                SetButton(actions[0], "Texto: " + new[] { "padrão", "grande", "maior" }[preferences.TextStep]);
                SetButton(actions[1], "Resumo: " + (preferences.Summary ? "sim" : "não"));
                SetButton(actions[2], "Fichas: " + (preferences.PanelsEnabled ? "sim" : "não"));
                SetButton(actions[3], "Contraste: " + (preferences.HighContrast ? "alto" : "padrão"));
                SetButton(actions[4], "Animação: " + (preferences.ReducedMotion ? "não" : "sim"));
                SetButton(actions[5], "Leitura: " + (preferences.LongerReading ? "longa" : "breve"));
                body.text="Aumente o texto, escolha a versão resumida ou ative alto contraste.\n\nA ficha abre à sua frente. Use Anterior e Próxima página para ler. Desvie o olhar para encerrar a leitura.";
            }
            ApplyContrast();
        }
        private void ApplyContrast()
        {
            background.color = preferences.HighContrast ? Color.black : MuseumGuideTheme.Surface;
            body.color = title.color = preferences.HighContrast ? Color.white : MuseumGuideTheme.Text;
        }
        private void ChangePage(int delta)
        {
            if (MenuOpen) { menuPage = Mathf.Clamp(menuPage + delta, 0, 1); RenderMenu(); }
            else { page = Mathf.Clamp(page + delta, 0, pages.Count - 1); RenderPage(); }
        }
        private void InvokeAction(int index)
        {
            if (!MenuOpen) return;
            if (menuPage == 0)
            {
                if (index == 0) { CloseMenu(); return; }
                if (index == 1) mode.Recenter();
                else if(index==2){HeadGazeMovement.CycleSpeed();RenderMenu();}
                else if(index==3){menuPage=1;RenderMenu();}
                else if(index==4)mode.ExitVR();
                return;
            }
            else if (menuPage == 1)
            {
                if (index == 0) preferences.CycleTextSize();
                else if (index == 1) preferences.ToggleSummary();
                else if(index==2)preferences.TogglePanels();
                else if(index==3)preferences.ToggleContrast();
                else if(index==4)preferences.ToggleMotion();else preferences.ToggleLinger();
            }
            RenderMenu();
        }
        private void Update()
        {
            if (!IsEnabled) return;
            bool paused = FindPlayerPaused();
            UpdateToolbar();
            if (paused) { ResetInteraction(); return; }
            // Voltar do Android ou um frame travado não completa dwell/gestos sozinho.
            float elapsed = Mathf.Clamp(Time.unscaledDeltaTime, 0f, .1f);
            group.alpha = preferences.ReducedMotion ? (visible ? 1 : 0) :
                Mathf.MoveTowards(group.alpha, visible ? 1 : 0, Time.unscaledDeltaTime / MuseumGuideTheme.Fade);
            var ray = new Ray(eye.transform.position, eye.transform.forward);
            bool overBoard=visible&&HitRect(Board,ray);
            bool overToolbar=toolbar.gameObject.activeSelf&&HitRect(toolbar,ray);
            ConsumesGaze = overBoard || overToolbar;
            Button selected = null;
            if (overBoard)
            {
                if (previous.interactable && HitRect((RectTransform)previous.transform, ray)) selected = previous;
                else if (next.interactable && HitRect((RectTransform)next.transform, ray)) selected = next;
                else foreach (var button in actions)
                    if (button.gameObject.activeSelf&&button.interactable && HitRect((RectTransform)button.transform, ray)) { selected = button; break; }
            }
            else if(overToolbar)
            {
                if(HitRect((RectTransform)menuControl.transform,ray))selected=menuControl;
            }
            if(highlighted!=selected)
            {
                if(highlighted!=null)highlighted.GetComponent<Image>().color=MuseumGuideTheme.Control;
                highlighted=selected;if(highlighted!=null)highlighted.GetComponent<Image>().color=MuseumGuideTheme.Focus;
            }
            bool fire = activation.Step(selected, elapsed, DwellSeconds);
            ring.fillAmount = activation.Progress;
            if (fire && selected != null) selected.onClick.Invoke();
        }
        private void SetActionCount(int count)
        {
            for(int i=0;i<actions.Length;i++)actions[i].gameObject.SetActive(i<count);
            float height=((count+1)/2)*(MuseumGuideTheme.VrButtonHeight+MuseumGuideTheme.VrGap)-MuseumGuideTheme.VrGap;
            var layout=actionGrid.GetComponent<LayoutElement>();layout.preferredHeight=layout.minHeight=height;
        }
        private bool FindPlayerPaused() => gaze.IsPaused;
        public static bool HitRect(RectTransform rect, Ray ray)
        {
            if (!rect.gameObject.activeInHierarchy) return false;
            var plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out float distance) || distance < 0) return false;
            var local = rect.InverseTransformPoint(ray.GetPoint(distance));
            return rect.rect.Contains(new Vector2(local.x, local.y));
        }
        public static Text MakeText(Transform parent, string value, int fontSize, float height)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>(MuseumGuideTheme.FontResource);
            text.text = value; text.fontSize = fontSize; text.color = MuseumGuideTheme.Text;
            text.supportRichText = false; text.raycastTarget = false; text.lineSpacing = 1.12f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            var layout = go.GetComponent<LayoutElement>(); layout.preferredHeight = layout.minHeight = height; layout.flexibleHeight = 0;
            return text;
        }
        public static Button MakeButton(Transform parent, string value, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Botao_" + value, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false); go.GetComponent<Image>().color = MuseumGuideTheme.Control;
            var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
            var layout = go.GetComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = MuseumGuideTheme.VrButtonHeight; layout.flexibleHeight = 0; layout.flexibleWidth = 1;
            var label = MakeText(go.transform, value, MuseumGuideTheme.VrButtonText, MuseumGuideTheme.VrButtonHeight); label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6, 2); label.rectTransform.offsetMax = new Vector2(-6, -2);
            return button;
        }
        private static Transform Row(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var row = go.GetComponent<HorizontalLayoutGroup>(); row.spacing = MuseumGuideTheme.VrGap; row.childForceExpandHeight = false;
            var layout = go.GetComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = MuseumGuideTheme.VrButtonHeight; layout.flexibleHeight = 0;
            return go.transform;
        }
        private static void SetButton(Button button, string value) => button.GetComponentInChildren<Text>().text = value;
        private Sprite MakeRing()
        {
            const int size = 64;
            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                ringTexture.SetPixel(x, y, new Color(1, 1, 1, distance >= 25 && distance <= 30 ? 1 : 0));
            }
            ringTexture.Apply();
            return Sprite.Create(ringTexture, new Rect(0, 0, size, size), Vector2.one * .5f);
        }
        private void OnApplicationPause(bool paused) => ResetInteraction();
        private void OnApplicationFocus(bool focused) => ResetInteraction();
        private void OnDestroy()
        {
            if (toolbar != null) Destroy(toolbar.gameObject);
            if (reticle != null) Destroy(reticle.gameObject);
            if (overlayMaterial != null) Destroy(overlayMaterial);
            if (ringSprite != null) Destroy(ringSprite);
            if (ringTexture != null) Destroy(ringTexture);
        }
    }
}
