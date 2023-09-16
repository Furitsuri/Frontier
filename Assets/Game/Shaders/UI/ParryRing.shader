Shader "Unlit/ParryRing" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.005
    }
    SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 outline : TEXCOORD0;
            };

            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // 輪郭線の幅を頂点シェーダー内で設定
                float outlineWidth = _OutlineWidth * (1.0 / o.vertex.w);
                o.outline = o.vertex + float4(outlineWidth, outlineWidth, 0, 0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 輪郭線の幅に応じてアウトラインを描画
                float2 d = fwidth(i.outline.xy);
                float2 threshold = smoothstep(0.5 - d, 0.5 + d, i.outline.xy);
                fixed4 outlineColor = lerp(_OutlineColor, _OutlineColor, threshold.x * threshold.y);

                return outlineColor;
            }
            ENDCG
        }
    }
}