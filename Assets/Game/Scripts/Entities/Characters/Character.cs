using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using ModestTree;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Frontier.Combat.Skill;
using static Frontier.Combat.ParrySkillHandler;

namespace Frontier.Entities
{
    [SerializeField]
    public class Character : MonoBehaviour, IBasicAnimationEvent, IParryAnimationEvent
    {
        [SerializeField]
        [Header("弾オブジェクト")]
        private GameObject _bulletObject;

        [SerializeField]
        [Header("パラメータ群")]
        protected CharacterParameters _params;

        // Injectされるインスタンス
        protected HierarchyBuilderBase _hierarchyBld                = null;
        protected BattleRoutineController _btlRtnCtrl               = null;
        protected CombatSkillEventController _combatSkillEventCtrl  = null;
        protected StageController _stageCtrl                        = null;

        private bool _isTransitNextPhaseCamera  = false;
        private bool _isOrderedRotation         = false;
        private readonly TimeScale _timeScale   = new TimeScale();
        private ICombatAnimationSequence _combatAnimSeq;
        private Quaternion _orderdRotation      = Quaternion.identity;
        private List<(Material material, Color originalColor)> _textureMaterialsAndColors   = new List<(Material, Color)>();
        private List<Command.COMMAND_TAG> _executableCommands                               = new List<Command.COMMAND_TAG>();
        private Func<ICombatAnimationSequence>[] _animSeqfactories;

        protected PARRY_PHASE _parryPhase                   = PARRY_PHASE.NONE;
        protected Character _opponent                       = null; // 戦闘時の対戦相手
        protected Bullet _bullet                            = null; // 矢などの弾
        protected ParrySkillNotifier _parrySkill            = null;

        public int AtkRemainingNum { get; set; } = 0;   // 攻撃シーケンスにおける残り攻撃回数
        public float ElapsedTime { get; set; } = 0f;
        public bool IsAttacked { get; set; } = false;
        public bool IsDeclaredDead { get; set; } = false;   // 死亡確定フラグ(攻撃シーケンスにおいて使用)
        public AnimationController AnimCtrl { get; } = new AnimationController();   // アニメーションコントローラの取得
        public ICombatAnimationSequence CombatAnimSeq => _combatAnimSeq;
        public GameObject BulletObject => _bulletObject;            // 弾オブジェクトの取得
        public ParrySkillNotifier GetParrySkill => _parrySkill;     // パリィスキル処理の取得
        public TimeScale GetTimeScale => _timeScale;                // タイムスケールの取得
        public CharacterParameters Params => _params;       // パラメータ群の取得(※CharacterParametersはstructなので参照渡しにする)

        // 攻撃用アニメーションタグ
        private static AnimDatas.AnimeConditionsTag[] AttackAnimTags = new AnimDatas.AnimeConditionsTag[]
        {
            AnimDatas.AnimeConditionsTag.SINGLE_ATTACK,
            AnimDatas.AnimeConditionsTag.DOUBLE_ATTACK,
            AnimDatas.AnimeConditionsTag.TRIPLE_ATTACK
        };

        private delegate bool IsExecutableCommand(Character character, StageController stageCtrl);
        private static IsExecutableCommand[] _executableCommandTables =
        {
            Command.IsExecutableMoveCommand,
            Command.IsExecutableAttackCommand,
            Command.IsExecutableWaitCommand,
        };

        #region PRIVATE_METHOD

        [Inject]
        void Construct( HierarchyBuilderBase hierarchyBld,  BattleRoutineController battleMgr, CombatSkillEventController combatSkillEventCtrl, StageController stageCtrl )
        {
            _hierarchyBld           = hierarchyBld;
            _btlRtnCtrl             = battleMgr;
            _combatSkillEventCtrl   = combatSkillEventCtrl;
            _stageCtrl              = stageCtrl;
        }

        void Awake()
        {
            _timeScale.OnValueChange    = AnimCtrl.UpdateTimeScale;
            IsDeclaredDead              = false;
            _params.Awake();

            AnimCtrl.Init(GetComponent<Animator>());

            _animSeqfactories = new Func<ICombatAnimationSequence>[]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<ClosedAttackAnimationSequence>(false),
                () => _hierarchyBld.InstantiateWithDiContainer<RangedAttackAnimationSequence>(false),
                () => _hierarchyBld.InstantiateWithDiContainer<ParryAnimationSequence>(false)
            };

            // キャラクターモデルのマテリアルが設定されているObjectを取得し、
            // Materialと初期のColor設定を保存
            RegistMaterialsRecursively(this.transform, Constants.OBJECT_TAG_NAME_CHARA_SKIN_MESH);
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
            var endCommand = _params.TmpParam.isEndCommand;
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                BeImpossibleAction();
            }
        }

        void OnDestroy()
        {
            // 戦闘時間管理クラスの登録を解除
            _btlRtnCtrl.TimeScaleCtrl.Unregist(_timeScale);
        }

        /// <summary>
        /// 所有しているスキルの通知クラスを初期化します
        /// </summary>
        void InitSkillNotifier()
        {
            for( int i = 0; i <  (int)Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                int skillID = (int)_params.CharacterParam.equipSkills[i];

                if ( (int)ID.SKILL_PARRY == skillID )
                {
                    _parrySkill = _hierarchyBld.InstantiateWithDiContainer<ParrySkillNotifier>(false);
                    _parrySkill.Init( this );
                }
            }
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

        #endregion  // PRIVATE_METHOD

        #region VIRTUAL_PUBLIC_METHOD

        /// <summary>
        /// 初期化処理を行います
        /// </summary>
        virtual public void Init()
        {
            _params.Init();

            ResetElapsedTime();

            // 戦闘時間管理クラスに自身の時間管理クラスを登録
            _btlRtnCtrl.TimeScaleCtrl.Regist( _timeScale );
            // スキルの通知クラスを初期化
            InitSkillNotifier();
        }

        /// <summary>
        /// 戦闘に使用するスキルを選択します
        /// </summary>
        virtual public void SelectUseSkills( SituationType type ) { }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        /// <returns>切替の有無</returns>
        virtual public bool ToggleUseSkillks( int index )
        {
            return false;
        }

        #endregion // VIRTUAL_PUBLIC_METHOD

        #region PUBLIC_METHOD

        /// <summary>
        /// 弾を設定します
        /// </summary>
        /// <param name="bullet">設定する弾</param>
        public void SetBullet( Bullet bullet )
        {
            Debug.Assert( bullet != null, "The argument 'bullet' is unexpectedly null." );

            _bullet = bullet;
        }

        /// <summary>
        /// キャラクターの位置を設定します
        /// </summary>
        /// <param name="gridIndex">マップグリッドのインデックス</param>
        /// <param name="dir">キャラクター角度</param>
        public void SetPosition(int gridIndex, in Quaternion dir)
        {
            _params.TmpParam.SetCurrentGridIndex( gridIndex );
            var info = _stageCtrl.GetGridInfo(gridIndex);
            transform.position = info.charaStandPos;
            transform.rotation = dir;
        }

        /// <summary>
        /// 指定インデックスのグリッドにキャラクターの向きを合わせるように命令を発行します
        /// </summary>
        /// <param name="targetPos">向きを合わせる位置</param>
        public void RotateToPosition(in Vector3 targetPos)
        {
            var selfPos = _stageCtrl.GetGridCharaStandPos(_params.TmpParam.gridIndex);
            var direction = targetPos - selfPos;
            direction.y = 0f;

            _orderdRotation = Quaternion.LookRotation(direction);
            _isOrderedRotation = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void RegisterCombatAnimation( COMBAT_ANIMATION_TYPE type )
        {
            _combatAnimSeq = _animSeqfactories[(int)type]();

            if( _combatAnimSeq != null )
            {
                _combatAnimSeq.Init(this, AttackAnimTags);
            }
        }

        /// <summary>
        /// 指定キャラクターのアクションゲージを消費させ、ゲージのUIの表示を更新します
        /// </summary>
        public void ConsumeActionGauge()
        {
            _params.CharacterParam.curActionGauge -= _params.CharacterParam.consumptionActionGauge;
            _params.CharacterParam.consumptionActionGauge = 0;

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                _btlRtnCtrl.BtlUi.GetPlayerParamSkillBox(i).StopFlick();
            }
        }

        /// <summary>
        /// 行動終了時など、行動不可の状態にします
        /// キャラクターモデルの色を変更し、行動不可であることを示す処理も含めます
        /// </summary>
        public void BeImpossibleAction()
        {
            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                _params.TmpParam.SetEndCommandStatus((Command.COMMAND_TAG)i, true);
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
            _params.TmpParam.Reset();

            // マテリアルの色味を通常の色味に戻す
            for (int i = 0; i < _textureMaterialsAndColors.Count; ++i)
            {
                _textureMaterialsAndColors[i].material.color = _textureMaterialsAndColors[i].originalColor;
            }
        }

        /// <summary>
        /// 指定のスキルの使用状態が切替可能かを判定します
        /// </summary>
        /// <param name="skillIdx">スキルの装備インデックス値</param>
        /// <returns>指定スキルの使用状態切替可否</returns>
        public bool CanToggleEquipSkill( int skillIdx, SituationType situationType )
        {
            if (Constants.EQUIPABLE_SKILL_MAX_NUM <= skillIdx)
            {
                Debug.Assert(false, "指定されているスキルの装備インデックス値がスキルの装備最大数を超えています。");

                return false;
            }

            // スキル使用ONの状態であれば、OFFにするだけなので、コストチェックする必要がない
            if( _params.TmpParam.isUseSkills[skillIdx] )
            {
                return true;
            }

            return _params.CharacterParam.CanUseEquipSkill(skillIdx, situationType);
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
        public void SetReceiveAttackSetting() {}

        /// <summary>
        /// 対戦相手の設定をリセットします
        /// </summary>
        public void ResetOnEndOfAttackSequence()
        {
            _opponent = null;                   // 対戦相手情報をリセット
            _params.TmpParam.ResetUseSkill();   // 使用スキル情報をリセット
        }

        /// <summary>
        /// キャラクターに対する時間計測をリセットします
        /// </summary>
        public void ResetElapsedTime()
        {
            ElapsedTime = 0;
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
        /// 実行可能なコマンドを抽出します
        /// </summary>
        /// <param name="executableCommands">抽出先の引き数</param>
        public void FetchExecutableCommand(out List<Command.COMMAND_TAG> executableCommands, in StageController stageCtrl)
        {
            _executableCommands.Clear();

            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                if (!_executableCommandTables[i](this, stageCtrl)) continue;

                _executableCommands.Add((Command.COMMAND_TAG)i);
            }

            executableCommands = _executableCommands;
        }

        /// <summary>
        /// 次のカメラ遷移に移れる状態かを判定します
        /// </summary>
        /// <returns>次のカメラ遷移に移れるか</returns>
        public bool IsTransitNextPhaseCamera()
        {
            if(_isTransitNextPhaseCamera)
            {
                _isTransitNextPhaseCamera = false;  // trueの場合は次回以後の判定のためにfalseに戻す
                return true;
            }

            return false;
        }

        /// <summary>
        /// 指定のスキルが使用登録されているかを判定します
        /// </summary>
        /// <param name="skillID">指定スキルID</param>
        /// <returns>使用登録されているか否か</returns>
        public bool IsSkillInUse( ID skillID )
        {
            for ( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if ( !_params.TmpParam.isUseSkills[i] ) continue;

                if ( _params.CharacterParam.equipSkills[i] == skillID ) return true;
            }

            return false;
        }

        /// <summary>
        /// 設定されている弾を取得します
        /// </summary>
        /// <returns>Prefabに設定されている弾</returns>
        public Bullet GetBullet() { return _bullet; }

        /// <summary>
        /// 戦闘における対戦相手を取得します
        /// </summary>
        /// <returns>対戦相手</returns>
        public Character GetOpponentChara() { return _opponent; }

        // アニメーションのイベントフラグから呼ばれる関数群です。
        // Unityの仕様上、アニメーションから呼び出される関数はCharacterに直接定義されている必要があるため、他クラスには分離出来ません。
        #region CALL_BY_ANIMATION 

        /// <summary>
        /// 死亡処理を開始します
        /// ※各キャラクターのアニメーションから呼ばれます
        /// </summary>
        public void DieOnAnimEvent()
        {
            _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList(this);
        }

        /// <summary>
        /// キャラクターに設定されている弾を発射します
        /// ※各キャラクターのアニメーションから呼ばれます
        /// </summary>
        public void FireBulletOnAnimEvent()
        {
            if (_bullet == null || _opponent == null) return;

            _bullet.gameObject.SetActive(true);

            // 射出地点、目標地点などを設定して弾を発射
            var firingPoint = transform.position;
            firingPoint.y += _params.CameraParam.UICameraLookAtCorrectY;
            _bullet.SetFiringPoint(firingPoint);
            var targetCoordinate = _opponent.transform.position;
            targetCoordinate.y += _opponent.Params.CameraParam.UICameraLookAtCorrectY;
            _bullet.SetTargetCoordinate(targetCoordinate);
            var gridLength = _stageCtrl.CalcurateGridLength(_params.TmpParam.gridIndex, _opponent.Params.TmpParam.gridIndex);
            _bullet.SetFlightTimeFromGridLength(gridLength);
            _bullet.StartUpdateCoroutine(HurtOpponentByAnimation);

            _isTransitNextPhaseCamera = true;   // 発射と同時に次のカメラに遷移させる

            // この攻撃によって相手が倒されるかどうかを判定
            _opponent.IsDeclaredDead = (_opponent.Params.CharacterParam.CurHP + _opponent.Params.TmpParam.expectedHpChange) <= 0;
            if (!_opponent.IsDeclaredDead && 0 < AtkRemainingNum)
            {
                --AtkRemainingNum;
                AnimCtrl.SetAnimator(AttackAnimTags[AtkRemainingNum]);
            }
        }

        /// <summary>
        /// 相手を攻撃した際の処理を開始します
        /// ※各キャラクターのアニメーションから呼ばれます
        /// </summary>
        public void AttackOpponentOnAnimEvent()
        {
            if (_opponent == null)
            {
                Debug.Assert(false);
            }

            IsAttacked = true;
            _opponent.Params.CharacterParam.CurHP += _opponent.Params.TmpParam.expectedHpChange;

            //　ダメージが0の場合はモーションを取らない
            if( _opponent.Params.TmpParam.expectedHpChange != 0 )
            {
                if (_opponent.Params.CharacterParam.CurHP <= 0)
                {
                    _opponent.Params.CharacterParam.CurHP = 0;
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.DIE);
                }
                // ガードスキル使用時は死亡時以外はダメージモーションを再生しない
                else if (!_opponent.IsSkillInUse(ID.SKILL_GUARD))
                {
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.GET_HIT);
                }
            }

            _btlRtnCtrl.BtlUi.ShowDamageOnCharacter(_opponent); // ダメージUIを表示
        }

        /// <summary>
        /// 対戦相手にダメージを与えるイベントを発生させます
        /// ※ 弾の着弾以外では近接攻撃アニメーションからも呼び出される設計です
        ///    近接攻撃キャラクターの攻撃アニメーションの適当なフレームでこのメソッドイベントを挿入してください
        /// </summary>
        public void HurtOpponentByAnimation()
        {
            if (_opponent == null)
            {
                Debug.Assert(false);
            }

            IsAttacked = true;
            _opponent.Params.CharacterParam.CurHP += _opponent.Params.TmpParam.expectedHpChange;

            //　ダメージが0の場合はモーションを取らない
            if (_opponent.Params.TmpParam.expectedHpChange != 0)
            {
                if (_opponent.Params.CharacterParam.CurHP <= 0)
                {
                    _opponent.Params.CharacterParam.CurHP = 0;
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.DIE);
                }
                // ガードスキル使用時は死亡時以外はダメージモーションを再生しない
                else if (!_opponent.IsSkillInUse(ID.SKILL_GUARD))
                {
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.GET_HIT);
                }
            }

            // ダメージUIを表示
            _btlRtnCtrl.BtlUi.ShowDamageOnCharacter(_opponent);
        }

        /// <summary>
        /// パリィイベントを開始します
        /// MEMO ; パリィは各キャラクターの攻撃アニメーションに設定されたタイミングから開始するため、
        ///        パリィを行うキャラクターのアニメーションではなく、
        ///        その対戦相手のアニメーションから呼ばれることに注意してください
        /// </summary>
        public void StartParryOnAnimEvent()
        {
            if( _opponent.GetParrySkill == null ) return;

            _opponent.GetParrySkill.StartParryJudgeEvent();
        }

        /// <summary>
        /// 相手の攻撃を弾く動作を行います
        /// ※各キャラクターのパリィ用アニメーションから呼ばれます
        /// </summary>
        public void ParryAttackOnAnimEvent()
        {
            _parrySkill.ParryOpponentEvent();
        }

        /// <summary>
        /// パリィの判定が得られる以前に、パリィによる振り払いモーションが再生されてしまうとまずいため、
        /// 振り払い直前でアニメーションを停止するために用います
        /// ※各キャラクターのパリィ用アニメーションから呼ばれます
        /// </summary>
        public void StopParryAnimationOnAnimEvent()
        {
            ParrySkillHandler parrySkillHdlr = _combatSkillEventCtrl.CurrentSkillHandler as ParrySkillHandler;
            if (parrySkillHdlr == null) return;

            if (!parrySkillHdlr.IsJudgeEnd())
            {
                _timeScale.Stop();
            }
        }

        #endregion // CALL_BY_ANIMATION

        #endregion // PUBLIC_METHOD
    }
}