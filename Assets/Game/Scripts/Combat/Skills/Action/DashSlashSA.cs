using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static UnityEngine.UI.GridLayoutGroup;

namespace Frontier.Combat
{
    public class DashSlashSA : SkillActionBase
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

        private IUiSystem _uiSystem                     = null;
        private BattleCharacterCoordinator _btlCharaCdr = null;
        private StageController _stageCtrl              = null;

        private DashSlashState _state;
        private bool _isAttackAnimEnded;
        private int _goalTileIndex = -1;
        private Vector3 _velocity;
        private Vector3 _goalPosition;
        private List<Character> _targetCharacters = null;

        [Inject]
        public DashSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem ) : base( owner )
        {
            _targetCharacters   = new List<Character>();
            _btlCharaCdr        = btlRtnCtrl.BtlCharaCdr;
            _stageCtrl          = stageCtrl;
            _uiSystem           = uiSystem;

            foreach( var key in targetCharaKeys )
            {
                var targetCharacter = _btlCharaCdr.GetCharacter( key );
                if( null != targetCharacter )
                {
                    _targetCharacters.Add( targetCharacter );
                }
            }
        }

        protected override void StartAction()
        {
            base.StartAction();
            Debug.Log( "DashSlashSA: StartAction" );

            _isAttackAnimEnded = false;
            SortTargetCharactersByDistance();

            // 全ターゲットのダメージ予測を計算
            foreach( var target in _targetCharacters )
            {
                _btlCharaCdr.ApplyDamageExpect( _owner, target );
            }

            _velocity = _owner.GetOrderedForward() * Constants.DASH_SLASH_INITIAL_SPEED;

            // ゴーストの位置が移動目標地点
            var ghostObject = _owner.GetGhostObject();
            Debug.Assert( ghostObject != null );
            _goalTileIndex  = ghostObject.TileIndex;
            _goalPosition   = ghostObject.transform.position;
            _owner.CleanupGhost();

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
                    UpdateSlashAnimEnd();

                    if( Methods.IsPassedPosition( _owner.GetPosition(), _goalPosition, _velocity ) )
                    {
                        _owner.ResetVelocityAcceleration();
                        _owner.BattleParams.TmpParam.CurrentTileIndex = _goalTileIndex;
                        _state = DashSlashState.WAIT_END;
                    }
                    break;
                case DashSlashState.WAIT_END:
                    UpdateSlashAnimEnd();
                    if( _isAttackAnimEnded )
                    {
                        _state = DashSlashState.END;
                    }
                    break;
                case DashSlashState.END:
                    EndAction();
                    break;
            }
        }

        protected override void EndAction()
        {
            base.EndAction();

            _stageCtrl.UnbindGridCursor();                          // アタッカーキャラクターの設定を解除
            _stageCtrl.ApplyGridCursor2CharacterTile( _owner );
            _stageCtrl.SetActiveGridCursor( true );                 // 選択グリッドを表示
            _stageCtrl.SetActiveTargetCursor( false );              // ターゲットカーソルを非表示
        }

        protected override bool IsFinished()
        {
            return _state == DashSlashState.END;
        }

        private void SortTargetCharactersByDistance()
        {
            Vector3 ownerPos = _owner.GetPosition();
            _targetCharacters.Sort( ( a, b ) =>
            {
                float distA = ( a.GetPosition() - ownerPos ).XZ().sqrMagnitude;
                float distB = ( b.GetPosition() - ownerPos ).XZ().sqrMagnitude;
                return distA.CompareTo( distB );
            } );
        }

        private bool IsInAttackRange( Character target )
        {
            Vector3 ownerPos  = _owner.GetPosition();
            Vector3 targetPos = target.GetPosition();
            return ( targetPos - ownerPos ).XZ().magnitude <= Constants.DASH_SLASH_ATTACK_TRIGGER_DISTANCE;
        }

        private void UpdateSlashAnimEnd()
        {
            if( !_isAttackAnimEnded )
            {
                _isAttackAnimEnded = _owner.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER );
            }
        }

        private bool UpdateAttack2TargetCharacters()
        {
            bool attacked = false;
            for( int i = _targetCharacters.Count - 1; i >= 0; --i )
            {
                if( IsInAttackRange( _targetCharacters[i] ) )
                {
                    ApplyDamageToTarget( _targetCharacters[i] );
                    _targetCharacters.RemoveAt( i );
                    attacked = true;
                }
            }
            return attacked;
        }

        private void ApplyDamageToTarget( Character target )
        {
            int hpChange = target.BattleParams.TmpParam.ExpectedHpChange;
            target.GetStatusRef.CurHP += hpChange;

            if( hpChange != 0 )
            {
                if( target.GetStatusRef.CurHP <= 0 )
                {
                    target.GetStatusRef.CurHP = 0;
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DIE );
                }
                else
                {
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
                }
            }

            _uiSystem.BattleUi.ShowDamageOnCharacter( target, 1f );
        }
    }
}
