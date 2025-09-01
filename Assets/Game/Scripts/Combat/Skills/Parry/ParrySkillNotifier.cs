using Frontier;
using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using UnityEngine;
using Frontier.Combat.Skill;

namespace Frontier.Combat.Skill
{
    /// <summary>
    /// パリィスキルを使用するエンティティに持たせるクラスです
    /// </summary>
    public class ParrySkillNotifier : SkillNotifierBase
    {
        /// <summary>
        /// 指定のパリィ操作クラスがイベント終了した際に呼び出すデリゲートを設定します
        /// </summary>
        /// <param name="parryCtrl">パリィ操作クラス</param>
        void SubscribeParryEvent( ParrySkillHandler parryCtrl )
        {
            parryCtrl.ProcessCompleted += ParryEventProcessCompleted;
        }

        /// <summary>
        /// 指定のパリィ操作クラスがイベント終了した際に呼び出すデリゲート設定を解除します
        /// </summary>
        /// <param name="parryCtrl">パリィ操作クラス</param>
        void UnsubscribeParryEvent( ParrySkillHandler parryCtrl )
        {
            parryCtrl.ProcessCompleted -= ParryEventProcessCompleted;
        }

        /// <summary>
        /// パリィイベント終了時に呼び出されるデリゲート
        /// </summary>
        /// <param name="sender">呼び出しを行うパリィイベントコントローラ</param>
        /// <param name="e">イベントハンドラ用オブジェクト(この関数ではempty)</param>
        void ParryEventProcessCompleted( object sender, ParrySkillHdlrEventArgs e )
        {
            ParrySkillHandler ParryHdlr = sender as ParrySkillHandler;
            ParryHdlr.EndParryEvent();

            UnsubscribeParryEvent( ParryHdlr );

            _combatSkillEventCtrl.ScheduleExit();
        }

        /// <summary>
        /// 対戦相手の攻撃をパリィ(弾く)するイベントを発生させます
        /// ※攻撃アニメーションから呼び出されます
        /// </summary>
        public void ParryOpponentEvent()
        {
            ParrySkillHandler parryCtrl = _combatSkillEventCtrl.CurrentSkillHandler as ParrySkillHandler;
            if ( parryCtrl == null ) return;

            // NONE以外の結果が通知されているはず
            Debug.Assert( !parryCtrl.IsMatchResult( JudgeResult.NONE ) );

            if ( _skillUser == null )
            {
                Debug.Assert( false );
            }

            if ( parryCtrl.IsMatchResult( JudgeResult.FAILED ) )
            {
                return;
            }

            // 成功時(ジャスト含む)にはパリィ挙動
            ParryRecieveEvent();
        }

        /// <summary>
        /// パリィを受けた際のイベントを発生させます
        /// </summary>
        public void ParryRecieveEvent()
        {
            Character opponent = _skillUser.GetOpponentChara();
            NullCheck.AssertNotNull( opponent );

            opponent.GetTimeScale.Reset();
            opponent.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
        }

        /// <summary>
        /// パリィ判定処理を開始します
        /// </summary>
        public void StartParryJudgeEvent()
        {
            if ( _skillUser.GetUsingSkillSlotIndexById( ID.SKILL_PARRY ) < 0 ) return;

            ParrySkillHandler parryCtrl = _combatSkillEventCtrl.CurrentSkillHandler as ParrySkillHandler;
            if ( parryCtrl == null ) return;

            SubscribeParryEvent( parryCtrl );
            parryCtrl.StartParryEvent( _skillUser, _skillUser.GetOpponentChara() );
            _combatSkillEventCtrl.ScheduleRun();
        }
    }
}