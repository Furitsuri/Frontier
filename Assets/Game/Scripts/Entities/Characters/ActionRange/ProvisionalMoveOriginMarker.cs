using System.Collections.Generic;
using Frontier.Stage;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 暫定的に移動した(移動が確定していない)キャラクターの移動前の位置を示す目印です。
    /// 移動前のタイルを白い枠で囲んだ上でキャラクターのモノクロの残像を表示し、そこから現在の位置までに実際に通った経路を
    /// キャラクター毎の色の矢印で示します。
    /// 残像は「過去」の位置を示すため、移動先などの「未来」を示す通常のゴースト(元の色のまま半透明)とは見た目を変えています。
    /// 表示中は毎フレーム対象キャラクターの状態を確認し、確定・巻き戻し等で暫定状態でなくなった時点で自動的に非表示になります。
    /// </summary>
    public class ProvisionalMoveOriginMarker : MonoBehaviour
    {
        private const float OUTLINE_HEIGHT          = 0.12f;    // タイル表面からの描画高さ(移動可能範囲等のタイルメッシュより上に描画する)
        private const float OUTLINE_SIZE_RATE       = 0.8f;     // タイルサイズに対する枠の大きさ
        private const float OUTLINE_WIDTH           = 0.07f;
        private static readonly Color OUTLINE_COLOR = new Color( 1f, 1f, 1f, 0.9f );    // 移動・攻撃範囲のタイル色(青・黄・赤)と被らない色

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;

        private LineRenderer _outlineRenderer           = null;
        private Material _outlineMaterial               = null;
        private MoveDirectionArrowPlacer _arrowPlacer   = null;
        private GhostObject _afterimage                 = null;
        private Player _afterimageSource                = null;     // 残像の生成元のキャラクター(同じキャラクターであれば残像を使い回す)
        private Player _target                          = null;

        public Player Target => _target;

        /// <summary>
        /// 枠の描画・矢印の配置に用いるインスタンスを生成します
        /// </summary>
        public void Setup()
        {
            LazyInject.GetOrCreate( ref _arrowPlacer, () => _hierarchyBld.InstantiateWithDiContainer<MoveDirectionArrowPlacer>( false ) );
            CreateOutlineRenderer();

            Hide();
        }

        /// <summary>
        /// 指定キャラクターの移動前の位置に目印を表示します
        /// </summary>
        /// <param name="target">暫定的に移動しているキャラクター</param>
        public void Show( Player target )
        {
            _target = target;

            ref var prevMoveInfo    = ref target.PrevMoveInformaiton;
            int originTileIndex     = prevMoveInfo.tmpParam.CurrentTileIndex;

            // 枠: 移動前のタイルを囲む(枠はこのオブジェクトの子のため、表示・非表示はこのオブジェクトに連動する)
            Vector3 outlineCenter = _stageCtrl.GetTileStaticData( originTileIndex ).CursorStandPos + Vector3.up * OUTLINE_HEIGHT;
            float half            = TILE_SIZE * OUTLINE_SIZE_RATE * 0.5f;
            _outlineRenderer.SetPositions( new Vector3[]
            {
                outlineCenter + new Vector3( -half, 0f, -half ),
                outlineCenter + new Vector3( -half, 0f,  half ),
                outlineCenter + new Vector3(  half, 0f,  half ),
                outlineCenter + new Vector3(  half, 0f, -half ),
            } );

            // 残像: 移動前のタイルに、移動前の向きで配置する
            if( _afterimageSource != target ) { RecreateAfterimage( target ); }
            _afterimage.transform.SetPositionAndRotation( _stageCtrl.GetTileStaticData( originTileIndex ).CharaStandPos, prevMoveInfo.rotDir );
            _afterimage.gameObject.SetActive( true );

            // 矢印: 移動前の位置から現在の位置までに実際に通った経路に沿って配置する(経路を保持していない移動の場合は残像のみ)。
            // 色は移動プレビュー時と同じくキャラクター毎の色とし、どのキャラクターの経路かを判別できるようにする
            IReadOnlyList<WaypointInformation> movedPath = prevMoveInfo.movedPath;
            if( null != movedPath && 0 < movedPath.Count )
            {
                _arrowPlacer.Init( target.GetCharacterKey() );
                _arrowPlacer.PlaceArrows( ActionRangeController.BuildPathArrowEntries( originTileIndex, movedPath, _stageCtrl.GetGridNumsXZ().Item2 ) );
            }
            else
            {
                _arrowPlacer.ClearArrows();
            }

            gameObject.SetActive( true );
        }

        /// <summary>
        /// 目印を非表示にします
        /// </summary>
        public void Hide()
        {
            _target = null;
            _arrowPlacer?.ClearArrows();
            if( null != _afterimage ) { _afterimage.gameObject.SetActive( false ); }

            gameObject.SetActive( false );
        }

        private void Update()
        {
            // 攻撃・スキル・待機の実行、ターン終了、キャンセルによる巻き戻し等で暫定状態でなくなった場合は非表示にする
            if( null == _target || !_target.IsProvisionallyMoved() )
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            _arrowPlacer?.ClearArrows();
            CleanupAfterimage();
            if( null != _outlineMaterial ) { Destroy( _outlineMaterial ); }
        }

        /// <summary>
        /// 移動前のタイルを囲む枠を描画するLineRendererを生成します
        /// </summary>
        private void CreateOutlineRenderer()
        {
            if( null != _outlineRenderer ) { return; }

            var child = new GameObject( "Outline" );
            child.transform.SetParent( transform, false );
            // TransformZ整列時に線の幅が地面(XZ平面)上に広がるよう、ローカルZ軸を真上へ向ける
            child.transform.rotation = Quaternion.Euler( 90f, 0f, 0f );

            _outlineMaterial = new Material( Shader.Find( "Sprites/Default" ) );

            _outlineRenderer                    = child.AddComponent<LineRenderer>();
            _outlineRenderer.useWorldSpace      = true;
            _outlineRenderer.loop               = true;
            _outlineRenderer.positionCount      = 4;
            _outlineRenderer.sharedMaterial     = _outlineMaterial;
            _outlineRenderer.widthMultiplier    = OUTLINE_WIDTH;
            _outlineRenderer.startColor         = OUTLINE_COLOR;
            _outlineRenderer.endColor           = OUTLINE_COLOR;
            _outlineRenderer.alignment          = LineAlignment.TransformZ;
            _outlineRenderer.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            _outlineRenderer.receiveShadows     = false;
            _outlineRenderer.numCornerVertices  = 2;
        }

        /// <summary>
        /// 指定キャラクターの現在の姿から残像を生成し直します
        /// </summary>
        private void RecreateAfterimage( Player source )
        {
            CleanupAfterimage();

            _afterimage         = GhostObject.Create( source.transform, source.gameObject.name, GhostObject.Style.Afterimage );
            _afterimageSource   = source;
        }

        private void CleanupAfterimage()
        {
            if( null != _afterimage ) { _afterimage.Cleanup(); }

            _afterimage         = null;
            _afterimageSource   = null;
        }
    }
}
