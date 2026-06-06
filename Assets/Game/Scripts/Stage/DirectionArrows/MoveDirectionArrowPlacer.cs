using Frontier;
using Frontier.Entities;
using Frontier.Registries;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

/// <summary>
/// 移動経路の各タイルに進行方向を示す矢印オブジェクトを配置・管理します。
/// キャラクターごとに矢印セットを保持し、複数キャラクターの矢印を同時表示・個別削除できます。
/// 経路情報(タイルインデックス・方向種別・回転)は外部から渡されます。
/// </summary>
public class MoveDirectionArrowPlacer
{
    [Inject] private StageController _stageCtrl  = null;
    [Inject] private PrefabRegistry  _prefabReg  = null;

    private readonly Dictionary<CharacterKey, List<GameObject>> _arrowMap = new Dictionary<CharacterKey, List<GameObject>>();

    /// <summary>
    /// タイルへの矢印配置に必要な情報を表します。
    /// </summary>
    public struct Entry
    {
        public int               TileIndex;
        public MoveDirectionType DirectionType;
        public Quaternion        Rotation;

        public Entry( int tileIndex, MoveDirectionType directionType, Quaternion rotation )
        {
            TileIndex     = tileIndex;
            DirectionType = directionType;
            Rotation      = rotation;
        }
    }

    /// <summary>
    /// 指定キャラクターの経路エントリに基づいて各タイルに矢印オブジェクトを配置します。
    /// 同キャラクターの既存矢印は置き換えられます。
    /// </summary>
    public void PlaceArrows( in CharacterKey charaKey, IReadOnlyList<Entry> entries )
    {
        ClearArrows( charaKey );

        Color arrowColor = ResolveArrowColor( charaKey );
        var placed = new List<GameObject>( entries.Count );

        foreach ( var entry in entries )
        {
            int typeIndex = (int)entry.DirectionType;
            if ( typeIndex < 0 || typeIndex >= (int)MoveDirectionType.NUM ) continue;

            GameObject prefab = _prefabReg.MoveDirectionPrefabs[typeIndex];
            if ( prefab == null ) continue;

            Vector3    pos = _stageCtrl.GetTileStaticData( entry.TileIndex ).CharaStandPos;
            GameObject obj = Object.Instantiate( prefab, pos, entry.Rotation );
            ApplyArrowColor( obj, arrowColor );
            placed.Add( obj );
        }

        _arrowMap[charaKey] = placed;
    }

    /// <summary>
    /// 指定キャラクターの矢印オブジェクトをすべて破棄します。
    /// </summary>
    public void ClearArrows( in CharacterKey charaKey )
    {
        if ( !_arrowMap.TryGetValue( charaKey, out var list ) ) return;

        DestroyArrowList( list );
        _arrowMap.Remove( charaKey );
    }

    /// <summary>
    /// 全キャラクターの矢印オブジェクトをすべて破棄します。
    /// </summary>
    public void ClearAllArrows()
    {
        foreach ( var list in _arrowMap.Values )
        {
            DestroyArrowList( list );
        }

        _arrowMap.Clear();
    }

    // -------------------------------------------------------
    // Private
    // -------------------------------------------------------

    /// <summary>
    /// CharacterKey から矢印の表示色を決定します。
    /// CHARACTER_TAG ごとに色相帯を割り当て、CharacterIndex で帯内の色相を30°ずつずらします。
    /// alpha=0.6 の半透明色を返します。
    /// </summary>
    private static Color ResolveArrowColor( in CharacterKey charaKey )
    {
        float baseHue = charaKey.CharacterTag switch
        {
            CHARACTER_TAG.PLAYER => 210f,   // 青系
            CHARACTER_TAG.ENEMY  =>   0f,   // 赤系
            CHARACTER_TAG.OTHER  => 120f,   // 緑系
            _                    =>  60f,   // 黄系(フォールバック)
        };

        float hue  = ( ( baseHue + charaKey.CharacterIndex * 30f ) % 360f ) / 360f;
        Color rgb  = Color.HSVToRGB( hue, 0.85f, 1.0f );
        return new Color( rgb.r, rgb.g, rgb.b, 0.6f );
    }

    /// <summary>
    /// GameObject 以下の全 Renderer にマテリアルインスタンスを生成し、
    /// URP 透明設定と指定色を適用します。
    /// </summary>
    private static void ApplyArrowColor( GameObject obj, Color color )
    {
        foreach ( var renderer in obj.GetComponentsInChildren<Renderer>() )
        {
            var mat = new Material( renderer.sharedMaterial );

            // URP Lit / Unlit 共通の透明化設定
            mat.SetFloat( "_Surface",  1f );                           // 1 = Transparent
            mat.SetFloat( "_Blend",    0f );                           // 0 = Alpha
            mat.SetInt(   "_SrcBlend", (int)BlendMode.SrcAlpha );
            mat.SetInt(   "_DstBlend", (int)BlendMode.OneMinusSrcAlpha );
            mat.SetInt(   "_ZWrite",   0 );
            mat.EnableKeyword( "_SURFACE_TYPE_TRANSPARENT" );
            mat.renderQueue = (int)RenderQueue.Transparent;

            mat.SetColor( "_BaseColor", color );

            renderer.material = mat;
        }
    }

    /// <summary>
    /// リスト内の矢印 GameObject と紐づくマテリアルインスタンスを破棄します。
    /// </summary>
    private static void DestroyArrowList( List<GameObject> list )
    {
        foreach ( var obj in list )
        {
            if ( obj == null ) continue;

            foreach ( var renderer in obj.GetComponentsInChildren<Renderer>() )
            {
                if ( renderer.material != null )
                {
                    Object.Destroy( renderer.material );
                }
            }

            Object.Destroy( obj );
        }
    }
}
