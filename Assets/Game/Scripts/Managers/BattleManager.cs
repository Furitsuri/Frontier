using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class BattleManager : Manager
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

        [Header("ステージコントローラオブジェクト")]
        [SerializeField]
        private GameObject _stageControllerObject;

        [Header("スキルコントローラオブジェクト")]
        [SerializeField]
        private GameObject _skillCtrlObject;

        [Header("ファイル読込マネージャ")]
        [SerializeField]
        private FileReadManager _fileReadMgr;

        private DiContainer _container = null;
        private DiInstaller _installer = null;
        private HierarchyBuilder _hierarchyBld = null;
        private UISystem _uiSystem = null;

        private BattlePhase _phase;
        private BattleCameraController _battleCameraCtrl;
        private BattleTimeScaleController _battleTimeScaleCtrl = new();
        private BattleUISystem _battleUi = null;
        private InputFacade _inputFacade;
        private StageController _stageCtrl = null;
        private SkillController _skillCtrl = null;
        private PhaseManagerBase _currentPhaseManager;
        private PhaseManagerBase[] _phaseManagers = new PhaseManagerBase[((int)TurnType.NUM)];
        private List<Player> _players = new List<Player>(Constants.CHARACTER_MAX_NUM);
        private List<Enemy> _enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
        private List<Other> _others = new List<Other>(Constants.CHARACTER_MAX_NUM);
        private CharacterHashtable _characterHash = new CharacterHashtable();
        private CharacterHashtable.Key _diedCharacterKey;
        private CharacterHashtable.Key _battleBossCharacterKey;
        private CharacterHashtable.Key _escortTargetCharacterKey;
        private bool _transitNextPhase = false;
        private int _phaseManagerIndex = 0;
        private int _currentStageIndex = 0;
        // 現在選択中のキャラクターインデックス
        public CharacterHashtable.Key SelectCharacterInfo { get; private set; } = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
        public BattleTimeScaleController TimeScaleCtrl => _battleTimeScaleCtrl;
        public SkillController SkillCtrl => _skillCtrl;

        /// <summary>
        /// Diコンテナから引数を注入します
        /// </summary>
        /// <param name="container">DIコンテナ</param>
        /// <param name="installer">DIインストーラ</param>
        /// <param name="hierarchyBld">オブジェクト・コンポーネント作成</param>
        /// <param name="uiSystem">UIシステム</param>
        [Inject]
        void Construct(DiContainer container, DiInstaller installer, HierarchyBuilder hierarchyBld, UISystem uiSystem)
        {
            _container      = container;
            _installer      = installer;
            _hierarchyBld   = hierarchyBld;
            _uiSystem       = uiSystem;
        }

        void Awake()
        {
            var btlCameraObj = GameObject.FindWithTag("MainCamera");
            if ( btlCameraObj != null ) 
            {
                _battleCameraCtrl = btlCameraObj.GetComponent<BattleCameraController>();
            }

            _phaseManagers[(int)TurnType.PLAYER_TURN]   = new PlayerPhaseManager();
            _phaseManagers[(int)TurnType.ENEMY_TURN]    = new EnemyPhaseManager();
            _currentPhaseManager = _phaseManagers[(int)TurnType.PLAYER_TURN];

            // TODO : ステージのファイルから読み込んで設定するように
            _diedCharacterKey           = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
            _battleBossCharacterKey     = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
            _escortTargetCharacterKey   = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
        }

        void Start()
        {
            Debug.Assert(_uiSystem != null, "UISystemのインスタンスが生成されていません。Injectの設定を確認してください。");

            if (_stageCtrl == null)
            {
                _stageCtrl = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageController>(_stageControllerObject, true, true);
            }

            if (_skillCtrl == null)
            {
                _skillCtrl = _hierarchyBld.CreateComponentAndOrganize<SkillController>(_skillCtrlObject, true);
            }

            _container.InjectGameObject(_fileReadMgr.gameObject);

            _battleUi = _uiSystem.BattleUI;
            _battleUi.gameObject.SetActive(true);
            _container.InjectGameObject(_battleUi.gameObject);

            Init();

            // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する ※デバッグシーンは除外
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                _fileReadMgr.PlayerLoad(_currentStageIndex, _stageCtrl.GetGridSize());
                _fileReadMgr.EnemyLord(_currentStageIndex, _stageCtrl.GetGridSize());
            }

            for (int i = 0; i < _phaseManagers.Length; ++i) _phaseManagers[i].Regist(this, _stageCtrl);
            _currentPhaseManager.Init();

            // 向きの値を設定
            Quaternion[] rot = new Quaternion[(int)Constants.Direction.NUM_MAX];
            for (int i = 0; i < (int)Constants.Direction.NUM_MAX; ++i)
            {
                rot[i] = Quaternion.AngleAxis(90 * i, Vector3.up);
            }

            // 各プレイヤーキャラクターの位置を設定
            for (int i = 0; i < _players.Count; ++i)
            {
                Player player = _players[i];
                // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
                int gridIndex = player.param.initGridIndex;
                // プレイヤーの画面上の位置を設定
                player.transform.position = _stageCtrl.GetGridCharaStandPos(gridIndex);
                // 向きを設定
                player.transform.rotation = rot[(int)player.param.initDir];
                // 対応するグリッドに立っているプレイヤーのインデックスを設定
                _stageCtrl.GetGridInfo(gridIndex).charaIndex = player.param.characterIndex;
            }

            // 各エネミーキャラクターの位置を設定
            for (int i = 0; i < _enemies.Count; ++i)
            {
                Enemy enemy = _enemies[i];
                // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
                int gridIndex = enemy.param.initGridIndex;
                // エネミーの画面上の位置を設定
                enemy.transform.position = _stageCtrl.GetGridCharaStandPos(gridIndex);
                // 向きを設定
                enemy.transform.rotation = rot[(int)enemy.param.initDir];
                // 対応するグリッドに立っているプレイヤーのインデックスを設定
                _stageCtrl.GetGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
            }

            // グリッド情報を更新
            _stageCtrl.UpdateGridInfo();
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
            _stageCtrl.FetchCurrentGridInfo(out info);
            _battleCameraCtrl.SetLookAtBasedOnSelectCursor(info.charaStandPos);

            SelectCharacterInfo = new CharacterHashtable.Key(info.characterTag, info.charaIndex);

            if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

            if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;

            // フェーズマネージャを更新
            _transitNextPhase = _currentPhaseManager.Update();
        }

        void LateUpdate()
        {
            if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

            if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;

            // 勝利、全滅チェックを行う
            if (CheckVictoryOrDefeat(_diedCharacterKey)) { return; }

            // フェーズ移動の正否
            if (!_transitNextPhase)
            {
                _currentPhaseManager.LateUpdate();
            }
            else
            {
                // 一時パラメータをリセット
                ResetTmpParamAllCharacter();

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
            _stageCtrl.Init(this);
            _skillCtrl.Init(this);

            // 初期フェイズを設定
            _phase = BattlePhase.BATTLE_START;
            // ファイル読込マネージャにカメラパラメータをロードさせる
            _fileReadMgr.CameraParamLord(_battleCameraCtrl);
            // スキルデータの読込
            _fileReadMgr.SkillDataLord();
        }

        /// <summary>
        /// DI用の関数です
        /// オブジェクトなどを注入します
        /// </summary>
        /// <param name="generator">Unityのインスタンス生成クラス</param>
        /// <param name="inputFacade">入力窓口</param>
        public void Inject(HierarchyBuilder hierarchyBld, InputFacade inputFacade)
        {
            _inputFacade = inputFacade;
        }

        /// <summary>
        /// 対象のキャラクター群が全滅しているかを確認します
        /// </summary>
        bool CheckCharacterAnnihilated(Character.CHARACTER_TAG characterTag)
        {
            bool isAnnihilated = true;

            switch (characterTag)
            {
                case Character.CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        if (!player.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
                case Character.CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        if (!enemy.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
                case Character.CHARACTER_TAG.OTHER:
                    // TODO : 必要になれば実装
                    isAnnihilated = false;
                    break;
            }

            return isAnnihilated;
        }

        /// <summary>
        /// 勝利、敗戦判定を行います
        /// </summary>
        /// <param name="diedCharacterKey">死亡したキャラクターのハッシュキー</param>
        /// <returns>勝利、敗戦処理に遷移するか否か</returns>
        bool CheckVictoryOrDefeat(CharacterHashtable.Key diedCharacterKey)
        {
            if (diedCharacterKey.characterTag != Character.CHARACTER_TAG.NONE)
            {
                // ステージにボスが設定されているかのチェック
                if (_battleBossCharacterKey.characterTag != Character.CHARACTER_TAG.NONE)
                {
                    if (diedCharacterKey == _battleBossCharacterKey)
                    {
                        // ステージクリアに遷移
                        StartStageClearAnim();

                        return true;
                    }
                }

                if (_escortTargetCharacterKey.characterTag != Character.CHARACTER_TAG.NONE)
                {
                    if (diedCharacterKey == _escortTargetCharacterKey)
                    {
                        // ゲームオーバーに遷移
                        StartGameOverAnim();

                        return true;
                    }
                }

                if (CheckCharacterAnnihilated(diedCharacterKey.characterTag))
                {
                    if (diedCharacterKey.characterTag == Character.CHARACTER_TAG.ENEMY)
                    {
                        // ステージクリアに遷移
                        StartStageClearAnim();
                    }
                    else if (diedCharacterKey.characterTag == Character.CHARACTER_TAG.PLAYER)
                    {
                        // ゲームオーバーに遷移
                        StartGameOverAnim();
                    }

                    return true;
                }
                else
                {
                    ResetDiedCharacter();
                }
            }

            return false;
        }

        public IEnumerator Battle()
        {
            yield return null;
        }

        /// <summary>
        /// プレイヤーをリストとハッシュに登録します
        /// </summary>
        /// <param name="player">登録するプレイヤー</param>
        public void AddPlayerToList(Player player)
        {
            var param = player.param;
            CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

            _players.Add(player);
            _characterHash.Add(key, player);
        }

        /// <summary>
        /// 敵をリストとハッシュに登録します
        /// </summary>
        /// <param name="enemy">登録する敵</param>
        public void AddEnemyToList(Enemy enemy)
        {
            var param = enemy.param;
            CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

            _enemies.Add(enemy);
            _characterHash.Add(key, enemy);
        }

        /// <summary>
        /// 該当キャラクターが死亡した際などにリストから対象を削除します
        /// </summary>
        /// <param name="player">削除対象のプレイヤー</param>
        public void RemovePlayerFromList(Player player)
        {
            _players.Remove(player);
            _characterHash.Remove(player);
        }

        /// <summary>
        /// 該当キャラクターが死亡した際などにリストから対象を削除します
        /// </summary>
        /// <param name="enemy">削除対象の敵</param>
        public void RemoveEnemyFromList(Enemy enemy)
        {
            _enemies.Remove(enemy);
            _characterHash.Remove(enemy);
        }

        /// <summary>
        /// ステージグリッドスクリプトを登録します
        /// </summary>
        /// <param name="script">登録するスクリプト</param>
        public void registStageController(Stage.StageController script)
        {
            _stageCtrl = script;
        }

        /// <summary>
        /// 直近の戦闘で死亡したキャラクターのキャラクタータグを設定します
        /// </summary>
        /// <param name="tag">死亡したキャラクターのキャラクタータグ</param>
        public void SetDiedCharacterKey(CharacterHashtable.Key key) { _diedCharacterKey = key; }

        /// <summary>
        /// 死亡キャラクターのキャラクタータグをリセットします
        /// </summary>
        public void ResetDiedCharacter()
        {
            _diedCharacterKey.characterTag = Character.CHARACTER_TAG.NONE;
            _diedCharacterKey.characterIndex = -1;
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
        /// ハッシュテーブルから指定のタグとインデックスをキーとするキャラクターを取得します
        /// </summary>
        /// <param name="tag">キャラクタータグ</param>
        /// <param name="index">キャラクターインデックス</param>
        /// <returns>指定のキーに対応するキャラクター</returns>
        public Character GetCharacterFromHashtable(Character.CHARACTER_TAG tag, int index)
        {
            if (tag == Character.CHARACTER_TAG.NONE || index < 0) return null;
            CharacterHashtable.Key hashKey = new CharacterHashtable.Key(tag, index);

            return _characterHash.Get(hashKey) as Character;
        }

        /// <summary>
        /// ハッシュテーブルから指定のタグとインデックスをキーとするキャラクターを取得します
        /// </summary>
        /// <param name="key">ハッシュキー</param>
        /// <returns>指定のキーに対応するキャラクター</returns>
        public Character GetCharacterFromHashtable(CharacterHashtable.Key key)
        {
            if (key.characterTag == Character.CHARACTER_TAG.NONE || key.characterIndex < 0) return null;

            return _characterHash.Get(key) as Character;
        }

        /// <summary>
        /// 全ての行動可能キャラクターの行動が終了したかを判定します
        /// </summary>
        /// <returns>全ての行動可能キャラクターの行動が終了したか</returns>
        public bool IsEndAllCharacterWaitCommand()
        {
            foreach (Player player in _players)
            {
                if (!player.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 現在選択しているグリッド上のキャラクターを取得します
        /// </summary>
        /// <returns>選択しているグリッド上のキャラクター</returns>
        public Character GetSelectCharacter()
        {
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            return GetCharacterFromHashtable(info.characterTag, info.charaIndex);
        }

        /// <summary>
        /// プレイヤーをリストから順番に取得します
        /// </summary>
        /// <returns>プレイヤーキャラクター</returns>
        public IEnumerable<Player> GetPlayerEnumerable()
        {
            foreach (Player player in _players)
            {
                yield return player;
            }

            yield break;
        }

        /// <summary>
        /// 敵をリストから順番に取得します
        /// </summary>
        /// <returns>敵キャラクター</returns>
        public IEnumerable<Enemy> GetEnemyEnumerable()
        {
            foreach (Enemy enemy in _enemies)
            {
                yield return enemy;
            }

            yield break;
        }

        /// <summary>
        /// 指定されたキャラクタータグの総ユニット数を取得します
        /// </summary>
        /// <param name="tag">指定するキャラクターのタグ</param>
        /// <returns>指定タグの総ユニット数</returns>
        public int GetCharacterCount(Character.CHARACTER_TAG tag)
        {
            switch (tag)
            {
                case Character.CHARACTER_TAG.PLAYER: return _players.Count;
                case Character.CHARACTER_TAG.ENEMY: return _enemies.Count;
                default: return _others.Count;
            }
        }

        /// <summary>
        /// 全てのプレイヤーキャラクターを待機済みに変更します
        /// 主にターンを終了させる際に使用します
        /// </summary>
        public void ApplyAllPlayerWaitEnd()
        {
            foreach (Player player in _players)
            {
                player.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT] = true;
            }
        }

        /// <summary>
        /// ダメージ予測を適応します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">標的キャラクター</param>
        public void ApplyDamageExpect(Character attacker, Character target)
        {
            if (target == null)
            {
                return;
            }

            int targetDef = (int)Mathf.Floor((target.param.Def + target.modifiedParam.Def) * target.skillModifiedParam.DefMagnification);
            int attackerAtk = (int)Mathf.Floor((attacker.param.Atk + attacker.modifiedParam.Atk) * attacker.skillModifiedParam.AtkMagnification);
            int changeHP = (targetDef - attackerAtk);

            target.tmpParam.expectedChangeHP = Mathf.Min(changeHP, 0);
            target.tmpParam.totalExpectedChangeHP = Mathf.Min(changeHP * attacker.skillModifiedParam.AtkNum, 0);
        }

        /// <summary>
        /// 全てのキャラクターの一時パラメータをリセットします
        /// </summary>
        public void ResetTmpParamAllCharacter()
        {
            foreach (Player player in _players)
            {
                player.BePossibleAction();
            }
            foreach (Enemy enemy in _enemies)
            {
                enemy.BePossibleAction();
            }
        }

        /// <summary>
        /// ダメージ予測をリセットします
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">標的キャラクター</param>
        public void ResetDamageExpect(Character attacker, Character target)
        {
            if (target == null)
            {
                return;
            }

            target.tmpParam.expectedChangeHP = 0;
        }

        /// <summary>
        /// 指定のキャラクター群のアクションゲージを回復させます
        /// </summary>
        /// <param name="tag">キャラクター群のタグ</param>
        public void RecoveryActionGaugeForGroup(Character.CHARACTER_TAG tag)
        {
            switch (tag)
            {
                case Character.CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        player.RecoveryActionGauge();
                    }
                    break;
                case Character.CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        enemy.RecoveryActionGauge();
                    }
                    break;
                case Character.CHARACTER_TAG.OTHER:
                    // TODO : OTHERを作成次第追加
                    break;
            }
        }

        /// <summary>
        /// 終了常態かどうかを判定します
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
            BattleUISystem.Instance.ToggleStageClearUI(true);
            BattleUISystem.Instance.StartStageClearAnim();
        }

        /// <summary>
        /// ゲームオーバー時のUIとアニメーションを表示します
        /// </summary>
        public void StartGameOverAnim()
        {
            BattleUISystem.Instance.ToggleGameOverUI(true);
            BattleUISystem.Instance.StartGameOverAnim();
        }
    }
}