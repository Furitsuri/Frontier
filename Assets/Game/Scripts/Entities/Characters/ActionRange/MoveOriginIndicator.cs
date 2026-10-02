using System.Collections.Generic;
using Frontier.Stage;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターの「移動前の位置」を示す表示(移動前のタイルに置くモノクロの残像と、そこからの経路を示す矢印)をまとめて扱います。
    /// 単体移動の操作中(PlMoveState)と、移動後の暫定移動状態の目印(ProvisionalMoveOriginMarker)の双方で共通して使用し、
    /// どちらの場面でもユーザーからの見え方が同じになるようにしています。
    /// 残像は「過去」の位置を示すため、移動先などの「未来」を示す通常のゴースト(元の色のまま半透明)とは見た目を変えています。
    /// 経路の矢印は、グループ移動のプレビューと同じ方式・同じキャラクター毎の色で表示します。
    /// 生成はHierarchyBuilderBase.InstantiateWithDiContainerで行い、不要になった際はDisposeを呼び出してください。
    /// </summary>
    public class MoveOriginIndicator
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;

        private MoveDirectionArrowPlacer _arrowPlacer   = null;
        private GhostObject _afterimage                 = null;
        private Player _afterimageSource                = null;     // 残像の生成元のキャラクター(同じキャラクターであれば残像を使い回す)
        private Player _target                          = null;
        private int _originTileIndex                    = -1;

        public Player Target => _target;
        public bool IsShowing => ( null != _target );

        /// <summary>
        /// 指定キャラクターの移動前の位置の表示を開始します。
        /// 移動前のタイル・向きは、キャラクターが保持している移動前情報(HoldBeforeMoveInfoで保存されたもの)を参照します。
        /// 経路の矢印は別途SetPathで指定してください。
        /// </summary>
        /// <param name="target">対象のキャラクター</param>
        /// <param name="isAfterimageVisible">残像を表示するか(実体が移動前のタイルに立っている場合などはfalseを指定する)</param>
        public void Show( Player target, bool isAfterimageVisible )
        {
            LazyInject.GetOrCreate( ref _arrowPlacer, () => _hierarchyBld.InstantiateWithDiContainer<MoveDirectionArrowPlacer>( false ) );

            _target = target;

            ref var prevMoveInfo    = ref target.PrevMoveInformaiton;
            _originTileIndex        = prevMoveInfo.tmpParam.CurrentTileIndex;

            // 残像: 移動前のタイルに、移動前の向きで配置する
            if( _afterimageSource != target || null == _afterimage ) { RecreateAfterimage( target ); }
            _afterimage.transform.SetPositionAndRotation( _stageCtrl.GetTileStaticData( _originTileIndex ).CharaStandPos, prevMoveInfo.rotDir );
            _afterimage.gameObject.SetActive( isAfterimageVisible );

            // 矢印の色は移動プレビュー時と同じくキャラクター毎の色とし、どのキャラクターの経路かを判別できるようにする
            _arrowPlacer.Init( target.GetCharacterKey() );
            _arrowPlacer.ClearArrows();
        }

        /// <summary>
        /// 移動前のタイルから伸びる経路の矢印を更新します。
        /// </summary>
        /// <param name="path">移動前のタイルを含まない、目的地までの経路。nullまたは空の場合は矢印を消去します</param>
        public void SetPath( IReadOnlyList<WaypointInformation> path )
        {
            if( !IsShowing ) { return; }

            if( null != path && 0 < path.Count )
            {
                _arrowPlacer.PlaceArrows( ActionRangeController.BuildPathArrowEntries( _originTileIndex, path, _stageCtrl.GetGridNumsXZ().Item2 ) );
            }
            else
            {
                _arrowPlacer.ClearArrows();
            }
        }

        /// <summary>
        /// 残像の表示・非表示を切り替えます
        /// </summary>
        public void SetAfterimageVisible( bool isVisible )
        {
            if( !IsShowing || null == _afterimage ) { return; }

            if( _afterimage.gameObject.activeSelf != isVisible ) { _afterimage.gameObject.SetActive( isVisible ); }
        }

        /// <summary>
        /// 表示を終了します(残像は次回の同じキャラクターの表示で使い回すため破棄せず非表示にします)
        /// </summary>
        public void Hide()
        {
            _target = null;
            _arrowPlacer?.ClearArrows();
            if( null != _afterimage ) { _afterimage.gameObject.SetActive( false ); }
        }

        /// <summary>
        /// 生成した残像・矢印を破棄します
        /// </summary>
        public void Dispose()
        {
            _target = null;
            _arrowPlacer?.ClearArrows();
            CleanupAfterimage();
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
