Shader "AtLightsEnd/HintOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.72, 0.16, 1)
        _WidthPixels ("Width in pixels", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        // Reserve stencil bit 64 for the selected object's visible silhouette.
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual ColorMask 0
            Stencil { Ref 64 ReadMask 64 WriteMask 64 Comp Always Pass Replace }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag() : SV_Target { return 0; }
            ENDCG
        }
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual
            Stencil { Ref 64 ReadMask 64 WriteMask 0 Comp NotEqual }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _OutlineColor;
            float _WidthPixels;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; };
            float4 vert(Input v) : SV_POSITION
            {
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
                float2 projected = mul((float2x2)UNITY_MATRIX_P, viewNormal.xy);
                float2 pixelDirection = projected * _ScreenParams.xy;
                pixelDirection /= max(length(pixelDirection), 0.0001);
                clip.xy += pixelDirection * (2.0 * _WidthPixels / _ScreenParams.xy) * clip.w;
                return clip;
            }
            fixed4 frag() : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
