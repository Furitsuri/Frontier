using Frontier.Stage;
using Frontier.Entities;
using System.Collections;
using UnityEngine;
using Zenject;
using Frontier.Combat;
using System;
using Frontier.Combat.Skill;

namespace Frontier.Battle
{
    public class BattleRoutineController : FocusRoutineBase
    {
        [Header("スキルコントローラオブジェクト")]
        [SerializeField]
        private GameObject _skillCtrlObject;

        [Header("戦闘ファイル読込オブジェクト")]
        [SerializeField]
        private GameObject _btlFileLoadObject;

        [Inject] private HierarchyBuilderBase _hierarchyBld  = null;
        [Inject] private StageController _stgCtrl            = null;
        [Inject] private BattleUISystem _btlUi               = null;

        private BattlePhase _phase;
        private BattleFileLoader _btlFileLoader                 = null;
        private BattleCameraController _battleCameraCtrl        = null;
        private BattleCharacterCoordinator _btlCharaCdr         = null;
        private BattleTimeScaleController _battleTimeScaleCtrl  = null;
        private PhaseHandlerBase _currentPhaseHdlr              = null;
        private PhaseHandlerBase[] _phaseHdlrs                  = new PhaseHandlerBase[((int)TurnType.NUM)];
        
        private bool _transitNextPhase = false;
        private int _phaseHandlerIndex = 0;
        private int _currentStageIndex = 0;
        // 現在選択中のキャラクターインデックス
        public CharacterKey SelectCharacterInfo { get; private set; } = new CharacterKey(CHARACTER_TAG.NONE, -1);
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

            Func<PhaseHandlerBase>[] phaseHdlrFactorys = new Func<PhaseHandlerBase>[]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<PlayerPhaseHandler>(false),
                () => _hierarchyBld.InstantiateWithDiContainer<EnemyPhaseHandler>(false),
            };
            for( int i = 0; i < _phaseHdlrs.Length; ++i )
            {
                if( _phaseHdlrs[i] == null ) _phaseHdlrs[i] = phaseHdlrFactorys[i]();
            }
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

            _stgCtrl.Init(this);
            _btlCharaCdr.Init();

            // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する ※デバッグシーンは除外
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                _btlFileLoader.CharacterLoad(_currentStageIndex);
            }

            _btlCharaCdr.PlaceAllCharactersAtStartPosition();           // 全キャラクターのステージ初期座標の設定
            _stgCtrl.TileInfoDataHdlr().UpdateTileInfo();               // タイル情報を更新
            _phase = BattlePhase.BATTLE_START;                          // 初期フェイズを設定
            _currentPhaseHdlr = _phaseHdlrs[(int)TurnType.PLAYER_TURN]; // PLAYERターンから開始(MEMO : ステージによって変更する場合はステージ読込処理から変更出来るように修正)
            _btlFileLoader.LoadCameraParams(_battleCameraCtrl);          // ファイル読込マネージャにカメラパラメータをロードさせる
            _btlFileLoader.LoadSkillsData();                             // スキルデータの読込
        }

        override public void UpdateRoutine()
        {
            base.UpdateRoutine();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                if (GameMain.instance.IsInvoking())
                {
                    return;
                }
            }

            // 現在のグリッド上に存在するキャラクター情報を更新
            TileInformation info;
            _stgCtrl.TileInfoDataHdlr().FetchCurrentTileInfo(out info);
            _battleCameraCtrl.SetLookAtBasedOnSelectCursor(info.charaStandPos);

            SelectCharacterInfo = info.CharaKey;

            // ステージクリア時、ゲーム―オーバー時のUIアニメーションが再生されている場合は終了
            if (_btlUi.StageClear.isActiveAndEnabled || _btlUi.GameOver.isActiveAndEnabled) return;

            _transitNextPhase = _currentPhaseHdlr.Update(); // フェーズマネージャを更新
        }

        override public void LateUpdateRoutine()
        {
            base.LateUpdateRoutine();

            if( _btlUi.StageClear.isActiveAndEnabled ) { return; }

            if( _btlUi.GameOver.isActiveAndEnabled ) { return; }

            // 勝利、全滅チェックを行う
            if (_btlCharaCdr.CheckVictoryOrDefeat(StartStageClearAnim, StartGameOverAnim)) { return; }

            // フェーズ移動の正否
            if (!_transitNextPhase)
            {
                _currentPhaseHdlr.LateUpdate();
            }
            else
            {
                // 一時パラメータをリセット
                _btlCharaCdr.ResetTmpParamAllCharacter();

                // 次のハンドラーに切り替える
                _phaseHandlerIndex  = (_phaseHandlerIndex + 1) % (int)TurnType.NUM;
                _currentPhaseHdlr   = _phaseHdlrs[_phaseHandlerIndex];
                _currentPhaseHdlr.Run();
            }
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// クラス内の処理を駆動します
        /// </summary>
        override public void Run()
        {
            base.Run();

            _currentPhaseHdlr.Run();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 中断させていた処理を再始動します
        /// </summary>
        override public void Restart()
        {
            base.Restart();

            _currentPhaseHdlr.Restart();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 処理を中断します
        /// </summary>
        override public void Pause()
        {
            base.Pause();

            _currentPhaseHdlr.Pause();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 処理を停止します
        /// </summary>
        override public void Exit()
        {
            base.Exit();

            _currentPhaseHdlr.Exit();
        }

        override public int GetPriority() { return (int)FocusRoutinePriority.BATTLE; }

        #endregion // IFocusRoutine Implementation
    }
}