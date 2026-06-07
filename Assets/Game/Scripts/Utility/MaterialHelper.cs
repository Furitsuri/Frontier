using UnityEngine;
using UnityEngine.Rendering;

namespace Frontier
{
    /// <summary>
    /// マテリアルの共通操作をまとめたユーティリティクラスです。
    /// </summary>
    public static class MaterialHelper
    {
        /// <summary>
        /// マテリアルにシェーダー種別を判定して半透明設定と色を適用します。
        /// URP Lit/Unlit は _BaseColor、Standard (レガシー) は _Color を使用します。
        /// </summary>
        public static void ConfigureTransparentMaterial( Material mat, Color color )
        {
            if ( mat.HasProperty( "_BaseColor" ) )
            {
                // URP Lit / Unlit
                mat.SetFloat( "_Surface",  1f );
                mat.SetFloat( "_Blend",    0f );
                mat.SetInt(   "_SrcBlend", (int)BlendMode.SrcAlpha );
                mat.SetInt(   "_DstBlend", (int)BlendMode.OneMinusSrcAlpha );
                mat.SetInt(   "_ZWrite",   0 );
                mat.EnableKeyword( "_SURFACE_TYPE_TRANSPARENT" );
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetColor( "_BaseColor", color );
            }
            else if ( mat.HasProperty( "_Color" ) )
            {
                // Standard (レガシー) シェーダー
                mat.SetFloat( "_Mode", 2f );    // Fade モード
                mat.SetInt(   "_SrcBlend", (int)BlendMode.SrcAlpha );
                mat.SetInt(   "_DstBlend", (int)BlendMode.OneMinusSrcAlpha );
                mat.SetInt(   "_ZWrite",   0 );
                mat.DisableKeyword( "_ALPHATEST_ON" );
                mat.EnableKeyword(  "_ALPHABLEND_ON" );
                mat.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetColor( "_Color", color );
            }
        }

        /// <summary>
        /// マテリアルにシェーダー種別を判定して不透明な色を適用します。
        /// URP Lit/Unlit は _BaseColor、Standard (レガシー) は _Color を使用します。
        /// </summary>
        public static void ApplyOpaqueColor( Material mat, Color color )
        {
            color.a = 1f;

            if ( mat.HasProperty( "_BaseColor" ) )
            {
                mat.SetColor( "_BaseColor", color );
            }
            else if ( mat.HasProperty( "_Color" ) )
            {
                mat.SetColor( "_Color", color );
            }
        }
    }
}
