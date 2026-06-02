using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.Battle
{
    /// <summary>
    /// スキル使用時のターゲット選択状態とロジックを管理します。
    /// ターゲティングモード別コールバックの管理、射程・対象リストの保持を担います。
    /// </summary>
    public class SkillTargetSelector
    {
        private delegate void ChangeDirectionCallback( TargetingRangeContext context, Direction dir, bool isWithMove, ref int currentRange, int maxRange );
        private delegate void RefreshTargetCallback( TargetingRangeContext context, bool isMovingSkill, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter );
        private delegate bool TryAdjustRangeCallback( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter );

        private readonly ChangeDirectionCallback[] _changeDirectionCallbacks;
        private readonly RefreshTargetCallback[]   _refreshFocusTargetCallbacks;
        private readonly TryAdjustRangeCallback[]  _tryAdjustRangeCallbacks;

        private int _currentRange;
        private int _maxRange;
        private bool _isAdjustableRange;
        private bool _isMovingSkill;
        private TargetingMode _targetingMode;
        private List<CharacterKey> _attackTargetCharaKeys = new List<CharacterKey>();
        private Character _targetCharacter;

        public List<CharacterKey> AttackTargetCharaKeys => _attackTargetCharaKeys;
        public Character TargetCharacter                => _targetCharacter;
        public int CurrentRange                         => _currentRange;
        public int MaxRange                             => _maxRange;
        public bool IsAdjustableRange                   => _isAdjustableRange;
        public bool IsMovingSkill                       => _isMovingSkill;
        public TargetingMode TargetingMode              => _targetingMode;
        public bool HasTarget                           => 0 < _attackTargetCharaKeys.Count;

        public SkillTargetSelector()
        {
            _changeDirectionCallbacks = new ChangeDirectionCallback[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.AcceptDirection,    // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.AcceptDirection,     // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.AcceptDirection,     // TargetingMode.DIRECTIONAL
                AllTargetingRange.AcceptDirection,             // TargetingMode.ALL
            };

            _refreshFocusTargetCallbacks = new RefreshTargetCallback[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.RefreshFocusTarget,    // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.RefreshFocusTarget,     // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.RefreshFocusTarget,     // TargetingMode.DIRECTIONAL
                AllTargetingRange.RefreshFocusTarget,             // TargetingMode.ALL
            };

            _tryAdjustRangeCallbacks = new TryAdjustRangeCallback[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.TryAdjustRange,    // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.TryAdjustRange,     // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.TryAdjustRange,     // TargetingMode.DIRECTIONAL
                AllTargetingRange.TryAdjustRange,             // TargetingMode.ALL
            };
        }

        /// <summary>
        /// スキルデータとターゲティングコンテキストをもとにターゲット選択状態を初期化します。
        /// </summary>
        public void Init( in SkillsData.Data skillData, TargetingRangeContext context )
        {
            _targetingMode     = skillData.TargetingMode;
            // DIRECTIONALは自身の向きに沿った直線状の範囲すべてが攻撃対象範囲となるため、RangeValueをそのまま適用
            _maxRange          = ( TargetingMode.DIRECTIONAL != _targetingMode ) ? skillData.TargetingRange : skillData.RangeValue;
            _currentRange      = _maxRange;
            _isAdjustableRange = skillData.IsAdjustableRange;
            _isMovingSkill     = skillData.IsMovingSkill;
            _targetCharacter   = null;

            context.Owner.BattleLogic.ActionRangeCtrl.RefreshTargetableRange(
                _targetingMode, true, _isMovingSkill,
                context.Owner.BattleParams.TmpParam.CurrentTileIndex, _currentRange );

            _refreshFocusTargetCallbacks[( int ) _targetingMode]?.Invoke(
                context, _isMovingSkill, ref _attackTargetCharaKeys, ref _targetCharacter );
        }

        /// <summary>
        /// 方向入力を受けてターゲット選択を更新します。
        /// </summary>
        public void AcceptDirection( Direction dir, TargetingRangeContext context )
        {
            _changeDirectionCallbacks[( int ) _targetingMode]?.Invoke(
                context, dir, _isMovingSkill, ref _currentRange, _maxRange );

            _refreshFocusTargetCallbacks[( int ) _targetingMode]?.Invoke(
                context, _isMovingSkill, ref _attackTargetCharaKeys, ref _targetCharacter );
        }

        /// <summary>
        /// 射程を調整します。変更があった場合は true を返します。
        /// </summary>
        public bool TryAdjustRange( int step, TargetingRangeContext context )
        {
            var callback = _tryAdjustRangeCallbacks[( int ) _targetingMode];
            return callback != null && callback(
                context, step, ref _currentRange, _maxRange, _isMovingSkill,
                ref _attackTargetCharaKeys, ref _targetCharacter );
        }

        /// <summary>
        /// フォーカス中のターゲットを更新します（Sub1/2 のターゲット切り替えで使用）。
        /// </summary>
        public void UpdateFocusedTarget( Character target )
        {
            _targetCharacter = target;
        }
    }
}
