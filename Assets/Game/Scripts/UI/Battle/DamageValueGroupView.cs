using Frontier.Entities;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター上に表示するダメージ表記(DamageUI)を、キャラクター単位で生成・管理します。
    /// 子に配置したテンプレートを複製して、キャラクターごとのダメージ表記を自身の子として生成します。
    /// 表示する文言・色の決定は呼び出し側で行い、このクラスは指示された内容を表示するのみです。
    /// BattleUIのEffectLayer(パラメータ・コマンド等のHUDより手前)に配置してください。
    /// </summary>
    public class DamageValueGroupView : MonoBehaviour
    {
        [Header( "テンプレート" )]
        [Tooltip( "キャラクターごとのダメージ表記の複製元。自身の子として配置し、非表示の状態で保持します" )]
        [SerializeField] private DamageUI _template;

        private PerCharacterUiPool<DamageUI> _pool;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlUiRect">BattleUISystemのRectTransform(表示位置の計算の基準)</param>
        /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
        public void Setup( RectTransform btlUiRect, Camera uiCamera )
        {
            _template.Setup();
            _template.Init( btlUiRect, uiCamera );

            _pool = new PerCharacterUiPool<DamageUI>( _template, transform, ui => ui.Init( btlUiRect, uiCamera ) );
        }

        /// <summary>
        /// 指定キャラクターの位置にダメージ表記を表示します。
        /// duration が 0 以上の場合、指定秒数後に自動で非表示にします。
        /// duration が負の値の場合は自動非表示を行わず、Hide() による明示的な非表示が必要です。
        /// </summary>
        /// <param name="chara">表示対象のキャラクター</param>
        /// <param name="text">表示する文言</param>
        /// <param name="color">文言の色</param>
        /// <param name="duration">自動非表示までの秒数。負の値で無効</param>
        public void Show( Character chara, string text, Color color, float duration )
        {
            var ui = _pool.GetOrCreate( chara );
            ui.CharacterTransform = chara.transform;
            ui.damageText.text    = text;
            ui.damageText.color   = color;
            ui.ShowWith( duration );
        }

        /// <summary>
        /// 指定キャラクターのダメージ表記を明示的に非表示にします
        /// </summary>
        public void Hide( Character chara )
        {
            if( _pool.TryGet( chara, out var ui ) )
            {
                ui.Hide();
            }
        }

        /// <summary>
        /// 全キャラクターのダメージ表記をまとめて非表示にします
        /// </summary>
        public void HideAll()
        {
            foreach( var ui in _pool.Values )
            {
                ui.Hide();
            }
        }

        /// <summary>
        /// 生成したすべてのダメージ表記を破棄します。
        /// バトル終了などのタイミングで明示的に破棄したい場合に呼び出してください。
        /// </summary>
        public void Cleanup()
        {
            _pool.Clear();
        }
    }
}
