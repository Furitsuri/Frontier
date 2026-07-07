
using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Combat
{
    public class JumpSlashSA : MovingSkillActionBase
    {
        // SkillID.SKILL_JUMP_SLASHが選択されたとき、攻撃範囲の後処理として
        // ActionRangeController.SetupAttackableRangeData から渡されるコールバックを生成する
        // 高さ情報が必要なため、StageController を閉じ込めたデリゲートを返す
        public static TileDataHandler.AttackableDataPostProcessor CreateFilterAttackTargets( StageController stageCtrl )
        {
            return ( dprtIdx, atkRng, colNum, actionableTileData ) =>
                FilterAttackTargetsImpl( dprtIdx, atkRng, colNum, actionableTileData, stageCtrl );
        }

        // ゴースト距離より遠いタイルの保持・除外を判定するスキル固有フィルタ
        // ゴーストの直後 1 マスかつ攻撃対象フラグがある場合のみ保持する
        public static TileDataHandler.RangeAdjustmentFilter CreateRangeAdjustmentFilter()
        {
            return ( candidateIdx, ghostRange, candidateDist, candidateTileData ) =>
                candidateDist == ghostRange + 1 &&
                Methods.HasAnyFlag( candidateTileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
        }

        private static void FilterAttackTargetsImpl( int dprtIdx, int atkRng, int colNum, ActionableTileData actionableTileData, StageController stageCtrl )
        {
            int[] dirOffsets = { colNum, 1, -colNum, -1 };
            foreach( int dirOffset in dirOffsets )
            {
                FilterAttackTargetsInDirection( dprtIdx, atkRng, dirOffset, actionableTileData, stageCtrl );
            }
        }

        // 指定の1方向について走査し、着地条件・高さ条件を満たさない敵を攻撃対象から除外する
        // 判定条件:
        //   着地タイル: 0 <= 1 - l + n (l=距離, n=自身タイル高さ-対象タイル高さ)
        //   攻撃対象:   着地タイルの直後 かつ 着地タイルとの高さの差が 1.0f 未満
        private static void FilterAttackTargetsInDirection( int dprtIdx, int atkRng, int dirOffset, ActionableTileData actionableTileData, StageController stageCtrl )
        {
            var attackableMap = actionableTileData.AttackableTileMap;
            float selfHeight  = stageCtrl.GetTileStaticData( dprtIdx ).Height;

            // 方向内の全敵タイルを収集しつつ ATTACKABLE_TARGET_EXIST フラグを一旦解除する
            var allEnemyIndices = new List<int>();
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

            // ジャンプ条件を満たす着地タイルを逐次更新し、その直後にいる敵のみフラグを復元する
            int   lastValidLandingL      = -1;
            float lastValidLandingHeight = 0f;
            for( int l = 1; l <= atkRng; ++l )
            {
                int tileIdx = dprtIdx + l * dirOffset;
                if( !attackableMap.TryGetValue( tileIdx, out var tileData ) ) { break; }

                float tileHeight = stageCtrl.GetTileStaticData( tileIdx ).Height;
                bool  isEnemy    = allEnemyIndices.Contains( tileIdx );

                if( isEnemy )
                {
                    if( lastValidLandingL == l - 1 )
                    {
                        // 直前が有効な着地タイル → 高さの差が 1.0f 未満なら攻撃対象として復元
                        if( Mathf.Abs( tileHeight - lastValidLandingHeight ) < 1.0f )
                        {
                            Methods.SetBitFlag( ref tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                        }
                        break;  // 着地タイル直後の敵を確認したら走査終了
                    }
                    // 直前が着地タイルでない場合は飛び越え対象 → フラグは解除済みのまま走査継続
                }
                else if( !tileData.CharaKey.IsValid() )
                {
                    // ジャンプ条件: 0 <= 1 - l + (selfHeight - tileHeight)
                    float n = selfHeight - tileHeight;
                    if( 0f > 1f - (float)l + n )
                    {
                        break;  // 条件不成立 → 以降のタイルへも到達不可
                    }
                    lastValidLandingL      = l;
                    lastValidLandingHeight = tileHeight;
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
