using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊メンバーのグリッド表示・カーソル移動・キャラクターのオフスクリーン生成/破棄・
    /// 選択行に追従するグリッドのスクロール制御・完了確認画面遷移時のチェック済みキャラクター
    /// 絞り込み表示という、TroopEditPresenter/CharacterParameterPresenterを操作する側の共通処理を
    /// まとめたクラス。TroopEditHandler(部隊編集画面)・RecruitEmployState(雇用画面)・
    /// RecruitDismissState(解雇画面)から共通して保持・利用される。選択中キャラクター
    /// (SelectedCharacter)もここで一元管理するため、呼び出し元はステータス確認等の対象キャラクター
    /// 取得をこのクラスに委譲できる。Confirm/Cancel時にどう振る舞うか(遷移先や入力コードの登録方式)
    /// は呼び出し元ごとに異なるため、このクラスの責務には含めない。
    /// </summary>
    public class TroopGridController
    {
        [Inject] private CharacterFactory _characterFactory = null;

        private TroopEditPresenter _troopEditPresenter      = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private List<Character> _spawnedCharacters = new List<Character>();
        private int _selectedIndex = 0;
        // 現在ビューポートの先頭に表示している行インデックス(スクロール位置)
        private int _topRow = 0;
        // 完了確認画面表示中のみ非null。チェック済み(表示中)キャラクターの、_spawnedCharactersに
        // おけるインデックス一覧。MoveSelection()はnullでない間、このサブセット内のみを対象に移動する
        private List<int> _confirmModeIndices = null;
        // Show()で自ら生成した場合はtrue(Close時に破棄する)、ShowExisting()で
        // 呼び出し元から借りているだけの場合はfalse(Close時に破棄してはいけない)
        private bool _ownsSpawnedCharacters = true;

        public List<Character> SpawnedCharacters => _spawnedCharacters;
        public int SelectedIndex => _selectedIndex;

        /// <summary>
        /// 現在選択中のキャラクターを返します(未表示の場合はnull)。ステータス確認画面等、
        /// 選択中キャラクターを対象に取る機能はこのプロパティ経由で参照してください。
        /// </summary>
        public Character SelectedCharacter => _spawnedCharacters.Count > 0 ? _spawnedCharacters[_selectedIndex] : null;

        /// <summary>
        /// 呼び出し元が生成したPresenterを紐づけます(一度だけ呼び出してください)。
        /// </summary>
        public void Init( TroopEditPresenter troopEditPresenter, CharacterParameterPresenter paramPresenter )
        {
            _troopEditPresenter = troopEditPresenter;
            _paramPresenter      = paramPresenter;
        }

        /// <summary>
        /// membersからオフスクリーンにキャラクターを生成し、グリッド・パラメータパネルを表示します。
        /// reserveOffsetZは、他の画面が同時に待機させているキャラクター群と座標が重ならないよう、
        /// 呼び出し元ごとに異なる値を指定してください。
        /// </summary>
        public void Show( IReadOnlyList<Status> members, float reserveOffsetZ )
        {
            _ownsSpawnedCharacters = true;

            BuildCharacters( members, reserveOffsetZ );

            _selectedIndex = Mathf.Clamp( _selectedIndex, 0, Mathf.Max( 0, _spawnedCharacters.Count - 1 ) );
            _topRow = 0;

            _troopEditPresenter.Show();
            _troopEditPresenter.DisplayMembers( _spawnedCharacters );
            _troopEditPresenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );

            RefreshCharacterParamDisplay();
            UpdateScroll( false );
        }

        /// <summary>
        /// 既に生成・配置済みのキャラクター一覧をそのままグリッドに表示します(雇用候補選択画面等、
        /// 呼び出し元が既にキャラクターを所有・管理している場合専用)。Show()と異なり生成・再配置は
        /// 行わず、Close()時にもGameObjectを破棄しません(所有権は呼び出し元のまま)。
        /// </summary>
        public void ShowExisting( IReadOnlyList<Character> characters )
        {
            _ownsSpawnedCharacters = false;

            _spawnedCharacters.Clear();
            _spawnedCharacters.AddRange( characters );

            _selectedIndex = Mathf.Clamp( _selectedIndex, 0, Mathf.Max( 0, _spawnedCharacters.Count - 1 ) );
            _topRow = 0;

            _troopEditPresenter.Show();
            _troopEditPresenter.DisplayMembers( _spawnedCharacters );
            _troopEditPresenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );

            RefreshCharacterParamDisplay();
            UpdateScroll( false );
        }

        /// <summary>
        /// グリッド・パラメータパネルを非表示にします。Show()経由で自ら生成したキャラクターは
        /// 破棄しますが、ShowExisting()経由で借りているだけのキャラクターは破棄せず参照を手放すだけです。
        /// </summary>
        public void Close()
        {
            _troopEditPresenter.Hide();
            _troopEditPresenter.ClearMembers();
            _paramPresenter.ClearCharacter();

            DestroySpawnedCharacters();
        }

        /// <summary>
        /// カーソル・選択中キャラクターのパラメータパネルを一時的に非表示にします
        /// (グリッド自体・各セルの表示はそのまま。突入時の会話ウィンドウ表示中など専用)。
        /// </summary>
        public void HideSelectionDisplay()
        {
            _troopEditPresenter.SetSelectedIndex( -1 );
            _paramPresenter.SetActive( false );
        }

        /// <summary>
        /// HideSelectionDisplay()で隠したカーソル・パラメータパネルを再表示します。
        /// </summary>
        public void ShowSelectionDisplay()
        {
            _troopEditPresenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );
            RefreshCharacterParamDisplay();
            UpdateScroll( false );
        }

        /// <summary>
        /// 外部要因(CharacterEdit画面からの復帰等)で選択位置が変わった場合に反映します。
        /// </summary>
        public void SetSelectedIndex( int index )
        {
            _selectedIndex = index;
            _troopEditPresenter.SetSelectedIndex( _selectedIndex );
            RefreshCharacterParamDisplay();
            UpdateScroll( false );
        }

        /// <summary>
        /// 雇用/解雇完了確認画面へ入る際に呼ばれます。チェック済みのキャラクターのみに絞り込んで
        /// アニメーションつきで詰め直し、カーソル・パラメータパネルを絞り込み後の先頭キャラクターに
        /// 合わせた上で、パラメータパネルを画面下部左側へアニメーション移動します。すべてのアニメーション
        /// (グリッド絞り込み・パネル移動)が完了した時点でonCompleteを呼びます。
        /// </summary>
        /// <param name="checkedIndices">チェック済みキャラクターの、現在の表示上のインデックス一覧</param>
        public void AnimateFocusOnConfirmScreen( List<int> checkedIndices, System.Action onComplete )
        {
            _confirmModeIndices = checkedIndices;
            _selectedIndex = checkedIndices.Count > 0 ? checkedIndices[0] : 0;

            _troopEditPresenter.AnimateFilterToChecked( checkedIndices );
            RefreshCharacterParamDisplay();
            _troopEditPresenter.SetPanelHorizontalMode( true, true, onComplete );
        }

        /// <summary>
        /// 雇用/解雇完了確認画面から戻る際(キャンセル・決定いずれの場合も)に呼ばれます。
        /// 絞り込みで非表示にしていたキャラクターを再表示し、その時点の_spawnedCharacters
        /// (決定時はCommit*()でSyncSpawnedCharacters()済み)に存在しないキャラクターのセルは
        /// 破棄した上で、アニメーションつきで元の配置に戻します。パラメータパネルも
        /// 画面下部中央へアニメーション移動し、完了した時点でonCompleteを呼びます
        /// (呼び出し元はこれを見て、復元アニメーション完了まで入力をブロックし続けます)。
        /// </summary>
        public void RestoreFromConfirmScreen( System.Action onComplete )
        {
            _confirmModeIndices = null;

            _troopEditPresenter.AnimateRestoreDisplay( _spawnedCharacters );

            _selectedIndex = 0;
            RefreshCharacterParamDisplay();
            UpdateScroll( false );

            _troopEditPresenter.SetPanelHorizontalMode( false, true, onComplete );
        }

        /// <summary>
        /// 雇用/解雇完了確認画面での決定(Commit*())によって変化した後のキャラクター一覧を、
        /// 表示の再構築(DisplayMembers等)を伴わずに反映します。実際の表示更新は、この直後に
        /// 呼ばれるRestoreFromConfirmScreen()のアニメーションに委ねます。
        /// </summary>
        public void SyncSpawnedCharacters( IReadOnlyList<Character> updatedCharacters )
        {
            _spawnedCharacters.Clear();
            _spawnedCharacters.AddRange( updatedCharacters );
        }

        /// <summary>
        /// 指定インデックスのキャラクターのGameObjectを破棄し、_spawnedCharactersから取り除きます
        /// (解雇確定時等)。表示の再構築(DisplayMembers)は行わないため、呼び出し元がこの直後に
        /// RestoreFromConfirmScreen()等で表示を更新する必要があります。
        /// </summary>
        /// <param name="indices">破棄するインデックス一覧(降順である必要はありません)</param>
        public void RemoveCharactersWithoutRebuild( List<int> indices )
        {
            var sorted = new List<int>( indices );
            sorted.Sort();

            for( int k = sorted.Count - 1; k >= 0; --k )
            {
                int index = sorted[k];
                if ( _spawnedCharacters[index] != null ) { Object.Destroy( _spawnedCharacters[index].gameObject ); }
                _spawnedCharacters.RemoveAt( index );
            }
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// 上下は同じ列を維持したまま前後の行へ(移動先の行の要素数が足りない場合は末尾要素へ丸める)、
        /// 左右は行をまたいだ連続的な並びとして折り返します。完了確認画面表示中(_confirmModeIndices
        /// がnullでない間)は、非表示中(チェックされていない)キャラクターを対象から除外し、
        /// 表示中のキャラクターのみを対象に同じロジックで移動します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
            if ( _confirmModeIndices != null ) { return MoveSelectionWithinConfirmSubset( dir ); }

            int count = _spawnedCharacters.Count;
            if ( count <= 1 ) { return false; }

            int newIndex;
            switch ( dir )
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
            UpdateScroll( true );

            return true;
        }

        /// <summary>
        /// 完了確認画面表示中のカーソル移動。_confirmModeIndices(表示中キャラクターの
        /// _spawnedCharactersにおけるインデックス一覧)自体を、詰め直し後の見た目通りの並びとみなし、
        /// その中でMoveSelection()と同じ行/列ロジックを適用した上で、元のインデックスへ変換します。
        /// </summary>
        private bool MoveSelectionWithinConfirmSubset( Direction dir )
        {
            int count = _confirmModeIndices.Count;
            if ( count <= 1 ) { return false; }

            int currentPos = _confirmModeIndices.IndexOf( _selectedIndex );
            if ( currentPos < 0 ) { currentPos = 0; }

            int newPos;
            switch ( dir )
            {
                case Direction.LEFT:
                    newPos = ( currentPos - 1 + count ) % count;
                    break;

                case Direction.RIGHT:
                    newPos = ( currentPos + 1 ) % count;
                    break;

                case Direction.FORWARD:
                    newPos = MoveRow( currentPos, count, -1 );
                    break;

                case Direction.BACK:
                    newPos = MoveRow( currentPos, count, 1 );
                    break;

                default:
                    return false;
            }

            _selectedIndex = _confirmModeIndices[newPos];
            _troopEditPresenter.SetSelectedIndex( _selectedIndex );
            RefreshCharacterParamDisplay();

            return true;
        }

        /// <summary>
        /// 現在の選択位置から、行方向へ(rowDeltaが1なら次行、-1なら前行)移動した際の
        /// 新しいインデックスを求めます。最上行/最下行を超える場合は逆側の行へ折り返し、
        /// 移動先の行に同じ列の要素が存在しない場合はその行の末尾要素に丸めます。
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

        /// <summary>
        /// 選択中キャラクターのパラメータ表示を更新します(パネル位置は下部中央に固定済みのため、
        /// ここでは再配置は行いません)。
        /// </summary>
        private void RefreshCharacterParamDisplay()
        {
            if ( _spawnedCharacters.Count == 0 ) { _paramPresenter.ClearCharacter(); return; }

            var character = _spawnedCharacters[_selectedIndex];
            _paramPresenter.AssignCharacter( character, LAYER_MASK_INDEX_CHARACTER );
            _paramPresenter.SetActive( true );

            var status = character.GetStatusRef;
            _troopEditPresenter.SetCharacterParamName( $"Lv.{status.Level}  {status.Name}" );
        }

        /// <summary>
        /// 選択中の行が常時表示範囲(TROOP_EDIT_VISIBLE_ROWS行)に収まるよう、表示先頭行(_topRow)を
        /// 更新し、ビューポートをスクロールします。選択行が表示範囲より下にある場合はその行が
        /// 最下段に来るまで、上にある場合はその行が最上段に来るまでスクロールします。
        /// </summary>
        private void UpdateScroll( bool animate )
        {
            int count = _spawnedCharacters.Count;
            int totalRows = ( count + TROOP_EDIT_GRID_COLUMNS - 1 ) / TROOP_EDIT_GRID_COLUMNS;
            int maxTopRow = Mathf.Max( 0, totalRows - TROOP_EDIT_VISIBLE_ROWS );
            int selectedRow = _selectedIndex / TROOP_EDIT_GRID_COLUMNS;

            if ( selectedRow < _topRow ) { _topRow = selectedRow; }
            else if ( selectedRow > _topRow + TROOP_EDIT_VISIBLE_ROWS - 1 ) { _topRow = selectedRow - TROOP_EDIT_VISIBLE_ROWS + 1; }

            _topRow = Mathf.Clamp( _topRow, 0, maxTopRow );

            _troopEditPresenter.ScrollToRow( _topRow, animate );
        }

        /// <summary>
        /// membersの並び順のまま、表示用のCharacterを生成します。3Dモデルはフィールド上には配置せず、
        /// 他の表示から干渉されないオフスクリーン座標に個別に配置します。
        /// </summary>
        private void BuildCharacters( IReadOnlyList<Status> members, float reserveOffsetZ )
        {
            DestroySpawnedCharacters();

            for ( int i = 0; i < members.Count; ++i )
            {
                var chara = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, members[i] );

                var reservePos = new Vector3( CHARACTER_SELECTION_SPACING_X * i, CHARACTER_SELECTION_OFFSET_Y, reserveOffsetZ );
                chara.SetPosition( reservePos );

                _spawnedCharacters.Add( chara );
            }
        }

        private void DestroySpawnedCharacters()
        {
            if ( _ownsSpawnedCharacters )
            {
                foreach ( var chara in _spawnedCharacters )
                {
                    if ( chara != null ) { Object.Destroy( chara.gameObject ); }
                }
            }

            _spawnedCharacters.Clear();
        }
    }
}
