using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Frontier
{
    public class SkillParryController : MonoBehaviour
    {
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

        [Header("UIスクリプト")]
        [SerializeField]
        private SkillParryUI _ui;

        [Header("パリィ判定リング縮小時間")]
        [SerializeField]
        private float _shrinkTime = 3f;

        [Header("キャラクターのスローモーションレート")]
        [SerializeField]
        private float _delayTimeScale = 0.1f;

        private ParryRingEffect _effect;
        private BattleManager _btlMgr = null;
        private Character _useParryCharacter = null;
        private Character _attackCharacter = null;
        private JudgeResult _judgeResult = JudgeResult.FAILED;
        private (float inner, float outer) _judgeRingSuccessRange;
        private (float inner, float outer) _judgeRingJustRange;
        private float _showUITime = 1.5f;
        // パリィイベント終了時のデリゲート
        public event EventHandler<EventArgs> ProcessCompleted;
        // 結果の取得
        public JudgeResult Result => _judgeResult;

        public bool IsEndParryEvent => (_judgeResult != JudgeResult.NONE);

        // Update is called once per frame
        void Update()
        {
            // キーが押されたタイミングで判定
            if (Input.GetKeyUp(KeyCode.Space))
            {
                float shrinkRadius = _effect.GetCurShrinkRingRadius();
                Debug.Log( shrinkRadius );

                // 判定とUIへの表示
                _ui.gameObject.SetActive(true);
                _judgeResult = JudgeResult.FAILED;
                if(_judgeRingJustRange.inner <= shrinkRadius && shrinkRadius <= _judgeRingJustRange.outer)
                {
                    _judgeResult = JudgeResult.JUST;
                }
                else if( _judgeRingSuccessRange.inner <= shrinkRadius && shrinkRadius <= _judgeRingSuccessRange.outer )
                {
                    _judgeResult = JudgeResult.SUCCESS;
                }
                _ui.ShowResult(_judgeResult);

                // エフェクト停止
                _effect.StopShrink();
                // UI以外の表示物の更新時間スケールを停止
                DelayBattleTimeScale(0f);
                // パリィ結果によるパラメータ変動を各キャラクターに適応
                ApplyModifiedParamFromResult(_useParryCharacter, _attackCharacter, _judgeResult);
                // 結果と共にイベント終了を呼び出し元に通知
                OnProcessCompleted(EventArgs.Empty);
            }

            // UI表示終了
            if (_ui.IsShowEnd())
            {
                enabled = false;
                OnDestroy();
            }
        }

        void FixedUpdate()
        {
            // フレームレートによるズレを防ぐためFixedで更新
            _effect.FixedUpdateEffect();    
        }

        void OnDestroy()
        {
            ResetBattleTimeScale();
        }

        /// <summary>
        /// UIやシェーダ以外の時間スケールを元に戻します
        /// </summary>
        void ResetBattleTimeScale()
        {
            DelayBattleTimeScale(1f);
        }

        /// <summary>
        /// 自身の防御値と相手の攻撃値でパリィ判定のリングレンジを求めます
        /// </summary>
        /// <param name="selfCharaDef">パリィ発動キャラクターの防御値</param>
        /// <param name="opponentCharaAtk">対戦相手の攻撃値</param>
        void CalcurateParryRingParam(int selfCharaDef, int opponentCharaAtk)
        {
            // TODO : いい感じの計算式でリング範囲を計算して設定する。
            //        縮小速度は変更するかは調整次第のため、一旦固定値
            _judgeRingSuccessRange = (0.4f, 0.6f);

            // シェーダーに適応
            _effect.SetJudgeRingRange(_judgeRingSuccessRange);
        }

        /// <summary>
        /// パリィ判定終了時に呼び出すイベントハンドラ
        /// </summary>
        /// <param name="e">イベントオブジェクト</param>
        void OnProcessCompleted(EventArgs e )
        {
            ProcessCompleted ?.Invoke( this, e );
        }

        /// <summary>
        /// パリィ結果から攻撃と防御の係数を各キャラクターに適応させます
        /// </summary>
        /// <param name="useParryChara">パリィ使用キャラクター</param>
        /// <param name="attackChara">攻撃キャラクター</param>
        /// <param name="result">パリィ結果</param>
        void ApplyModifiedParamFromResult(Character useParryChara, Character attackChara, JudgeResult result)
        {
            switch (result)
            {
                case SkillParryController.JudgeResult.SUCCESS:
                    attackChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    break;
                case SkillParryController.JudgeResult.FAILED:
                    useParryChara.skillModifiedParam.DefMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param2;
                    break;
                case SkillParryController.JudgeResult.JUST:
                    useParryChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param3;
                    attackChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            _btlMgr.ApplyDamageExpect(attackChara, useParryChara);
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="self">パリィ発動キャラクター</param>
        /// <param name="opponent">対戦相手</param>
        public void Init(BattleManager btlMgr)
        {
            _btlMgr = btlMgr;

            _ui.Init(_showUITime);
            _ui.gameObject.SetActive(false);

            // 実行されるまでは無効に
            gameObject.SetActive(false);
        }

        /// <summary>
        /// パリィ判定処理を開始します
        /// </summary>
        /// <param name="self">パリィを行うキャラクター</param>
        /// <param name="opponent">対戦相手</param>
        public void StartParryEvent(Character useCharacter, Character opponent)
        {
            _useParryCharacter = useCharacter;
            _attackCharacter = opponent;

            gameObject.SetActive(true);
            // パリィエフェクトのシェーダー情報をカメラに描画するため、メインカメラにアタッチ
            Camera.main.gameObject.AddComponent<ParryRingEffect>();
            _effect = Camera.main.gameObject.GetComponent<ParryRingEffect>();
            Debug.Assert(_effect != null);
            // MEMO : _effectはアタッチのタイミングの都合上Initではなくここで初期化
            _effect.Init(_shrinkTime);
            _effect.SetEnable(true);

            // 防御側の防御力と攻撃側の攻撃力からパリィ判定範囲を算出して設定
            int selfDef = (int)Mathf.Floor( (_useParryCharacter.param.Def + _useParryCharacter.modifiedParam.Def) * _useParryCharacter.skillModifiedParam.DefMagnification );
            int oppoAtk = (int)Mathf.Floor( (_attackCharacter.param.Atk + _attackCharacter.modifiedParam.Atk) * _attackCharacter.skillModifiedParam.AtkMagnification );
            CalcurateParryRingParam(selfDef, oppoAtk);

            // パリィ中のスローモーション速度を設定
            DelayBattleTimeScale(_delayTimeScale);
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
            _btlMgr.TimeScaleCtrl.SetTimeScale(timeScale);
        }

        /// <summary>
        /// パリィ判定処理を終了します
        /// </summary>
        public void EndParryEvent()
        {
            ResetBattleTimeScale();

            _effect.Destroy();

            gameObject.SetActive(false);
        }
    }
}