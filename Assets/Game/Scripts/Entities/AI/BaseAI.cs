using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities.Ai
{
    public abstract class BaseAi
    {
        /// <summary>
        /// 思考に関する設定を適用します。思考タイプ毎の設定値を持つAIは、ここで自身のフィールドへ取り込みます
        /// </summary>
        /// <param name="setting">思考に関する設定</param>
        virtual public void ApplyThinkingSetting( in ThinkingSetting setting ) {}

        /// <summary>
        /// 初期化します
        /// </summary>
        virtual public void Init( Character owner ) {}

        /// <summary>
        /// 目的地のグリッドインデックスを取得します
        /// </summary>
        /// <returns>目的地のグリッドインデックス</returns>
        virtual public int GetDestinationGridIndex() { return -1; }

        /// <summary>
        /// 攻撃対象にしているキャラクターを取得します
        /// </summary>
        /// <returns>攻撃対象にしているキャラクター</returns>
        virtual public Character GetTargetCharacter() { return null; }

        /// <summary>
        /// 選択した攻撃(攻撃を行う位置、通常攻撃かスキルか、攻撃対象など)を取得します
        /// </summary>
        /// <param name="option">選択した攻撃</param>
        /// <returns>攻撃を選択しているか否か</returns>
        virtual public bool TryGetAttackOption( out AttackOption option ) { option = default; return false; }

        /// <summary>
        /// 目的地と攻撃対象を決定します
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        /// <returns>目的地と攻撃対象それぞれが決定されたか否か</returns>
        virtual public (bool, bool) DetermineDestinationAndTarget( BattleParameters battleParams, in Status status, in int[] ownerTileCosts, in CharacterKey charaKey ) { return (false, false); }

        /// <summary>
        /// 移動目標と攻撃対象キャラクターをリセットします
        /// TODO : 再行動スキルなどを実装する場合は、対象に再行動を適応した際にこの関数を呼び出してください
        /// </summary>
        virtual public void ResetDestinationAndTarget() {}

        /// <summary>
        /// 既に移動対象や攻撃対象を決定しているかどうかの情報を取得します
        /// </summary>
        /// <returns>決定の有無</returns>
        virtual public bool IsDetermined() { return false; }

        /// <summary>
        /// このフェーズで行動(移動・攻撃)し得るかを取得します。
        /// 行動を決定する処理を持たない思考タイプや、待ち構えていてまだ行動を開始していないAIはfalseを返します。
        /// 軍全体の行動の調整(TroopActionCoordinator)において、調整の対象とするかの判定に用います
        /// </summary>
        /// <returns>行動し得るか</returns>
        virtual public bool CanTakeAction() { return false; }

        /// <summary>
        /// 他の味方が攻撃を行う位置として必要としているタイルと、その必要度を設定します。
        /// 次に目的地と攻撃対象を決定する際、これらのタイルに立つ攻撃の候補は評価を下げます(味方の攻撃位置を塞がないようにするため)
        /// </summary>
        /// <param name="weights">タイルのインデックスと、そのタイルの必要度。nullの場合は考慮しない</param>
        virtual public void SetContestedTiles( IReadOnlyDictionary<int, float> weights ) {}
    }
}