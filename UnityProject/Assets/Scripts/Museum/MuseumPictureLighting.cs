using UnityEngine;
using System.Linq;

namespace MuseumModerna
{
    /// <summary>Focos reais das luminárias; orçamento local para celulares.</summary>
    [DisallowMultipleComponent]
    public sealed class MuseumPictureLighting : MonoBehaviour
    {
        public const float Intensity = 1.7f;
        public const float ActivationDistance = 3.8f;
        private Light[] spots;
        private float[] distances;
        private bool[] selected;
        private float nextSelection;
        private Camera eye;
        private void Awake()
        {
            spots=GetComponentsInChildren<Light>(true).Where(l=>l.type==LightType.Spot).ToArray();
            distances=new float[spots.Length]; selected=new bool[spots.Length];
            foreach(var spot in spots) { spot.intensity=0; spot.enabled=false; }
        }
        private void Update() => Tick(Time.unscaledDeltaTime);

        private void Tick(float deltaTime)
        {
            if (eye==null) eye=Camera.main;
            if (eye==null) return;
            if(Time.unscaledTime>=nextSelection)
            {
                nextSelection=Time.unscaledTime+.2f;
                for(int i=0;i<spots.Length;i++)
                {
                    selected[i]=false;
                    var spot=spots[i];
                    var exhibit=spot.GetComponentInParent<PaintingExhibit>();
                    var center=exhibit!=null?exhibit.transform.position:spot.transform.position;
                    var offset=eye.transform.position-center;
                    offset.y=0;
                    // Não acenda o quadro da sala vizinha através de uma parede.
                    bool visible=exhibit==null || Vector3.Dot(offset,exhibit.transform.forward)>.1f;
                    if(visible && exhibit!=null && Physics.Linecast(eye.transform.position,center,out var hit,~0,QueryTriggerInteraction.Ignore))
                        visible=hit.collider.GetComponentInParent<PaintingExhibit>()==exhibit;
                    distances[i]=visible?offset.sqrMagnitude:float.PositiveInfinity;
                }
                for(int slot=0;slot<3;slot++)
                {
                    int best=-1; float distance=ActivationDistance*ActivationDistance;
                    for(int i=0;i<spots.Length;i++)
                        if(!selected[i] && distances[i]<distance) { best=i; distance=distances[i]; }
                    if(best<0) break;
                    selected[best]=true;
                }
            }
            for(int i=0;i<spots.Length;i++)
            {
                spots[i].intensity=Mathf.MoveTowards(spots[i].intensity,selected[i]?Intensity:0,Mathf.Max(0,deltaTime)*4f);
                spots[i].enabled=spots[i].intensity>.001f;
            }
        }
        private void OnDisable()
        {
            if(spots==null) return;
            foreach(var spot in spots) { spot.enabled=false; spot.intensity=0; }
        }
    }
}
