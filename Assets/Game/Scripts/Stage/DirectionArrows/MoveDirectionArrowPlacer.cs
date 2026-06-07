using Frontier;
using Frontier.Entities;
using Frontier.Registries;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

/// <summary>
/// 1キャラクター分の移動方向矢印オブジェクトを配置・管理します。
/// ActionRangeController が所有し、Init で色を確定してから使用します。
/// </summary>
public class MoveDirectionArrowPlacer
{
    [Inject] private StageController _stageCtrl = null;
    [Inject] private PrefabRegistry  _prefabReg = null;

    private Color              _arrowColor;
    private List<GameObject>   _arrows = new List<GameObject>();

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
    /// キャラクターキーから表示色を決定して保持します。
    /// ActionRangeController.Init から呼んでください。
    /// </summary>
    public void Init( in CharacterKey charaKey )
    {
        _arrowColor = ResolveArrowColor( charaKey );
    }

    /// <summary>
    /// 経路エントリに基づいて各タイルに矢印オブジェクトを配置します。
    /// 既存の矢印は置き換えられます。
    /// </summary>
    public void PlaceArrows( IReadOnlyList<Entry> entries )
    {
        ClearArrows();

        foreach ( var entry in entries )
        {
            int typeIndex = (int)entry.DirectionType;
            if ( typeIndex < 0 || typeIndex >= (int)MoveDirectionType.NUM ) continue;

            GameObject prefab = _prefabReg.MoveDirectionPrefabs[typeIndex];
            if ( prefab == null ) continue;

            Vector3    pos = _stageCtrl.GetTileStaticData( entry.TileIndex ).CharaStandPos;
            GameObject obj = Object.Instantiate( prefab, pos, entry.Rotation );
            ApplyArrowColor( obj, _arrowColor );
            _arrows.Add( obj );
        }
    }

    /// <summary>
    /// 矢印オブジェクトをすべて破棄します。
    /// </summary>
    public void ClearArrows()
    {
        foreach ( var obj in _arrows )
        {
            if ( obj == null ) continue;

            foreach ( var renderer in obj.GetComponentsInChildren<Renderer>() )
            {
                if ( renderer.material != null ) Object.Destroy( renderer.material );
            }

            Object.Destroy( obj );
        }

        _arrows.Clear();
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
            CHARACTER_TAG.PLAYER => 210f,
            CHARACTER_TAG.ENEMY  =>   0f,
            CHARACTER_TAG.OTHER  => 120f,
            _                    =>  60f,
        };

        float hue = ( ( baseHue + charaKey.CharacterIndex * 30f ) % 360f ) / 360f;
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

            mat.SetFloat( "_Surface",  1f );
            mat.SetFloat( "_Blend",    0f );
            mat.SetInt(   "_SrcBlend", (int)BlendMode.SrcAlpha );
            mat.SetInt(   "_DstBlend", (int)BlendMode.OneMinusSrcAlpha );
            mat.SetInt(   "_ZWrite",   0 );
            mat.EnableKeyword( "_SURFACE_TYPE_TRANSPARENT" );
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor( "_BaseColor", color );

            renderer.material = mat;
        }
    }
}
