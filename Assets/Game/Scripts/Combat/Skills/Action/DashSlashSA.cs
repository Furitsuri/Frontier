
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
        /// <summary>
        /// ダッシュ斬りの範囲計算ルールを生成します(SkillRangeRules から参照されます)
        /// </summary>
        public static SkillRangeRules CreateRangeRules( StageController stageCtrl )
        {
            return new SkillRangeRules(
                reachability  : ( dprtIdx, prevIdx, tgtIdx, remainingRange ) => IsPassableByDash( prevIdx, tgtIdx, stageCtrl ),
                postProcessor : FilterAttackTargets );
        }

        // 地面を走って移動するため、直前のタイルとの高低差が一定未満である場合のみ先へ進める
        // (出発地点との高低差ではなく、1マスごとの段差で判定する)
        private static bool IsPassableByDash( int prevIdx, int tgtIdx, StageController stageCtrl )
        {
            float prevHeight = stageCtrl.GetTileStaticData( prevIdx ).Height;
            float tgtHeight  = stageCtrl.GetTileStaticData( tgtIdx ).Height;
            return Mathf.Abs( tgtHeight - prevHeight ) < Constants.DASH_SLASH_PASSABLE_HEIGHT_DIFF;
        }

        private static void FilterAttackTargets( int dprtIdx, int atkRng, int colNum, ActionableTileData actionableTileData )
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
                    // 味方が存在しているなど着地不可なタイル → 次のタイルを走査
                    continue;
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
        // 出発タイルから着地先までの経路上の各タイルの立ち位置(先頭が出発タイル)
        private readonly List<Vector3> _pathStandPositions = new List<Vector3>();
        // 経路上で現在侵入しているタイルの位置(_pathStandPositionsの添字)
        private int _pathTileCursor;
        private Vector3 _velocity;

        [Inject]
        public DashSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem )
            : base( owner, targetCharaKeys, btlRtnCtrl, stageCtrl, uiSystem )
        {
            _attackAnimTag = AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER;
            _skillID       = SkillID.DASH_SLASH;
        }

        protected override void StartAction()
        {
            base.StartAction();
            Debug.Log( "DashSlashSA: StartAction" );

            // 向き(GetOrderedForward)ではなく、実座標から算出した目標地点への方向を用いることで、
            // 向きの補間誤差(CHARACTER_ROT_THRESHOLD等)による直線からのズレを防ぐ
            var dirXZ = ( _goalPosition - _owner.GetPosition() ).XZ().normalized;
            _velocity = dirXZ * Constants.DASH_SLASH_INITIAL_SPEED;

            BuildDashPath();

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
                    FollowEnteredTileHeight();
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

        /// <summary>
        /// 出発タイルから着地先までの経路上の各タイルの立ち位置を、出発タイルを先頭として順に求めます
        /// </summary>
        private void BuildDashPath()
        {
            _pathStandPositions.Clear();
            _pathTileCursor = 0;

            int startTileIdx = _owner.BattleParams.TmpParam.CurrentTileIndex;
            _pathStandPositions.Add( _stageCtrl.GetTileStaticData( startTileIdx ).CharaStandPos );

            int steps = _stageCtrl.CalculateTotalRange( startTileIdx, _goalTileIndex );
            if( steps <= 0 ) { return; }

            // 着地先は出発タイルから直線上にあるため、1マスあたりのインデックスの差は一定となる
            int stepOffset = ( _goalTileIndex - startTileIdx ) / steps;
            for( int l = 1; l <= steps; ++l )
            {
                _pathStandPositions.Add( _stageCtrl.GetTileStaticData( startTileIdx + l * stepOffset ).CharaStandPos );
            }
        }

        /// <summary>
        /// 経路上のどのタイルへ侵入しているかを水平方向の進んだ距離から判定し、
        /// 新たなタイルへ侵入した時点で、キャラクターの高さをそのタイルの高さに合わせます
        /// </summary>
        private void FollowEnteredTileHeight()
        {
            var   curPos    = _owner.GetPosition();
            float traveled  = ( curPos - _pathStandPositions[0] ).XZ().magnitude;
            // タイルの中心同士の中間点を越えた時点で、次のタイルへ侵入したとみなす
            int   enteredIdx = Mathf.Clamp( Mathf.RoundToInt( traveled / Constants.TILE_SIZE ), 0, _pathStandPositions.Count - 1 );
            if( enteredIdx == _pathTileCursor ) { return; }

            _pathTileCursor = enteredIdx;
            _owner.SetPosition( new Vector3( curPos.x, _pathStandPositions[enteredIdx].y, curPos.z ) );
        }

        protected override bool IsInAttackRange( Character target )
        {
            Vector3 ownerPos  = _owner.GetPosition();
            Vector3 targetPos = target.GetPosition();
            return ( targetPos - ownerPos ).XZ().magnitude <= Constants.DASH_SLASH_ATTACK_TRIGGER_DISTANCE;
        }
    }
}
