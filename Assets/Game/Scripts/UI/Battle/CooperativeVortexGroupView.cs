using Frontier.Entities;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 連携演出用の渦巻きエフェクト(CooperativeVortexUI)を、キャラクター単位で生成・管理します。
    /// 子に配置したテンプレートを複製して、キャラクターごとの渦巻きエフェクトを自身の子として生成します。
    /// BattleUIのEffectLayer(パラメータ・コマンド等のHUDより手前)に配置してください。
    /// </summary>
    public class CooperativeVortexGroupView : MonoBehaviour
    {
        [Header( "テンプレート" )]
        [Tooltip( "キャラクターごとの渦巻きエフェクトの複製元。自身の子として配置し、非表示の状態で保持します" )]
        [SerializeField] private CooperativeVortexUI _template;

        private PerCharacterUiPool<CooperativeVortexUI> _pool;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlUiRect">BattleUISystemのRectTransform(表示位置の計算の基準)</param>
        /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
        public void Setup( RectTransform btlUiRect, Camera uiCamera )
        {
            _template.Setup();
            _template.Init( btlUiRect, uiCamera );

            _pool = new PerCharacterUiPool<CooperativeVortexUI>( _template, transform, ui => ui.Init( btlUiRect, uiCamera ) );
        }

        /// <summary>
        /// 連携演出用の渦巻きエフェクトを、指定キャラクターの画面上の位置に表示します。
        /// duration 秒かけて縮小しながら回転し、経過後に自動的に非表示になります。
        /// </summary>
        /// <param name="initialScale">縮小開始時の拡大率(等倍=1)</param>
        public void Show( Character chara, float duration, float initialScale = 1f )
        {
            var ui = _pool.GetOrCreate( chara );
            ui.CharacterTransform = chara.transform;
            ui.Play( duration, initialScale );
        }

        /// <summary>
        /// 生成したすべての渦巻きエフェクトを破棄します。
        /// </summary>
        public void Cleanup()
        {
            _pool.Clear();
        }
    }
}
