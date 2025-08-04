using Frontier;
using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using UnityEditor.Search;
using UnityEngine;
using Zenject;

/// <summary>
/// パリィスキルを使用するエンティティに持たせるクラスです
/// </summary>
public class ParrySkillNotifier
{
    private ParrySkillHandler.PARRY_PHASE _parryPhase;
    private BattleRoutineController _btlRtnCtrl = null;
    // スキル使用者
    private Character _skillUser = null;

    [Inject]
    public void Construct( BattleRoutineController btlRtnCtrl )
    {
        _btlRtnCtrl = btlRtnCtrl;
    }

    // パリィの成否結果
    public ParrySkillHandler.JudgeResult ParryResult { get; set; } = ParrySkillHandler.JudgeResult.NONE;

    /// <summary>
    /// 指定のパリィ操作クラスがイベント終了した際に呼び出すデリゲートを設定します
    /// </summary>
    /// <param name="parryCtrl">パリィ操作クラス</param>
    void SubscribeParryEvent(ParrySkillHandler parryCtrl)
    {
        parryCtrl.ProcessCompleted += ParryEventProcessCompleted;
    }

    /// <summary>
    /// 指定のパリィ操作クラスがイベント終了した際に呼び出すデリゲート設定を解除します
    /// </summary>
    /// <param name="parryCtrl">パリィ操作クラス</param>
    void UnsubscribeParryEvent(ParrySkillHandler parryCtrl)
    {
        parryCtrl.ProcessCompleted -= ParryEventProcessCompleted;
    }

    /// <summary>
    /// パリィイベント終了時に呼び出されるデリゲート
    /// </summary>
    /// <param name="sender">呼び出しを行うパリィイベントコントローラ</param>
    /// <param name="e">イベントハンドラ用オブジェクト(この関数ではempty)</param>
    void ParryEventProcessCompleted(object sender, ParrySkillHdlrEventArgs e)
    {
        ParryResult = e.Result;

        ParrySkillHandler ParryHdlr = sender as ParrySkillHandler;
        ParryHdlr.EndParryEvent();

        UnsubscribeParryEvent(ParryHdlr);
    }

    /// <summary>
    /// 対戦相手の攻撃をパリィ(弾く)するイベントを発生させます
    /// ※攻撃アニメーションから呼び出されます
    /// </summary>
    virtual public void ParryOpponentEvent()
    {
        // NONE以外の結果が通知されているはず
        Debug.Assert(ParryResult != ParrySkillHandler.JudgeResult.NONE);

        if (_skillUser == null)
        {
            Debug.Assert(false);
        }

        if (ParryResult == ParrySkillHandler.JudgeResult.FAILED)
        {
            return;
        }

        // 成功時(ジャスト含む)にはパリィ挙動
        ParryRecieveEvent();
    }

    /// <summary>
    /// パリィを受けた際のイベントを発生させます
    /// </summary>
    virtual public void ParryRecieveEvent()
    {
        Character opponent = _skillUser.GetOpponentChara();
        NullCheck.AssertNotNull(opponent);

        opponent.GetTimeScale.Reset();
        opponent.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.GET_HIT);
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="user">スキルの使用者</param>
    public void Init( Character user )
    {
        _skillUser = user;
    }

    /// <summary>
    /// パリィ結果をリセットします
    /// </summary>
    public void ResetParryResult()
    {
        ParryResult = ParrySkillHandler.JudgeResult.NONE;
    }

    /// <summary>
    /// パリィシーケンスを開始します
    /// </summary>
    public void StartParrySequence()
    {
        _parryPhase = ParrySkillHandler.PARRY_PHASE.EXEC_PARRY;
        _skillUser.ResetElapsedTime();

        _skillUser.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.PARRY);
        // タイムスケールを遅くし、パリィ挙動をスローモーションで見せる
        _skillUser.GetTimeScale.SetTimeScale(0.1f);
    }

    /// <summary>
    /// パリィ判定処理を開始します
    /// </summary>
    public void StartParryJudgeEvent()
    {
        if (!_skillUser.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) return;

        ParrySkillHandler parryCtrl = _btlRtnCtrl.SkillCtrl.CurrentSkillHandler as ParrySkillHandler;
        SubscribeParryEvent(parryCtrl);
        parryCtrl.StartParryEvent( _skillUser, _skillUser.GetOpponentChara() );
    }

    /// <summary>
    /// 戦闘において、攻撃された側がパリィを行った際の行動を更新します
    /// </summary>
    /// <param name="departure">攻撃開始座標</param>
    /// <param name="destination">攻撃目標座標</param>
    /// <returns>終了判定</returns>
    public bool UpdateParryOnTargeter(in Vector3 departure, in Vector3 destination)
    {
        bool isJustParry = false;

        switch (_parryPhase)
        {
            case ParrySkillHandler.PARRY_PHASE.EXEC_PARRY:
                if (isJustParry)
                {
                    _skillUser.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.SINGLE_ATTACK);

                    _parryPhase = ParrySkillHandler.PARRY_PHASE.AFTER_ATTACK;
                }
                else
                {
                    if (_skillUser.AnimCtrl.IsEndAnimationOnConditionTag(AnimDatas.AnimeConditionsTag.PARRY))
                    {
                        _skillUser.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.WAIT);

                        return true;
                    }
                }
                break;
            case ParrySkillHandler.PARRY_PHASE.AFTER_ATTACK:
                break;
        }
        return false;
    }

    /// <summary>
    /// パリィ結果が指定の値と合致しているかを判定します
    /// </summary>
    /// <param name="result">指定するパリィ結果</param>
    /// <returns>合致しているか否か</returns>
    public bool IsMatchResult(ParrySkillHandler.JudgeResult result )
    {
        return ParryResult == result;
    }
}