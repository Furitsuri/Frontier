// 中心部が透過し、外周に近づくほど水色の帯が現れる円形バリア風エフェクト用シェーダー
Shader "Frontier/CircularBarrier"
{
    Properties
    {
        // UI(Canvas/CanvasRenderer)が描画時にメインテクスチャの割り当て先を要求するためのダミー宣言(未使用)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Fill Color", Color) = (0.35, 0.85, 1, 1)
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.48
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.3)) = 0.015
        _InnerFadeRadius ("Inner Fade Radius", Range(0, 0.5)) = 0.12
        _FillSoftness ("Fill Softness", Range(0.01, 0.5)) = 0.3
        _FillIntensity ("Fill Intensity", Range(0, 3)) = 0.55
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _OuterRadius;
            float _EdgeSoftness;
            float _InnerFadeRadius;
            float _FillSoftness;
            float _FillIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                float r = length(p);

                // 外周(_OuterRadius)を境に内側は残し、外側だけをソフトに切り落とす
                float outerMask = 1.0 - smoothstep(_OuterRadius, _OuterRadius + _EdgeSoftness, r);

                // 中心付近は透過させ、外周に近づくほど水色の塗りを濃くする
                float fill = smoothstep(_InnerFadeRadius, _InnerFadeRadius + _FillSoftness, r);
                float fillAlpha = fill * outerMask * _FillIntensity;

                fixed3 col = _Color.rgb * fillAlpha;
                float alpha = saturate(fillAlpha);

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
