using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities.Ai;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    public class BattleLogicBase : MonoBehaviour
    {
		[Header( "戦闘パラメータ群" )]
		[SerializeField] protected BattleParameters _battleParams;

		[Inject] protected IUiSystem _uiSystem                  = null;
        [Inject] protected HierarchyBuilderBase _hierarchyBld   = null;
        [Inject] protected StageController _stageCtrl           = null;

        protected Character _opponent = null;

        protected bool _isPrevMoving = false;
        protected int[] _tileCostTable                              = null;                 // タイル移動時のコストテーブル(並列計算する可能性があるため、キャラ毎に保持)
        protected ReadOnlyReference<Character> _readOnlyOwner       = null;
        protected TransformHandler _transformHdlr                   = null;
        protected ActionRangeController _actionRangeCtrl            = null;                 // 行動範囲管理クラス
        protected BaseAi _baseAi                                    = null;                 // PlayerもAIに行動を任せる場合があるため、Characterに持たせる
        protected SkillNotifierBase[] _skillNotifier                = null;                 // スキル使用通知
        protected ThinkingType _thikType                            = ThinkingType.BASE;    // 思考タイプ
        protected PARRY_PHASE _parryPhase                           = PARRY_PHASE.NONE;
        private ICombatAnimationSequence _combatAnimSeq             = null;
        private List<COMMAND_TAG> _executableCommands               = new List<COMMAND_TAG>();
        private Func<ICombatAnimationSequence>[] _animSeqfactories;

        public bool IsDeclaredDead { get; set; } = false;                           // 死亡確定フラグ(攻撃シーケンスにおいて使用)
        public int[] TileCostTable => _tileCostTable;                               // タイル移動コストテーブルの取得
        public ICombatAnimationSequence CombatAnimSeq => _combatAnimSeq;
		public BattleParameters BattleParams => _battleParams;
		public BaseAi GetAi() => _baseAi;                                           // AIの取得
        public Character GetOpponent() => _opponent;
        public ActionRangeController ActionRangeCtrl => _actionRangeCtrl;           // 行動範囲管理クラスの取得
        public SkillNotifierBase SkillNotifier( int idx ) => _skillNotifier[idx];   // スキル通知処理の取得


        private delegate bool IsExecutableCommand( Character character, StageController stageCtrl );

        static private IsExecutableCommand[] _executableCommandTables =
        {
            Command.IsExecutableMoveCommand,
            Command.IsExecutableAttackCommand,
            Command.IsExecutableWaitCommand,
        };

        // 各勢力における敵対勢力(攻撃可能)キャラクタータグ
        static public Func<CHARACTER_TAG, bool>[] IsOpponentFaction = new Func<CHARACTER_TAG, bool>[( int ) CHARACTER_TAG.NUM]
        {
            tag => (tag == CHARACTER_TAG.ENEMY || tag == CHARACTER_TAG.OTHER),  // PLAYERにおける攻撃可能勢力
            tag => (tag == CHARACTER_TAG.PLAYER || tag == CHARACTER_TAG.OTHER),  // ENEMYにおける攻撃可能勢力
            tag => (tag == CHARACTER_TAG.PLAYER || tag == CHARACTER_TAG.ENEMY),  // OTHERにおける攻撃可能勢力
        };

        /// <summary>
        /// 特に何もしませんが、アクティブ設定のチェックボックスをInspector上に出現させるために定義
        /// </summary>
        void Update()
        {
            // 移動と攻撃が終了していれば、行動不可に遷移
            var endCommand = _battleParams.TmpParam.isEndCommand;
            if( endCommand[( int ) COMMAND_TAG.MOVE] && endCommand[( int ) COMMAND_TAG.ATTACK] )
            {
                BeImpossibleAction();
            }
        }

        public void Regist( Character owner )
        {
            _readOnlyOwner = new ReadOnlyReference<Character>( owner );
            _transformHdlr = owner.GetTransformHandler;
        }

        /// <summary>
        /// キャラクターの位置を設定します
        /// </summary>
        /// <param name="gridIndex">マップグリッドのインデックス</param>
        /// <param name="dir">キャラクター角度</param>
        public void SetPositionOnStage( int tileIndex, in Quaternion dir )
        {
            _battleParams.TmpParam.SetCurrentGridIndex( tileIndex );
            _transformHdlr.SetPosition( _stageCtrl.GetTileStaticData( tileIndex ).CharaStandPos );
            _transformHdlr.SetRotation( dir );
        }

        /// <summary>
        /// 実行可能なコマンドを抽出します
        /// </summary>
        /// <param name="executableCommands">抽出先の引き数</param>
        public void FetchExecutableCommand( out List<COMMAND_TAG> executableCommands, in StageController stageCtrl )
        {
            _executableCommands.Clear();

            for( int i = 0; i < ( int ) COMMAND_TAG.NUM; ++i )
            {
                if( !_executableCommandTables[i]( _readOnlyOwner.Value, stageCtrl ) ) continue;

                _executableCommands.Add( ( COMMAND_TAG ) i );
            }

            executableCommands = _executableCommands;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public void RegisterCombatAnimation( COMBAT_ANIMATION_TYPE type )
        {
            _combatAnimSeq = _animSeqfactories[( int ) type]();

            if( _combatAnimSeq != null )
            {
                _combatAnimSeq.Init( _readOnlyOwner.Value, BattleAnimationEventReceiver.AttackAnimTags );
            }
        }

        /// <summary>
        /// 指定キャラクターのアクションゲージを消費させ、ゲージのUIの表示を更新します
        /// </summary>
        public void ConsumeActionGauge()
        {
            _readOnlyOwner.Value.GetStatusRef.curActionGauge -= _readOnlyOwner.Value.GetStatusRef.consumptionActionGauge;
            _readOnlyOwner.Value.GetStatusRef.consumptionActionGauge = 0;

            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                _uiSystem.BattleUi.GetPlayerParamSkillBox( i ).StopFlick();
            }
        }

        /// <summary>
        /// 行動再開時に行動可能状態にします
        /// キャラクターのモデルの色を元に戻す処理も含めます
        /// </summary>
        public void BePossibleAction()
        {
            _battleParams.TmpParam.Reset();

            // マテリアルの色味を通常の色味に戻す
            for( int i = 0; i < _readOnlyOwner.Value.TextureMaterialAndColors.Count; ++i )
            {
                _readOnlyOwner.Value.TextureMaterialAndColors[i].material.color = _readOnlyOwner.Value.TextureMaterialAndColors[i].originalColor;
            }
        }

        /// <summary>
        /// 行動終了時など、行動不可の状態にします
        /// キャラクターモデルの色を変更し、行動不可であることを示す処理も含めます
        /// </summary>
        public void BeImpossibleAction()
        {
            for( int i = 0; i < ( int ) COMMAND_TAG.NUM; ++i )
            {
                _battleParams.TmpParam.SetEndCommandStatus( ( COMMAND_TAG ) i, true );
            }

            // 行動終了を示すためにマテリアルの色味をグレーに変更
            for( int i = 0; i < _readOnlyOwner.Value.TextureMaterialAndColors.Count; ++i )
            {
                _readOnlyOwner.Value.TextureMaterialAndColors[i].material.color = Color.gray;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costTable"></param>
        public void ApplyCostTable( int[] costTable )
        {
            _tileCostTable = costTable;
        }

        /// <summary>
        /// 指定のスキルが使用登録されているかを判定します
        /// </summary>
        /// <param name="skillID">指定スキルID</param>
        /// <returns>使用登録されているか否か</returns>
        public int GetUsingSkillSlotIndexById( ID skillID )
        {
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( !_battleParams.TmpParam.isUseSkills[i] ) { continue; }

                if( _readOnlyOwner.Value.GetStatusRef.equipSkills[i] == skillID ) { return i; }
            }

            return -1;
        }

        /// <summary>
        /// 指定のスキルの使用状態が切替可能かを判定します
        /// </summary>
        /// <param name="skillIdx">スキルの装備インデックス値</param>
        /// <returns>指定スキルの使用状態切替可否</returns>
        public bool CanToggleEquipSkill( int skillIdx, SituationType situationType )
        {
            if( Constants.EQUIPABLE_SKILL_MAX_NUM <= skillIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がスキルの装備最大数を超えています。" );

                return false;
            }

            // スキル使用ONの状態であれば、OFFにするだけなので、コストチェックする必要がない
            if( _battleParams.TmpParam.isUseSkills[skillIdx] )
            {
                return true;
            }

            return _readOnlyOwner.Value.GetStatusRef.CanUseEquipSkill( skillIdx, situationType );
        }

        /// <summary>
        /// 対戦相手を設定します
        /// </summary>
        /// <param name="opponent">対戦相手</param>
        public void SetOpponentCharacter( Character opponent )
        {
            _opponent = opponent;
        }

        /// <summary>
        /// 攻撃を受ける際の設定を行います
        /// </summary>
        public void SetReceiveAttackSetting() { }

        /// <summary>
        /// 対戦相手の設定をリセットします
        /// </summary>
        public void ResetOnEndOfAttackSequence()
        {
            _opponent = null;                       // 対戦相手情報をリセット
            _battleParams.TmpParam.ResetUseSkill(); // 使用スキル情報をリセット
        }

        virtual public void Setup()
        {
            LazyInject.GetOrCreate( ref _battleParams, () => _hierarchyBld.InstantiateWithDiContainer<BattleParameters>( false ) );
            LazyInject.GetOrCreate( ref _actionRangeCtrl, () => _hierarchyBld.InstantiateWithDiContainer<ActionRangeController>( false ) );
            _skillNotifier = new SkillNotifierBase[EQUIPABLE_SKILL_MAX_NUM];

            _battleParams.Setup();

            IsDeclaredDead = false;
            _animSeqfactories = new Func<ICombatAnimationSequence>[]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<ClosedAttackAnimationSequence>(false),
                () => _hierarchyBld.InstantiateWithDiContainer<RangedAttackAnimationSequence>(false),
                () => _hierarchyBld.InstantiateWithDiContainer<ParryAnimationSequence>(false)
            };
        }

        virtual public void Init()
        {
            _isPrevMoving = false;
            _battleParams.Init();
            _actionRangeCtrl.Init( _readOnlyOwner.Value );

            InitSkillNotifier();                                // スキルの通知クラスを初期化
            ApplyCostTable( TileCostTables.defaultCostTable );  // タイル移動コストテーブルを初期化
        }

        /// <summary>
        /// キャラクターの思考タイプを設定します
        /// </summary>
        /// <param name="type">設定する思考タイプ</param>
        virtual public void SetThinkType( ThinkingType type ) { }

        /// <summary>
        /// 戦闘に使用するスキルを選択します
        /// </summary>
        virtual public void SelectUseSkills( SituationType type ) { }

        /// <summary>
        /// 攻撃可能範囲の表示・非表示を切り替えます
        /// </summary>
        virtual public void ToggleDisplayDangerRange() { }

        /// <summary>
        /// 攻撃可能範囲の表示・非表示を設定します
        /// </summary>
        /// <param name="isShow"></param>
        virtual public void SetDisplayDangerRange( bool isShow ) { }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        /// <returns>切替の有無</returns>
        virtual public bool ToggleUseSkillks( int index ) { return false; }

        /// <summary>
        /// キャラクターを、作成済みのパスに沿って移動させます
        /// </summary>
        /// <param name="moveSpeedRate">移動速度レート</param>
        /// <returns>移動が終了したか</returns>
        virtual public bool UpdateMovePath( float moveSpeedRate = 1.0f )
        {
            var pathHdlr = _actionRangeCtrl.MovePathHdlr;

            // 移動ルートの最終インデックスに到達している場合は、目標タイルに到達しているため終了
            if( pathHdlr.IsEndPathTrace() ) { return true; }

            bool toggleAnimation = false;
            var focusedTileData = pathHdlr.GetFocusedTileStaticData();
            var focusedTilePos = focusedTileData.CharaStandPos;
            Vector3 prevDirXZ = ( focusedTilePos - _transformHdlr.GetPreviousPosition() ).XZ().normalized;
            Vector3 focusDirXZ = ( focusedTilePos - _transformHdlr.GetPosition() ).XZ().normalized;
            Action<float, float, Vector3, Vector3> jumpAction = ( float dprtHeight, float destHeight, Vector3 dprtPos, Vector3 destPos ) =>
            {
                // 高低差が一定以上ある場合はジャンプ動作を開始
                if( NEED_JUMP_HEIGHT_DIFFERENCE <= ( int ) Math.Abs( destHeight - dprtHeight ) )
                {
                    _transformHdlr.StartJump( in dprtPos, in destPos, moveSpeedRate );
                }
            };

            // 現在の目標タイルに到達している場合はインデックス値をインクリメントすることで目標タイルを更新する
            if( Vector3.Dot( prevDirXZ, focusDirXZ ) <= 0 )
            {
                _transformHdlr.SetPosition( focusedTilePos );   // 位置を目標タイルに合わせる
                _transformHdlr.ResetVelocityAcceleration();     // 速度、加速度をリセット
                _battleParams.TmpParam.gridIndex = pathHdlr.GetFocusedWaypointIndex();    // キャラクターが保持するタイルインデックスを更新
                pathHdlr.IncrementFocusedWaypointIndex();                            // 目標インデックス値をインクリメントして次の目標タイルに更新

                // 最終インデックスに到達している場合は移動アニメーションを停止して終了
                if( pathHdlr.IsEndPathTrace() )
                {
                    if( _isPrevMoving ) { toggleAnimation = true; }

                    _isPrevMoving = false;
                }
                // まだ移動が続く場合は次の目標タイルを目指して速度と向きを設定
                else
                {
                    var nextTileData = pathHdlr.GetFocusedTileStaticData();
                    var nextTilePos = nextTileData.CharaStandPos;
                    Vector3 nextDirXZ = ( nextTilePos - _transformHdlr.GetPosition() ).XZ().normalized;

                    _transformHdlr.SetVelocityAcceleration( nextDirXZ * CHARACTER_MOVE_SPEED * moveSpeedRate, Vector3.zero );
                    _transformHdlr.SetRotation( Quaternion.LookRotation( nextDirXZ ) );

                    jumpAction( focusedTileData.Height, nextTileData.Height, focusedTilePos, nextTilePos );
                }
            }
            else
            {
                // 移動開始の場合は速度と向きを設定
                if( !_isPrevMoving )
                {
                    var currentTileData = _stageCtrl.GetTileStaticData( _battleParams.TmpParam.gridIndex );

                    _transformHdlr.SetVelocityAcceleration( focusDirXZ * CHARACTER_MOVE_SPEED * moveSpeedRate, Vector3.zero );
                    _transformHdlr.SetRotation( Quaternion.LookRotation( focusDirXZ ) );
                    toggleAnimation = true;

                    jumpAction( currentTileData.Height, focusedTileData.Height, currentTileData.CharaStandPos, focusedTilePos );
                }

                _isPrevMoving = true;
            }

            if( toggleAnimation ) { _readOnlyOwner.Value.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.MOVE, _isPrevMoving ); } // 移動アニメーションの切替

            return !_isPrevMoving;
        }

        /// <summary>
        /// 所有しているスキルの通知クラスを初期化します
        /// </summary>
        private void InitSkillNotifier()
        {
            for( int i = 0; i < ( int ) EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                int skillID = ( int ) _readOnlyOwner.Value.GetStatusRef.equipSkills[i];
                if( skillID < 0 ) { continue; }

                _skillNotifier[i] = SkillsData.skillNotifierFactory[skillID]();
                _skillNotifier[i].Init( _readOnlyOwner.Value );
            }
        }
    }
}