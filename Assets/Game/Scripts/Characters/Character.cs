using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Frontier.Character;
using static Frontier.SkillsData;

namespace Frontier
{
    [SerializeField]
    public class Character : MonoBehaviour
    {
        public class Command
        {
            public enum COMMAND_TAG
            {
                MOVE = 0,
                ATTACK,
                WAIT,

                NUM,
            }

            public static bool IsExecutableCommandBase(Character character)
            {
                if (!character.tmpParam.IsExecutableCommand(COMMAND_TAG.WAIT)) return false;

                return true;
            }

            public static bool IsExecutableMoveCommand(Character character, StageController stageCtrl)
            {
                if (!IsExecutableCommandBase(character)) return false;

                return !character.tmpParam.isEndCommand[(int)COMMAND_TAG.MOVE];
            }

            public static bool IsExecutableAttackCommand(Character character, StageController stageCtrl)
            {
                if (!IsExecutableCommandBase(character)) return false;

                if( character.tmpParam.isEndCommand[(int)COMMAND_TAG.ATTACK] ) return false;

                // 現在グリッドから攻撃可能な対象の居るグリッドが存在すれば、実行可能
                bool isExecutable = stageCtrl.RegistAttackAbleInfo(character.tmpParam.gridIndex, character.param.attackRange, character.param.characterTag);
           
                // 実行不可である場合は登録した攻撃情報を全てクリア
                if( !isExecutable )
                {
                    stageCtrl.ClearAttackableInfo();
                }

                return isExecutable;
            }

            public static bool IsExecutableWaitCommand(Character character, StageController stageCtrl)
            {
                return IsExecutableCommandBase(character);
            }
        }

        public enum CHARACTER_TAG
        {
            NONE = -1,
            PLAYER,
            ENEMY,
            OTHER,

            NUM,
        }

        /// <summary>
        /// 移動タイプ
        /// </summary>
        public enum MOVE_TYPE
        {
            NORMAL = 0,
            HORSE,
            FLY,
            HEAVY,

            NUM,
        }

        /// <summary>
        /// 近接攻撃更新用フェイズ
        /// </summary>
        public enum CLOSED_ATTACK_PHASE
        {
            NONE = -1,

            CLOSINGE,
            ATTACK,
            DISTANCING,

            NUM,
        }

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

        // キャラクターの持つパラメータ
        [System.Serializable]
        public struct Parameter
        {
            // キャラクタータイプ
            public CHARACTER_TAG characterTag;
            // キャラクター番号
            public int characterIndex;
            // 最大HP
            public int MaxHP;
            // 現在HP
            public int CurHP;
            // 攻撃力
            public int Atk;
            // 防御力
            public int Def;
            // 移動レンジ
            public int moveRange;
            // 攻撃レンジ
            public int attackRange;
            // アクションゲージ最大値
            public int maxActionGauge;
            // アクションゲージ現在値
            public int curActionGauge;
            // アクションゲージ回復値
            public int recoveryActionGauge;
            // アクションゲージ消費値
            public int consumptionActionGauge;
            // ステージ開始時グリッド座標(インデックス)
            public int initGridIndex;
            // ステージ開始時向き
            public Constants.Direction initDir;
            // 装備しているスキル
            public SkillsData.ID[] equipSkills;

            /// <summary>
            /// 指定のスキルが有効か否かを返します
            /// </summary>
            /// <param name="index">指定インデックス</param>
            /// <returns>有効か否か</returns>
            public bool IsValidSkill(int index)
            {
                return SkillsData.ID.SKILL_NONE < equipSkills[index] && equipSkills[index] < SkillsData.ID.SKILL_NUM;
            }

            /// <summary>
            /// アクションゲージ消費量をリセットします
            /// </summary>
            public void ResetConsumptionActionGauge()
            {
                consumptionActionGauge = 0;
            }
        }

        // バフ・デバフなどで上乗せされるパラメータ
        public struct ModifiedParameter
        {
            // 攻撃力
            public int Atk;
            // 防御力
            public int Def;
            // 移動レンジ
            public int moveRange;
            // アクションゲージ回復値
            public int recoveryActionGauge;

            public void Reset()
            {
                Atk = 0; Def = 0; moveRange = 0; recoveryActionGauge = 0;
            }
        }

        // スキルによって上乗せされるパラメータ
        public struct SkillModifiedParameter
        {
            public int AtkNum;
            public float AtkMagnification;
            public float DefMagnification;

            public void Reset()
            {
                AtkNum = 1; AtkMagnification = 1f; DefMagnification = 1f;
            }
        }

        // 戦闘中のみ使用するパラメータ
        public struct TmpParameter
        {
            // 該当コマンドの終了フラグ
            public bool[] isEndCommand;
            // 該当スキルの使用フラグ
            public bool[] isUseSkills;
            // 現在位置を示すグリッドインデックス
            public int gridIndex;
            // 1回の攻撃におけるHPの予測変動量(複数回攻撃におけるダメージ総量を考慮しない)
            public int expectedChangeHP;
            // 全ての攻撃におけるHPの予測変動量(複数回攻撃におけるダメージ総量を考慮する)
            public int totalExpectedChangeHP;

            /// <summary>
            /// 指定コマンドが実行可能か否かを判定します
            /// </summary>
            /// <param name="cmdTag">指定コマンドのタグ</param>
            /// <returns>実行可否</returns>
            public bool IsExecutableCommand(Command.COMMAND_TAG cmdTag)
            {
                return !isEndCommand[(int)cmdTag];
            }

            /// <summary>
            /// スキルの使用フラグをリセットします
            /// </summary>
            public void ResetUseSkill()
            {
                for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
                {
                    isUseSkills[i] = false;
                }
            }

            /// <summary>
            /// 全てのパラメータをリセットします
            /// </summary>
            public void Reset()
            {
                for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
                {
                    isEndCommand[i] = false;
                }

                totalExpectedChangeHP = expectedChangeHP = 0;
            }
        }

        [System.Serializable]
        public struct CameraParameter
        {
            [Header("攻撃シーケンス時カメラオフセット")]
            public Vector3 OffsetOnAtkSequence;
            [Header("パラメータ表示UI用カメラオフセット(Y座標)")]
            public float UICameraLengthY;
            [Header("パラメータ表示UI用カメラオフセット(Z座標)")]
            public float UICameraLengthZ;
            // UI表示用カメラターゲット(Y方向)
            public float UICameraLookAtCorrectY;

            public CameraParameter(in Vector3  offset, float lengthY, float lengthZ, float lookAtCorrectY)
            {
                OffsetOnAtkSequence     = offset;
                UICameraLengthY         = lengthY;
                UICameraLengthZ         = lengthZ;
                UICameraLookAtCorrectY  = lookAtCorrectY;
            }
        }

        [SerializeField]
        private GameObject _bulletObject;

        private bool _isTransitNextPhaseCamera  = false;
        private bool _isOrderedRotation         = false;
        private bool _isAttacked                = false;
        private float _elapsedTime              = 0f;
        private readonly TimeScale _timeScale   = new TimeScale();
        private Quaternion _orderdRotation      = Quaternion.identity;
        private List<(Material material, Color originalColor)> _textureMaterialsAndColors   = new List<(Material, Color)>();
        private List<Command.COMMAND_TAG> _executableCommands                               = new List<Command.COMMAND_TAG>();
        // 攻撃シーケンスにおける残り攻撃回数
        protected int _atkRemainingNum          = 0;
        protected BattleManager _btlMgr         = null;
        protected StageController _stageCtrl    = null;
        protected Character _opponent           = null;
        protected Bullet _bullet                = null;
        protected CLOSED_ATTACK_PHASE _closingAttackPhase;
        protected PARRY_PHASE _parryPhase;
        public Parameter param;
        public TmpParameter tmpParam;
        public ModifiedParameter modifiedParam;
        public SkillModifiedParameter skillModifiedParam;
        public CameraParameter camParam;
        // 死亡確定フラグ(攻撃シーケンスにおいて使用)
        public bool IsDeclaredDead { get; set; } = false;
        // パリィ結果
        public SkillParryController.JudgeResult ParryResult { get; set; } = SkillParryController.JudgeResult.NONE;
        public AnimationController AnimCtrl { get; } = new AnimationController();

        // 攻撃用アニメーションタグ
        private static AnimDatas.ANIME_CONDITIONS_TAG[] AttackAnimTags = new AnimDatas.ANIME_CONDITIONS_TAG[]
        {
            AnimDatas.ANIME_CONDITIONS_TAG.SINGLE_ATTACK,
            AnimDatas.ANIME_CONDITIONS_TAG.DOUBLE_ATTACK,
            AnimDatas.ANIME_CONDITIONS_TAG.TRIPLE_ATTACK
        };

        private delegate bool IsExecutableCommand(Character character, StageController stageCtrl);
        private static IsExecutableCommand[] _executableCommandTables =
        {
            Command.IsExecutableMoveCommand,
            Command.IsExecutableAttackCommand,
            Command.IsExecutableWaitCommand,
        };

        #region PRIVATE_METHOD
        void Awake()
        {
            _timeScale.OnValueChange    = AnimCtrl.UpdateTimeScale;
            IsDeclaredDead              = false;
            param.equipSkills           = new SkillsData.ID[Constants.EQUIPABLE_SKILL_MAX_NUM];
            tmpParam.isEndCommand       = new bool[(int)Command.COMMAND_TAG.NUM];
            tmpParam.isUseSkills        = new bool[Constants.EQUIPABLE_SKILL_MAX_NUM];
            tmpParam.Reset();
            modifiedParam.Reset();
            skillModifiedParam.Reset();
            AnimCtrl.Init(GetComponent<Animator>());

            // キャラクターモデルのマテリアルが設定されているObjectを取得し、
            // Materialと初期のColor設定を保存
            RegistMaterialsRecursively(this.transform, Constants.OBJECT_TAG_NAME_CHARA_SKIN_MESH);

            // 弾オブジェクトが設定されていれば生成
            // 使用時まで非アクティブにする
            if (_bulletObject != null)
            {
                GameObject bulletObject = Instantiate(_bulletObject);
                if (bulletObject != null)
                {
                    _bullet = bulletObject.GetComponent<Bullet>();
                    bulletObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            // 向き回転命令
            if (_isOrderedRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _orderdRotation, Constants.CHARACTER_ROT_SPEED * Time.deltaTime);

                float angleDiff = Quaternion.Angle(transform.rotation, _orderdRotation);
                if (Math.Abs(angleDiff) < Constants.CHARACTER_ROT_THRESHOLD)
                {
                    _isOrderedRotation = false;
                }
            }

            // 移動と攻撃が終了していれば、行動不可に遷移
            var endCommand = tmpParam.isEndCommand;
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                BeImpossibleAction();
            }
        }

        void OnDestroy()
        {
            // 戦闘時間管理クラスの登録を解除
            _btlMgr.TimeScaleCtrl.Unregist(_timeScale);
        }

        /// <summary>
        /// 再帰を用いて、指定のタグで登録されているオブジェクトのマテリアルを登録します
        /// ※ 色変更の際に用いる
        /// </summary>
        /// <param name="parent">オブジェクトの親</param>
        /// <param name="tagName">検索するタグ名</param>
        void RegistMaterialsRecursively( Transform parent, string tagName )
        {
            Transform children = parent.GetComponentInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.CompareTag(tagName))
                {
                    // モデルによって、マテリアルがMeshとSkinMeshの両方のパターンに登録されているケースがあるため、
                    // どちらも検索する
                    var skinMeshRenderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (skinMeshRenderer != null)
                    {
                        _textureMaterialsAndColors.Add((skinMeshRenderer.material, skinMeshRenderer.material.color));
                    }
                    var meshRenderer = child.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        _textureMaterialsAndColors.Add(( meshRenderer.material, meshRenderer.material.color));
                    }
                }

                RegistMaterialsRecursively(child, tagName);
            }
        }

        /// <summary>
        /// 指定のパリィ操作クラスがイベント終了した際に呼び出すデリゲートを設定します
        /// </summary>
        /// <param name="parryCtrl">パリィ操作クラス</param>
        void SubscribeParryEvent( SkillParryController parryCtrl )
        {
            parryCtrl.ProcessCompleted += ParryEventProcessCompleted;
        }

        /// <summary>
        /// 指定のパリィ操作クラスがイベント終了した際に呼び出すデリゲート設定を解除します
        /// </summary>
        /// <param name="parryCtrl">パリィ操作クラス</param>
        void UnsubscribeParryEvent(SkillParryController parryCtrl)
        {
            parryCtrl.ProcessCompleted -= ParryEventProcessCompleted;
        }

        /// <summary>
        /// パリィイベント終了時に呼び出されるデリゲート
        /// </summary>
        /// <param name="sender">呼び出しを行うパリィイベントコントローラ</param>
        /// <param name="e">イベントハンドラ用オブジェクト(この関数ではempty)</param>
        void ParryEventProcessCompleted( object sender, SkillParryCtrlEventArgs e )
        {
            ParryResult = e.Result;

            SkillParryController parryCtrl = sender as SkillParryController;
            parryCtrl.EndParryEvent();

            UnsubscribeParryEvent(parryCtrl);
        }

        #endregion  // PRIVATE_METHOD

        #region VIRTUAL_PUBLIC_METHOD

        /// <summary>
        /// 初期化処理を行います
        /// </summary>
        virtual public void Init( BattleManager btlMgr, StageController stgCtrl )
        {
            _btlMgr             = btlMgr;
            _stageCtrl          = stgCtrl;
            tmpParam.gridIndex  = param.initGridIndex;
            _elapsedTime        = 0f;

            // 戦闘時間管理クラスに自身の時間管理クラスを登録
            _btlMgr.TimeScaleCtrl.Regist( _timeScale );
        }

        /// <summary>
        /// 死亡処理を行います
        /// </summary>
        virtual public void Die() { }

        /// <summary>
        /// 対戦相手にダメージを与えるイベントを発生させます
        /// ※ 弾の着弾以外では近接攻撃アニメーションからも呼び出される設計です
        ///    近接攻撃キャラクターの攻撃アニメーションの適当なフレームでこのメソッドイベントを挿入してください
        /// </summary>
        virtual public void AttackOpponentEvent()
        {
            if (_opponent == null)
            {
                Debug.Assert(false);
            }

            _isAttacked = true;
            _opponent.param.CurHP += _opponent.tmpParam.expectedChangeHP;

            //　ダメージが0の場合はモーションを取らない
            if (_opponent.tmpParam.expectedChangeHP != 0)
            {
                if (_opponent.param.CurHP <= 0)
                {
                    _opponent.param.CurHP = 0;
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.DIE);
                }
                // ガードスキル使用時は死亡時以外はダメージモーションを再生しない
                else if (!_opponent.IsSkillInUse(SkillsData.ID.SKILL_GUARD))
                {
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.GET_HIT);
                }
            }

            // ダメージUIを表示
            BattleUISystem.Instance.SetDamageUIPosByCharaPos(_opponent, _opponent.tmpParam.expectedChangeHP);
            BattleUISystem.Instance.ToggleDamageUI(true);
        }

        /// <summary>
        /// 対戦相手の攻撃をパリィ(弾く)するイベントを発生させます　※攻撃アニメーションから呼び出されます
        /// </summary>
        virtual public void ParryOpponentEvent()
        {
            // NONE以外の結果が通知されているはず
            Debug.Assert(ParryResult != SkillParryController.JudgeResult.NONE);

            if (_opponent == null)
            {
                Debug.Assert(false);
            }

            if (ParryResult == SkillParryController.JudgeResult.FAILED)
            {
                return;
            }

            // 成功時(ジャスト含む)にはパリィ挙動
            _opponent.ParryRecieveEvent();
        }

        /// <summary>
        /// パリィを受けた際のイベントを発生させます
        /// </summary>
        virtual public void ParryRecieveEvent()
        {
            _timeScale.Reset();
            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.GET_HIT);
        }

        /// <summary>
        /// 弾を発射します
        /// MEMO : モーションからイベントフラグ処理として呼ばれます
        /// </summary>
        virtual public void FireBullet()
        {
            if (_bullet == null || _opponent == null) return;

            _bullet.gameObject.SetActive(true);

            // 射出地点、目標地点などを設定して弾を発射
            var firingPoint = transform.position;
            firingPoint.y += camParam.UICameraLookAtCorrectY;
            _bullet.SetFiringPoint(firingPoint);
            var targetCoordinate = _opponent.transform.position;
            targetCoordinate.y += _opponent.camParam.UICameraLookAtCorrectY;
            _bullet.SetTargetCoordinate(targetCoordinate);
            var gridLength = _stageCtrl.CalcurateGridLength(tmpParam.gridIndex, _opponent.tmpParam.gridIndex);
            _bullet.SetFlightTimeFromGridLength(gridLength);
            _bullet.StartUpdateCoroutine(AttackOpponentEvent);

            // 発射と同時に次のカメラに遷移させる
            _isTransitNextPhaseCamera = true;

            // この攻撃によって相手が倒されるかどうかを判定
            _opponent.IsDeclaredDead = ( _opponent.param.CurHP + _opponent.tmpParam.expectedChangeHP ) <= 0;
            if( !_opponent.IsDeclaredDead && 0 < _atkRemainingNum )
            {
                --_atkRemainingNum;
                AnimCtrl.SetAnimator(AttackAnimTags[_atkRemainingNum]);
            }
        }

        /// <summary>
        /// 戦闘に使用するスキルを選択します
        /// </summary>
        virtual public void SelectUseSkills(SituationType type)
        {
        }

        #endregion // VIRTUAL_PUBLIC_METHOD

        #region PUBLIC_METHOD

        /// <summary>
        /// キャラクターの位置を設定します
        /// </summary>
        /// <param name="gridIndex">マップグリッドのインデックス</param>
        /// <param name="dir">キャラクター角度</param>
        public void SetPosition(int gridIndex, in Vector3 pos, in Quaternion dir)
        {
            tmpParam.gridIndex = gridIndex;
            // var info = Stage.StageController.Instance.GetGridInfo(gridIndex);
            transform.position = pos;
            transform.rotation = dir;
        }

        /// <summary>
        /// 指定インデックスのグリッドにキャラクターの向きを合わせるように命令を発行します
        /// </summary>
        /// <param name="targetPos">向きを合わせる位置</param>
        public void RotateToPosition( in Vector3 targetPos )
        {
            var selfPos     = _stageCtrl.GetGridCharaStandPos( tmpParam.gridIndex );
            var direction   = targetPos - selfPos;
            direction.y     = 0f;

            _orderdRotation     = Quaternion.LookRotation(direction);
            _isOrderedRotation  = true;
        }

        /// <summary>
        /// 行動終了時など、行動不可の状態にします
        /// キャラクターモデルの色を変更し、行動不可であることを示す処理も含めます
        /// </summary>
        public void BeImpossibleAction()
        {
            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                tmpParam.isEndCommand[i] = true;
            }

            // 行動終了を示すためにマテリアルの色味をグレーに変更
            for (int i = 0; i < _textureMaterialsAndColors.Count; ++i)
            {
                _textureMaterialsAndColors[i].material.color = Color.gray;
            }
        }

        /// <summary>
        /// 行動再開時に行動可能状態にします
        /// キャラクターのモデルの色を元に戻す処理も含めます
        /// </summary>
        public void BePossibleAction()
        {
            tmpParam.Reset();

            // マテリアルの色味を通常の色味に戻す
            for (int i = 0; i < _textureMaterialsAndColors.Count; ++i)
            {
                _textureMaterialsAndColors[i].material.color = _textureMaterialsAndColors[i].originalColor;
            }
        }

        /// <summary>
        /// 近接攻撃シーケンスを開始します
        /// </summary>
        public void StartClosedAttackSequence()
        {
            _isAttacked         = false;
            _closingAttackPhase = CLOSED_ATTACK_PHASE.CLOSINGE;
            _elapsedTime        = 0f;

            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, true);
        }

        /// <summary>
        /// 遠隔攻撃シーケンスを開始します
        /// </summary>
        public void StartRangedAttackSequence()
        {
            _isAttacked         = false;
            _atkRemainingNum    = skillModifiedParam.AtkNum - 1;   // 攻撃回数を1消費
            var attackAnimtag   = AttackAnimTags[_atkRemainingNum];

            AnimCtrl.SetAnimator(attackAnimtag);
        }

        /// <summary>
        /// パリィシーケンスを開始します
        /// </summary>
        public void StartParrySequence()
        {
            _parryPhase = PARRY_PHASE.EXEC_PARRY;
            _elapsedTime = 0f;

            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.PARRY);
            // タイムスケールを遅くし、パリィ挙動をスローモーションで見せる
            _timeScale.SetTimeScale(0.1f);
        }

        /// <summary>
        /// パリィ判定処理を開始します
        /// MEMO : モーションのイベントフラグから呼び出します
        /// </summary>
        public void StartParryJudgeEvent()
        {
            if (!_opponent.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) return;
            
            SkillParryController parryCtrl = _btlMgr.SkillCtrl.ParryController;
            _opponent.SubscribeParryEvent(parryCtrl);
            parryCtrl.StartParryEvent(_opponent, this);
        }

        /// <summary>
        /// 実行可能なコマンドを更新します
        /// </summary>
        public void UpdateExecutableCommand(in StageController stageCtrl)
        {
            _executableCommands.Clear();

            for( int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i )
            {
                if (!_executableCommandTables[i](this, stageCtrl)) continue;

                _executableCommands.Add( (Command.COMMAND_TAG)i );
            }
        }

        /// <summary>
        /// 近接攻撃時の流れを更新します
        /// </summary>
        /// <param name="departure">近接攻撃の開始地点</param>
        /// <param name="destination">近接攻撃の終了地点</param>
        /// <returns>終了判定</returns>
        public bool UpdateClosedAttack(in Vector3 departure, in Vector3 destination)
        {
            var attackAnimtag = AttackAnimTags[skillModifiedParam.AtkNum - 1];

            if (GetBullet() != null) return false;

            float t = 0f;
            bool isReservedParry = _opponent.IsSkillInUse(SkillsData.ID.SKILL_PARRY);

            switch (_closingAttackPhase)
            {
                case CLOSED_ATTACK_PHASE.CLOSINGE:
                    _elapsedTime += Time.deltaTime;
                    t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_CLOSING_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    gameObject.transform.position = Vector3.Lerp(departure, destination, t);
                    if (1.0f <= t)
                    {
                        _elapsedTime = 0f;
                        AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, false);
                        AnimCtrl.SetAnimator(attackAnimtag);

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.ATTACK;
                    }
                    break;

                case CLOSED_ATTACK_PHASE.ATTACK:
                    if (IsEndAttackAnimSequence())
                    {
                        AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.DISTANCING;
                    }
                    break;
                case CLOSED_ATTACK_PHASE.DISTANCING:
                    // 攻撃前の場所に戻る
                    _elapsedTime += Time.deltaTime;
                    t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_DISTANCING_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    gameObject.transform.position = Vector3.Lerp(destination, departure, t);
                    if (1.0f <= t)
                    {
                        _elapsedTime = 0f;
                        _closingAttackPhase = CLOSED_ATTACK_PHASE.NONE;

                        return true;
                    }
                    break;
                default: break;
            }

            return false;
        }

        /// <summary>
        /// 遠隔攻撃時の流れを更新します
        /// </summary>
        /// <param name="departure">遠隔攻撃の開始地点</param>
        /// <param name="destination">遠隔攻撃の終了地点</param>
        /// <returns>終了判定</returns>
        public bool UpdateRangedAttack(in Vector3 departure, in Vector3 destination)
        {
            if (GetBullet() == null) return false;

            // 遠隔攻撃は特定のフレームでカメラ対象とパラメータを変更する
            if (IsTransitNextPhaseCamera())
            {
                _btlMgr.GetCameraController().TransitNextPhaseCameraParam(null, GetBullet().transform);
            }
            // 攻撃終了した場合はWaitに切り替え
            if (IsEndAttackAnimSequence())
            {
                AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);
            }

            // 対戦相手が攻撃を被弾済み、かつ、Wait状態に切り替え済みの場合に終了
            return _isAttacked && AnimCtrl.IsPlayingAnimationOnConditionTag(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);
        }

        /// <summary>
        /// 戦闘において、攻撃した側がパリィを受けた際の行動を更新します
        /// MEMO : パリィを受けた側は基本的に行動しないためfalseを返すのみ
        /// </summary>
        /// <param name="departure">攻撃開始座標</param>
        /// <param name="destination">攻撃目標座標</param>
        /// <returns>終了判定</returns>
        public bool UpdateParryOnAttacker(in Vector3 departure, in Vector3 destination)
        {
            return false;
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

            switch( _parryPhase )
            {
                case PARRY_PHASE.EXEC_PARRY:
                    if (isJustParry)
                    {
                        AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.SINGLE_ATTACK);

                        _parryPhase = PARRY_PHASE.AFTER_ATTACK;
                    }
                    else {
                        if (AnimCtrl.IsEndAnimationOnConditionTag(AnimDatas.ANIME_CONDITIONS_TAG.PARRY))
                        {
                            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);

                            return true;
                        }
                    }
                    break;
                case PARRY_PHASE.AFTER_ATTACK:
                    break;
            }

            return false;
        }

        /// <summary>
        /// パリィ処理の都合上でアニメーションを停止させます ※モーションから呼び出されます
        /// </summary>
        public void StopAnimationOnParry()
        {
            if(!_btlMgr.SkillCtrl.ParryController.IsJudgeEnd())
            {
                _timeScale.Stop();
            }
        }

        /// <summary>
        /// 対戦相手を設定します
        /// </summary>
        /// <param name="opponent">対戦相手</param>
        public void SetOpponentCharacter(Character opponent)
        {
            _opponent = opponent;
        }

        /// <summary>
        /// 攻撃を受ける際の設定を行います
        /// </summary>
        public void SetReceiveAttackSetting()
        {
            ParryResult = SkillParryController.JudgeResult.NONE;
        }

        /// <summary>
        /// 対戦相手の設定をリセットします
        /// </summary>
        public void ResetOnEndOfAttackSequence()
        {
            // 対戦相手情報をリセット
            _opponent = null;
            // 使用スキル情報をリセット
            tmpParam.ResetUseSkill();
        }

        /// <summary>
        /// ゲームオブジェクトを削除します
        /// </summary>
        public void Remove()
        {
            Destroy(gameObject);
            Destroy(this);
        }

        /// <summary>
        /// アクションゲージを消費します
        /// </summary>
        public void ConsumeActionGauge()
        {
            param.curActionGauge -= param.consumptionActionGauge;
            param.consumptionActionGauge = 0;

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                BattleUISystem.Instance.GetPlayerParamSkillBox(i).StopFlick();
            }
        }

        /// <summary>
        /// アクションゲージをrecoveryActionGaugeの分だけ回復します
        /// 基本的に自ターン開始時に呼びます
        /// </summary>
        public void RecoveryActionGauge()
        {
            param.curActionGauge = Mathf.Clamp(param.curActionGauge + param.recoveryActionGauge, 0, param.maxActionGauge);
        }

        /// <summary>
        /// 実行可能なコマンドを抽出します
        /// </summary>
        /// <param name="executableCommands">抽出先の引き数</param>
        public void FetchExecutableCommand(out List<Command.COMMAND_TAG> executableCommands, in StageController stageCtrl)
        {
            UpdateExecutableCommand(stageCtrl);

            executableCommands = _executableCommands;
        }

        public bool IsPlayer() { return param.characterTag == CHARACTER_TAG.PLAYER; }

        public bool IsEnemy() { return param.characterTag == CHARACTER_TAG.ENEMY; }

        public bool IsOther() { return param.characterTag == CHARACTER_TAG.OTHER; }

        /// <summary>
        /// 攻撃アニメーションの終了判定を返します
        /// </summary>
        /// <returns>攻撃アニメーションが終了しているか</returns>
        public bool IsEndAttackAnimSequence()
        {
            return AnimCtrl.IsEndAnimationOnStateName(AnimDatas.AtkEndStateName) ||  // 最後の攻撃のState名は必ずAtkEndStateNameで一致させる
                (_opponent.IsDeclaredDead && AnimCtrl.IsEndCurrentAnimation());                  // 複数回攻撃時でも、途中で相手が死亡することが確約される場合は攻撃を終了する
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsTransitNextPhaseCamera()
        {
            if(_isTransitNextPhaseCamera)
            {
                // trueの場合は次回以後の判定のためにfalseに戻す
                _isTransitNextPhaseCamera = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 死亡判定を返します
        /// </summary>
        /// <returns>死亡しているか否か</returns>
        public bool IsDead()
        {
            return param.CurHP <= 0;
        }

        /// <summary>
        /// 指定のスキルが使用登録されているかを判定します
        /// </summary>
        /// <param name="skillID">指定スキルID</param>
        /// <returns>使用登録されているか否か</returns>
        public bool IsSkillInUse(SkillsData.ID skillID)
        {
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                if (!tmpParam.isUseSkills[i]) continue;

                if (param.equipSkills[i] == skillID) return true;
            }

            return false;
        }

        /// <summary>
        /// 設定されている弾を取得します
        /// </summary>
        /// <returns>Prefabに設定されている弾</returns>
        public Bullet GetBullet() { return _bullet; }

        #endregion // PUBLIC_METHOD
    }
}