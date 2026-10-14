using Frontier.Battle;
using Frontier.Entities;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Battle
{
    /// <summary>
    /// キャラクターの頭上に表示するデバッグ用ラベルの表示内容を決定します。
    /// 表示の可否は DebugSettings を参照し、表示する文字列は DebugCharacterInfo で組み立てます
    /// </summary>
    public class DebugCharacterLabelPresenter
    {
        [Inject] private IUiSystem _uiSystem        = null;
        [Inject] private DebugSettings _settings    = null;

        private DebugCharacterLabelUI _view = null;

        public void Init()
        {
            _view = _uiSystem.DebugUi?.CharacterLabelView;
        }

        /// <summary>
        /// 表示内容を更新します
        /// </summary>
        /// <param name="btlCharaCdr">戦闘キャラクターの管理</param>
        public void Update( BattleCharacterCoordinator btlCharaCdr )
        {
            if( _view == null ) { return; }

            if( !_settings.ShowThinkingTypeLabel )
            {
                _view.HideAll();
                return;
            }

            _view.BeginUpdate();
            foreach( var chara in btlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                _view.Show( chara, DebugCharacterInfo.GetThinkingLabel( chara ) );
            }
            _view.EndUpdate();
        }

        /// <summary>
        /// 全てのラベルを非表示にします
        /// </summary>
        public void HideAll()
        {
            _view?.HideAll();
        }
    }
}

#endif // UNITY_EDITOR
