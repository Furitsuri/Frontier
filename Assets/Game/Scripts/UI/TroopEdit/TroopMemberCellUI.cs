using Frontier.Entities;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 部隊編集画面のグリッド上に並ぶ、キャラクター1体分のセル。
    /// CharacterSelectionDisplayと同様、CharacterCameraでキャラクターの3Dモデルを
    /// リアルタイムに描画してRawImageへ反映する。スライドやフォーカス切替等の
    /// 選択演出は持たず、割り当てられたキャラクターを常時表示するだけの最小構成。
    /// </summary>
    public class TroopMemberCellUI : UiMonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private RawImage _portrait = null;
        private CharacterCamera _characterCamera = null;
        private Character _character = null;

        private void Update()
        {
            if ( _character == null ) return;

            _characterCamera?.Update( _character.CameraParam );
        }

        public override void Setup()
        {
            base.Setup();

            LazyInject.GetOrCreate( ref _portrait, () => GetComponent<RawImage>() );
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
