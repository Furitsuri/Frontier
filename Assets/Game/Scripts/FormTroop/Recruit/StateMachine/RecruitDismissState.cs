using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.TroopEdit;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 「解雇」選択時に表示する自軍メンバー一覧グリッド画面。
    /// FieldSceneの部隊編集画面(TroopEditHandler/TroopEditPresenter/TroopEditUI)が持つ
    /// グリッド表示・カーソル移動・ステータス表示位置調整のロジックをそのまま(Presenter/Viewは共有、
    /// カーソル移動やキャラクターのライフサイクルはState木の作法に合わせて移植)利用する。
    /// </summary>
    public sealed class RecruitDismissState : RecruitPhaseStateBase
    {
        private enum RecruitDismissTransitTag
        {
            CONFIRM_DISMISS = 0,
        }

        [Inject] private UserDomain _userDomain             = null;
        [Inject] private CharacterFactory _characterFactory = null;

        private TroopEditPresenter _troopEditPresenter             = null;
        private CharacterParameterPresenter _paramPresenter        = null;
        private List<Character> _spawnedCharacters = new List<Character>();
        private List<int> _rewardAnimas            = new List<int>();
        private int _selectedIndex        = 0;
        private int _pendingDismissIndex  = -1;  // 確認Stateからリクエストされた解雇対象(-1は未リクエスト)

        public override void Init( object context )
        {
            base.Init( context );

            LazyInject.GetOrCreate( ref _troopEditPresenter, () => _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false ) );
            _troopEditPresenter.Init();
            _troopEditPresenter.SetTitleKey( LocKey.UI_CMD_DISMISS );

            LazyInject.GetOrCreate( ref _paramPresenter, () => _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _troopEditPresenter.CharacterParamUI, false }, false ) );
            _paramPresenter.Init();

            _pendingDismissIndex = -1;
            _selectedIndex       = 0;

            RefreshRoster();
        }

        public override object ExitState()
        {
            _troopEditPresenter.Hide();
            _troopEditPresenter.ClearMembers();
            _paramPresenter.ClearCharacter();

            DestroySpawnedCharacters();

            return base.ExitState();
        }

        /// <summary>
        /// 解雇確認画面からキャンセル(NO)で戻ってきた際、または解雇確定(YES)後の一覧再構築のために
        /// 表示を復帰します。RequestDismiss()でリクエストが来ていれば、ここで実際の解雇処理を行います。
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            if( 0 <= _pendingDismissIndex )
            {
                int reward = _rewardAnimas[_pendingDismissIndex];
                _userDomain.AddAnima( reward );
                _userDomain.DismissMember( _pendingDismissIndex );

                _pendingDismissIndex = -1;
            }

            RefreshRoster();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "SELECT",  CanAcceptDefault, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,    "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
               (GuideIcon.CANCEL,     "BACK",    CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode)
            );
        }

        protected override bool CanAcceptConfirm()
        {
            return _spawnedCharacters.Count > 0;
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return MoveSelection( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // 解雇対象・報酬額を確認Stateへ渡す(解雇確定はRestartState側で行う)
            SetSendTransitionContext( _selectedIndex );
            TransitState( ( int ) RecruitDismissTransitTag.CONFIRM_DISMISS );

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }

        /// <summary>
        /// 確認Stateから、指定インデックスのメンバーの解雇を要求します。
        /// 実際の解雇処理(除名+アニマ加算)はRestartState()で行います。
        /// </summary>
        public void RequestDismiss( int index )
        {
            _pendingDismissIndex = index;
        }

        /// <summary>
        /// 選択中の報酬アニマ量を返します(確認画面のメッセージ表示等に使う想定)。
        /// </summary>
        public int GetRewardAnima( int index ) => _rewardAnimas[index];

        /// <summary>
        /// UserDomain.Membersから表示用キャラクターを再生成し、グリッド・ヘッダー・
        /// ステータスパネルを最新の状態に更新します。
        /// </summary>
        private void RefreshRoster()
        {
            DestroySpawnedCharacters();

            var members = _userDomain.Members;
            _rewardAnimas.Clear();

            for( int i = 0; i < members.Count; ++i )
            {
                var chara = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, members[i] );

                // 雇用候補キャラクター(CharacterCandidate)が同シーン内でCHARACTER_SELECTION_OFFSET_Zの
                // オフスクリーン待機位置を使い続けているため、座標が重ならないよう専用のZ座標を使う
                var reservePos = new Vector3( CHARACTER_SELECTION_SPACING_X * i, CHARACTER_SELECTION_OFFSET_Y, DISMISS_CHARACTER_OFFSET_Z );
                chara.SetPosition( reservePos );

                _spawnedCharacters.Add( chara );

                // MEMO : 解雇報酬の仕様は未確定のため、暫定的に1〜20のランダム値とする
                _rewardAnimas.Add( Random.Range( 1, 21 ) );
            }

            _selectedIndex = Mathf.Clamp( _selectedIndex, 0, Mathf.Max( 0, _spawnedCharacters.Count - 1 ) );

            _troopEditPresenter.Show();
            _troopEditPresenter.DisplayMembers( _spawnedCharacters );

            for( int i = 0; i < _rewardAnimas.Count; ++i )
            {
                _troopEditPresenter.SetRewardAnima( i, _rewardAnimas[i] );
            }

            _troopEditPresenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );
            _troopEditPresenter.SetHeaderInfo( _userDomain.Anima, _userDomain.Members.Count, TROOP_MAX_MEMBERS );

            RefreshCharacterParamDisplay();
        }

        /// <summary>
        /// 選択中キャラクターのパラメータ表示を更新し、カーソル・キャラクターと重ならない位置へ再配置します。
        /// </summary>
        private void RefreshCharacterParamDisplay()
        {
            if( _spawnedCharacters.Count == 0 ) { _paramPresenter.ClearCharacter(); return; }

            var character = _spawnedCharacters[_selectedIndex];
            _paramPresenter.AssignCharacter( character, LAYER_MASK_INDEX_CHARACTER );
            _paramPresenter.SetActive( true );

            var status = character.GetStatusRef;
            _troopEditPresenter.SetCharacterParamName( $"Lv.{status.Level}  {status.Name}" );
            _troopEditPresenter.SetCharacterParamCorner( _spawnedCharacters.Count - 1 );
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。TroopEditHandler.MoveSelectionと同じ規約
        /// (上下は同じ列を維持したまま前後の行へ、左右は行をまたいだ連続的な並びとして折り返す)。
        /// </summary>
        private bool MoveSelection( Direction dir )
        {
            int count = _spawnedCharacters.Count;
            if( count <= 1 ) { return false; }

            int newIndex;
            switch( dir )
            {
                case Direction.LEFT:
                    newIndex = ( _selectedIndex - 1 + count ) % count;
                    break;

                case Direction.RIGHT:
                    newIndex = ( _selectedIndex + 1 ) % count;
                    break;

                case Direction.FORWARD:
                    newIndex = MoveRow( _selectedIndex, count, -1 );
                    break;

                case Direction.BACK:
                    newIndex = MoveRow( _selectedIndex, count, 1 );
                    break;

                default:
                    return false;
            }

            _selectedIndex = newIndex;
            _troopEditPresenter.SetSelectedIndex( _selectedIndex );
            RefreshCharacterParamDisplay();

            return true;
        }

        /// <summary>
        /// TroopEditHandler.MoveRowと同じ規約で、行方向へ移動した際の新しいインデックスを求めます。
        /// </summary>
        private static int MoveRow( int index, int count, int rowDelta )
        {
            int totalRows = ( count + TROOP_EDIT_GRID_COLUMNS - 1 ) / TROOP_EDIT_GRID_COLUMNS;
            int row = index / TROOP_EDIT_GRID_COLUMNS;
            int col = index % TROOP_EDIT_GRID_COLUMNS;

            int targetRow = ( row + rowDelta + totalRows ) % totalRows;
            int itemsInTargetRow = ( targetRow == totalRows - 1 ) ? count - targetRow * TROOP_EDIT_GRID_COLUMNS : TROOP_EDIT_GRID_COLUMNS;
            int targetCol = Mathf.Min( col, itemsInTargetRow - 1 );

            return targetRow * TROOP_EDIT_GRID_COLUMNS + targetCol;
        }

        private void DestroySpawnedCharacters()
        {
            foreach( var chara in _spawnedCharacters )
            {
                if( chara != null ) { Object.Destroy( chara.gameObject ); }
            }

            _spawnedCharacters.Clear();
        }
    }
}
