Shader "AtLightsEnd/DialogueSpriteHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Highlight ("Highlight", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Highlight;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 color=tex2D(_MainTex,i.uv)*i.color;
                float2 d=fwidth(i.uv)*2.5;
                float edge=0;
                edge=max(edge,tex2D(_MainTex,i.uv+float2(d.x,0)).a);
                edge=max(edge,tex2D(_MainTex,i.uv-float2(d.x,0)).a);
                edge=max(edge,tex2D(_MainTex,i.uv+float2(0,d.y)).a);
                edge=max(edge,tex2D(_MainTex,i.uv-float2(0,d.y)).a);
                float outline=saturate(edge-color.a)*_Highlight;
                color.rgb=lerp(color.rgb,float3(1,0.77,0.3),outline);
                color.a=max(color.a,outline);
                return color;
            }
            ENDCG
        }
    }
}
