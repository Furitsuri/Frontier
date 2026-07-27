using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// グリッドカーソルを合わせているキャラクターの移動・攻撃範囲を表示するための共有シングルトンです。
    /// PlSelectTileState(及びその派生であるPlGroupMoveState)・DeploymentRootStateなど、
    /// 複数のStateインスタンスをまたいでカーソル移動の度に呼び出すことで、常に現在のカーソル位置に
    /// 対応した表示を維持します。既に行動を終了し何も出来ない状態のキャラクターには表示しません。
    /// </summary>
    public class HoveredCharacterRangeDisplay
    {
        [Inject] private BattleRoutineController _btlRtnCtrl = null;
        [Inject] private StageController _stageCtrl           = null;

        private CharacterKey _currentKey = CharacterKey.Invalid;

        [Inject]
        public HoveredCharacterRangeDisplay() { }

        /// <summary>
        /// カーソルが指しているキャラクターに応じて表示を更新します。
        /// 対象なし、または表示対象外(行動終了済み等)の場合はnullを渡してください。
        /// </summary>
        public void Refresh( Character hoveredCharacter )
        {
            CharacterKey newKey = ( null != hoveredCharacter && IsEligible( hoveredCharacter ) )
                ? hoveredCharacter.GetCharacterKey()
                : CharacterKey.Invalid;

            if( newKey == _currentKey ) { return; }

            Clear();

            if( newKey.IsValid() )
            {
                int dprtIdx         = hoveredCharacter.BattleParams.TmpParam.CurrentTileIndex;
                var actionRangeCtrl = hoveredCharacter.BattleLogic.ActionRangeCtrl;

                // 移動コマンドを既に使用済みの場合、SetupActionableRangeDataはステータス上の移動力を
                // そのまま使って範囲を計算してしまい、実際には移動出来ないのに移動可能であるかのような
                // 範囲が表示されてしまう。その場合は攻撃範囲のみを表示する
                if( hoveredCharacter.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.MOVE] )
                {
                    actionRangeCtrl.SetupAttackableRangeData( dprtIdx );
                    actionRangeCtrl.DrawAttackableRange();
                }
                else
                {
                    float dprtHeight = _stageCtrl.GetTileStaticData( dprtIdx ).Height;
                    actionRangeCtrl.SetupActionableRangeData( dprtIdx, dprtHeight );
                    actionRangeCtrl.DrawActionableRange();
                }
            }

            _currentKey = newKey;
        }

        /// <summary>
        /// 現在表示中のホバー範囲を非表示にします
        /// </summary>
        public void Clear()
        {
            if( !_currentKey.IsValid() ) { return; }

            _btlRtnCtrl.BtlCharaCdr.GetCharacter( _currentKey )?.BattleLogic.ActionRangeCtrl.ClearActionableRangeDataWithRender();
            _currentKey = CharacterKey.Invalid;
        }

        private bool IsEligible( Character character )
        {
            return !character.BattleParams.TmpParam.IsEndAction();
        }
    }
}
