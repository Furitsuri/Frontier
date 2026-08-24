// 戦闘ステージ背景用の3色グラデーションSkyboxシェーダー
// 視線の高さ(view direction のY成分)を基準に、上段(_TopColor)/中段(_MiddleColor)/下段(_BottomColor)の3色を補間する
Shader "Frontier/Stage/BackgroundGradientSkybox"
{
    Properties
    {
        [Header(Gradient Colors)]
        _TopColor    ("上段カラー", Color) = (1, 1, 1, 1)
        _MiddleColor ("中段カラー(水平線)", Color) = (0.29, 0.56, 0.85, 1)
        _BottomColor ("下段カラー", Color) = (0, 0, 0, 1)

        [Header(Gradient Falloff)]
        _TopExponent    ("上段方向のグラデーション強さ", Range(0.1, 8)) = 1.0
        _BottomExponent ("下段方向のグラデーション強さ", Range(0.1, 8)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex  : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            fixed4 _TopColor;
            fixed4 _MiddleColor;
            fixed4 _BottomColor;
            float  _TopExponent;
            float  _BottomExponent;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Skyboxのメッシュはカメラを中心に描画されるため、オブジェクト座標をそのまま視線方向として使える
                o.viewDir = normalize(v.vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 視線の高さ(-1:真下 ～ 0:水平線 ～ +1:真上)を基準に、中段カラーから上段/下段カラーへ補間する
                float height = i.viewDir.y;

                fixed4 col;
                if (height >= 0)
                {
                    float t = pow(saturate(height), _TopExponent);
                    col = lerp(_MiddleColor, _TopColor, t);
                }
                else
                {
                    float t = pow(saturate(-height), _BottomExponent);
                    col = lerp(_MiddleColor, _BottomColor, t);
                }

                return col;
            }
            ENDCG
        }
    }
}
