// 透過対応タイル用シェーダー（Water など）
// TileStandard.shader と同じ上面/側面切り替えロジックを持ちつつ、
// 上面（水面）を半透明で描画し、側面（水中・側壁）は不透明で描画する。
Shader "Frontier/TileTransparent"
{
    // =====================================================================
    // Properties: Unity のインスペクターに表示されるパラメーター
    // =====================================================================
    Properties
    {
        // 上面に使うテクスチャ（例: 水面テクスチャ）
        _TopTex ("上面テクスチャ (水面など)", 2D) = "white" {}

        // 側面・底面に使うテクスチャ（例: 水中・岩肌テクスチャ）
        _SideTex ("側面・底面テクスチャ (水中など)", 2D) = "white" {}

        // 側面の上部を「上面テクスチャ」で塗る割合（0〜0.5）
        // Water の場合、水面が側面上部に少し食い込む表現に使う
        _TopSideFraction ("側面上部の TopTex 割合", Range(0.0, 0.5)) = 0.05

        // 上面（＆側面上部）の不透明度（0=完全透明, 1=完全不透明）
        // Water の水面らしさを出すため、デフォルトは 0.6（やや透明）
        _TopAlpha ("上面の不透明度", Range(0.0, 1.0)) = 0.6

        // 側面・底面の不透明度（0=完全透明, 1=完全不透明）
        // デフォルト 0: 隣り合う水タイル間の仕切りが見えなくなりシームレスな水面になる。
        // 端のタイルで側壁を見せたい場合は値を上げて調整する。
        _SideAlpha ("側面の不透明度", Range(0.0, 1.0)) = 0.0

        // 乗算カラー。Tile.cs の ApplyDeployableColor() から書き込まれる。
        // 配置不可タイルは (0.5, 0.5, 0.5, 1.0) で50%暗転する。
        _Color ("カラー", Color) = (1,1,1,1)
    }

    SubShader
    {
        // ---- 透明オブジェクトとして描画 ----
        // Queue=Transparent : 不透明オブジェクトの描画後に描かれる（透過が正しく見えるため）
        // RenderType=Transparent : シェーダー置換などで透明として扱われる
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM

        // alpha:fade を追加することでアルファブレンディングが有効になる
        // (TileStandard との唯一の #pragma 差分)
        #pragma surface surf Standard fullforwardshadows alpha:fade vertex:vert
        #pragma target 3.0

        // ---- シェーダー変数（Properties と対応） ----
        sampler2D _TopTex;
        sampler2D _SideTex;
        float     _TopSideFraction;
        float     _TopAlpha;
        float     _SideAlpha;
        fixed4    _Color;

        // =====================================================================
        // Input 構造体（TileStandard.shader と同一）
        // =====================================================================
        struct Input
        {
            float2 uv_TopTex;   // 上面テクスチャの UV 座標
            float2 uv_SideTex;  // 側面テクスチャの UV 座標
            float3 worldNormal; // ワールド空間での面の法線（Surface Shader が自動設定）
            float  localY;      // オブジェクト空間での Y 座標（頂点シェーダーから渡す）
        };

        // =====================================================================
        // 頂点シェーダー（TileStandard.shader と同一）
        // =====================================================================
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localY = v.vertex.y; // -0.5 〜 +0.5
        }

        // =====================================================================
        // サーフェス関数
        // =====================================================================
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // ------------------------------------------------------------------
            // Step 1: 上面かどうかを判定（TileStandard と同じロジック）
            // ------------------------------------------------------------------
            bool isTopFace = IN.worldNormal.y > 0.5;

            // ------------------------------------------------------------------
            // Step 2: 側面の上部N割を TopTex で塗る領域かを判定
            // ------------------------------------------------------------------
            float sideTopThreshold = 0.5 - _TopSideFraction;
            bool  isSideTopArea    = !isTopFace && (IN.localY > sideTopThreshold);

            // ------------------------------------------------------------------
            // Step 3: テクスチャのサンプリング
            // ------------------------------------------------------------------
            fixed4 topColor  = tex2D(_TopTex,  IN.uv_TopTex)  * _Color;
            fixed4 sideColor = tex2D(_SideTex, IN.uv_SideTex) * _Color;

            // ------------------------------------------------------------------
            // Step 4: 領域に応じて色と透明度を決定
            //
            //   上面・側面上部 → topColor に _TopAlpha を掛けて半透明（水面）
            //   側面下部・底面 → sideColor に _SideAlpha を掛ける
            //                    _SideAlpha=0 なら側面が完全透明 → 隣接タイル間の仕切りが消える
            //
            // _Color.a は ApplyDeployableColor() では常に 1.0 なので
            // topColor.a = テクスチャのアルファ × 1.0 = テクスチャのアルファ
            // ------------------------------------------------------------------
            fixed4 finalColor;
            if (isTopFace || isSideTopArea)
            {
                finalColor   = topColor;
                finalColor.a = topColor.a * _TopAlpha;  // _TopAlpha で水面の透明度を制御
            }
            else
            {
                finalColor   = sideColor;
                finalColor.a = sideColor.a * _SideAlpha; // _SideAlpha で側面の透明度を制御
            }

            // ------------------------------------------------------------------
            // Step 5: SurfaceOutputStandard に書き込む
            // ------------------------------------------------------------------
            o.Albedo     = finalColor.rgb;
            o.Metallic   = 0.0;
            o.Smoothness = 0.4; // 水面らしく TileStandard より光沢を上げる（0.1→0.4）
            o.Alpha      = finalColor.a;
        }

        ENDCG
    }

    // 透明シェーダーが使えない環境でのフォールバック
    FallBack "Transparent/Diffuse"
}
