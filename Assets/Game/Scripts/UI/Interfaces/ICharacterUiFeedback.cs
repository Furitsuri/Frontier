using Frontier;
using Frontier.Entities;

/// <summary>
/// 戦闘中のキャラクター自身に紐づくUI演出(ダメージ表記・HPゲージ・スキルボックスの明滅停止等)を
/// 呼び出すための限定インターフェースです。
/// Character・BattleLogicBase・BattleAnimationEventReceiverのような戦闘エンティティ層のクラスに、
/// IUiSystem全体(RecruitUiやDeployUi等、無関係な画面まで)を見せないようにする目的で用意しています。
/// </summary>
public interface ICharacterUiFeedback
{
    void ShowDamageOnCharacter( Character chara, float duration = -1f );
    void ShowHpGaugeOnCharacter( Character chara );
    void RemoveHpGaugeOnCharacter( Character chara );
    SkillBoxUI GetPlayerParamSkillBox( int index );
}
