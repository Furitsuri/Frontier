using Frontier.Entities;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 戦闘に参加している全キャラクターの頭上HPゲージ(CharacterHpGaugeUI)を、キャラクター単位で生成・管理します。
    /// 子に配置したテンプレートを複製して、キャラクターごとのHPゲージを自身の子として生成します。
    /// BattleUIのCharacterFollowLayer(パラメータ・コマンド等のHUDより奥)に配置してください。
    /// </summary>
    public class CharacterHpGaugeGroupView : MonoBehaviour
    {
        [Header( "テンプレート" )]
        [Tooltip( "キャラクターごとのHPゲージの複製元。自身の子として配置し、非表示の状態で保持します" )]
        [SerializeField] private CharacterHpGaugeUI _template;

        private PerCharacterUiPool<CharacterHpGaugeUI> _pool;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlUiRect">BattleUISystemのRectTransform(HPゲージの位置計算の基準)</param>
        /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
        public void Setup( RectTransform btlUiRect, Camera uiCamera )
        {
            _template.Setup();
            _template.Init( btlUiRect, uiCamera );

            _pool = new PerCharacterUiPool<CharacterHpGaugeUI>( _template, transform, ui => ui.Init( btlUiRect, uiCamera ) );
        }

        /// <summary>
        /// 指定キャラクターのHPゲージを表示します。キャラクターが戦闘に参加している間、常時表示され続けます。
        /// </summary>
        public void Show( Character chara )
        {
            _pool.GetOrCreate( chara ).ShowFor( chara );
        }

        /// <summary>
        /// 指定キャラクターのHPゲージを破棄します。
        /// キャラクターが戦闘から離脱した場合(死亡・配置解除など)に呼び出してください。
        /// </summary>
        public void Remove( Character chara )
        {
            _pool.Remove( chara );
        }

        /// <summary>
        /// 現在表示中の全キャラクターのHPゲージをまとめて表示/非表示にします。
        /// 攻撃シーケンスなど、専用カメラ演出中に一時的に隠す用途を想定しています。
        /// </summary>
        public void SetAllActive( bool isActive )
        {
            foreach( var ui in _pool.Values )
            {
                ui.gameObject.SetActive( isActive );
            }
        }

        /// <summary>
        /// 指定キャラクターのHPゲージに、予測ダメージ分の点滅表示を設定します。
        /// 攻撃対象選択中、対象キャラクターに対して呼び出してください。
        /// </summary>
        public void SetPredictedDamage( Character chara, int amount )
        {
            if( _pool.TryGet( chara, out var ui ) )
            {
                ui.SetPredictedDamage( amount );
            }
        }

        /// <summary>
        /// 表示中の全キャラクターのHPゲージから、予測ダメージ点滅表示を解除します。
        /// </summary>
        public void ClearAllPredictedDamage()
        {
            foreach( var ui in _pool.Values )
            {
                ui.ClearPredictedDamage();
            }
        }

        /// <summary>
        /// 暫定移動中のキャラクター頭上に表示するアイコンを、指定キャラクターに限り一時的に非表示にします。
        /// 他のキャラクターのアイコンは表示されたままとなります。nullを渡すと、非表示の指定を解除します。
        /// </summary>
        public void SuppressProvisionalMoveIcon( Character character )
        {
            foreach( var pair in _pool.Pairs )
            {
                bool isSuppressed = ( null != character ) && ( pair.Key == character.GetInstanceID() );
                pair.Value.SetProvisionalMoveIconVisible( !isSuppressed );
            }
        }

        /// <summary>
        /// 生成したすべてのHPゲージを破棄します。
        /// </summary>
        public void Cleanup()
        {
            _pool.Clear();
        }
    }
}
