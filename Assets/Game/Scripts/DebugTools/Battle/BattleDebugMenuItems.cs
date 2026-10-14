using Frontier.Battle;
using Frontier.DebugTools.DebugMenu;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Battle
{
    /// <summary>
    /// 戦闘中に使用するデバッグメニューの項目を登録します。
    /// 戦闘の開始時に登録し、終了時に解除します
    /// </summary>
    public class BattleDebugMenuItems
    {
        [Inject] private DebugMenuItemRegistry _registry    = null;
        [Inject] private DebugSettings _settings            = null;

        private BattleCharacterCoordinator _btlCharaCdr = null;
        private Character _target                       = null;    // 「選択中キャラクター」ページの項目の操作対象

        /// <summary>
        /// 戦闘中に使用する項目を登録します
        /// </summary>
        /// <param name="btlCharaCdr">戦闘キャラクターの管理</param>
        public void Register( BattleCharacterCoordinator btlCharaCdr )
        {
            _btlCharaCdr = btlCharaCdr;
            _target      = null;

            _registry.UnregisterAll( this );    // 戦闘が再初期化された場合に二重登録しないよう、一度解除する

            RegisterDisplayItems();
            RegisterSelectedCharacterItems();
        }

        /// <summary>
        /// 登録した項目を全て解除します
        /// </summary>
        public void Unregister()
        {
            _registry.UnregisterAll( this );

            _btlCharaCdr = null;
            _target      = null;
        }

        private void RegisterDisplayItems()
        {
            var page = BattleDebugMenuPages.DISPLAY;

            _registry.Register( this, new DebugToggleItem( page, "THINK TYPE LABEL", () => _settings.ShowThinkingTypeLabel, v => _settings.ShowThinkingTypeLabel = v ) );
        }

        private void RegisterSelectedCharacterItems()
        {
            var page = BattleDebugMenuPages.SELECTED_CHARACTER;

            // ページを開いた時点でグリッドカーソル上にいるキャラクターを操作対象とする
            _registry.RegisterPageOpenedCallback( this, page, SelectTargetAtCursor );

            _registry.Register( this, new DebugChoiceItem( page, "TARGET", () => DebugCharacterInfo.GetName( GetTarget() ), StepTarget ) );
            _registry.Register( this, new DebugIntItem( page, "HP",
                () => GetTarget()?.GetStatusRef.CurHP ?? 0,
                v => { var target = GetTarget(); if( target != null ) { target.GetStatusRef.CurHP = v; } },
                () => 1,    // 0にしても死亡処理は走らないため、HPの変更で死亡させることは出来ないようにする
                () => GetTarget()?.GetStatusRef.MaxHP ?? 0 ) );
            _registry.Register( this, new DebugReadOnlyItem( page, "THINK TYPE",      () => DebugCharacterInfo.GetThinkingType( GetTarget() ) ) );
            _registry.Register( this, new DebugReadOnlyItem( page, "INTERCEPT TYPE",  () => DebugCharacterInfo.GetInterceptType( GetTarget() ) ) );
            _registry.Register( this, new DebugReadOnlyItem( page, "INTERCEPT GROUP", () => DebugCharacterInfo.GetInterceptGroupId( GetTarget() ) ) );
            _registry.Register( this, new DebugReadOnlyItem( page, "INTERCEPT STATE", () => DebugCharacterInfo.GetInterceptState( GetTarget() ) ) );
        }

        /// <summary>
        /// 操作対象を取得します。対象が戦闘から外れている(撃破された等)場合はnullを返します
        /// </summary>
        private Character GetTarget()
        {
            if( _target == null || _btlCharaCdr == null ) { return null; }
            if( !_btlCharaCdr.IsContains( _target.GetCharacterKey() ) ) { return null; }

            return _target;
        }

        /// <summary>
        /// グリッドカーソル上のキャラクターを操作対象にします。
        /// カーソル上にキャラクターがいない場合は、戦闘中のキャラクターの先頭を対象にします
        /// </summary>
        private void SelectTargetAtCursor()
        {
            if( _btlCharaCdr == null ) { return; }

            _target = _btlCharaCdr.GetSelectCharacter() ?? GetTargetCandidates().FirstOrDefault();
        }

        /// <summary>
        /// 操作対象を、戦闘中のキャラクターの並び順で前後に切り替えます
        /// </summary>
        private void StepTarget( int delta )
        {
            var candidates = GetTargetCandidates();
            if( candidates.Count <= 0 ) { return; }

            int index = candidates.IndexOf( GetTarget() );
            index = ( index < 0 ) ? 0 : ( index + delta + candidates.Count ) % candidates.Count;
            _target = candidates[index];
        }

        private List<Character> GetTargetCandidates()
        {
            if( _btlCharaCdr == null ) { return new List<Character>(); }

            return _btlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ).ToList();
        }
    }
}

#endif // UNITY_EDITOR
