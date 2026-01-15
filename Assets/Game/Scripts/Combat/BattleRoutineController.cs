using Frontier.Registries;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class BattleRoutineController : SubRoutineController
    {
        /*
        [Header( "スキルコントローラオブジェクト" )]
        [SerializeField] private GameObject _skillCtrlObject;
        */

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private PrefabRegistry _prefabReg          = null;

        private int _currentStageIndex = 0;
        private BattlePhaseType _currentPhase;
        private BattleFileLoader _btlFileLoader = null;
        private BattleCameraController _battleCameraCtrl = null;
        private BattleCharacterCoordinator _btlCharaCdr = null;
        private BattleTimeScaleController _battleTimeScaleCtrl = null;
        private BattleRoutinePresenter _presenter = null;
        private StageController _stgCtrl = null;
        private Dictionary<BattlePhaseType, PhaseHandlerBase> _phaseHandlers;

        public BattleTimeScaleController TimeScaleCtrl => _battleTimeScaleCtrl;
        public BattleCharacterCoordinator BtlCharaCdr => _btlCharaCdr;

        public IEnumerator Battle()
        {
            yield return null;
        }

        /// <summary>
        /// 戦闘カメラコントローラを取得します
        /// </summary>
        /// <returns>戦闘カメラコントローラ</returns>
        public BattleCameraController GetCameraController()
        {
            return _battleCameraCtrl;
        }

        private void Setup()
        {
            var btlCameraObj = GameObject.FindWithTag( "MainCamera" );
            LazyInject.GetOrCreate( ref _stgCtrl, () => _hierarchyBld.InstantiateWithDiContainer<StageController>( true ) );
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<BattleRoutinePresenter>( true ) );
            LazyInject.GetOrCreate( ref _battleCameraCtrl, () => btlCameraObj.GetComponent<BattleCameraController>() );
            LazyInject.GetOrCreate( ref _btlFileLoader, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleFileLoader>( _prefabReg.BattleFileLoaderPrefab, true, false, typeof( BattleFileLoader ).Name ) );
            LazyInject.GetOrCreate( ref _btlCharaCdr, () => _hierarchyBld.InstantiateWithDiContainer<BattleCharacterCoordinator>( false ) );
            LazyInject.GetOrCreate( ref _battleTimeScaleCtrl, () => _hierarchyBld.InstantiateWithDiContainer<BattleTimeScaleController>( false ) );

            if( SkillsData.skillNotifierFactory == null )
            {
                SkillsData.BuildSkillNotifierFactory( _hierarchyBld );
            }

            _phaseHandlers = new Dictionary<BattlePhaseType, PhaseHandlerBase>
            {
                { BattlePhaseType.Deployment,   _hierarchyBld.InstantiateWithDiContainer<DeploymentPhaseHandler>(false) },
                { BattlePhaseType.Player,       _hierarchyBld.InstantiateWithDiContainer<PlayerPhaseHandler>(false) },
                { BattlePhaseType.Enemy,        _hierarchyBld.InstantiateWithDiContainer<EnemyPhaseHandler>(false) },
                { BattlePhaseType.Other,        _hierarchyBld.InstantiateWithDiContainer<OtherPhaseHandler>(false) }
            };
        }

        /// <summary>
        /// 次のフェーズへの移行先を取得します
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private BattlePhaseType GetNextPhase( BattlePhaseType current )
        {
            if( current == BattlePhaseType.Deployment )
            {
                _presenter.SetActiveBattleUI( true );                       // 戦闘用UIの表示をON
                _stgCtrl.TileDataHdlr().ClearUndeployableColorOfTiles();    // 配置不可タイルの色をクリア

                return BattlePhaseType.Player;          // 配置が終わったら通常ループに移行
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
        // SubRoutineControllerの実装
        // =========================================================
        #region SubRoutineController Implementation

        /// <summary>
        /// 各種パラメータを初期化させます
        /// </summary>
        public override void Init()
        {
            Setup();

            _stgCtrl.Init();
            _btlCharaCdr.Init();

            // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する ※デバッグシーンは除外
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if( !Methods.IsDebugScene() )
#endif
            {
                _btlFileLoader.CharacterLoad( _currentStageIndex );
            }

            _btlCharaCdr.PlaceAllCharactersAtStartPosition();           // 全キャラクターのステージ初期座標の設定
            _stgCtrl.TileDataHdlr().UpdateTileDynamicDatas();           // タイル情報を更新
            _currentPhase = BattlePhaseType.Deployment;                 // 初期フェイズを設定(配置フェーズ)
            _presenter.SetActiveBattleUI( false );                      // 配置フェーズ移行前に戦闘用UIの表示をOFF
            _btlFileLoader.LoadCameraParams( _battleCameraCtrl );       // ファイル読込マネージャにカメラパラメータをロードさせる
            _btlFileLoader.LoadSkillsData();                            // スキルデータの読込
        }

        public override void Update()
        {
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

            // ステージクリア時、ゲーム―オーバー時のUIアニメーションが再生されている場合は終了
            if( _presenter.IsActiveStageClearAnimation() || _presenter.IsActiveGameOverAnimation() ) { return; }

            _phaseHandlers[_currentPhase].Update();

            _stgCtrl.TileDataHdlr().UpdateTileDynamicDatas();   // タイル情報を更新
        }

        public override void LateUpdate()
        {
            if( _presenter.IsActiveStageClearAnimation() )  { return; }  // ステージクリア時のUIアニメーションが再生されている場合は終了
            if( _presenter.IsActiveGameOverAnimation() )    { return; }  // ゲーム―オーバー時のUIアニメーションが再生されている場合は終了

            // 勝利、全滅チェックを行う
            if( _btlCharaCdr.CheckVictoryOrDefeat( _presenter.StartStageClearAnim, _presenter.StartGameOverAnim ) ) { return; }

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

        public override void FixedUpdate() { }

        /// <summary>
        /// SubRoutineControllerの実装です
        /// クラス内の処理を駆動します
        /// </summary>
        public override void Run()
        {
            Init();

            _phaseHandlers[_currentPhase].Run();
        }

        /// <summary>
        /// SubRoutineControllerの実装です
        /// 中断させていた処理を再始動します
        /// </summary>
        public override void Restart()
        {
            _phaseHandlers[_currentPhase].Restart();
        }

        /// <summary>
        /// SubRoutineControllerの実装です
        /// 処理を中断します
        /// </summary>
        public override void Pause()
        {
            _phaseHandlers[_currentPhase].Pause();
        }

        /// <summary>
        /// SubRoutineControllerの実装です
        /// 処理を停止します
        /// </summary>
        public override void Exit()
        {
            _phaseHandlers[_currentPhase].Exit();
        }

        #endregion // SubRoutineController Implementation
    }
}