using Frontier.Registries;
using Frontier.Stage;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// 暫定的に移動した(移動が確定していない)キャラクターの移動前の位置を示す目印です。
    /// 移動前のタイルを汎用のタイル外枠マーカー(TileOutlineMarker)で目立たせた上で、移動前の位置の表示(MoveOriginIndicator:
    /// モノクロの残像と、そこから現在の位置までの経路の矢印)を行います。外枠・矢印はいずれもキャラクター毎の色(CharacterKeyColor)で表示します。
    /// 残像・矢印の表示は、単体移動の操作中(PlMoveState)と同じMoveOriginIndicatorを用いるため、移動中と移動後で見え方が揃います。
    /// 表示中は毎フレーム対象キャラクターの状態を確認し、確定・巻き戻し等で暫定状態でなくなった時点で自動的に非表示になります。
    /// </summary>
    public class ProvisionalMoveOriginMarker : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;
        [Inject] private PrefabRegistry _prefabReg          = null;

        private TileOutlineMarker _tileMarker       = null;
        private MoveOriginIndicator _originIndicator = null;

        public Player Target => _originIndicator?.Target;

        /// <summary>
        /// タイルの外枠表示・移動前の位置の表示に用いるインスタンスを生成します
        /// </summary>
        public void Setup()
        {
            LazyInject.GetOrCreate( ref _tileMarker, () => _hierarchyBld.CreateComponentAndOrganize<TileOutlineMarker>( _prefabReg.TileOutlineMarkerPrefab, false ) );
            LazyInject.GetOrCreate( ref _originIndicator, () => _hierarchyBld.InstantiateWithDiContainer<MoveOriginIndicator>( false ) );

            Hide();
        }

        /// <summary>
        /// 指定キャラクターの移動前の位置に目印を表示します
        /// </summary>
        /// <param name="target">暫定的に移動しているキャラクター</param>
        public void Show( Player target )
        {
            ref var prevMoveInfo = ref target.PrevMoveInformaiton;

            // 移動前のタイルを外枠で示す。色は経路の矢印と同じくキャラクター毎の色とし、どのキャラクターの移動前の位置かを判別できるようにする
            _tileMarker.SetColor( CharacterKeyColor.Resolve( target.GetCharacterKey() ) );
            _tileMarker.Show( _stageCtrl.GetTileStaticData( prevMoveInfo.tmpParam.CurrentTileIndex ).CursorStandPos );

            // 残像と、移動前の位置から現在の位置までの経路(移動完了時に保持した最短経路)の矢印を表示する
            _originIndicator.Show( target, true );
            _originIndicator.SetPath( prevMoveInfo.movedPath );

            gameObject.SetActive( true );
        }

        /// <summary>
        /// 目印を非表示にします
        /// </summary>
        public void Hide()
        {
            _originIndicator?.Hide();
            if( null != _tileMarker ) { _tileMarker.Hide(); }

            gameObject.SetActive( false );
        }

        private void Update()
        {
            // 攻撃・スキル・待機の実行、ターン終了、キャンセルによる巻き戻し等で暫定状態でなくなった場合は非表示にする
            var target = Target;
            if( null == target || !target.IsProvisionallyMoved() )
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            _originIndicator?.Dispose();
            if( null != _tileMarker ) { Destroy( _tileMarker.gameObject ); }
        }
    }
}
