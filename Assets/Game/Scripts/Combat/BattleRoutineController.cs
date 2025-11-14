using Frontier.Combat.Skill;
using Frontier.CombatPreparation;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class BattleRoutineController : FocusRoutineBase
    {
        private enum BattlePhaseType
        {
            Placement,
            Player,
            Enemy,
            Other
        }

        [Header("スキルコントローラオブジェクト")]
        [SerializeField]
        private GameObject _skillCtrlObject;

        [Header("戦闘ファイル読込オブジェクト")]
        [SerializeField]
        private GameObject _btlFileLoadObject;

        [Inject] private HierarchyBuilderBase _hierarchyBld  = null;
        [Inject] private StageController _stgCtrl            = null;
        [Inject] private BattleUISystem _btlUi               = null;

        private int _currentStageIndex = 0;
        private BattlePhase _phase;
        private BattleFileLoader _btlFileLoader                 = null;
        private BattleCameraController _battleCameraCtrl        = null;
        private BattleCharacterCoordinator _btlCharaCdr         = null;
        private BattleTimeScaleController _battleTimeScaleCtrl  = null;
        private Dictionary<BattlePhaseType, PhaseHandlerBase> _phaseHandlers;
        private BattlePhaseType _currentPhase;
        
        // 現在選択中のキャラクターインデックス
        public CharacterKey SelectCharacterKey { get; private set; } = new CharacterKey(CHARACTER_TAG.NONE, -1);
        public BattleUISystem BtlUi => _btlUi;
        public BattleTimeScaleController TimeScaleCtrl => _battleTimeScaleCtrl;
        public BattleCharacterCoordinator BtlCharaCdr => _btlCharaCdr;

        void Awake()
        {
            var btlCameraObj = GameObject.FindWithTag("MainCamera");
            if ( btlCameraObj != null ) 
            {
                _battleCameraCtrl = btlCameraObj.GetComponent<BattleCameraController>();
            }

            if (_btlFileLoader == null)
            {
                _btlFileLoader = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleFileLoader>(_btlFileLoadObject, true, false, typeof(BattleFileLoader).Name);
                NullCheck.AssertNotNull(_btlFileLoader, nameof( _btlFileLoader ) );
            }

            if (_btlCharaCdr == null)
            {
                _btlCharaCdr = _hierarchyBld.InstantiateWithDiContainer<BattleCharacterCoordinator>(false);
                NullCheck.AssertNotNull(_btlCharaCdr, nameof( _btlCharaCdr ) );
            }

            if( _battleTimeScaleCtrl == null)
            {
                _battleTimeScaleCtrl = _hierarchyBld.InstantiateWithDiContainer<BattleTimeScaleController>(false);
                NullCheck.AssertNotNull( _battleTimeScaleCtrl, nameof( _battleTimeScaleCtrl ) );
            }

            if ( SkillsData.skillNotifierFactory == null )
            {
                SkillsData.BuildSkillNotifierFactory( _hierarchyBld );
            }

            _phaseHandlers = new Dictionary<BattlePhaseType, PhaseHandlerBase>
            {
                { BattlePhaseType.Placement, _hierarchyBld.InstantiateWithDiContainer<DeploymentPhaseHandler>(false) },
                { BattlePhaseType.Player,    _hierarchyBld.InstantiateWithDiContainer<PlayerPhaseHandler>(false) },
                { BattlePhaseType.Enemy,     _hierarchyBld.InstantiateWithDiContainer<EnemyPhaseHandler>(false) },
                { BattlePhaseType.Other,     _hierarchyBld.InstantiateWithDiContainer<OtherPhaseHandler>(false) }
            };
        }

        public IEnumerator Battle()
        {
            yield return null;
        }

        /// <summary>
        /// ステージグリッドスクリプトを登録します
        /// </summary>
        /// <param name="script">登録するスクリプト</param>
        public void registStageController(Stage.StageController script)
        {
            _stgCtrl = script;
        }

        /// <summary>
        /// 戦闘カメラコントローラを取得します
        /// </summary>
        /// <returns>戦闘カメラコントローラ</returns>
        public BattleCameraController GetCameraController()
        {
            return _battleCameraCtrl;
        }

        /// <summary>
        /// 終了状態かどうかを判定します
        /// </summary>
        /// <returns>true : 終了</returns>
        public bool IsEnd()
        {
            return _phase == BattlePhase.BATTLE_END;
        }

        /// <summary>
        /// ステージクリア時のUIとアニメーションを表示します
        /// </summary>
        public void StartStageClearAnim()
        {
            _btlUi.ToggleStageClearUI(true);
            _btlUi.StartStageClearAnim();
        }

        /// <summary>
        /// ゲームオーバー時のUIとアニメーションを表示します
        /// </summary>
        public void StartGameOverAnim()
        {
            _btlUi.ToggleGameOverUI(true);
            _btlUi.StartGameOverAnim();
        }

        /// <summary>
        /// MonoBehaviorを取得します
        /// </summary>
        /// <returns>このクラス自身</returns>
        public MonoBehaviour GetUnderlyingBehaviour()
        {
            return this;
        }

        /// <summary>
        /// 次のフェーズへの移行先を取得します
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private BattlePhaseType GetNextPhase( BattlePhaseType current )
        {
            if( current == BattlePhaseType.Placement )
            {
                return BattlePhaseType.Player; // 配置が終わったら通常ループに移行
            }

            // 第三勢力キャラクターが存在する場合は、第三勢力キャラクターのフェイズを追加
            if( 0 < _btlCharaCdr.GetCharacterCount( CHARACTER_TAG.OTHER ) )
            {
                return current switch
                {
                    BattlePhaseType.Player => BattlePhaseType.Enemy,
                    BattlePhaseType.Enemy => BattlePhaseType.Other,
                    BattlePhaseType.Other => BattlePhaseType.Player,
                    _ => BattlePhaseType.Player
                };
            }
            else
            {
                return current switch
                {
                    BattlePhaseType.Player => BattlePhaseType.Enemy,
                    BattlePhaseType.Enemy => BattlePhaseType.Player,
                    _ => BattlePhaseType.Player
                };
            }
        }

        // =========================================================
        // IFocusRoutineの実装
        // =========================================================
        #region IFocusRoutine Implementation

        /// <summary>
        /// 各種パラメータを初期化させます
        /// </summary>
        override public void Init()
        {
            base.Init();

            _stgCtrl.Init();
            _btlCharaCdr.Init();

            // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する ※デバッグシーンは除外
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                _btlFileLoader.CharacterLoad(_currentStageIndex);
            }

            _btlCharaCdr.PlaceAllCharactersAtStartPosition();           // 全キャラクターのステージ初期座標の設定
            _stgCtrl.TileDataHdlr().UpdateTileDynamicDatas();           // タイル情報を更新
            _phase = BattlePhase.BATTLE_START;                          // 初期フェイズを設定
            _currentPhase = BattlePhaseType.Placement;                  // 初期フェイズを設定
            _btlFileLoader.LoadCameraParams(_battleCameraCtrl);         // ファイル読込マネージャにカメラパラメータをロードさせる
            _btlFileLoader.LoadSkillsData();                            // スキルデータの読込
        }

        override public void UpdateRoutine()
        {
            base.UpdateRoutine();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if( !Methods.IsDebugScene() )
#endif
            {
                if( GameMain.instance.IsInvoking() )
                {
                    return;
                }
            }

            // 現在選択しているタイル上に存在するキャラクター情報を更新
            (var tileSData, var tileDData) = _stgCtrl.TileDataHdlr().GetCurrentTileDatas();
            _battleCameraCtrl.SetLookAtBasedOnSelectCursor( tileSData.CharaStandPos );

            SelectCharacterKey = tileDData.CharaKey;

            // ステージクリア時、ゲーム―オーバー時のUIアニメーションが再生されている場合は終了
            if( _btlUi.StageClear.isActiveAndEnabled || _btlUi.GameOver.isActiveAndEnabled ) return;

            _phaseHandlers[_currentPhase].Update();

            _stgCtrl.TileDataHdlr().UpdateTileDynamicDatas();   // タイル情報を更新
        }

        override public void LateUpdateRoutine()
        {
            base.LateUpdateRoutine();

            if( _btlUi.StageClear.isActiveAndEnabled ) { return; }  // ステージクリア時のUIアニメーションが再生されている場合は終了
            if( _btlUi.GameOver.isActiveAndEnabled ) { return; }    // ゲーム―オーバー時のUIアニメーションが再生されている場合は終了

            // 勝利、全滅チェックを行う
            if (_btlCharaCdr.CheckVictoryOrDefeat(StartStageClearAnim, StartGameOverAnim)) { return; }

            var handler = _phaseHandlers[_currentPhase];
            if( handler.LateUpdate() )
            {
                // 一時パラメータをリセット
                _btlCharaCdr.ResetTmpParamAllCharacter();

                // 次のハンドラーに切り替える
                handler.Exit();
                _currentPhase = GetNextPhase( _currentPhase );
                _phaseHandlers[_currentPhase].Run();
            }
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// クラス内の処理を駆動します
        /// </summary>
        override public void Run()
        {
            base.Run();

            _phaseHandlers[_currentPhase].Run();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 中断させていた処理を再始動します
        /// </summary>
        override public void Restart()
        {
            base.Restart();

            _phaseHandlers[_currentPhase].Restart();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 処理を中断します
        /// </summary>
        override public void Pause()
        {
            base.Pause();

            _phaseHandlers[_currentPhase].Pause();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 処理を停止します
        /// </summary>
        override public void Exit()
        {
            base.Exit();

            _phaseHandlers[_currentPhase].Exit();
        }

        override public int GetPriority() { return (int)FocusRoutinePriority.BATTLE; }

        #endregion // IFocusRoutine Implementation
    }
}