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

        // 露出している（隣にタイルが無い）側面・底面の不透明度（0=完全透明, 1=完全不透明）
        // デフォルト 1: 端の水タイルでは側壁（水中）がしっかり見えるようにする。
        _SideAlpha ("露出側面の不透明度", Range(0.0, 1.0)) = 1.0

        // 4方向それぞれの側面を表示するか否かのマスク (1=表示, 0=非表示)
        //   x = +X(右), y = -X(左), z = +Z(前), w = -Z(後)
        // 隣に水タイルがある方向を 0 にすることで、水タイル同士の仕切りが消え
        // シームレスな水面になる（マインクラフトの面カリングと同じ考え方）。
        // Tile 側（StageData.ApplyWaterSideFaceMasks）から隣接状況に応じて書き込まれる。
        _SideAlphaDirs ("側面表示マスク (xPos,xNeg,zPos,zNeg)", Vector) = (1,1,1,1)

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
        float4    _SideAlphaDirs;
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
            // Step 1.5: この側面が「どの方向を向いた面か」から表示マスクを取得
            //   隣に水タイルがある方向のマスクは 0 になっており、その面は消える。
            //   上面（isTopFace）には適用しない（水面は常に描画する）。
            // ------------------------------------------------------------------
            float sideMask = 1.0;
            if (!isTopFace)
            {
                if      (IN.worldNormal.x >  0.5) sideMask = _SideAlphaDirs.x; // +X(右)
                else if (IN.worldNormal.x < -0.5) sideMask = _SideAlphaDirs.y; // -X(左)
                else if (IN.worldNormal.z >  0.5) sideMask = _SideAlphaDirs.z; // +Z(前)
                else if (IN.worldNormal.z < -0.5) sideMask = _SideAlphaDirs.w; // -Z(後)
            }

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
            //   上面          → topColor に _TopAlpha を掛けて半透明（水面）。常に描画。
            //   側面上部       → topColor に _TopAlpha と sideMask を掛ける（露出時のみ水面の縁が見える）
            //   側面下部・底面 → sideColor に _SideAlpha と sideMask を掛ける
            //                    sideMask=0（隣が水）なら側面が消えてシームレスな水面に、
            //                    sideMask=1（露出）なら _SideAlpha の不透明度で側壁が見える。
            //
            // _Color.a は ApplyDeployableColor() では常に 1.0 なので
            // topColor.a = テクスチャのアルファ × 1.0 = テクスチャのアルファ
            // ------------------------------------------------------------------
            fixed4 finalColor;
            if (isTopFace)
            {
                finalColor   = topColor;
                finalColor.a = topColor.a * _TopAlpha;             // 水面（常に描画）
            }
            else if (isSideTopArea)
            {
                finalColor   = topColor;
                finalColor.a = topColor.a * _TopAlpha * sideMask;  // 側面上部の水面の縁
            }
            else
            {
                finalColor   = sideColor;
                finalColor.a = sideColor.a * _SideAlpha * sideMask; // 側壁（水中）
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
