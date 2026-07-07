
using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Combat
{
    public class DashSlashSA : MovingSkillActionBase
    {
        // SkillID.SKILL_DASH_SLASHが選択されたとき、攻撃範囲の後処理として
        // ActionRangeController.SetupAttackableRangeData から渡されるコールバック
        public static readonly TileDataHandler.AttackableDataPostProcessor FilterAttackTargets = FilterAttackTargetsImpl;

        private static void FilterAttackTargetsImpl( int dprtIdx, int atkRng, int colNum, ActionableTileData actionableTileData )
        {
            // 4方向それぞれについて「着地タイルを跨いでいない敵」を攻撃対象から除外する
            int[] dirOffsets = { colNum, 1, -colNum, -1 };
            foreach( int dirOffset in dirOffsets )
            {
                FilterAttackTargetsInDirection( dprtIdx, atkRng, dirOffset, actionableTileData );
            }
        }

        // 指定の1方向について走査し、有効な着地タイル（空きタイル）を跨がない敵を除外する
        // 例: [敵][敵][空] → 敵×2は有効、[空][敵][敵] → 敵×2は除外、[敵][敵][敵] → 全除外
        private static void FilterAttackTargetsInDirection( int dprtIdx, int atkRng, int dirOffset, ActionableTileData actionableTileData )
        {
            var attackableMap       = actionableTileData.AttackableTileMap;
            var pendingEnemyIndices = new List<int>();

            int tileIdx = dprtIdx;
            for( int step = 0; step < atkRng; ++step )
            {
                tileIdx += dirOffset;

                // マップに存在しない場合は範囲外・CANNOT_MOVE・端境界などで走査を停止
                if( !attackableMap.TryGetValue( tileIdx, out var tileData ) ) { break; }

                if( Methods.HasAnyFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    // 敵タイル → 有効な着地タイルが後続にあるか確認が必要なので保留
                    pendingEnemyIndices.Add( tileIdx );
                }
                else if( !tileData.CharaKey.IsValid() )
                {
                    // 空タイル（キャラクターなし）→ 有効な着地点として確定し、保留中の敵を全て有効化
                    pendingEnemyIndices.Clear();
                }
                else
                {
                    // 味方など通過・着地不可なキャラクターが存在 → 以降の走査を中断
                    break;
                }
            }

            // 走査終了時に保留中の敵は有効な着地タイルが見つからなかったため攻撃対象から除外
            foreach( int idx in pendingEnemyIndices )
            {
                var tile = attackableMap[idx];
                Methods.UnsetBitFlag( ref tile.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
            }
        }

        private enum DashSlashState
        {
            START,
            WAIT_SLASH,
            SLASHING,
            WAIT_END,
            END
        }

        private DashSlashState _state;
        private Vector3 _velocity;

        [Inject]
        public DashSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem )
            : base( owner, targetCharaKeys, btlRtnCtrl, stageCtrl, uiSystem )
        {
            _attackAnimTag = AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER;
        }

        protected override void StartAction()
        {
            base.StartAction();
            Debug.Log( "DashSlashSA: StartAction" );

            // 向き(GetOrderedForward)ではなく、実座標から算出した目標地点への方向を用いることで、
            // 向きの補間誤差(CHARACTER_ROT_THRESHOLD等)による直線からのズレを防ぐ
            var dirXZ = ( _goalPosition - _owner.GetPosition() ).XZ().normalized;
            _velocity = dirXZ * Constants.DASH_SLASH_INITIAL_SPEED;

            _state = DashSlashState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            switch( _state )
            {
                case DashSlashState.START:
                    _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_INITIAL );
                    _state = DashSlashState.WAIT_SLASH;
                    break;
                case DashSlashState.WAIT_SLASH:
                    if( _owner.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_INITIAL ) )
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER );
                        _owner.SetVelocityAndAcceleration( _velocity, Vector3.zero );
                        _state = DashSlashState.SLASHING;
                    }
                    break;
                case DashSlashState.SLASHING:
                    UpdateAttack2TargetCharacters();
                    UpdateAttackAnimEnd();

                    if( Methods.IsPassedPosition( _owner.GetPosition(), _goalPosition, _velocity ) )
                    {
                        _owner.ResetVelocityAcceleration();
                        _owner.SetPosition( _goalPosition );
                        _owner.BattleParams.TmpParam.CurrentTileIndex = _goalTileIndex;
                        _state = DashSlashState.WAIT_END;
                    }
                    break;
                case DashSlashState.WAIT_END:
                    UpdateAttackAnimEnd();
                    if( _isAttackAnimEnded )
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.WAIT );
                        _state = DashSlashState.END;
                    }
                    break;
                case DashSlashState.END:
                    EndAction();
                    break;
            }
        }

        protected override bool IsFinished()
        {
            return _state == DashSlashState.END;
        }

        protected override bool IsInAttackRange( Character target )
        {
            Vector3 ownerPos  = _owner.GetPosition();
            Vector3 targetPos = target.GetPosition();
            return ( targetPos - ownerPos ).XZ().magnitude <= Constants.DASH_SLASH_ATTACK_TRIGGER_DISTANCE;
        }
    }
}
