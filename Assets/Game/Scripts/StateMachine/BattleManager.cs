using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class BattleManager : MonoBehaviour
{
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
        BATTLE_PLAYER_EXECUTE,
        BATTLE_ENEMY_COMMAND,
        BATTLE_ENEMY_EXECUTE,
        BATTLE_RESULT,
        BATTLE_END,
    }

    public enum TurnType
    {
        PLAYER_TURN = 0,
        ENEMY_TURN,
    }

    public static BattleManager instance = null;
    public GameObject stageGridObject;
    private BattlePhase _phase;
    private PhaseManagerBase _phaseManager;
    private StageGrid _stageGrid;
    private List<Player> _players = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> _enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private Character _prevCharacter = null;
    // 現在選択中のキャラクターインデックス
    public int SelectCharacterIndex { get; private set; } = -1;
    // 攻撃フェーズ中において、攻撃を開始するキャラクター
    public Character AttackerCharacter { get; private set; } = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        if (StageGrid.instance == null)
        {
            Instantiate(stageGridObject);
        }

        _phaseManager = new PlayerPhaseManager();
    }

    void Start()
    {
        _phaseManager.Init();

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
            int gridIndex                               = player.param.initGridIndex;
            // プレイヤーの画面上の位置を設定
            player.transform.position                   = _stageGrid.getGridCharaStandPos(gridIndex);
            // 向きを設定
            player.transform.rotation = rot[(int)player.param.initDir];
            // 対応するグリッドに立っているプレイヤーのインデックスを設定
            _stageGrid.getGridInfo(gridIndex).charaIndex = player.param.characterIndex;
        }

        // 各エネミーキャラクターの位置を設定
        for (int i = 0; i < _enemies.Count; ++i)
        {
            Enemy enemy = _enemies[i];
            // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
            int gridIndex = enemy.param.initGridIndex;
            // エネミーの画面上の位置を設定
            enemy.transform.position = _stageGrid.getGridCharaStandPos(gridIndex);
            // 向きを設定
            enemy.transform.rotation = rot[(int)enemy.param.initDir];
            // 対応するグリッドに立っているプレイヤーのインデックスを設定
            _stageGrid.getGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    void Update()
    {
        // 現在のグリッド上に存在するキャラクター情報を更新
        SelectCharacterIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        // キャラクターのパラメータ表示の更新
        UpdateCharacterParameter();

        _phaseManager.Update();
    }

    void LateUpdate()
    {
        _phaseManager.LateUpdate();

        // ステージグリッド上のキャラ情報を更新
        StageGrid.instance.ClearGridsCharaIndex();
        foreach( Player player in _players )
        {
            _stageGrid.getGridInfo(player.tmpParam.gridIndex).charaIndex = player.param.characterIndex;
        }
        foreach (Enemy enemy in _enemies)
        {
            _stageGrid.getGridInfo(enemy.tmpParam.gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    // プレイヤー行動フェーズ
    void PlayerPhase()
    {
        _phase = BattlePhase.BATTLE_PLAYER_COMMAND;
    }

    /// <summary>
    /// 選択グリッド上におけるキャラクターのパラメータ情報を更新します
    /// </summary>
    void UpdateCharacterParameter()
    {
        var BattleUI = BattleUISystem.Instance;

        // 攻撃キャラクターが存在する場合は更新しない
        if (AttackerCharacter != null)
        {
            // パラメータ表示を更新
            var character = SearchCharacterFromCharaIndex(SelectCharacterIndex);
            BattleUI.ToggleEnemyParameter(true);

            // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
            if (character != null && _prevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            _prevCharacter = character;
        }
        else
        {
            // パラメータ表示を更新
            var character = SearchCharacterFromCharaIndex(SelectCharacterIndex);
            BattleUI.TogglePlayerParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

            // 前フレームで選択したキャラと異なる場合はレイヤーを元に戻す
            if (_prevCharacter != null && _prevCharacter != character)
            {
                _prevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
            }
            // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
            if (character != null && _prevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            _prevCharacter = character;
        }
    }

    public IEnumerator Battle()
    {
        while (_phase != BattlePhase.BATTLE_END)
        {
            yield return null;
            Debug.Log(_phase);

            switch (_phase)
            {
                case BattlePhase.BATTLE_START:
                    _phase = BattlePhase.BATTLE_PLAYER_COMMAND;
                    break;
                case BattlePhase.BATTLE_PLAYER_COMMAND:
                    PlayerPhase();
                    break;
                case BattlePhase.BATTLE_PLAYER_EXECUTE:
                    _phase = BattlePhase.BATTLE_ENEMY_COMMAND;
                    break;
                case BattlePhase.BATTLE_ENEMY_COMMAND:
                    _phase = BattlePhase.BATTLE_ENEMY_EXECUTE;
                    break;
                case BattlePhase.BATTLE_ENEMY_EXECUTE:
                    _phase = BattlePhase.BATTLE_RESULT;
                    break;
                case BattlePhase.BATTLE_RESULT:
                    _phase = BattlePhase.BATTLE_END;
                    break;
                case BattlePhase.BATTLE_END:
                    break;
            }
        }
    }

    // プレイヤーをリストに登録
    public void AddPlayerToList( Player player )
    {
        _players.Add( player );
    }

    // エネミーをリストに登録
    public void AddEnemyToList( Enemy enemy )
    {
        _enemies.Add( enemy );
    }

    public void registStageGrid( StageGrid script )
    {
        _stageGrid = script;
    }

    /// <summary>
    /// 攻撃キャラクターを設定します
    /// パラメータUI表示カリングのためにレイヤーを変更します
    /// </summary>
    /// <param name="character">アタッカー設定するキャラクター</param>
    public void SetAttackerCharacter(Character character)
    {
        AttackerCharacter = character;
        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRenderAttacker"));
    }

    /// <summary>
    /// 攻撃キャラクターの設定を解除します
    /// パラメータUI表示カリングのために変更していたレイヤーを元に戻します
    /// </summary>
    public void ResetAttackerCharacter()
    {
        AttackerCharacter = null;
        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
    }


    /// <summary>
    /// 攻撃フェーズ状態かどうかを確認します
    /// </summary>
    /// <returns>true ; 攻撃フェーズ状態である false : 攻撃フェーズ状態ではない </returns>
    public bool IsAttackPhaseState()
    {
        // 攻撃フェーズ状態の際にはAttackerCharacterが必ずnullではない
        return AttackerCharacter != null;
    }

    /// <summary>
    /// キャラクターインデックスを素に該当するプレイヤーを探します
    /// </summary>
    /// <param name="characterIndex">検索するプレイヤーキャラクターのインデックス</param>
    /// <returns>該当したプレイヤーキャラクター</returns>
    public Player SearchPlayerFromCharaIndex( int characterIndex)
    {
        foreach (Player player in _players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// キャラクターインデックスを素に該当するキャラクターを探します
    /// </summary>
    /// <param name="characterIndex">検索するキャラクターのインデックス</param>
    /// <returns>該当したキャラクター</returns>
    public Character SearchCharacterFromCharaIndex(int characterIndex)
    {
        foreach (Player player in _players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.param.characterIndex == characterIndex)
            {
                return enemy;
            }
        }

        return null;
    }

    /// <summary>
    /// 現在選択しているグリッド上のキャラクターを取得します
    /// </summary>
    /// <returns>選択しているグリッド上のキャラクター</returns>
    public Character GetSelectCharacter()
    {
        return SearchCharacterFromCharaIndex(SelectCharacterIndex);
    }

    /// <summary>
    /// 終了常態かどうかを判定します
    /// </summary>
    /// <returns>true : 終了</returns>
    public bool isEnd()
    {
        return _phase == BattlePhase.BATTLE_END;
    }
}