using System.Collections.Generic;
using Frontier.Entities;
using Frontier.Registries;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 攻撃予告線を伸ばしているキャラクターが画面から見切れている場合に、そのキャラクターの顔アイコンを、
    /// 予告線のうち画面内に収まっている部分の端(画面端の付近)へ表示するPresenterです。
    /// 見切れているかどうか・どこへ表示するかの判断はこのクラスが行い、アイコン(AttackForecastIconUI)は指示された位置への表示のみを担います。
    /// 戦闘用カメラの移動・回転に追従させるため、対象が設定されている間は、UIの描画直前に毎フレーム表示を更新します。
    /// </summary>
    public class AttackForecastIconPresenter
    {
        [Inject] private IUiSystem _uiSystem                        = null;
        [Inject] private PrefabRegistry _prefabReg                  = null;
        [Inject] private CharacterPortraitRegistry _portraitReg     = null;

        // 表示の対象としている攻撃予告線(同じ添字のアイコンが対応する)
        private readonly List<AttackForecastLine> _lines = new List<AttackForecastLine>();
        // 生成済みのアイコン(対象の数が増えた場合のみ追加で生成し、以降は使い回す)
        private readonly List<AttackForecastIconUI> _icons = new List<AttackForecastIconUI>();
        private bool _isUpdating = false;

        [Inject]
        public AttackForecastIconPresenter() { }

        /// <summary>
        /// アイコンを表示し得る対象を設定し直します。
        /// 実際にアイコンが表示されるのは、対象のうち、攻撃側のキャラクターが画面から見切れているものに限られます。
        /// </summary>
        /// <param name="targets">表示中の攻撃予告線と、その線を伸ばしている攻撃側のキャラクターの組</param>
        public void SetTargets( IReadOnlyList<(AttackForecastLine line, Character attacker)> targets )
        {
            _lines.Clear();

            for( int i = 0; i < targets.Count; ++i )
            {
                if( _icons.Count <= i )
                {
                    _icons.Add( _uiSystem.BattleUi.CreateAttackForecastIcon( _prefabReg.AttackForecastIconPrefab.GetComponent<AttackForecastIconUI>() ) );
                }

                _icons[i].SetPortrait( _portraitReg.GetOrCapture( targets[i].attacker ) );
                _lines.Add( targets[i].line );
            }

            for( int i = targets.Count; i < _icons.Count; ++i )
            {
                _icons[i].Hide();
            }

            SetUpdating( 0 < _lines.Count );
            if( _isUpdating ) { UpdateIcons(); }
        }

        /// <summary>
        /// 対象を全て解除し、表示中のアイコンを全て非表示にします
        /// </summary>
        public void Clear()
        {
            _lines.Clear();
            SetUpdating( false );

            foreach( var icon in _icons )
            {
                // シーンの破棄等で既に破棄されている場合は対象外とする
                if( null != icon ) { icon.Hide(); }
            }
        }

        /// <summary>
        /// 毎フレームの表示の更新を開始・停止します。
        /// 戦闘用カメラの更新が済んだ後の位置を用いるため、UIの描画直前に呼ばれるイベントで更新します。
        /// </summary>
        private void SetUpdating( bool isUpdating )
        {
            if( _isUpdating == isUpdating ) { return; }
            _isUpdating = isUpdating;

            if( isUpdating ) { Canvas.willRenderCanvases += UpdateIcons; }
            else { Canvas.willRenderCanvases -= UpdateIcons; }
        }

        /// <summary>
        /// 各対象について、攻撃側のキャラクターが画面から見切れていれば、予告線上の画面内に収まる端へアイコンを表示します
        /// </summary>
        private void UpdateIcons()
        {
            var worldCamera = Camera.main;
            if( null == worldCamera ) { return; }

            for( int i = 0; i < _lines.Count; ++i )
            {
                // シーンの破棄等で既に破棄されている場合は、以降の更新を取りやめる
                if( null == _lines[i] || null == _icons[i] )
                {
                    _lines.Clear();
                    SetUpdating( false );
                    return;
                }

                if( _lines[i].TryGetScreenEdgePoint( worldCamera, _icons[i].ScreenEdgeMarginPixels, out Vector2 screenPos ) )
                {
                    _icons[i].Show( screenPos );
                }
                else
                {
                    _icons[i].Hide();
                }
            }
        }
    }
}
