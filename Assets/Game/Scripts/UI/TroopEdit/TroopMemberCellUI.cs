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

        [Header( "Lv./名前の下に表示するアニマ量バッジ(報酬/コスト兼用、未使用の呼び出し元では非表示のまま)" )]
        [FormerlySerializedAs( "_rewardAnimaText" )]
        [SerializeField] private TextMeshProUGUI _amountBadgeText;

        [Header( "左上に表示するチェックマーク(雇用/解雇画面共通、未使用の呼び出し元では非表示のまま)" )]
        [FormerlySerializedAs( "_employedMarkObject" )]
        [SerializeField] private GameObject _checkMarkObject;

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

        // 解雇報酬(得られるアニマ)は符号+、緑色で表示する
        private const string RewardSign      = "+";
        private const string RewardColorHex  = "#4CD964";
        // 雇用コスト(消費するアニマ)は符号-、赤色で表示する
        private const string CostSign        = "-";
        private const string CostColorHex    = "#FF3B30";

        /// <summary>
        /// Lv./名前の下のアニマ量バッジを設定します。nullの場合は非表示にします。
        /// アニマのスプライトアイコン(TMP Sprite Asset、文字サイズに追従、常に白のまま)+
        /// 符号付き数値(指定した色)で表示する、報酬(解雇画面)・コスト(雇用画面)共通の表示処理です。
        /// </summary>
        private void SetAmountBadge( int? amount, string sign, string colorHex )
        {
            if ( _amountBadgeText == null ) return;

            _amountBadgeText.gameObject.SetActive( amount.HasValue );
            // voffsetでスプライトのみ上へ補正し、数値の文字高さと縦位置を揃える。
            // colorタグはスプライトの外側に置き、アイコン自体は常に白のまま、符号+数値だけ着色する
            if ( amount.HasValue ) { _amountBadgeText.text = $"<voffset=0.4em><sprite name=\"anima_icon\"></voffset><color={colorHex}>{sign}{amount.Value}</color>"; }
        }

        /// <summary>
        /// Lv./名前の下に解雇報酬アニマ量を表示します(解雇画面専用、nullで非表示)。「+」符号・緑色で表示します。
        /// </summary>
        public void SetRewardAnima( int? amount ) => SetAmountBadge( amount, RewardSign, RewardColorHex );

        /// <summary>
        /// Lv./名前の下に雇用コストを表示します(雇用画面専用、nullで非表示)。「-」符号・赤色で表示します。
        /// </summary>
        public void SetCost( int? cost ) => SetAmountBadge( cost, CostSign, CostColorHex );

        /// <summary>
        /// 左上のチェックマークの表示を切り替えます(雇用画面での雇用チェック・解雇画面での
        /// 解雇チェック共通)。
        /// </summary>
        public void SetChecked( bool isChecked ) => _checkMarkObject?.SetActive( isChecked );

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
