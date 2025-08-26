using Frontier.Battle;
using Frontier.Entities;
using System;
using UnityEditor.Search;
using UnityEngine;
using Zenject;
using static Constants;
using static Frontier.Combat.ParrySkillHandler;

namespace Frontier.Combat
{
    /// <summary>
    /// パリィスキルの処理を行います
    /// </summary>
    public class ParrySkillHandler : CombatSkillEventHandlerBase
    {
        /// <summary>
        /// パリィ更新用フェイズ
        /// </summary>
        public enum PARRY_PHASE
        {
            NONE = -1,

            EXEC_PARRY,
            AFTER_ATTACK,

            NUM,
        }

        /// <summary>
        /// パリィ判定の種類
        /// </summary>
        public enum JudgeResult
        {
            NONE = -1,
            SUCCESS,    // 成功
            FAILED,     // 失敗
            JUST,       // ジャスト成功

            MAX,
        }

        [SerializeField]
        [Header("UIスクリプト")]
        private SkillParryUI _ui;

        [SerializeField]
        [Header("成功時エフェクト")]
        private ParticleSystem _successParticle = null;

        [SerializeField]
        [Header("失敗時エフェクト")]
        private ParticleSystem _failureParticle = null;

        [SerializeField]
        [Header("ジャスト成功時エフェクト")]
        private ParticleSystem _justParticle = null;

        [SerializeField]
        [Header("パリィ判定リング縮小時間")]
        private float _shrinkTime = 3f;

        [SerializeField]
        [Header("キャラクターのスローモーションレート")]
        private float _delayTimeScale = 0.1f;

        [SerializeField]
        [Header("失敗判定に自動遷移するサイズ倍率")]
        private float _radiusRateAutoTransitionToFail = 0.75f;

        [SerializeField]
        [Header("結果を表示する秒数")]
        private float _showUITime = 1.5f;

        private int _inputCodeHash                  = -1;
        private float _radiusThresholdOnFail        = 0f;
        private InputFacade _inputFcd               = null;
        private BattleRoutineController _btlRtnCtrl = null;
        private ParryRingEffect _ringEffect         = null;
        private ParryResultEffect _resultEffect     = null;
        private Character _useParryCharacter        = null;
        private Character _attackCharacter          = null;
        private JudgeResult _judgeResult            = JudgeResult.NONE; // パリィの成否結果
        public JudgeResult ParryResult { get; set; } = ParrySkillHandler.JudgeResult.NONE;
        private (float inner, float outer) _judgeRingSuccessRange   = (0f, 0f);
        private (float inner, float outer) _judgeRingJustRange      = (0f, 0f);

        // パリィイベント終了時のデリゲート
        public event EventHandler<ParrySkillHdlrEventArgs> ProcessCompleted;

        [Inject]
        void Construct( InputFacade inputFcd, BattleRoutineController btlRtnCtrl )
        {
            _inputFcd   = inputFcd;
            _btlRtnCtrl = btlRtnCtrl;
        }

        void Start()
        {
            if (_resultEffect == null)
            {
                _resultEffect = new ParryResultEffect();
            }
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            _ui.Init(_showUITime);
            _ui.gameObject.SetActive(false);

            ParticleSystem[] particles = new ParticleSystem[]
            {
                _successParticle,
                _failureParticle,
                _justParticle
            };
            _resultEffect.Init(particles);

            // 実行されるまでは無効に
            gameObject.SetActive(false);

            _judgeResult = JudgeResult.NONE;

            _inputCodeHash = Hash.GetStableHash(GetType().Name);
            RegisterInputCodes( _inputCodeHash );
        }

        override public void Exit()
        {
            _inputFcd.UnregisterInputCodes(_inputCodeHash);
        }

        // Update is called once per frame
        override public void Update()
        {
            if (_ringEffect == null || _resultEffect == null) return;

            // エフェクト終了と同時に無効に切替
            if ( _resultEffect.IsEndPlaying() )
            {
                _ui.terminate();
                _resultEffect.terminate();

                ParrySkillHdlrEventArgs args = new ParrySkillHdlrEventArgs();
                args.Result = _judgeResult;

                // 結果と共にイベント終了を呼び出し元に通知
                OnProcessCompleted(args);

                // MonoBehaviorを無効に
                gameObject.SetActive(false);
            }

            // 結果が既に出ている場合はここで終了
            if ( IsJudgeEnd() ) return;

            // shrinkRadiusが成功範囲外に出ている場合は、パリィ失敗とする
            float shrinkRadius = _ringEffect.GetCurShrinkRingRadius();
            if (shrinkRadius < _radiusThresholdOnFail)
            {
                _judgeResult    = JudgeResult.FAILED;
            }
        }

        override public void LateUpdate()
        {
            if ( IsJudgeEnd() && !_resultEffect.IsPlyaing() )
            {
                _ui.gameObject.SetActive(true);
                _ui.ShowResult(_judgeResult);
                _resultEffect.PlayEffect(_judgeResult);

                // 縮小エフェクト停止
                _ringEffect.StopShrink();

                // UI以外の表示物の更新時間スケールを停止
                DelayBattleTimeScale(0f);

                // パリィ結果によるパラメータ変動を各キャラクターに適応
                ApplyModifiedParamFromResult(_useParryCharacter, _attackCharacter, _judgeResult);
            }
        }

        override public void FixedUpdate()
        {
            if( _ringEffect == null) return;

            // フレームレートによるズレを防ぐためFixedで更新
            _ringEffect.FixedUpdateEffect();
        }

        /// <summary>
        /// パリィ判定処理を開始します
        /// </summary>
        /// <param name="useCharacter">パリィを行うキャラクター</param>
        /// <param name="opponent">対戦相手</param>
        public void StartParryEvent( Character useCharacter, Character opponent )
        {
            NullCheck.AssertNotNull(useCharacter);
            NullCheck.AssertNotNull(opponent);

            gameObject.SetActive(true);
            _useParryCharacter  = useCharacter;
            _attackCharacter    = opponent;

            // パリィエフェクトのシェーダー情報をカメラに描画するため、メインカメラにアタッチ
            Camera.main.gameObject.AddComponent<ParryRingEffect>();
            _ringEffect = Camera.main.gameObject.GetComponent<ParryRingEffect>();
            Debug.Assert(_ringEffect != null);

            // MEMO : _ringEffect, 及び_uiはアタッチのタイミングの都合上Initではなくここで初期化
            _ringEffect.Init(_shrinkTime);
            _ringEffect.SetEnable(true);
            _ui.Init(_showUITime);
            _ui.gameObject.SetActive(false);

            // 防御側の防御力と攻撃側の攻撃力からパリィ判定範囲を算出して設定
            int selfDef = (int)Mathf.Floor( (_useParryCharacter.Params.CharacterParam.Def + _useParryCharacter.modifiedParam.Def) * _useParryCharacter.skillModifiedParam.DefMagnification );
            int oppoAtk = (int)Mathf.Floor( (_attackCharacter.Params.CharacterParam.Atk + _attackCharacter.modifiedParam.Atk) * _attackCharacter.skillModifiedParam.AtkMagnification );
            CalcurateParryRingParam(selfDef, oppoAtk);

            // パリィ中のキャラクタースローモーション速度を設定
            DelayBattleTimeScale(_delayTimeScale);

            // パリィモーションの開始
            // _useParryCharacter.StartParryAnimation();
            _useParryCharacter.RegisterCombatAnimation( COMBAT_ANIMATION_TYPE.PARRY );
            _useParryCharacter.CombatAnimSeq.StartSequence();

            // 結果をNONEに初期化
            _judgeResult = JudgeResult.NONE;
        }

        /// <summary>
        /// UIやシェーダ以外の時間スケールを指定値に変更します
        /// </summary>
        /// <param name="timeScale">遅らせるスケール値</param>
        public void DelayBattleTimeScale(float timeScale)
        {
            if (1f < timeScale) timeScale = 1f;

            // タイムスケールを変更すると、UIなどのアニメーション速度にも影響を与えてしまうため保留(シェーダーエフェクトは別)
            _btlRtnCtrl.TimeScaleCtrl.SetTimeScale(timeScale);
        }

        /// <summary>
        /// パリィ判定処理を終了します
        /// </summary>
        public void EndParryEvent()
        {
            // タイムスケールを元に戻す
            ResetBattleTimeScale();

            _ringEffect.Destroy();
        }

        /// <summary>
        /// 判定が終了したかを返します
        /// </summary>
        /// <returns>判定が終了したか</returns>
        public bool IsJudgeEnd()
        {
            return _judgeResult != JudgeResult.NONE;
        }

        /// <summary>
        /// パリィ結果が指定の値と合致しているかを判定します
        /// </summary>
        /// <param name="result">指定するパリィ結果</param>
        /// <returns>合致しているか否か</returns>
        public bool IsMatchResult( JudgeResult result )
        {
            return _judgeResult == result;
        }

        /// <summary>
        /// UIやシェーダ以外の時間スケールを元に戻します
        /// </summary>
        private void ResetBattleTimeScale()
        {
            DelayBattleTimeScale(1f);
        }

        /// <summary>
        /// 自身の防御値と相手の攻撃値でパリィ判定のリングレンジを求めます
        /// </summary>
        /// <param name="selfCharaDef">パリィ発動キャラクターの防御値</param>
        /// <param name="opponentCharaAtk">対戦相手の攻撃値</param>
        private void CalcurateParryRingParam(int selfCharaDef, int opponentCharaAtk)
        {
            // TODO : いい感じの計算式でリング範囲を計算して設定する。
            //        縮小速度は変更するかは調整次第のため、一旦固定値
            _judgeRingSuccessRange = (0.4f, 0.6f);

            // シェーダーに適応
            _ringEffect.SetJudgeRingRange(_judgeRingSuccessRange);

            // 失敗判定に自動遷移する半径の閾値を決定
            // MEMO : 成功範囲の中央値と指定倍率との積とする
            _radiusThresholdOnFail = ((_judgeRingSuccessRange.inner + _judgeRingSuccessRange.outer) * 0.5f) * _radiusRateAutoTransitionToFail;
        }

        /// <summary>
        /// パリィ判定終了時に呼び出すイベントハンドラ
        /// </summary>
        /// <param name="e">イベントオブジェクト</param>
        private void OnProcessCompleted(ParrySkillHdlrEventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// パリィ結果から攻撃と防御の係数を各キャラクターに適応させます
        /// </summary>
        /// <param name="useParryChara">パリィ使用キャラクター</param>
        /// <param name="attackChara">攻撃キャラクター</param>
        /// <param name="result">パリィ結果</param>
        private void ApplyModifiedParamFromResult(Character useParryChara, Character attackChara, JudgeResult result)
        {
            switch (result)
            {
                case ParrySkillHandler.JudgeResult.SUCCESS:
                    attackChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    break;
                case ParrySkillHandler.JudgeResult.FAILED:
                    useParryChara.skillModifiedParam.DefMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param2;
                    break;
                case ParrySkillHandler.JudgeResult.JUST:
                    attackChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    useParryChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param3;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect(attackChara, useParryChara);
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        private void RegisterInputCodes( int hash )
        {
            _inputFcd.RegisterInputCodes(
                (GuideIcon.CONFIRM, "PARRY EXEC", CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hash)
            );
        }

        private bool CanAcceptConfirm()
        {
            // 判定が終了していないことが条件
            return !IsJudgeEnd();
        }

        private bool AcceptConfirm( bool isInput )
        {
            if (!isInput) return false;

            float shrinkRadius = _ringEffect.GetCurShrinkRingRadius();

            _judgeResult = JudgeResult.FAILED;
            if( shrinkRadius.IsBetween( _judgeRingJustRange.inner, _judgeRingJustRange.outer ) )
            {
                _judgeResult = JudgeResult.JUST;
            }
            else if( shrinkRadius.IsBetween( _judgeRingSuccessRange.inner, _judgeRingSuccessRange.outer ) )
            {
                _judgeResult = JudgeResult.SUCCESS;
            }

            return true;
        }
    }

    /// <summary>
    /// ParrySkillHandlerの結果通知に使用します
    /// </summary>
    public class ParrySkillHdlrEventArgs : EventArgs
    {
        public ParrySkillHandler.JudgeResult Result { get; set; }
    }
}