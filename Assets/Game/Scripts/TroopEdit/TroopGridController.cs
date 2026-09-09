using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊メンバーのグリッド表示・カーソル移動・キャラクターのオフスクリーン生成/破棄・
    /// パラメータパネル位置更新という、TroopEditPresenter/CharacterParameterPresenterを
    /// 操作する側の共通処理をまとめたクラス。TroopEditHandler(部隊編集画面)とRecruitDismissState
    /// (解雇画面)から共通して保持・利用される。Confirm/Cancel時にどう振る舞うか(遷移先や
    /// 入力コードの登録方式)は呼び出し元ごとに異なるため、このクラスの責務には含めない。
    /// </summary>
    public class TroopGridController
    {
        [Inject] private CharacterFactory _characterFactory = null;

        private TroopEditPresenter _troopEditPresenter      = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private List<Character> _spawnedCharacters = new List<Character>();
        private int _selectedIndex = 0;

        public List<Character> SpawnedCharacters => _spawnedCharacters;
        public int SelectedIndex => _selectedIndex;

        /// <summary>
        /// 呼び出し元が生成したPresenterを紐づけます(一度だけ呼び出してください)。
        /// </summary>
        public void Init( TroopEditPresenter troopEditPresenter, CharacterParameterPresenter paramPresenter )
        {
            _troopEditPresenter = troopEditPresenter;
            _paramPresenter      = paramPresenter;
        }

        /// <summary>
        /// membersからオフスクリーンにキャラクターを生成し、グリッド・ヘッダー・パラメータパネルを表示します。
        /// reserveOffsetZは、他の画面が同時に待機させているキャラクター群と座標が重ならないよう、
        /// 呼び出し元ごとに異なる値を指定してください。
        /// </summary>
        public void Show( IReadOnlyList<Status> members, float reserveOffsetZ, int anima )
        {
            BuildCharacters( members, reserveOffsetZ );

            _selectedIndex = Mathf.Clamp( _selectedIndex, 0, Mathf.Max( 0, _spawnedCharacters.Count - 1 ) );

            _troopEditPresenter.Show();
            _troopEditPresenter.DisplayMembers( _spawnedCharacters );
            _troopEditPresenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );
            _troopEditPresenter.SetHeaderInfo( anima, members.Count, TROOP_MAX_MEMBERS );

            RefreshCharacterParamDisplay();
        }

        /// <summary>
        /// グリッド・パラメータパネルを非表示にし、生成済みキャラクターを破棄します。
        /// </summary>
        public void Close()
        {
            _troopEditPresenter.Hide();
            _troopEditPresenter.ClearMembers();
            _paramPresenter.ClearCharacter();

            DestroySpawnedCharacters();
        }

        /// <summary>
        /// 指定インデックスのキャラクターだけを取り除きます(解雇確定時等)。Show()と異なり、
        /// 残りのキャラクターのGameObjectは破棄・再生成しないため、再生中のアニメーションが
        /// 途切れません(グリッドのセルUI自体はGridLayoutGroupの再配置のため再生成されます)。
        /// </summary>
        public void RemoveCharacterAt( int index, int anima )
        {
            if ( _spawnedCharacters[index] != null ) { Object.Destroy( _spawnedCharacters[index].gameObject ); }
            _spawnedCharacters.RemoveAt( index );

            _selectedIndex = Mathf.Clamp( _selectedIndex, 0, Mathf.Max( 0, _spawnedCharacters.Count - 1 ) );

            _troopEditPresenter.DisplayMembers( _spawnedCharacters );
            _troopEditPresenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );
            _troopEditPresenter.SetHeaderInfo( anima, _spawnedCharacters.Count, TROOP_MAX_MEMBERS );

            RefreshCharacterParamDisplay();
        }

        /// <summary>
        /// 外部要因(CharacterEdit画面からの復帰等)で選択位置が変わった場合に反映します。
        /// </summary>
        public void SetSelectedIndex( int index )
        {
            _selectedIndex = index;
            _troopEditPresenter.SetSelectedIndex( _selectedIndex );
            RefreshCharacterParamDisplay();
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// 上下は同じ列を維持したまま前後の行へ(移動先の行の要素数が足りない場合は末尾要素へ丸める)、
        /// 左右は行をまたいだ連続的な並びとして折り返します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
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
        /// 選択中キャラクターのパラメータ表示を更新し、カーソル・キャラクターと重ならない位置へ再配置します。
        /// </summary>
        private void RefreshCharacterParamDisplay()
        {
            if ( _spawnedCharacters.Count == 0 ) { _paramPresenter.ClearCharacter(); return; }

            var character = _spawnedCharacters[_selectedIndex];
            _paramPresenter.AssignCharacter( character, LAYER_MASK_INDEX_CHARACTER );
            _paramPresenter.SetActive( true );

            var status = character.GetStatusRef;
            _troopEditPresenter.SetCharacterParamName( $"Lv.{status.Level}  {status.Name}" );
            _troopEditPresenter.SetCharacterParamCorner( _spawnedCharacters.Count - 1 );
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
            foreach ( var chara in _spawnedCharacters )
            {
                if ( chara != null ) { Object.Destroy( chara.gameObject ); }
            }

            _spawnedCharacters.Clear();
        }
    }
}
