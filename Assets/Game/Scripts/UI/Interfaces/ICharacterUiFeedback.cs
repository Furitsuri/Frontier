using Frontier.Entities;

/// <summary>
/// 戦闘中のキャラクター自身に紐づくUI演出(ダメージ表記・HPゲージ等)を呼び出すための限定インターフェースです。
/// Character・BattleAnimationEventReceiverのような戦闘エンティティ層のクラスに、
/// IUiSystem全体(RecruitUiやDeployUi等、無関係な画面まで)を見せないようにする目的で用意しています。
/// キャラクターインスタンスに紐づかないUI操作(パラメータパネルのSkillBox制御等)は対象外とし、
/// そちらはBattleRoutinePresenter等、既存のPresenter層に委譲してください。
/// </summary>
public interface ICharacterUiFeedback
{
    void ShowDamageOnCharacter( Character chara, float duration = -1f );
    void ShowHpGaugeOnCharacter( Character chara );
    void RemoveHpGaugeOnCharacter( Character chara );
}
