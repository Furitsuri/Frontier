using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面の表示・状態を管理するPresenter。
    /// UserDomain.Members(値型のStatus一覧)からCharacterFactoryを介して表示用のCharacterを
    /// 再構築し、TroopEditUIへ渡す。SaveLoadPresenterと同様、Viewは動的生成せず
    /// GeneralUISystem上に事前配置されたTroopEditViewを参照する。
    /// </summary>
    public class TroopEditPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;
        [Inject] private UserDomain _userDomain = null;
        [Inject] private CharacterFactory _characterFactory = null;

        private TroopEditUI _view = null;
        private List<Character> _spawnedCharacters = new List<Character>();

        /// <summary>
        /// GeneralUi.TroopEditView(既にシーンに存在するUI)への参照を取得します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _view = _uiSystem.GeneralUi.TroopEditView;
        }

        /// <summary>
        /// 現在の部隊メンバーからキャラクターを再構築し、画面を表示します。
        /// </summary>
        public void Show()
        {
            BuildTroopCharacters();

            _view.DisplayMembers( _spawnedCharacters );
            _view.SetHeaderInfo( _userDomain.Money, _userDomain.Members.Count, TROOP_MAX_MEMBERS );
            _view.Show();
        }

        /// <summary>
        /// 画面を閉じ、表示用に再構築したキャラクターを破棄します。
        /// </summary>
        public void Hide()
        {
            _view.Hide();
            _view.ClearMembers();

            DestroySpawnedCharacters();
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
                if ( chara != null ) { Object.Destroy( chara.gameObject ); }
            }

            _spawnedCharacters.Clear();
        }
    }
}
