using Frontier.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header( "右上に表示するアニマ量バッジ(報酬/コスト兼用、未使用の呼び出し元では非表示のまま)" )]
        [FormerlySerializedAs( "_rewardAnimaText" )]
        [SerializeField] private TextMeshProUGUI _amountBadgeText;

        [Header( "左上に表示する雇用チェックマーク(未使用の呼び出し元では非表示のまま)" )]
        [SerializeField] private GameObject _employedMarkObject;

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
        /// 右上のアニマ量バッジを設定します。nullの場合は非表示にします。
        /// アニマのスプライトアイコン(TMP Sprite Asset、文字サイズに追従)+数値で表示する、
        /// 報酬(解雇画面)・コスト(雇用画面)共通の表示処理です。
        /// </summary>
        private void SetAmountBadge( int? amount )
        {
            if ( _amountBadgeText == null ) return;

            _amountBadgeText.gameObject.SetActive( amount.HasValue );
            // voffsetでスプライトのみ少し上へ補正する(文字のベースラインはそのまま)
            if ( amount.HasValue ) { _amountBadgeText.text = $"<voffset=0.15em><sprite name=\"anima_icon\"></voffset>{amount.Value}"; }
        }

        /// <summary>
        /// 右上に解雇報酬アニマ量を表示します(解雇画面専用、nullで非表示)。
        /// </summary>
        public void SetRewardAnima( int? amount ) => SetAmountBadge( amount );

        /// <summary>
        /// 右上に雇用コストを表示します(雇用画面専用、nullで非表示)。
        /// </summary>
        public void SetCost( int? cost ) => SetAmountBadge( cost );

        /// <summary>
        /// 左上の雇用チェックマークの表示を切り替えます(雇用画面専用)。
        /// </summary>
        public void SetEmployed( bool isEmployed ) => _employedMarkObject?.SetActive( isEmployed );

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
