using Frontier.Stage;
using Frontier.Entities;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class BattleRoutineController : MonoBehaviour, IBattleHandler
    {
        /// <summary>
        /// バトル状態の遷移
        /// </summary>
        enum BattlePhase
        {
            BATTLE_START = 0,
            BATTLE_PLAYER_COMMAND,
            BATTLE_RESULT,
            BATTLE_END,
        }

        /// <summary>
        /// 戦闘におけるターンの種類
        /// </summary>
        public enum TurnType
        {
            PLAYER_TURN = 0,
            ENEMY_TURN,

            NUM
        }

        [Header("スキルコントローラオブジェクト")]
        [SerializeField]
        private GameObject _skillCtrlObject;

        [Header("戦闘ファイル読込オブジェクト")]
        [SerializeField]
        private GameObject _btlFileLoadObject;

        private DiInstaller _installer          = null;
        private HierarchyBuilder _hierarchyBld  = null;
        private InputFacade _inputFcd           = null;
        private StageController _stgCtrl        = null;
        private UISystem _uiSystem              = null;

        private BattlePhase _phase;
        private BattleFileLoader _btlFileLoader             = null;
        private BattleCameraController _battleCameraCtrl    = null;
        private BattleUISystem _battleUi                    = null;
        private SkillController _skillCtrl                  = null;
        private PhaseManagerBase _currentPhaseManager       = null;
        private BattleCharacterCoordinator _btlCharaCdr     = null;
        private BattleTimeScaleController _battleTimeScaleCtrl = new();
        private PhaseManagerBase[] _phaseManagers = new PhaseManagerBase[((int)TurnType.NUM)];
        
        private bool _transitNextPhase = false;
        private int _phaseManagerIndex = 0;
        private int _currentStageIndex = 0;
        // 現在選択中のキャラクターインデックス
        public CharacterHashtable.Key SelectCharacterInfo { get; private set; } = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
        public BattleTimeScaleController TimeScaleCtrl => _battleTimeScaleCtrl;
        public SkillController SkillCtrl => _skillCtrl;
        public BattleCharacterCoordinator BtlCharaCdr => _btlCharaCdr;

        /// <summary>
        /// Diコンテナから引数を注入します
        /// </summary>
        /// <param name="container">DIコンテナ</param>
        /// <param name="installer">DIインストーラ</param>
        /// <param name="hierarchyBld">オブジェクト・コンポーネント作成</param>
        /// <param name="uiSystem">UIシステム</param>
        [Inject]
        void Construct(DiInstaller installer, HierarchyBuilder hierarchyBld, InputFacade inputFcd, StageController stgCtrl, UISystem uiSystem)
        {
            _installer      = installer;
            _hierarchyBld   = hierarchyBld;
            _inputFcd       = inputFcd;
            _stgCtrl        = stgCtrl;
            _uiSystem       = uiSystem;
        }

        void Awake()
        {
            var btlCameraObj = GameObject.FindWithTag("MainCamera");
            if ( btlCameraObj != null ) 
            {
                _battleCameraCtrl = btlCameraObj.GetComponent<BattleCameraController>();
            }
        }

        void Start()
        {
            Debug.Assert(_uiSystem != null, "UISystemのインスタンスが生成されていません。Injectの設定を確認してください。");

            if (_skillCtrl == null)
            {
                _skillCtrl = _hierarchyBld.CreateComponentAndOrganize<SkillController>(_skillCtrlObject, true);
            }

            if( _btlFileLoader == null )
            {
                _btlFileLoader = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleFileLoader>(_btlFileLoadObject, true,  false);
            }

            if (_btlCharaCdr == null)
            {
                _btlCharaCdr = _hierarchyBld.InstantiateWithDiContainer<BattleCharacterCoordinator>();
            }

            _battleUi = _uiSystem.BattleUI;
            _installer.InstallBindings<BattleUISystem>( _battleUi );

            Init();

            _btlCharaCdr.Init();

            // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する ※デバッグシーンは除外
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                _btlFileLoader.CharacterLoad(_currentStageIndex);
            }

            _phaseManagers[(int)TurnType.PLAYER_TURN]   = _hierarchyBld.InstantiateWithDiContainer<PlayerPhaseManager>();
            _phaseManagers[(int)TurnType.ENEMY_TURN]    = _hierarchyBld.InstantiateWithDiContainer<EnemyPhaseManager>();
            _currentPhaseManager = _phaseManagers[(int)TurnType.PLAYER_TURN];
            _currentPhaseManager.Init();

            _btlCharaCdr.PlaceAllCharactersAtStartPosition();

            // グリッド情報を更新
            _stgCtrl.UpdateGridInfo();
        }

        void Update()
        {
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
            Stage.GridInfo info;
            _stgCtrl.FetchCurrentGridInfo(out info);
            _battleCameraCtrl.SetLookAtBasedOnSelectCursor(info.charaStandPos);

            SelectCharacterInfo = new CharacterHashtable.Key(info.characterTag, info.charaIndex);

            if (_battleUi.StageClear.isActiveAndEnabled) return;

            if (_battleUi.GameOver.isActiveAndEnabled) return;

            // フェーズマネージャを更新
            _transitNextPhase = _currentPhaseManager.Update();
        }

        void LateUpdate()
        {
            if (_battleUi.StageClear.isActiveAndEnabled) return;

            if (_battleUi.GameOver.isActiveAndEnabled) return;

            // 勝利、全滅チェックを行う
            if ( _btlCharaCdr.CheckVictoryOrDefeat(StartStageClearAnim, StartGameOverAnim) ) { return; }

            // フェーズ移動の正否
            if (!_transitNextPhase)
            {
                _currentPhaseManager.LateUpdate();
            }
            else
            {
                // 一時パラメータをリセット
                _btlCharaCdr.ResetTmpParamAllCharacter();

                // 次のマネージャに切り替える
                _phaseManagerIndex = (_phaseManagerIndex + 1) % (int)TurnType.NUM;
                _currentPhaseManager = _phaseManagers[_phaseManagerIndex];
                _currentPhaseManager.Init();
            }
        }

        /// <summary>
        /// 各種パラメータを初期化させます
        /// </summary>
        public void Init()
        {
            _stgCtrl.Init(this);
            _skillCtrl.Init(this);

            // 初期フェイズを設定
            _phase = BattlePhase.BATTLE_START;
            // ファイル読込マネージャにカメラパラメータをロードさせる
            _btlFileLoader.CameraParamLord(_battleCameraCtrl);
            // スキルデータの読込
            _btlFileLoader.SkillDataLord();
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
        public bool isEnd()
        {
            return _phase == BattlePhase.BATTLE_END;
        }

        /// <summary>
        /// ステージクリア時のUIとアニメーションを表示します
        /// </summary>
        public void StartStageClearAnim()
        {
            _battleUi.ToggleStageClearUI(true);
            _battleUi.StartStageClearAnim();
        }

        /// <summary>
        /// ゲームオーバー時のUIとアニメーションを表示します
        /// </summary>
        public void StartGameOverAnim()
        {
            _battleUi.ToggleGameOverUI(true);
            _battleUi.StartGameOverAnim();
        }
    }
}