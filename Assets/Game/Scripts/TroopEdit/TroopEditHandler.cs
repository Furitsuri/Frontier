using Frontier.CharacterEdit;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面の入力受付・選択状態・キャラクターデータのライフサイクルを担当するハンドラ。
    /// フィールドに限らず、複数のシーン・呼び出し元から共通して開かれる可能性があるため、
    /// 特定のシーンに紐づかない独立したクラスとしている(SaveLoadHandlerと同様)。
    /// 呼び出し元からShow()で開かれ、閉じた際はコールバックで呼び出し元へ通知する。
    /// 表示用のCharacterや選択インデックスといった実データはここが保持し、Presenterは
    /// Viewへの単純な転送のみを行う(将来のレベルアップ画面遷移等で、選択中のCharacterを
    /// Handlerが直接扱えるようにするため)。
    /// </summary>
    public class TroopEditHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain = null;
        [Inject] private CharacterFactory _characterFactory = null;

        private TroopEditPresenter _presenter = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private CharacterEditHandler _characterEditHandler = null;
        private List<Character> _spawnedCharacters = new List<Character>();
        private int _selectedIndex = 0;
        private Action _onClosed = null;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Setup()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false );
            _presenter.Init();

            // isNeedCamera:false ... 選択中キャラクターの3Dモデル描画は行わない(枠外の一覧グリッド側で表示済みのため)
            _paramPresenter = _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _presenter.CharacterParamUI, false }, false );
            _paramPresenter.Init();
        }

        /// <summary>
        /// 部隊編集画面を開き、入力コードの受付を開始します。
        /// カーソルは先頭(左上)のキャラクターにリセットされます。
        /// </summary>
        /// <param name="onClosed">画面を閉じた際に呼ばれるコールバック(呼び出し元がメニューへ戻る処理を行う)</param>
        public void Show( Action onClosed )
        {
            _onClosed = onClosed;

            BuildTroopCharacters();
            _selectedIndex = 0;

            // 先にパネルをアクティブ化してから並べる。非アクティブな階層ではGridLayoutGroupの
            // レイアウト計算が行われず、セルの位置が未確定のままカーソル位置をコピーしてしまうため。
            _presenter.Show();
            _presenter.DisplayMembers( _spawnedCharacters );
            _presenter.SetSelectedIndex( _spawnedCharacters.Count > 0 ? _selectedIndex : -1 );
            _presenter.SetHeaderInfo( _userDomain.Money, _userDomain.Members.Count, TROOP_MAX_MEMBERS );
            RefreshCharacterParamDisplay();

            RegisterNavInputCodes();
        }

        /// <summary>
        /// 画面内でのカーソル移動・キャンセルに対応する入力コードを登録します。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( TroopEditHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                new InputCode( GuideIcon.ALL_CURSOR, "SELECT", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, _navHashCode ) { RepeatDelay = DIRECTION_INPUT_REPEAT_DELAY },
                ( GuideIcon.CONFIRM,    "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,     "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode )
            );
        }

        private bool AcceptDirection( InputContext context )
        {
            return MoveSelection( context.Cursor );
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;
            if ( _spawnedCharacters.Count == 0 ) return false;

            OpenCharacterEditScreen();

            return true;
        }

        private void EnsureCharacterEditHandler()
        {
            if ( _characterEditHandler != null ) return;

            _characterEditHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<CharacterEditHandler>( true, false, nameof( CharacterEditHandler ) );
            _characterEditHandler.Setup();
        }

        /// <summary>
        /// 選択中キャラクターの編集画面を開きます。グリッド表示は非表示にし、
        /// 復帰時にL1/R1で切り替わった可能性のある最終的な選択位置を反映します。
        /// </summary>
        private void OpenCharacterEditScreen()
        {
            EnsureCharacterEditHandler();

            _presenter.Hide();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.RegisterInputCodes();

            var context = new CharacterEditContext( _spawnedCharacters, _selectedIndex );
            _characterEditHandler.Show( context, OnCharacterEditClosed );
        }

        private void OnCharacterEditClosed( int finalIndex )
        {
            _selectedIndex = finalIndex;

            _presenter.Show();
            _presenter.SetSelectedIndex( _selectedIndex );
            RefreshCharacterParamDisplay();

            RegisterNavInputCodes();
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// 上下移動は同じ列を維持したまま前後の行へ、最上行/最下行を超える場合は逆側の行へ折り返します
        /// (対象の行の要素数が列数に満たない場合は、その行の末尾要素へ丸めます)。
        /// 左右移動は行をまたいだ連続的な並びとして扱い、先頭列での左入力は1つ上の行の末尾へ、
        /// 末尾列での右入力は1つ下の行の先頭へ移動します(最上行のさらに上は最下行、その逆も同様)。
        /// </summary>
        private bool MoveSelection( Direction dir )
        {
            int count = _spawnedCharacters.Count;
            if ( count <= 1 ) return false;

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
            _presenter.SetSelectedIndex( _selectedIndex );
            RefreshCharacterParamDisplay();

            return true;
        }

        /// <summary>
        /// 選択中キャラクターのパラメータ表示を更新し、画面左側へ再配置します。
        /// カーソルの行に関わらず、常にグリッド最終行の下(入力ガイドバーぎりぎり)への
        /// 配置を優先します。タイトルとグリッド1行目の間はほぼ余白が無いため、
        /// 下側に収まらない場合のみタイトルぎりぎりの上側へ配置します。
        /// </summary>
        private void RefreshCharacterParamDisplay()
        {
            if ( _spawnedCharacters.Count == 0 ) { _paramPresenter.ClearCharacter(); return; }

            var character = _spawnedCharacters[_selectedIndex];
            _paramPresenter.AssignCharacter( character, LAYER_MASK_INDEX_CHARACTER );
            _paramPresenter.SetActive( true );

            // パネルはカーソルから離れた位置に表示されるため、誰の情報かを明示するヘッダーを付ける
            var status = character.GetStatusRef;
            _presenter.SetCharacterParamName( $"Lv.{status.Level}  {status.Name}" );

            _presenter.SetCharacterParamCorner( _spawnedCharacters.Count - 1 );
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

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        private void Close()
        {
            _presenter.Hide();
            _presenter.ClearMembers();
            _paramPresenter.ClearCharacter();

            DestroySpawnedCharacters();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();

            var callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        /// <summary>
        /// UserDomain.Membersの並び順のまま、表示用のCharacterを生成します。
        /// 3Dモデルはフィールド上には配置せず、配置候補キャラクター表示(CharacterSelectionUI)と
        /// 同じ考え方で、他の表示から干渉されないオフスクリーン座標に個別に配置します。
        /// </summary>
        private void BuildTroopCharacters()
        {
            DestroySpawnedCharacters();

            var members = _userDomain.Members;
            for ( int i = 0; i < members.Count; ++i )
            {
                var chara = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, members[i] );

                var reservePos = new Vector3( CHARACTER_SELECTION_SPACING_X * i, CHARACTER_SELECTION_OFFSET_Y, CHARACTER_SELECTION_OFFSET_Z );
                chara.SetPosition( reservePos );

                _spawnedCharacters.Add( chara );
            }
        }

        private void DestroySpawnedCharacters()
        {
            foreach ( var chara in _spawnedCharacters )
            {
                if ( chara != null ) { Destroy( chara.gameObject ); }
            }

            _spawnedCharacters.Clear();
        }
    }
}
