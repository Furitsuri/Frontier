using Frontier.Battle;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.Sequences
{
    /// <summary>
    /// 戦闘(通常攻撃・スキル・連携スキル)のシーケンス終了時に、戦闘に参加したキャラクターの
    /// 戦闘1回限りの状態(スキルの効果・使用ONのフラグ・バフエフェクト・対戦相手)を元に戻す処理をまとめたものです。
    /// 通常攻撃とスキルとで、戦闘後の後片付けの範囲が食い違わないよう、各シーケンスはここを経由して後片付けを行います。
    /// </summary>
    public static class CombatSequenceCleanup
    {
        /// <summary>
        /// 攻撃を行うキャラクターと、攻撃対象のキャラクター(キーで指定)を、参加者の一覧に加えます。
        /// MEMO : 戦闘で死亡したキャラクターは、その時点でBattleCharacterCoordinatorの登録から外れキーで取得出来なくなるため、
        ///        ダメージを与える前(シーケンスの開始時)に呼び出し、キャラクターの参照として保持しておく必要があります
        /// </summary>
        /// <param name="attacker">攻撃を行うキャラクター</param>
        /// <param name="targetKeys">攻撃対象のキャラクターのキー</param>
        /// <param name="btlCharaCdr">キーからキャラクターを取得するためのコーディネーター</param>
        /// <param name="participants">参加者の一覧(追加先)</param>
        public static void CollectParticipants( Character attacker, IEnumerable<CharacterKey> targetKeys, BattleCharacterCoordinator btlCharaCdr, List<Character> participants )
        {
            AddUnique( participants, attacker );

            if( null == targetKeys || null == btlCharaCdr ) { return; }

            foreach( var key in targetKeys )
            {
                AddUnique( participants, btlCharaCdr.GetCharacter( key ) );
            }
        }

        /// <summary>
        /// 参加者の一覧に、重複しないようにキャラクターを加えます
        /// </summary>
        public static void AddUnique( List<Character> participants, Character chara )
        {
            if( null == chara || participants.Contains( chara ) ) { return; }

            participants.Add( chara );
        }

        /// <summary>
        /// 戦闘に参加したキャラクターの、戦闘1回限りの状態を元に戻します
        /// </summary>
        /// <param name="participants">戦闘に参加したキャラクター(nullは無視します)</param>
        public static void ResetParticipants( IEnumerable<Character> participants )
        {
            foreach( var chara in participants )
            {
                if( null == chara || null == chara.BattleLogic ) { continue; }

                chara.BattleLogic.ResetOnEndOfAttackSequence();
            }
        }

        /// <inheritdoc cref="ResetParticipants(IEnumerable{Character})"/>
        public static void ResetParticipants( params Character[] participants )
        {
            ResetParticipants( ( IEnumerable<Character> ) participants );
        }
    }
}
