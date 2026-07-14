// 中心付近から外周に向かって伸びる、長さがランダムな集中線(放射状の線)を描画するシェーダー
// CircularBarrier と同じ RectTransform 上に重ねて使用し、円形バリアに勢いを加える用途を想定
Shader "Frontier/ConcentrationLines"
{
    Properties
    {
        // UI(Canvas/CanvasRenderer)が描画時にメインテクスチャの割り当て先を要求するためのダミー宣言(未使用)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Line Color", Color) = (0.85, 1, 1, 1)
        _LineCount ("Line Count", Range(4, 64)) = 28
        _LineWidth ("Line Width", Range(0.001, 0.5)) = 0.06
        _InnerRadius ("Inner Radius", Range(0, 0.3)) = 0.04
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.48
        _LengthVariance ("Length Variance", Range(0, 1)) = 0.6
        _EdgeFade ("Edge Fade", Range(0.001, 0.2)) = 0.05
        _RotationSpeed ("Rotation Speed (deg/sec)", Float) = 12.0
        _Intensity ("Intensity", Range(0, 5)) = 1.2
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
            float _LineCount;
            float _LineWidth;
            float _InnerRadius;
            float _OuterRadius;
            float _LengthVariance;
            float _EdgeFade;
            float _RotationSpeed;
            float _Intensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // セクター番号ごとに安定した疑似乱数を返す(線の長さのばらつきに使用)
            float hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453123);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                float r = length(p);
                float a = atan2(p.y, p.x) + radians(_RotationSpeed) * _Time.y;

                // 角度を0〜1に正規化し、_LineCount本のセクターに分割する
                float angle01 = frac(a / (2.0 * UNITY_PI) + 1000.0);
                float sector = angle01 * _LineCount;
                float sectorIndex = floor(sector);
                float sectorFrac = frac(sector);

                // 各セクターの中心付近だけを残し、細い線として切り出す
                float lineMask = 1.0 - smoothstep(0.0, _LineWidth, abs(sectorFrac - 0.5));

                // セクターごとにランダムな長さを割り当てる
                float rnd = hash(sectorIndex);
                float lineMaxRadius = lerp(_OuterRadius * (1.0 - _LengthVariance), _OuterRadius, rnd);

                float innerFade = smoothstep(_InnerRadius, _InnerRadius + _EdgeFade, r);
                float outerFade = 1.0 - smoothstep(lineMaxRadius - _EdgeFade, lineMaxRadius, r);
                float radialMask = innerFade * outerFade;

                float alpha = lineMask * radialMask * _Intensity;
                fixed3 col = _Color.rgb * alpha;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
