using Frontier.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 部隊編集画面のグリッド上に並ぶ、キャラクター1体分のセル。
    /// CharacterSelectionDisplayと同様、CharacterCameraでキャラクターの3Dモデルを
    /// リアルタイムに描画してRawImageへ反映する。下部にはLv.と名前を表示する。
    /// スライドやフォーカス切替等の選択演出は持たず、割り当てられたキャラクターを
    /// 常時表示するだけの最小構成。
    /// </summary>
    public class TroopMemberCellUI : UiMonoBehaviour
    {
        [Header( "キャラクターの3Dモデルを映すRawImage" )]
        [SerializeField] private RawImage _portrait;

        [Header( "Lv.と名前を表示するテキスト" )]
        [SerializeField] private TextMeshProUGUI _nameLevelText;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private CharacterCamera _characterCamera = null;
        private Character _character = null;

        private void Update()
        {
            if ( _character == null ) return;

            _characterCamera?.Update( _character.CameraParam );
        }

        /// <summary>
        /// 表示対象のキャラクターを割り当て、専用カメラでの描画を開始します。
        /// </summary>
        public void AssignCharacter( Character character )
        {
            _character = character;

            LazyInject.GetOrCreate( ref _characterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );
            _characterCamera.Setup( gameObject, "TroopMemberCamera" );
            _characterCamera.Init( "TroopMemberCamera", _character.gameObject.layer, 0f, ref _portrait );
            _characterCamera.AssignCharacter( _character, _character.gameObject.layer );

            var status = _character.GetStatusRef;
            _nameLevelText.text = $"Lv.{status.Level}  {status.Name}";
        }

        /// <summary>
        /// 専用カメラ・RenderTextureを破棄します。セルを削除する前に呼び出してください。
        /// </summary>
        public void Dispose()
        {
            _characterCamera?.Dispose();
            _characterCamera = null;
            _character = null;
        }
    }
}
