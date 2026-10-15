
using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier.Combat
{
    public class JumpSlashSA : MovingSkillActionBase
    {
        /// <summary>
        /// ジャンプ斬りの範囲計算ルールを生成します(SkillRangeRules から参照されます)
        /// 高さ情報が必要なため、StageController を閉じ込めたデリゲートを設定します
        /// </summary>
        public static SkillRangeRules CreateRangeRules( StageController stageCtrl )
        {
            return new SkillRangeRules(
                postProcessor         : ( dprtIdx, atkRng, colNum, actionableTileData ) => FilterAttackTargets( dprtIdx, atkRng, colNum, actionableTileData, stageCtrl ),
                ghostResolver         : ResolveLandingTile,
                rangeAdjustmentFilter : IsKeepTileBeyondGhost );
        }

        // ゴースト距離より遠いタイルの保持・除外を判定するスキル固有フィルタ
        // ゴーストの直後 1 マスかつ攻撃対象フラグがある場合のみ保持する
        private static bool IsKeepTileBeyondGhost( int candidateIdx, int ghostRange, int candidateDist, TileDynamicData candidateTileData )
        {
            return candidateDist == ghostRange + 1 &&
                Methods.HasAnyFlag( candidateTileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
        }

        private static void FilterAttackTargets( int dprtIdx, int atkRng, int colNum, ActionableTileData actionableTileData, StageController stageCtrl )
        {
            int[] dirOffsets = { colNum, 1, -colNum, -1 };
            foreach( int dirOffset in dirOffsets )
            {
                FilterAttackTargetsInDirection( dprtIdx, atkRng, dirOffset, actionableTileData, stageCtrl );
            }
        }

        // 指定の1方向について走査し、着地条件・高さ条件を満たさない敵を攻撃対象から除外する
        private static void FilterAttackTargetsInDirection( int dprtIdx, int atkRng, int dirOffset, ActionableTileData actionableTileData, StageController stageCtrl )
        {
            var attackableMap = actionableTileData.AttackableTileMap;

            // 方向内の全敵タイルを収集しつつ ATTACKABLE_TARGET_EXIST フラグを一旦解除する
            var allEnemyIndices = new HashSet<int>();
            for( int l = 1; l <= atkRng; ++l )
            {
                int tileIdx = dprtIdx + l * dirOffset;
                if( !attackableMap.TryGetValue( tileIdx, out var tileData ) ) { break; }

                if( Methods.HasAnyFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    allEnemyIndices.Add( tileIdx );
                    Methods.UnsetBitFlag( ref tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                }
            }

            // 着地タイルの直後にいる敵のみフラグを復元する
            ScanJumpLine( dprtIdx, dirOffset, atkRng, attackableMap, allEnemyIndices.Contains, tileData => true, stageCtrl, out _, out int targetTileIdx );
            if( 0 <= targetTileIdx )
            {
                Methods.SetBitFlag( ref attackableMap[targetTileIdx].Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
            }
        }

        /// <summary>
        /// 着地先(ゴースト)のタイルを決定します。攻撃対象の絞り込みと同じ条件で走査し、ジャンプで着地可能な最も遠いタイルを返します。
        /// 着地可能なタイルが存在しない場合は-1を返します。
        /// </summary>
        private static int ResolveLandingTile( int dprtIdx, ActionableTileData actionableTileData, StageController stageCtrl, CharacterKey selfKey )
        {
            var targetableMap = actionableTileData.TargetableTileMap;
            if( targetableMap.Count <= 0 ) { return -1; }

            // ターゲット可能タイルは全て同一方向の直線上にあるため、最も近いタイル(隣接タイル)との差分から進行方向を求める
            int nearestTileIdx = targetableMap.Keys.OrderBy( idx => stageCtrl.CalculateTotalRange( dprtIdx, idx ) ).First();
            if( stageCtrl.CalculateTotalRange( dprtIdx, nearestTileIdx ) != 1 ) { return -1; }

            int dirOffset  = nearestTileIdx - dprtIdx;
            int maxL       = targetableMap.Keys.Max( idx => stageCtrl.CalculateTotalRange( dprtIdx, idx ) );
            var isOpponent = BattleLogicBase.IsOpponentFaction[( int ) selfKey.CharacterTag];

            ScanJumpLine( dprtIdx, dirOffset, maxL, targetableMap,
                idx => isOpponent( targetableMap[idx].CharaKey.CharacterTag ),
                tileData => tileData.IsStandableBy( selfKey ),
                stageCtrl, out int landingTileIdx, out _ );

            return landingTileIdx;
        }

        /// <summary>
        /// 指定方向へ1マスずつ走査し、ジャンプで着地可能な最も遠いタイルと、その直後にいる攻撃対象のタイルを求めます。
        /// 攻撃対象の絞り込みと着地先の決定で同じ条件を用いるための共通処理です。
        /// 判定条件:
        ///   着地タイル: 0 <= 1 - l + n (l=距離, n=自身タイル高さ-対象タイル高さ)
        ///   攻撃対象:   着地タイルの直後 かつ 着地タイルとの高さの差が 1.0f 未満
        /// </summary>
        /// <param name="tileMap">走査するタイルの範囲。含まれないタイルに達した時点で走査を終了する</param>
        /// <param name="isOpponentTile">敵が存在するタイルかどうか</param>
        /// <param name="isLandable">キャラクターの存在しないタイルのうち、着地可能なタイルかどうか(他キャラクターの着地予約など)</param>
        /// <param name="landingTileIdx">着地可能な最も遠いタイル。存在しない場合は-1</param>
        /// <param name="targetTileIdx">攻撃対象となる敵のタイル。存在しない場合は-1</param>
        private static void ScanJumpLine( int dprtIdx, int dirOffset, int maxL, Dictionary<int, TileDynamicData> tileMap, Func<int, bool> isOpponentTile, Func<TileDynamicData, bool> isLandable, StageController stageCtrl, out int landingTileIdx, out int targetTileIdx )
        {
            landingTileIdx = -1;
            targetTileIdx  = -1;

            float selfHeight             = stageCtrl.GetTileStaticData( dprtIdx ).Height;
            int   lastValidLandingL      = -1;
            float lastValidLandingHeight = 0f;
            for( int l = 1; l <= maxL; ++l )
            {
                int tileIdx = dprtIdx + l * dirOffset;
                if( !tileMap.TryGetValue( tileIdx, out var tileData ) ) { break; }

                float tileHeight = stageCtrl.GetTileStaticData( tileIdx ).Height;

                if( isOpponentTile( tileIdx ) )
                {
                    if( lastValidLandingL == l - 1 )
                    {
                        // 直前が有効な着地タイル → 高さの差が 1.0f 未満なら攻撃対象とする
                        if( Mathf.Abs( tileHeight - lastValidLandingHeight ) < 1.0f )
                        {
                            targetTileIdx = tileIdx;
                        }
                        break;  // 着地タイル直後の敵を確認したら走査終了
                    }
                    // 直前が着地タイルでない場合は飛び越え対象 → 走査継続
                }
                else if( !tileData.CharaKey.IsValid() )
                {
                    // ジャンプ条件: 0 <= 1 - l + (selfHeight - tileHeight)
                    float n = selfHeight - tileHeight;
                    if( 0f > 1f - (float)l + n )
                    {
                        break;  // 条件不成立 → 以降のタイルへも到達不可
                    }

                    if( isLandable( tileData ) )
                    {
                        lastValidLandingL      = l;
                        lastValidLandingHeight = tileHeight;
                        landingTileIdx         = tileIdx;
                    }
                }
                else
                {
                    break;  // 味方など通過不可なキャラクターが存在 → 走査停止
                }
            }
        }

        private enum JumpSlashState
        {
            START,
            JUMPING,
            WAIT_END,
            END
        }

        private JumpSlashState _state;
        private bool _isAtLatter;
        private Vector3 _xzVelocity;

        [Inject]
        public JumpSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem )
            : base( owner, targetCharaKeys, btlRtnCtrl, stageCtrl, uiSystem )
        {
            _attackAnimTag = AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER;
            _skillID       = SkillID.JUMP_SLASH;
        }

        protected override void StartAction()
        {
            base.StartAction();

            _isAtLatter = false;

            _state = JumpSlashState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            switch( _state )
            {
                case JumpSlashState.START:
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_INITIAL );

                        // 着地タイルへの放物運動を開始する
                        // XZ 方向の速度を設定した後、StartSkillJump で Y 方向の初速・加速度を上書きする
                        var startPos = _owner.GetPosition();
                        var xzDir = ( _goalPosition - startPos ).XZ().normalized;
                        _xzVelocity = xzDir * Constants.JUMP_SLASH_INITIAL_SPEED;
                        _owner.SetVelocityAndAcceleration( _xzVelocity, Vector3.zero );
                        float speedRate = Constants.JUMP_SLASH_INITIAL_SPEED / Constants.CHARACTER_MOVE_SPEED;
                        _owner.StartSkillJump( startPos, _goalPosition, speedRate );

                        _state = JumpSlashState.JUMPING;
                        break;
                    }
                case JumpSlashState.JUMPING:
                    {
                        // 前フレームより Y 座標が下がった時点を放物運動の頂点通過とみなし、アニメーションを切り替える
                        if( !_isAtLatter )
                        {
                            float curY = _owner.GetPosition().y;
                            float prevY = _owner.GetPreviousPosition().y;
                            if( curY < prevY )
                            {
                                _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER );
                                _isAtLatter = true;

                                // 頂点を過ぎて降下(＝被弾)に転じるタイミングでカメラを寄りアングルへ遷移させる
                                _cameraProcess?.TransitNextPhase();
                            }
                        }

                        // UpdateAttack2TargetCharacters();
                        UpdateAttackAnimEnd();

                        if( Methods.IsPassedPosition( _owner.GetPosition(), _goalPosition, _xzVelocity ) )
                        {
                            UpdateAttack2TargetCharacters();

                            _owner.ResetVelocityAcceleration();
                            _owner.SetPosition( _goalPosition );
                            _owner.BattleParams.TmpParam.CurrentTileIndex = _goalTileIndex;
                            _state = JumpSlashState.WAIT_END;
                        }
                        break;
                    }
                case JumpSlashState.WAIT_END:
                    {
                        UpdateAttackAnimEnd();
                        if( _isAttackAnimEnded )
                        {
                            _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.WAIT );
                            _state = JumpSlashState.END;
                        }
                        break;
                    }
                case JumpSlashState.END:
                    EndAction();
                    break;
            }
        }

        protected override bool IsFinished()
        {
            return _state == JumpSlashState.END;
        }

        // スキル使用者と攻撃対象の Y 座標の差が 1.0f 未満になった際を攻撃判定タイミングとする
        protected override bool IsInAttackRange( Character target )
        {
            float ownerY  = _owner.GetPosition().y;
            float targetY = target.GetPosition().y;
            return Mathf.Abs( ownerY - targetY ) < 1.0f;
        }
    }
}
