using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Sequences
{
    /// <summary>
    /// 攻撃・スキルで倒したキャラクターの死亡アニメーションの完了を待ちます。
    /// 通常の攻撃(NormalAttackSequenceBase)とスキル(SkillActionBase)の双方で使用し、待ち方を揃えます。
    ///
    /// 死亡通知(BattleCharacterCoordinator.NotifyCharacterDied)は死亡アニメーションのイベントから行われるため、
    /// 完了を待たずにシーケンスを終了すると、HPが0のまま死亡通知前のキャラクターが盤面に残った状態で戦闘が再開され、
    /// 次に行動するキャラクターの対象に選ばれた後に破棄される等の不整合が起きます。
    /// </summary>
    public class DeathAnimationWaiter
    {
        private readonly List<Character> _dyingCharacters = new List<Character>();
        private float _elapsedTime = 0f;

        /// <summary>
        /// 待機の対象を全て取り除きます
        /// </summary>
        public void Clear()
        {
            _dyingCharacters.Clear();
            _elapsedTime = 0f;
        }

        /// <summary>
        /// 死亡アニメーションの完了を待つキャラクターを追加します(追加済みの場合は何もしません)
        /// </summary>
        public void Track( Character chara )
        {
            if( chara == null || _dyingCharacters.Contains( chara ) ) { return; }

            _dyingCharacters.Add( chara );
        }

        /// <summary>
        /// 待機を進め、待機の対象全員の死亡アニメーションが完了したかを返します。
        /// 上限の時間(DIE_ANIMATION_WAIT_TIMEOUT)を超えた場合は、警告を出力した上で完了したものとして扱います
        /// </summary>
        /// <returns>全員の死亡アニメーションが完了したか(対象がいない場合はtrue)</returns>
        public bool Update()
        {
            _dyingCharacters.RemoveAll( chara => chara == null || chara.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DIE ) );
            if( _dyingCharacters.Count <= 0 ) { return true; }

            _elapsedTime += DeltaTimeProvider.DeltaTime;
            if( Constants.DIE_ANIMATION_WAIT_TIMEOUT <= _elapsedTime )
            {
                Debug.LogWarning( $"死亡アニメーションの完了を検知出来なかったため、待機を打ち切りました。(残り{_dyingCharacters.Count}体)" );
                Clear();
                return true;
            }

            return false;
        }
    }
}
