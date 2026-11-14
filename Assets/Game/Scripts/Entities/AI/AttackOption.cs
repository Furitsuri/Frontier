using Frontier.Combat;
using System.Collections.Generic;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// AIが選択し得る攻撃の候補です。
    /// どこに立ち、何(通常攻撃またはスキル)で、誰を攻撃するかを表し、
    /// スキルの場合はそのまま SequenceFacade.RegistSkillAction へ渡せる情報を保持します。
    /// </summary>
    public struct AttackOption
    {
        public int StandTileIndex;                  // 攻撃を行う位置(移動先)のタイル
        public SkillID SkillID;                     // 使用するスキル。SkillID.NONEの場合は通常攻撃
        public Direction Direction;                 // 向きによって対象が決まるスキル(TargetingMode.DIRECTIONAL)の向き。それ以外はDirection.NONE
        public int LandingTileIndex;                // 移動を伴うスキルの着地先のタイル。それ以外は-1
        public int CenterTileIndex;                 // 一定の範囲を対象とするスキル(TargetingMode.PART_OF_RANGE)の範囲の中心のタイル。それ以外は-1
        public Character MainTarget;                // 主となる攻撃対象
        public List<CharacterKey> AttackTargetKeys; // 攻撃対象の一覧(範囲攻撃の場合は複数)
        public List<int> AffectedTileIndices;       // スキルの効果が及ぶタイル(予兆として表示する範囲)。PART_OF_RANGE・DIRECTIONALのスキルのみ。それ以外はnull

        public bool IsNormalAttack => SkillID == SkillID.NONE;
    }
}
