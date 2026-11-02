// タイル用カスタムシェーダー
// 上面・側面上部・側面下部/底面でテクスチャを切り替える。
// マインクラフトの草ブロックのように、上面は草、側面上部も少し草、それ以下は土、という表現が可能。
Shader "Frontier/TileStandard"
{
    // =====================================================================
    // Properties: Unity のインスペクターに表示されるパラメーター
    // =====================================================================
    Properties
    {
        // 上面に使うテクスチャ（例: 草のテクスチャ）
        _TopTex ("上面テクスチャ", 2D) = "white" {}

        // 側面・底面に使うテクスチャ（例: 土のテクスチャ）
        _SideTex ("側面・底面テクスチャ", 2D) = "white" {}

        // 側面の上部を「上面テクスチャ」で塗る割合（0〜0.5）
        // 0.0 = 側面は完全に SideTex のみ
        // 0.1 = 側面の上部10%を TopTex で塗る（マインクラフト風）
        _TopSideFraction ("側面上部の TopTex 割合", Range(0.0, 0.5)) = 0.1

        // 乗算カラー。Tile.cs の ApplyDeployableColor() が material.color に書き込む値がここに入る。
        // 配置不可タイルは (0.5, 0.5, 0.5) で50%暗転する。
        _Color ("カラー", Color) = (1,1,1,1)

        // カメラの注視点を遮っている間の抜き具合。TileOcclusionFader から書き込まれる
        [HideInInspector] _OcclusionFade ("遮蔽時の抜き具合", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        // 不透明オブジェクトとして描画
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        // Surface Shader の宣言
        //   surf           : サーフェス関数の名前
        //   Standard       : 物理ベースライティング（PBR）を使う
        //   fullforwardshadows : すべてのライト種別で影を受け取る
        //   vertex:vert    : カスタム頂点関数 vert を使う
        #pragma surface surf Standard fullforwardshadows vertex:vert
        // 3.0 では遮蔽ディザ用の screenPos を加えると補間レジスタ数(10)を超えるため 3.5 とする
        #pragma target 3.5

        #include "TileOcclusionDither.cginc"

        // ---- シェーダー変数（Properties と対応） ----
        sampler2D _TopTex;
        sampler2D _SideTex;
        float     _TopSideFraction;
        fixed4    _Color;

        // =====================================================================
        // Input 構造体
        // 頂点シェーダーからフラグメント処理（surf）へ渡すデータ。
        // "uv_XXX" という名前にすると Unity が自動的に UV 座標を設定してくれる。
        // =====================================================================
        struct Input
        {
            float2 uv_TopTex;   // 上面テクスチャの UV 座標
            float2 uv_SideTex;  // 側面テクスチャの UV 座標
            float3 worldNormal; // ワールド空間での面の法線ベクトル（Surface Shader が自動設定）
            float  localY;      // オブジェクト空間での Y 座標（頂点シェーダーから渡す）
            float4 screenPos;   // スクリーン座標（遮蔽時のディザ抜きに使う。Surface Shader が自動設定）
            float3 worldPos;    // ワールド座標（遮蔽時、地面より下へ抜ける視線を判定するのに使う。Surface Shader が自動設定）
        };

        // =====================================================================
        // 頂点シェーダー (vert)
        // メッシュの各頂点ごとに呼ばれる。
        // ここでオブジェクト空間の Y 座標を取り出して surf に渡す。
        // =====================================================================
        void vert(inout appdata_full v, out Input o)
        {
            // out 変数は必ず初期化する（Unity の Surface Shader の慣習）
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // v.vertex はオブジェクト空間での頂点座標。
            // Unity の標準キューブは Y が -0.5 〜 +0.5 に正規化されている。
            // スケールをどんな値にしても、この範囲は変わらない。
            o.localY = v.vertex.y;
        }

        // =====================================================================
        // サーフェス関数 (surf)
        // ピクセルごとに呼ばれ、そのピクセルの色・質感を決定する。
        // =====================================================================
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // カメラの注視点を遮っている場合、注視点周りのピクセルを網点状に抜く
            ClipTileOcclusion( IN.screenPos, IN.worldPos );

            // ------------------------------------------------------------------
            // Step 1: このピクセルが「上面」かどうかを判定する
            // ------------------------------------------------------------------
            // worldNormal.y は面が上を向くほど 1.0 に近づく。
            //   上面: worldNormal.y ≈ 1.0
            //   側面: worldNormal.y ≈ 0.0
            //   底面: worldNormal.y ≈ -1.0
            // 0.5 を閾値にして「上面」と「それ以外」を分ける。
            bool isTopFace = IN.worldNormal.y > 0.5;

            // ------------------------------------------------------------------
            // Step 2: 側面のうち「上部N割」に該当するかを判定する
            // ------------------------------------------------------------------
            // localY の範囲は -0.5 〜 +0.5。
            // _TopSideFraction = 0.1 のとき:
            //   閾値 = 0.5 - 0.1 = 0.4
            //   localY > 0.4 の部分が「側面上部10%」
            float sideTopThreshold = 0.5 - _TopSideFraction;

            // 上面でない かつ localY が閾値より高い → 側面の草領域
            bool isSideTopArea = !isTopFace && (IN.localY > sideTopThreshold);

            // ------------------------------------------------------------------
            // Step 3: 各テクスチャをサンプリングし、_Color を乗算する
            // ------------------------------------------------------------------
            fixed4 topColor  = tex2D(_TopTex,  IN.uv_TopTex)  * _Color;
            fixed4 sideColor = tex2D(_SideTex, IN.uv_SideTex) * _Color;

            // ------------------------------------------------------------------
            // Step 4: 使用するテクスチャを決定する
            //   上面                → topColor  (草)
            //   側面の上部N%        → topColor  (草がはみ出した部分)
            //   側面の下部・底面    → sideColor (土)
            // ------------------------------------------------------------------
            fixed4 finalColor = (isTopFace || isSideTopArea) ? topColor : sideColor;

            // ------------------------------------------------------------------
            // Step 5: SurfaceOutputStandard に結果を書き込む
            // ------------------------------------------------------------------
            o.Albedo     = finalColor.rgb; // ピクセルの色
            o.Metallic   = 0.0;            // 金属感: タイルなので0
            o.Smoothness = 0.1;            // 光沢: わずかに（0だとのっぺり、1だと鏡面）
            o.Alpha      = finalColor.a;
        }

        ENDCG

        // =====================================================================
        // ShadowCaster パス
        // このパスは「ライトのシャドウマップ」と「カメラの深度テクスチャ」の両方の描画に使われる。
        //   シャドウマップ   : 抜かない（遮蔽中でも柱などの影は残す）
        //   カメラの深度     : 本体と同じ網点で抜く（抜いた穴の奥に見える床へ、柱の影が誤って落ちないようにする）
        // =====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"
            #include "TileOcclusionDither.cginc"

            struct v2fShadow
            {
                V2F_SHADOW_CASTER;
                float4 screenPos : TEXCOORD1;
                float3 worldPos  : TEXCOORD2;
            };

            v2fShadow vertShadow( appdata_base v )
            {
                v2fShadow o;
                UNITY_INITIALIZE_OUTPUT( v2fShadow, o );
                TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
                o.screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
                o.worldPos  = mul( unity_ObjectToWorld, v.vertex ).xyz;
                return o;
            }

            float4 fragShadow( v2fShadow i ) : SV_Target
            {
                // 平行光源のシャドウマップは平行投影、メインカメラは透視投影のため、射影行列で両者を見分ける
                bool isCameraDepth = UNITY_MATRIX_P[3][3] != 1.0;
                if( isCameraDepth ) { ClipTileOcclusion( i.screenPos, i.worldPos ); }

                SHADOW_CASTER_FRAGMENT( i )
            }
            ENDCG
        }
    }

    // このシェーダーが使えない環境でのフォールバック
    FallBack "Diffuse"
}
