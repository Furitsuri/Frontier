using Frontier.Combat;
using Frontier.Entities;
using Frontier.Registries;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Frontier.Loaders;

namespace Frontier.Battle
{
    public class BattleRoutineController : SubRoutineController
    {
        /*
        [Header( "スキルコントローラオブジェクト" )]
        [SerializeField] private GameObject _skillCtrlObject;
        */

        [Inject] private HierarchyBuilderBase _hierarchyBld         = null;
        [Inject] private PrefabRegistry _prefabReg                  = null;

        private int _currentStageIndex                  = 0;

        public void SetStageIndex( int stageIndex ) { _currentStageIndex = stageIndex; }
        private BattlePhaseType _currentPhase           = BattlePhaseType.Deployment;
        // 戦闘開始後に敵を倒して得たアニマの累計値。戦闘開始前から所持していたUserDomain.Animaとは別に、
        // 戦闘中のみ0からカウントする(戦闘開始時にInit()でリセットされる)。
        private int _battleAnima                        = 0;
        // クリアまでにかかったターン数。プレイヤーフェーズが1巡するごとに加算する
        // (Deployment→Playerへの最初の移行時点で1になる)。
        private int _turnCount                          = 0;
        private BattleFileLoader _btlFileLoader         = null;
        private BattleCameraController _btlCameraCtrl   = null;
        private BattleCharacterCoordinator _btlCharaCdr = null;
        private BattleRoutinePresenter _presenter       = null;
        private StageController _stgCtrl                = null;
        private Dictionary<BattlePhaseType, PhaseHandlerBase> _phaseHandlers;
        // ステージクリア判定後、「STAGE CLEAR」演出の開始からリザルト画面表示までを一貫して担う専用ステート。
        // 通常のフェーズ循環(_phaseHandlers)には含めず、ここから直接駆動する。
        private StageClearState _stageClearState        = null;

        public BattleCharacterCoordinator BtlCharaCdr => _btlCharaCdr;
        public BattleCameraController GetBtlCameraCtrl => _btlCameraCtrl;
        public BattleFileLoader GetBtlFileLoader => _btlFileLoader;
        public BattleRoutinePresenter BtlPresenter => _presenter;
        public int BattleAnima => _battleAnima;
        public int TurnCount => _turnCount;

        /// <summary>
        /// 戦闘中に獲得したアニマを加算し、表示を更新します
        /// </summary>
        public void AddBattleAnima( int amount )
        {
            _battleAnima += amount;
            _presenter.SetBattleAnimaDisplay( _battleAnima );
        }

        public IEnumerator Battle()
        {
            yield return null;
        }

        // =========================================================
        // SubRoutineControllerの実装
        // =========================================================
        #region SubRoutineController Implementation

        public override void Setup()
        {
            LazyInject.GetOrCreate( ref _stgCtrl,       () => _hierarchyBld.InstantiateWithDiContainer<StageController>( true ) );
            LazyInject.GetOrCreate( ref _presenter,     () => _hierarchyBld.InstantiateWithDiContainer<BattleRoutinePresenter>( true ) );
            LazyInject.GetOrCreate( ref _btlFileLoader, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleFileLoader>( _prefabReg.BattleFileLoaderPrefab, true, false, typeof( BattleFileLoader ).Name ) );
            LazyInject.GetOrCreate( ref _btlCameraCtrl, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleCameraController>( _prefabReg.BattleCameraPrefab, true, true, typeof( BattleCameraController ).Name ) );
            LazyInject.GetOrCreate( ref _btlCharaCdr,   () => _hierarchyBld.InstantiateWithDiContainer<BattleCharacterCoordinator>( false ) );
            LazyInject.GetOrCreate( ref _stageClearState, () => _hierarchyBld.InstantiateWithDiContainer<StageClearState>( false ) );

            if( SkillsData.ReactiveSkillNotifierFactory == null )
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

            _stgCtrl.Setup();
            _presenter.Setup();
            _btlCameraCtrl.Setup( true );
        }

        /// <summary>
        /// 各種パラメータを初期化させます
        /// </summary>
        public override void Init()
        {
            _presenter.Init();
            _btlCameraCtrl.Init();
            _btlCharaCdr.Init();
            _stgCtrl.Init( _btlCameraCtrl );

            _battleAnima = 0;                           // 戦闘開始時に戦闘中アニマをリセット
            _presenter.SetBattleAnimaDisplay( _battleAnima );
            _turnCount = 0;                             // 戦闘開始時にターン数をリセット

            // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する ※デバッグシーンは除外
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if( !Methods.IsDebugScene() )
#endif
            {
                _btlFileLoader.CharacterLoad( _currentStageIndex );
            }

            _stgCtrl.TileDataHdlr().UpdateTileDynamicDatas();           // タイル情報を更新
            _currentPhase = BattlePhaseType.Deployment;                 // 初期フェイズを設定(配置フェーズ)
            _presenter.SetActiveBattleUI( false );                      // 配置フェーズ移行前に戦闘用UIの表示をOFF
            _btlFileLoader.LoadCameraParams( _btlCameraCtrl );          // ファイル読込マネージャにカメラパラメータをロードさせる
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

            // ステージクリア時、ゲーム―オーバー時のUIアニメーションが再生されている場合は終了
            if( _stageClearState.IsActive || _presenter.IsActiveGameOverAnimation() ) { return; }

            _phaseHandlers[_currentPhase].Update();

            _stgCtrl.TileDataHdlr().UpdateTileDynamicDatas();   // タイル情報を更新
        }

        public override bool LateUpdate()
        {
            // 死亡していたキャラクターの実体(GameObject)を破棄する。
            // 攻撃・スキルシーケンス実行中はSequenceHandlerにフォーカスが移りGameRoutineController自体が
            // Pauseされるため、このLateUpdate()はシーケンスが完全に終了した後でなければ呼ばれない。
            // そのため、ここに到達した時点でシーケンス側のキャラクター参照の使用は必ず完了しており安全に破棄できる。
            _btlCharaCdr.DisposePendingDeadCharacters();

            // 保留していた撃破報酬(アニマ)を、演出付きで放出する。上記と同じ理由でシーケンス完全終了後にのみ
            // 呼ばれるため、演出開始タイミングが攻撃・スキルシーケンスの最中と重なることはない。
            // 実際のアニマ加算は演出がUIへ到達した時点(コールバック)で行われる。
            _btlCharaCdr.DispensePendingAnimaRewards( ( position, amount ) =>
            {
                _presenter.PlayAnimaRewardEffect( position, () => AddBattleAnima( amount ) );
            } );

            // ステージクリア判定後、「STAGE CLEAR」演出の開始・終了・CONFIRM入力待ちを経てリザルト画面を
            // 表示するまでは、専用ステートの更新のみを行い、通常のフェーズ進行やシーン遷移(return true)は行わない。
            if( _stageClearState.IsActive )
            {
                _stageClearState.Update();
                return false;
            }

            if( _presenter.IsActiveGameOverAnimation() )    { return false; }  // ゲーム―オーバー時のUIアニメーションが再生中の場合は終了

            // 勝利、全滅チェックを行う
            if( _btlCharaCdr.CheckVictoryOrDefeat( () =>
                {
                    // 現在のフェーズが登録している入力コード(CONFIRM等)を解除してから
                    // StageClearStateの入力コードを登録しないと、アイコンの重複登録エラーが発生する
                    _phaseHandlers[_currentPhase].Pause();

                    _stageClearState.Begin( () => _battleAnima, _turnCount );
                },
                _presenter.StartGameOverAnim ) ) { return true; }

            var handler = _phaseHandlers[_currentPhase];
            if( handler.LateUpdate() )
            {
                // フェーズ終了時の全てのキャラクターへの処理を実行
                _btlCharaCdr.AdjustAllCharactersEndOfPhase();

                // 次のハンドラーに切り替える
                handler.Exit();
                _currentPhase = GetNextPhase( _currentPhase );
                if( _currentPhase == BattlePhaseType.Player ) { _turnCount++; }  // プレイヤーフェーズが1巡するごとにターン数を加算
                _phaseHandlers[_currentPhase].Enter();
            }

            return false;
        }

        public override void FixedUpdate() { }

        /// <summary>
        /// SubRoutineControllerの実装です
        /// クラス内の処理を駆動します
        /// </summary>
        public override void Run()
        {
            Init();

            _phaseHandlers[_currentPhase].Enter();
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
            _presenter.Exit();
        }

        #endregion // SubRoutineController Implementation

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
    }
}