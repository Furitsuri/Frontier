using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 敵撃破時のアニマ獲得エフェクト(AnimaRewardEffectUI)を生成・管理します。
    /// 同時に複数の撃破報酬が発生した場合に備え、キャラクター単位ではなく、再生していないものを使い回すプールとして管理します。
    /// 子に配置したテンプレートは、プールの1つ目のインスタンスとしてそのまま使用します。
    /// BattleUIのEffectLayer(パラメータ・コマンド等のHUDより手前)に配置してください。
    /// </summary>
    public class AnimaRewardEffectGroupView : MonoBehaviour
    {
        [Header( "テンプレート" )]
        [Tooltip( "アニマ獲得エフェクトの複製元(兼1つ目のインスタンス)。自身の子として配置します" )]
        [SerializeField] private AnimaRewardEffectUI _template;

        private RectTransform _btlUiRectTransform;
        private Camera _btlUiCamera;

        // 生成済みのインスタンス一覧(非アクティブなものを使い回す)
        private readonly List<AnimaRewardEffectUI> _effects = new List<AnimaRewardEffectUI>();

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="btlUiRect">BattleUISystemのRectTransform(表示位置の計算の基準)</param>
        /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
        public void Setup( RectTransform btlUiRect, Camera uiCamera )
        {
            _btlUiRectTransform = btlUiRect;
            _btlUiCamera        = uiCamera;

            _template.Setup();
            _template.Init( _btlUiRectTransform, _btlUiCamera );
            _effects.Clear();
            _effects.Add( _template );
        }

        /// <summary>
        /// 敵撃破位置から指定の位置へ向けてアニマ獲得エフェクトを再生します。
        /// 演出が目標位置へ到達した時点でonArrivedが呼ばれます(実際のアニマ加算は呼び出し側の責務)。
        /// </summary>
        /// <param name="worldPosition">演出の開始位置(ワールド座標)</param>
        /// <param name="targetLocalPos">演出の到達位置(BattleUI上のローカル座標)</param>
        /// <param name="onArrived">到達時に呼ばれる処理</param>
        public void Play( Vector3 worldPosition, Vector2 targetLocalPos, Action onArrived )
        {
            GetOrCreate().Play( worldPosition, targetLocalPos, onArrived );
        }

        /// <summary>
        /// いずれかのアニマ獲得エフェクトが再生中(球が目標位置へ向けて移動中)かどうかを取得します。
        /// ステージクリア演出は、全ての球がUIへ到達するまで開始を待つために使用します。
        /// </summary>
        public bool IsAnyPlaying()
        {
            foreach( var effect in _effects )
            {
                if( effect.IsPlaying ) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 非アクティブなインスタンスを1つ返します。無ければテンプレートを複製して新規生成します。
        /// </summary>
        private AnimaRewardEffectUI GetOrCreate()
        {
            foreach( var effect in _effects )
            {
                if( !effect.gameObject.activeSelf ) { return effect; }
            }

            var newEffect = Instantiate( _template, transform );
            newEffect.Setup();
            newEffect.Init( _btlUiRectTransform, _btlUiCamera );
            _effects.Add( newEffect );
            return newEffect;
        }
    }
}
