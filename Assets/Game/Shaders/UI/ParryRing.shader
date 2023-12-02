Shader "Unlit/ParryRing"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.0, 1.0) ) = 0.5

        // 判定範囲を表示するリング描画
        [Header(Success Judge Ring)]
        _RangeOutlineColor ("Range Ring Outline Color", Color) = (1,1,1,1)
        _JudgeRingInnerRadius ("Range Ring Inner Radius", Range(0, 1)) = 0.4
        _JudgeRingOuterRadius ("Range Ring Outer Radius", Range(0, 1)) = 0.5

        // ジャストタイミング判定範囲を表示するリング描画
        [Header(Success Just Timing Judge Ring)]
        _JustRangeOutlineColor ("Just Range Ring Outline Color", Color) = (1,1,0,1)
        _JustJudgeRingInnerRadius ("Just Range Ring Inner Radius", Range(0, 1)) = 0.45
        _JustJudgeRingOuterRadius ("Just Range Ring Outer Radius", Range(0, 1)) = 0.46

        [Space]
        [Header(Shrink Ring)]
        // 縮むリング描画
        _ShrinkOutlineColor ("Shrink Ring Outline Color", Color) = (1,1,1,1)
        _ShrinkInitRadius ("Shrink Ring Init Radius", Range(0, 3)) = 1
        _ShrinkRingWidth ("Shrink Ring Width", Range(0, 0.5)) = 0.02
        _ShrinkRingSizeRate ("Shrink Ring Size Rate", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZTest Always
        // 深度順序
        ZWrite On
        // Alpha用
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB

        // カメラから受け取った描画情報をそのまま映す(何もしない)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed _Brightness;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= _Brightness;

                return col;
            }
            ENDCG
        }

        GrabPass{}
        
        // パリィ判定範囲のリングを描画
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 outline : TEXCOORD0;
            };

            fixed4 _RangeOutlineColor;
            float _JudgeRingInnerRadius;
            float _JudgeRingOuterRadius;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // 輪郭線の幅を頂点シェーダー内で設定
                // float outlineWidth = _RangeOutlineWidth * (1.0 / o.vertex.w);
                // 画面中央に表示
                o.outline = o.vertex + float4(0.5, 0.5, 0, 0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 輪郭線の幅に応じてアウトラインを描画
                float2 d = fwidth(i.outline.xy);
                float2 threshold = smoothstep(0.5 - d, 0.5 + d, i.outline.xy);
                float2 distance = abs(i.outline.xy - 0.5);
                float isRing = step(length(distance), _JudgeRingOuterRadius) - step(length(distance), _JudgeRingInnerRadius);

                fixed4 outlineColor = lerp(_RangeOutlineColor, _RangeOutlineColor, threshold.x * threshold.y) * isRing;

                return outlineColor;
            }
            ENDCG
        }

        GrabPass{}

        // パリィジャスト判定範囲のリングを描画
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 outline : TEXCOORD0;
            };

            fixed4 _JustRangeOutlineColor;
            float _JustJudgeRingInnerRadius;
            float _JustJudgeRingOuterRadius;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // 輪郭線の幅を頂点シェーダー内で設定
                // float outlineWidth = _RangeOutlineWidth * (1.0 / o.vertex.w);
                // 画面中央に表示
                o.outline = o.vertex + float4(0.5, 0.5, 0, 0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 輪郭線の幅に応じてアウトラインを描画
                float2 d = fwidth(i.outline.xy);
                float2 threshold = smoothstep(0.5 - d, 0.5 + d, i.outline.xy);
                float2 distance = abs(i.outline.xy - 0.5);
                float isRing = step(length(distance), _JustJudgeRingOuterRadius) - step(length(distance), _JustJudgeRingInnerRadius);

                fixed4 outlineColor = lerp(_JustRangeOutlineColor, _JustRangeOutlineColor, threshold.x * threshold.y) * isRing;

                return outlineColor;
            }
            ENDCG
        }

        GrabPass{}

        // 時間経過と共に縮むリングを描画
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 outline : TEXCOORD0;
            };

            fixed4 _ShrinkOutlineColor;

            float _ShrinkInitRadius;
            float _ShrinkRingWidth;
            float _ShrinkRingSizeRate;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // 画面中央に表示
                o.outline = o.vertex+ float4(0.5, 0.5, 0, 0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 輪郭線の幅に応じてアウトラインを描画
                float2 d = fwidth(i.outline.xy);
                float2 threshold = smoothstep( 0.5 - d, 0.5 + d, i.outline.xy);
                float2 distance = abs(i.outline.xy - 0.5);
                float isRing = step(length(distance), _ShrinkInitRadius * _ShrinkRingSizeRate  + _ShrinkRingWidth) - step(length(distance), _ShrinkInitRadius * _ShrinkRingSizeRate );
                fixed4 outlineColor = lerp(_ShrinkOutlineColor, _ShrinkOutlineColor, threshold.x * threshold.y) * isRing;

                return outlineColor;
            }
            ENDCG
        }
    }
}