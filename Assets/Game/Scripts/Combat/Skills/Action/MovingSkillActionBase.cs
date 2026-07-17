
using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// 移動を伴うスキルアクション(ダッシュ斬り、ジャンプ斬り等)の共通処理を担う基底クラスです。
    /// ゴーストの着地地点取得・タイル予約・対象への攻撃適用などを共通化し、
    /// 移動方式固有の処理(速度計算、アニメーション状態遷移、攻撃判定方法)はサブクラスに委ねます。
    /// </summary>
    public abstract class MovingSkillActionBase : SkillActionBase
    {
        protected BattleCharacterCoordinator _btlCharaCdr = null;
        protected StageController _stageCtrl              = null;

        protected bool _isAttackAnimEnded;
        protected int _goalTileIndex = -1;
        protected Vector3 _goalPosition;
        protected List<Character> _targetCharacters = null;

        // 攻撃アニメーションの終了判定に用いるタグ。使用するアクションによって異なるため、
        // サブクラスのコンストラクタで適切なタグを代入すること
        protected AnimDatas.AnimeConditionsTag _attackAnimTag = AnimDatas.AnimeConditionsTag.NONE;

        public MovingSkillActionBase( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem ) : base( owner, uiSystem, btlRtnCtrl.GetBtlCameraCtrl )
        {
            _targetCharacters   = new List<Character>();
            _btlCharaCdr        = btlRtnCtrl.BtlCharaCdr;
            _stageCtrl          = stageCtrl;

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

            _isAttackAnimEnded = false;
            SortTargetCharactersByDistance();

            // 全ターゲットのダメージ予測を計算
            foreach( var target in _targetCharacters )
            {
                _btlCharaCdr.ApplyDamageExpect( _owner, target );
            }

            // ゴーストの位置が移動目標地点
            var ghostObject = _owner.GetGhostObject();
            Debug.Assert( ghostObject != null );
            _goalTileIndex  = ghostObject.TileIndex;
            _goalPosition   = ghostObject.transform.position;
            _owner.CleanupGhost();

            // 着地予定地を予約し、他キャラクターが移動中に留まれないようにする（EndActionで解除）
            _stageCtrl.TileDataHdlr().ReserveTile( _goalTileIndex );
        }

        protected override void EndAction()
        {
            base.EndAction();

            _stageCtrl.TileDataHdlr().ReleaseTile( _goalTileIndex ); // 着地予定地の予約を解除

            _stageCtrl.UnbindGridCursor();                          // アタッカーキャラクターの設定を解除
            _stageCtrl.ApplyGridCursor2CharacterTile( _owner );
            _stageCtrl.SetActiveGridCursor( true );                 // 選択グリッドを表示
            _stageCtrl.SetActiveTargetCursor( false );              // ターゲットカーソルを非表示
        }

        /// <summary>
        /// 対象キャラクターが現在攻撃判定の範囲内にいるかをサブクラス固有の方法で判定します
        /// </summary>
        protected abstract bool IsInAttackRange( Character target );

        protected void SortTargetCharactersByDistance()
        {
            Vector3 ownerPos = _owner.GetPosition();
            _targetCharacters.Sort( ( a, b ) =>
            {
                float distA = ( a.GetPosition() - ownerPos ).XZ().sqrMagnitude;
                float distB = ( b.GetPosition() - ownerPos ).XZ().sqrMagnitude;
                return distA.CompareTo( distB );
            } );
        }

        protected void UpdateAttackAnimEnd()
        {
            if( !_isAttackAnimEnded )
            {
                _isAttackAnimEnded = _owner.AnimCtrl.IsEndAnimationOnConditionTag( _attackAnimTag );
            }
        }

        protected bool UpdateAttack2TargetCharacters()
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
    }
}
