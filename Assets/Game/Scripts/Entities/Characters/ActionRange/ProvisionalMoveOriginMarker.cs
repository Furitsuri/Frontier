using System.Collections.Generic;
using Frontier.Registries;
using Frontier.Stage;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// 暫定的に移動した(移動が確定していない)キャラクターの移動前の位置を示す目印です。
    /// 移動前のタイルを汎用のタイル外枠マーカー(TileOutlineMarker)で目立たせた上でキャラクターのモノクロの残像を表示し、
    /// そこから現在の位置までに実際に通った経路を矢印で示します。外枠・矢印はいずれもキャラクター毎の色(CharacterKeyColor)で表示します。
    /// 残像は「過去」の位置を示すため、移動先などの「未来」を示す通常のゴースト(元の色のまま半透明)とは見た目を変えています。
    /// 表示中は毎フレーム対象キャラクターの状態を確認し、確定・巻き戻し等で暫定状態でなくなった時点で自動的に非表示になります。
    /// </summary>
    public class ProvisionalMoveOriginMarker : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;
        [Inject] private PrefabRegistry _prefabReg          = null;

        private TileOutlineMarker _tileMarker         = null;
        private MoveDirectionArrowPlacer _arrowPlacer   = null;
        private GhostObject _afterimage                 = null;
        private Player _afterimageSource                = null;     // 残像の生成元のキャラクター(同じキャラクターであれば残像を使い回す)
        private Player _target                          = null;

        public Player Target => _target;

        /// <summary>
        /// タイルの外枠表示・矢印の配置に用いるインスタンスを生成します
        /// </summary>
        public void Setup()
        {
            LazyInject.GetOrCreate( ref _tileMarker, () => _hierarchyBld.CreateComponentAndOrganize<TileOutlineMarker>( _prefabReg.TileOutlineMarkerPrefab, false ) );
            LazyInject.GetOrCreate( ref _arrowPlacer, () => _hierarchyBld.InstantiateWithDiContainer<MoveDirectionArrowPlacer>( false ) );

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
            var originTileData      = _stageCtrl.GetTileStaticData( originTileIndex );

            // 移動前のタイルを外枠で示す。色は経路の矢印と同じくキャラクター毎の色とし、どのキャラクターの移動前の位置かを判別できるようにする
            _tileMarker.SetColor( CharacterKeyColor.Resolve( target.GetCharacterKey() ) );
            _tileMarker.Show( originTileData.CursorStandPos );

            // 残像: 移動前のタイルに、移動前の向きで配置する
            if( _afterimageSource != target ) { RecreateAfterimage( target ); }
            _afterimage.transform.SetPositionAndRotation( originTileData.CharaStandPos, prevMoveInfo.rotDir );
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
            if( null != _tileMarker ) { _tileMarker.Hide(); }
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
            if( null != _tileMarker ) { Destroy( _tileMarker.gameObject ); }
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
