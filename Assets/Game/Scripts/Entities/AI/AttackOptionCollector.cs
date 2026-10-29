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
        /// 攻撃の候補をすべて列挙します
        /// </summary>
        /// <param name="owner">攻撃を行うキャラクター</param>
        /// <returns>攻撃の候補</returns>
        public List<AttackOption> Collect( Character owner )
        {
            var options = new List<AttackOption>();
            Enumerate( owner, option => { options.Add( option ); return false; } );

            return options;
        }

        /// <summary>
        /// 攻撃の候補が1つでも存在するかを判定します。
        /// 候補が見つかった時点で列挙を打ち切るため、Collectよりも軽量です
        /// </summary>
        /// <param name="owner">攻撃を行うキャラクター</param>
        /// <returns>存在するか否か</returns>
        public bool HasAnyOption( Character owner )
        {
            return Enumerate( owner, option => true );
        }

        /// <summary>
        /// 攻撃の候補を列挙し、見つかる度にonFoundを呼び出します
        /// </summary>
        /// <param name="onFound">候補が見つかった際の処理。trueを返した場合は列挙を打ち切ります</param>
        /// <returns>列挙を打ち切ったか否か</returns>
        private bool Enumerate( Character owner, Func<AttackOption, bool> onFound )
        {
            var tmpParam = owner.BattleParams.TmpParam;

            // 攻撃またはスキルを実行済みであれば、攻撃は行えない
            if( tmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] || tmpParam.IsEndCommand[( int ) COMMAND_TAG.SKILL] ) { return false; }

            var usableSkillIDs  = CollectUsableAttackSkills( owner );
            var tileData        = new ActionableTileData();

            foreach( int standTileIdx in CollectStandableTiles( owner ) )
            {
                if( EnumerateNormalAttack( owner, standTileIdx, tileData, onFound ) ) { return true; }

                foreach( var skillID in usableSkillIDs )
                {
                    if( EnumerateSkillAttack( owner, standTileIdx, skillID, tileData, onFound ) ) { return true; }
                }
            }

            return false;
        }

        /// <summary>
        /// 攻撃を行う位置として立つことが出来るタイルを求めます。
        /// 移動済みの場合は現在のタイルのみとなります
        /// </summary>
        private List<int> CollectStandableTiles( Character owner )
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
                if( 0 <= pair.Value.EstimatedMoveRange && pair.Value.IsStandableBy( ownerKey ) )
                {
                    standableTiles.Add( pair.Key );
                }
            }

            return standableTiles;
        }

        /// <summary>
        /// 装備しているスキルのうち、攻撃の候補となるスキルを求めます。
        /// 攻撃シチュエーションで使用する攻撃用のスキルで、未使用かつアクションゲージが足りているもののみが対象です。
        /// MEMO : 回復・サポートなど自軍を対象とするスキルは、攻撃の候補ではないため対象外としています
        /// </summary>
        private List<SkillID> CollectUsableAttackSkills( Character owner )
        {
            var skillIDs = new List<SkillID>( EQUIPABLE_SKILL_MAX_NUM );
            var tmpParam = owner.BattleParams.TmpParam;

            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                SkillID skillID = owner.GetEquipSkillID( i );
                if( !SkillsData.IsValidSkill( skillID ) ) { continue; }

                var skillData = SkillsData.data[( int ) skillID];
                if( skillData.SituationType != SituationType.ATTACK ||
                    skillData.ActionType != ActionType.ATTACK ||
                    tmpParam.IsSkillsUsed[i] ||
                    owner.GetStatusRef.CurActionGauge < tmpParam.ActGaugeConsumption + skillData.Cost )
                {
                    continue;
                }

                skillIDs.Add( skillID );
            }

            return skillIDs;
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
                    return EnumeratePartOfRangeTargets( owner, standTileIdx, skillID, skillData.TargetingRange, tileData, onFound );
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
        private bool EnumeratePartOfRangeTargets( Character owner, int standTileIdx, SkillID skillID, int expandRange, ActionableTileData tileData, Func<AttackOption, bool> onFound )
        {
            var foundTargetSets = new List<HashSet<CharacterKey>>();
            var expandData      = new ActionableTileData();

            foreach( int centerTileIdx in tileData.AttackableTileMap.Keys )
            {
                expandData.Init();
                _stageCtrl.TileDataHdlr().BeginExpandTargetableTilesWithPartOfRange( centerTileIdx, expandRange, owner.GetCharacterTag(), expandData );

                var targetKeys = ToCharacterKeys( expandData.RefAttackTargetTileIndicies );
                if( targetKeys.Count <= 0 ) { continue; }

                var targetSet = new HashSet<CharacterKey>( targetKeys );
                if( foundTargetSets.Any( set => set.SetEquals( targetSet ) ) ) { continue; }
                foundTargetSets.Add( targetSet );

                if( TryNotify( owner, standTileIdx, skillID, Direction.NONE, -1, targetKeys, onFound ) ) { return true; }
            }

            return false;
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
        /// </summary>
        /// <returns>列挙を打ち切るか否か</returns>
        private bool TryNotify( Character owner, int standTileIdx, SkillID skillID, Direction dir, int landingTileIdx, List<CharacterKey> targetKeys, Func<AttackOption, bool> onFound )
        {
            Character mainTarget    = null;
            int minRange            = int.MaxValue;

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

            var option = new AttackOption
            {
                StandTileIndex      = standTileIdx,
                SkillID             = skillID,
                Direction           = dir,
                LandingTileIndex    = landingTileIdx,
                MainTarget          = mainTarget,
                AttackTargetKeys    = targetKeys,
            };

            return onFound( option );
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
