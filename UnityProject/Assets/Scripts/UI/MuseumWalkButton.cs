using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuseumModerna
{
    /// <summary>Explicit hold-to-walk for touch and mouse; releases on exit, pause or focus loss.</summary>
    public sealed class MuseumWalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private HeadGazeMovement movement;
        private PlayerController player;
        private CanvasGroup group;
        private bool held;
        private bool ownsWalk;
        private int? activePointer;
        private RectTransform rect;
        private Text label;
        public static void Build(Transform parent,Sprite surface)
        {
            var go=new GameObject("Segurar_para_caminhar",typeof(RectTransform),typeof(Image),typeof(CanvasGroup),typeof(MuseumWalkButton));
            go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
            rect.anchoredPosition=Vector2.one*MuseumGuideTheme.Padding;
            rect.sizeDelta=new Vector2(MuseumGuideTheme.WalkButtonWidth,MuseumGuideTheme.WalkButtonHeight);
            var image=go.GetComponent<Image>();image.sprite=surface;image.type=Image.Type.Sliced;image.color=MuseumGuideTheme.Control;
            var text=VrWorldGuide.MakeText(go.transform,"",MuseumGuideTheme.Body,MuseumGuideTheme.WalkButtonHeight);
            text.gameObject.name="Rotulo";
            text.alignment=TextAnchor.MiddleCenter;text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=new Vector2(8,4);text.rectTransform.offsetMax=new Vector2(-8,-4);
            // Awake runs before the child label exists on a dynamically created object.
            go.GetComponent<MuseumWalkButton>().label=text;
        }
        private void Awake()
        {
            movement=FindAnyObjectByType<HeadGazeMovement>();player=FindAnyObjectByType<PlayerController>();group=GetComponent<CanvasGroup>();
            rect=(RectTransform)transform;
            label=GetComponentInChildren<Text>();
        }
        private void Update()
        {
            // O botão permanece disponível mesmo que o usuário também tenha escolhido
            // caminhar por inclinação: oferece um caminho de recuperação sem sensor.
            bool available=movement!=null&&player!=null&&player.State!=PlayerState.Paused&&
                (MobileVrMode.Instance==null||!MobileVrMode.Instance.IsActive);
            group.alpha=available?1:0;group.blocksRaycasts=available;group.interactable=available;
            bool portrait=Screen.height>Screen.width;
            rect.anchorMin=rect.anchorMax=rect.pivot=portrait?Vector2.up:Vector2.zero;
            rect.anchoredPosition=portrait?new Vector2(MuseumGuideTheme.Padding,-(MuseumGuideTheme.Padding+MuseumGuideTheme.ButtonHeight+MuseumGuideTheme.Gap)):
                Vector2.one*MuseumGuideTheme.Padding;
            if(label!=null) label.text=HeadGazeMovement.TapToWalk
                ? (movement!=null&&movement.ManualWalkingRequested?"CAMINHANDO\nToque para parar":"TOQUE PARA CAMINHAR\nToque novamente para parar")
                : "SEGURE PARA CAMINHAR\nSolte para parar";
            GetComponent<Image>().color=movement!=null&&movement.ManualWalkingRequested
                ? MuseumGuideTheme.Focus : MuseumGuideTheme.Control;
            if(!available)Release();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if(!group.interactable||eventData.button!=PointerEventData.InputButton.Left||activePointer.HasValue)return;
            activePointer=eventData.pointerId;
            if(HeadGazeMovement.TapToWalk)
            {
                if(movement.ManualWalkingRequested)movement.CancelManualWalk();else movement.RequestManualWalk();
                ownsWalk=movement.ManualWalkingRequested;
                return;
            }
            held=true;movement.RequestManualWalk();ownsWalk=movement.ManualWalkingRequested;GetComponent<Image>().color=MuseumGuideTheme.Focus;
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if(activePointer!=eventData.pointerId)return;
            activePointer=null;
            if(held)Release();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if(activePointer==eventData.pointerId&&held)Release();
        }
        private void OnApplicationFocus(bool focused){if(!focused)Release();}
        private void OnApplicationPause(bool paused){if(paused)Release();}
        private void OnDisable()=>Release();
        private void Release()
        {
            held=false;activePointer=null;
            if(ownsWalk)movement?.CancelManualWalk();
            ownsWalk=false;GetComponent<Image>().color=MuseumGuideTheme.Control;
        }
    }
}
