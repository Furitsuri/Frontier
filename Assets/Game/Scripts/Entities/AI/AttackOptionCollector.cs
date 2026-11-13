using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// キャラクターが現在の位置から移動した上で選択し得る、攻撃の候補(AttackOption)を列挙します。
    /// 回復など自軍を対象とするスキルの使用も、同じ形の候補として列挙します(Collect(Character)のみ。対象は味方)。
    /// AIの思考タイプによらない共通処理であり、候補の評価・選択は各AI側で行います。
    /// 表示用の範囲データやキャラクターの向き・ゴーストなどには一切触れません。
    /// MEMO : 計算時点では使用者が元のタイルに存在したままであるため、移動後の位置からの攻撃を試算する際に、
    ///        元のタイルが使用者自身によって塞がれている扱いとなる場合があります(ジャンプ斬りの飛び越え判定等)
    /// </summary>
    public class AttackOptionCollector
    {
        [Inject] private BattleRoutineController _btlRtnCtrl   = null;
        [Inject] private StageController _stageCtrl            = null;

        /// <summary>
        /// 攻撃の候補と、回復など自軍を対象とするスキルの使用の候補を、すべて列挙します
        /// </summary>
        /// <param name="owner">攻撃を行うキャラクター</param>
        /// <returns>攻撃の候補</returns>
        public List<AttackOption> Collect( Character owner )
        {
            var options = new List<AttackOption>();
            Enumerate( owner, null, true, option => { options.Add( option ); return false; } );

            return options;
        }

        /// <summary>
        /// 攻撃の候補をすべて列挙します(自軍を対象とするスキルの使用の候補は含みません)。
        /// 指定のキャラクターが立っているタイルは、そのキャラクターが移動して空くものとみなし、攻撃を行う位置の候補に含めます
        /// (軍全体の行動の調整において、「味方が空けてくれれば攻撃出来る位置」を求めるために使用します)
        /// </summary>
        /// <param name="owner">攻撃を行うキャラクター</param>
        /// <param name="vacatableKeys">移動して空くものとみなすキャラクター(nullの場合は空かないものとする)</param>
        /// <returns>攻撃の候補</returns>
        public List<AttackOption> Collect( Character owner, ICollection<CharacterKey> vacatableKeys )
        {
            var options = new List<AttackOption>();
            Enumerate( owner, vacatableKeys, false, option => { options.Add( option ); return false; } );

            return options;
        }

        /// <summary>
        /// 攻撃の候補が1つでも存在するかを判定します(自軍を対象とするスキルの使用の候補は含みません)。
        /// 候補が見つかった時点で列挙を打ち切るため、Collectよりも軽量です
        /// </summary>
        /// <param name="owner">攻撃を行うキャラクター</param>
        /// <returns>存在するか否か</returns>
        public bool HasAnyOption( Character owner )
        {
            return Enumerate( owner, null, false, option => true );
        }

        /// <summary>
        /// 回復など自軍を対象とするスキルの使用の候補が1つでも存在するかを判定します
        /// (回復の場合、範囲内にHPが減っている味方がいる場合のみ候補となります)
        /// </summary>
        /// <param name="owner">スキルを使用するキャラクター</param>
        /// <returns>存在するか否か</returns>
        public bool HasAnyAllyTargetOption( Character owner )
        {
            return Enumerate( owner, null, true, option => SkillsData.IsAllyTargetSkill( option.SkillID ), isAllyTargetSkillsOnly: true );
        }

        /// <summary>
        /// 攻撃の候補を列挙し、見つかる度にonFoundを呼び出します
        /// </summary>
        /// <param name="vacatableKeys">立っているタイルが移動して空くものとみなすキャラクター(nullの場合は空かないものとする)</param>
        /// <param name="includeAllyTargetSkills">回復など自軍を対象とするスキルの使用も候補に含めるか</param>
        /// <param name="onFound">候補が見つかった際の処理。trueを返した場合は列挙を打ち切ります</param>
        /// <param name="isAllyTargetSkillsOnly">自軍を対象とするスキルの候補のみを列挙するか(通常攻撃・攻撃用のスキルの範囲は計算しません)</param>
        /// <returns>列挙を打ち切ったか否か</returns>
        private bool Enumerate( Character owner, ICollection<CharacterKey> vacatableKeys, bool includeAllyTargetSkills, Func<AttackOption, bool> onFound, bool isAllyTargetSkillsOnly = false )
        {
            var tmpParam = owner.BattleParams.TmpParam;

            // 攻撃またはスキルを実行済みであれば、攻撃は行えない
            if( tmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] || tmpParam.IsEndCommand[( int ) COMMAND_TAG.SKILL] ) { return false; }

            var usableSkillIDs  = CollectUsableSkills( owner, includeAllyTargetSkills );
            var tileData        = new ActionableTileData();

            // 自軍を対象とするスキルの候補のみが必要な場合、それ以外のスキルは列挙しない。
            // 該当するスキルが無ければ、立てるタイルや範囲を求めるまでもなく候補は存在しない
            if( isAllyTargetSkillsOnly )
            {
                usableSkillIDs.RemoveAll( skillID => !SkillsData.IsAllyTargetSkill( skillID ) );
                if( usableSkillIDs.Count <= 0 ) { return false; }
            }

            foreach( int standTileIdx in CollectStandableTiles( owner, vacatableKeys ) )
            {
                if( !isAllyTargetSkillsOnly && EnumerateNormalAttack( owner, standTileIdx, tileData, onFound ) ) { return true; }

                foreach( var skillID in usableSkillIDs )
                {
                    if( EnumerateSkillAttack( owner, standTileIdx, skillID, tileData, onFound ) ) { return true; }
                }
            }

            return false;
        }

        /// <summary>
        /// 攻撃を行う位置として立つことが出来るタイルを求めます。
        /// 移動済みの場合は現在のタイルのみとなります。
        /// vacatableKeysに含まれるキャラクターが立っているタイルは、そのキャラクターが移動して空くものとみなして含めます
        /// </summary>
        private List<int> CollectStandableTiles( Character owner, ICollection<CharacterKey> vacatableKeys )
        {
            int dprtTileIdx = owner.BattleParams.TmpParam.CurrentTileIndex;

            if( owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.MOVE] ) { return new List<int> { dprtTileIdx }; }

            var status      = owner.GetStatusRef;
            var ownerKey    = owner.GetCharacterKey();
            var moveData    = new ActionableTileData();
            float height    = _stageCtrl.GetTileStaticData( dprtTileIdx ).Height;

            // 攻撃範囲は不要であるため、攻撃レンジを0として移動可能範囲のみを求める
            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, status.moveRange, status.jumpForce, 0, height, owner.BattleLogic.TileCostTable, in ownerKey, ref moveData );

            var standableTiles = new List<int>( moveData.MoveableTileMap.Count );
            foreach( var pair in moveData.MoveableTileMap )
            {
                if( pair.Value.EstimatedMoveRange < 0 ) { continue; }

                if( pair.Value.IsStandableBy( ownerKey ) || IsVacatable( pair.Value, vacatableKeys ) )
                {
                    standableTiles.Add( pair.Key );
                }
            }

            return standableTiles;
        }

        /// <summary>
        /// 指定のタイルに立っているキャラクターが、移動して空くものとみなすキャラクターであるかを取得します
        /// (移動を伴うスキルの着地先等として予約されているタイルは、空かないものとします)
        /// </summary>
        private static bool IsVacatable( TileDynamicData tileData, ICollection<CharacterKey> vacatableKeys )
        {
            if( vacatableKeys == null || !tileData.CharaKey.IsValid() ) { return false; }
            if( Methods.HasAnyFlag( tileData.Flag, TileBitFlag.RESERVED ) ) { return false; }

            return vacatableKeys.Contains( tileData.CharaKey );
        }

        /// <summary>
        /// 装備しているスキルのうち、候補となるスキルを求めます。
        /// 攻撃シチュエーションで使用する攻撃用のスキル(及び、指定時は回復スキル)で、未使用かつアクションゲージが足りているもののみが対象です。
        /// MEMO : サポートスキルは、AIでの評価の方法が未定のため対象外としています
        /// </summary>
        /// <param name="includeAllyTargetSkills">回復など自軍を対象とするスキルも含めるか</param>
        private List<SkillID> CollectUsableSkills( Character owner, bool includeAllyTargetSkills )
        {
            var skillIDs = new List<SkillID>( EQUIPABLE_SKILL_MAX_NUM );
            var tmpParam = owner.BattleParams.TmpParam;

            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                SkillID skillID = owner.GetEquipSkillID( i );
                if( !SkillsData.IsValidSkill( skillID ) ) { continue; }

                var skillData = SkillsData.data[( int ) skillID];
                bool isTargetActionType = ( skillData.ActionType == ActionType.ATTACK ) ||
                                          ( includeAllyTargetSkills && skillData.ActionType == ActionType.HEAL );
                if( skillData.SituationType != SituationType.ATTACK ||
                    !isTargetActionType ||
                    tmpParam.IsSkillsUsed[i] ||
                    owner.GetStatusRef.CurActionGauge < tmpParam.ActGaugeConsumption + skillData.Cost )
                {
                    continue;
                }

                // 回復スキルは、HPが減っているキャラクターが対象とする勢力に1体もいなければ候補になり得ない(TryNotifyを参照)。
                // そのため、使用する位置ごとの範囲の計算(射程が広く重い)を行う前に除外する
                if( ActionType.HEAL == skillData.ActionType && !HasAnyDamagedCharacter( owner, SkillRangeRules.Get( skillID, _stageCtrl, owner ).TargetFaction ) )
                {
                    continue;
                }

                skillIDs.Add( skillID );
            }

            return skillIDs;
        }

        /// <summary>
        /// 指定のキャラクターから見て対象とする勢力に、HPが減っているキャラクターが1体でもいるかを取得します
        /// </summary>
        /// <param name="owner">基準となるキャラクター</param>
        /// <param name="targetFaction">対象とする勢力</param>
        /// <returns>いるか否か</returns>
        private bool HasAnyDamagedCharacter( Character owner, TargetFaction targetFaction )
        {
            var isTargetTag = BattleLogicBase.GetTargetFactionPredicate( targetFaction, owner.GetCharacterTag() );

            foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                if( !isTargetTag( chara.GetCharacterTag() ) ) { continue; }

                var status = chara.GetStatusRef;
                if( status.CurHP < status.MaxHP ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 指定の位置から通常攻撃を行う場合の候補を列挙します
        /// </summary>
        private bool EnumerateNormalAttack( Character owner, int standTileIdx, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            owner.BattleLogic.ActionRangeCtrl.ExtractAttackableRangeDataForNormalAttack( standTileIdx, tileData );

            return EnumerateSingleTargets( owner, standTileIdx, SkillID.NONE, tileData, onFound );
        }

        /// <summary>
        /// 指定の位置から指定のスキルで攻撃を行う場合の候補を列挙します
        /// </summary>
        private bool EnumerateSkillAttack( Character owner, int standTileIdx, SkillID skillID, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            var actionRangeCtrl = owner.BattleLogic.ActionRangeCtrl;
            var skillData       = SkillsData.data[( int ) skillID];
            var rules           = SkillRangeRules.Get( skillID, _stageCtrl, owner );

            actionRangeCtrl.ExtractAttackableRangeDataForSkill( standTileIdx, skillID, rules, ref tileData );

            switch( skillData.TargetingMode )
            {
                case TargetingMode.NORMAL_ATTACK:
                    return EnumerateSingleTargets( owner, standTileIdx, skillID, tileData, onFound );
                case TargetingMode.PART_OF_RANGE:
                    return EnumeratePartOfRangeTargets( owner, standTileIdx, skillID, skillData.TargetingRange, rules.TargetFaction, tileData, onFound );
                case TargetingMode.DIRECTIONAL:
                    // NPCは射程を調整せず、常に最大の射程で使用する(移動を伴うスキルでは、最も遠くへ着地する)
                    int maxRange = actionRangeCtrl.ResolveSkillMaxRange( standTileIdx, skillID, rules );
                    return EnumerateDirectionalTargets( owner, standTileIdx, skillID, maxRange, skillData.IsMovingSkill, rules, tileData, onFound );
                case TargetingMode.ALL:
                    return EnumerateAllTargets( owner, standTileIdx, skillID, tileData, onFound );
                default:
                    return false;
            }
        }

        /// <summary>
        /// 攻撃可能範囲内の攻撃対象1体ずつを、それぞれ候補として列挙します
        /// </summary>
        private bool EnumerateSingleTargets( Character owner, int standTileIdx, SkillID skillID, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            foreach( var pair in tileData.AttackableTileMap )
            {
                if( !Methods.HasAnyFlag( pair.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) ) { continue; }

                var targetKeys = new List<CharacterKey> { _stageCtrl.GetTileDynamicData( pair.Key ).CharaKey };
                if( TryNotify( owner, standTileIdx, skillID, Direction.NONE, -1, targetKeys, onFound ) ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 攻撃可能範囲内の各タイルを中心として範囲を展開し、展開した範囲内の攻撃対象をまとめて候補として列挙します。
        /// 攻撃対象の組み合わせが同じとなる候補は、1つにまとめます
        /// </summary>
        private bool EnumeratePartOfRangeTargets( Character owner, int standTileIdx, SkillID skillID, int expandRange, TargetFaction targetFaction, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            var foundTargetSets = new List<HashSet<CharacterKey>>();
            var expandData      = new ActionableTileData();

            foreach( int centerTileIdx in tileData.AttackableTileMap.Keys )
            {
                expandData.Init();
                _stageCtrl.TileDataHdlr().BeginExpandTargetableTilesWithPartOfRange( centerTileIdx, expandRange, owner.GetCharacterTag(), expandData, targetFaction );

                var targetKeys = ToCharacterKeys( expandData.RefAttackTargetTileIndicies );
                if( TargetFaction.ALLY == targetFaction ) { CorrectOwnerPositionInTargets( owner, standTileIdx, expandData, targetKeys ); }
                if( targetKeys.Count <= 0 ) { continue; }

                var targetSet = new HashSet<CharacterKey>( targetKeys );
                if( foundTargetSets.Any( set => set.SetEquals( targetSet ) ) ) { continue; }
                foundTargetSets.Add( targetSet );

                if( TryNotify( owner, standTileIdx, skillID, Direction.NONE, -1, targetKeys, onFound ) ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 自軍を対象とする範囲の対象の一覧について、使用者自身が対象に含まれるかを、移動後の位置(攻撃を行う位置)で判定し直します。
        /// 候補の計算時点では使用者は元のタイルに立ったままであるため、範囲の計算上は元のタイルで対象に含まれるかが判定されてしまうためです
        /// </summary>
        private static void CorrectOwnerPositionInTargets( Character owner, int standTileIdx, ActionableTileData expandData, List<CharacterKey> targetKeys )
        {
            var ownerKey = owner.GetCharacterKey();

            targetKeys.Remove( ownerKey );
            if( expandData.TargetableTileMap.ContainsKey( standTileIdx ) ) { targetKeys.Add( ownerKey ); }
        }

        /// <summary>
        /// 前後左右の各向きについて、向きに沿った範囲内の攻撃対象をまとめて候補として列挙します
        /// </summary>
        private bool EnumerateDirectionalTargets( Character owner, int standTileIdx, SkillID skillID, int range, bool isMovingSkill, SkillRangeRules rules, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            for( int i = 0; i < ( int ) Direction.NUM; ++i )
            {
                var dir     = ( Direction ) i;
                var forward = Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[i], 0f ) * Vector3.forward;

                tileData.ClearTargetableTile();
                tileData.ClearAttackTargetTileIndicies();
                int landingTileIdx = DirectionalTargetingRange.ExtractTargetableTiles( standTileIdx, forward, range, isMovingSkill, rules, _stageCtrl, owner.GetCharacterKey(), tileData );

                // 移動を伴うスキルで着地先が存在しない場合は実行出来ない
                if( isMovingSkill && landingTileIdx < 0 ) { continue; }

                var targetKeys = ToCharacterKeys( tileData.RefAttackTargetTileIndicies );
                if( targetKeys.Count <= 0 ) { continue; }

                if( TryNotify( owner, standTileIdx, skillID, dir, landingTileIdx, targetKeys, onFound ) ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 攻撃可能範囲内の攻撃対象すべてを、1つの候補として列挙します
        /// </summary>
        private bool EnumerateAllTargets( Character owner, int standTileIdx, SkillID skillID, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            var targetTileIndices = tileData.AttackableTileMap
                .Where( pair => Methods.HasAnyFlag( pair.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                .Select( pair => pair.Key )
                .ToList();

            var targetKeys = ToCharacterKeys( targetTileIndices );
            if( targetKeys.Count <= 0 ) { return false; }

            return TryNotify( owner, standTileIdx, skillID, Direction.NONE, -1, targetKeys, onFound );
        }

        /// <summary>
        /// 候補を作成してonFoundへ渡します。主となる攻撃対象は、攻撃を行う位置から最も近い攻撃対象とします
        /// (回復スキルの場合は、対象の中で最もHPが減っている味方とします)
        /// </summary>
        /// <returns>列挙を打ち切るか否か</returns>
        private bool TryNotify( Character owner, int standTileIdx, SkillID skillID, Direction dir, int landingTileIdx, List<CharacterKey> targetKeys, Func<AttackOption, bool> onFound )
        {
            Character mainTarget    = null;
            int minRange            = int.MaxValue;

            if( SkillsData.IsValidSkill( skillID ) && ActionType.HEAL == SkillsData.data[( int ) skillID].ActionType )
            {
                // HPが減っている味方が1体もいない場合は、回復しても意味が無いため候補としない
                mainTarget = FindMostDamagedTarget( targetKeys );
                if( mainTarget == null ) { return false; }

                return onFound( CreateOption( standTileIdx, skillID, dir, landingTileIdx, mainTarget, targetKeys ) );
            }

            foreach( var key in targetKeys )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( key );
                if( target == null ) { continue; }

                int range = _stageCtrl.CalculateTotalRange( standTileIdx, target.BattleParams.TmpParam.CurrentTileIndex );
                if( range < minRange )
                {
                    minRange    = range;
                    mainTarget  = target;
                }
            }

            if( mainTarget == null ) { return false; }

            return onFound( CreateOption( standTileIdx, skillID, dir, landingTileIdx, mainTarget, targetKeys ) );
        }

        private static AttackOption CreateOption( int standTileIdx, SkillID skillID, Direction dir, int landingTileIdx, Character mainTarget, List<CharacterKey> targetKeys )
        {
            return new AttackOption
            {
                StandTileIndex      = standTileIdx,
                SkillID             = skillID,
                Direction           = dir,
                LandingTileIndex    = landingTileIdx,
                MainTarget          = mainTarget,
                AttackTargetKeys    = targetKeys,
            };
        }

        /// <summary>
        /// 対象の中から、最もHPが減っている(最大HPまでの不足分が最も大きい)キャラクターを求めます
        /// </summary>
        /// <returns>最もHPが減っているキャラクター。全員のHPが満タンの場合はnull</returns>
        private Character FindMostDamagedTarget( List<CharacterKey> targetKeys )
        {
            Character mostDamaged   = null;
            int maxLackingHp        = 0;

            foreach( var key in targetKeys )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( key );
                if( target == null ) { continue; }

                var status      = target.GetStatusRef;
                int lackingHp   = status.MaxHP - status.CurHP;
                if( maxLackingHp < lackingHp )
                {
                    maxLackingHp    = lackingHp;
                    mostDamaged     = target;
                }
            }

            return mostDamaged;
        }

        /// <summary>
        /// 攻撃対象タイルの一覧から、そのタイルに存在するキャラクターの一覧を求めます
        /// </summary>
        private List<CharacterKey> ToCharacterKeys( IReadOnlyList<int> tileIndices )
        {
            var keys = new List<CharacterKey>( tileIndices.Count );
            foreach( int tileIdx in tileIndices )
            {
                var key = _stageCtrl.GetTileDynamicData( tileIdx ).CharaKey;
                if( key.IsValid() && !keys.Contains( key ) ) { keys.Add( key ); }
            }

            return keys;
        }
    }
}
