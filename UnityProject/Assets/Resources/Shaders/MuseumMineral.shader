Shader "Museum/Mineral"
{
    Properties
    {
        _Color ("Mineral color", Color) = (0.8,0.8,0.8,1)
        _MainTex ("Mineral grain", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.18
        _Metallic ("Metallic", Range(0,1)) = 0
        _GrainScale ("World grain scale", Float) = 1.4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness, _Metallic, _GrainScale;
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 weights=pow(abs(IN.worldNormal),4);
            weights/=max(dot(weights,1),.001);
            float3 p=IN.worldPos*_GrainScale;
            float grain=tex2D(_MainTex,p.yz).r*weights.x+tex2D(_MainTex,p.xz).r*weights.y+tex2D(_MainTex,p.xy).r*weights.z;
            o.Albedo=_Color.rgb*lerp(.97,1.025,grain);
            o.Metallic=_Metallic; o.Smoothness=_Glossiness*(.8+.2*grain); o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
