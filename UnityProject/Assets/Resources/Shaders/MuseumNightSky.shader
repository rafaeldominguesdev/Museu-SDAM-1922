Shader "Museum/Night Sky"
{
    Properties
    {
        _MoonTex ("Lunar relief", 2D) = "white" {}
        _MoonDirection ("Moon direction", Vector) = (0.16,1,-0.18,0)
        _MoonRadius ("Angular radius", Range(0.01,0.15)) = 0.068
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MoonTex;
            float4 _MoonDirection;
            float _MoonRadius;
            struct appdata { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o;
            }
            float hash(float2 p)
            {
                float3 q=frac(float3(p.xyx)*0.1031);
                q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z);
            }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float3 stars(float2 uv,float scale,float threshold)
            {
                float2 p=uv*scale, cell=floor(p);
                float seed=hash(cell+19.7);
                float2 center=0.22+0.56*float2(hash(cell),hash(cell+6.17));
                float distance=length(frac(p)-center);
                float radius=lerp(.025,.072,seed*seed);
                float aa=max(length(fwidth(p))*.55,.008);
                float core=1-smoothstep(max(0,radius-aa),radius+aa,distance);
                float halo=exp(-distance*distance/0.009)*.12;
                float visible=step(threshold,seed);
                return (core+halo)*visible*lerp(float3(.56,.69,1),float3(1,.91,.75),hash(cell+47))*.9;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Unity resolves this to the current eye in stereo. Subtract that eye,
                // not the midpoint, so the sky has infinite rather than roof-depth parallax.
                float3 eye=_WorldSpaceCameraPos;
                // Direction, not mesh UVs: a shared infinite sky across all seven openings.
                float3 ray=normalize(i.world-eye);
                float2 uv=ray.xz/(1+max(ray.y,-.9));
                float cloud=noise(uv*7)+.45*noise(uv*19)+.18*noise(uv*53);
                float band=exp(-pow((uv.x*.7+uv.y-.15)*5,2));
                float3 sky=lerp(float3(.025,.042,.083),float3(.008,.015,.038),saturate(ray.y));
                sky+=float3(.010,.014,.025)*cloud*band;
                sky+=stars(uv,58,.64)+stars(uv+3.7,103,.85)*.48;
                float3 moon=normalize(_MoonDirection.xyz);
                float3 side=normalize(cross(moon,float3(0,0,1)));
                float3 up=cross(side,moon);
                float2 p=float2(dot(ray,side),dot(ray,up))/_MoonRadius;
                float r=length(p), aa=max(fwidth(r),.002);
                float facing=step(0,dot(ray,moon));
                float disk=(1-smoothstep(1-aa,1+aa,r))*facing;
                sky+=float3(.09,.13,.21)*exp(-r*r*.9)*facing;
                float3 normal=float3(p,sqrt(saturate(1-dot(p,p))));
                float relief=tex2D(_MoonTex,p*.5+.5).r;
                float illumination=.16+.84*saturate(dot(normal,normalize(float3(-.32,.24,1))));
                float3 lunar=float3(.91,.93,.97)*relief*illumination;
                return fixed4(lerp(sky,lunar,disk),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
