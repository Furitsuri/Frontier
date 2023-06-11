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
    public GameObject m_StageGridObject;
    private PhaseManagerBase m_PhaseManager;
    private StageGrid m_StageGrid;
    private BattlePhase m_Phase;
    private List<Player> m_Players = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> m_Enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private Character m_PrevCharacter = null;
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
            Instantiate(m_StageGridObject);
        }

        m_PhaseManager = new PlayerPhaseManager();
    }

    void Start()
    {
        m_PhaseManager.Init();

        // 向きの値を設定
        Quaternion[] rot = new Quaternion[(int)Constants.Direction.NUM_MAX];
        for (int i = 0; i < (int)Constants.Direction.NUM_MAX; ++i)
        {
            rot[i] = Quaternion.AngleAxis(90 * i, Vector3.up);
        }


        // 各プレイヤーキャラクターの位置を設定
        for (int i = 0; i < m_Players.Count; ++i)
        {
            Player player = m_Players[i];
            // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
            int gridIndex                               = player.param.initGridIndex;
            // プレイヤーの画面上の位置を設定
            player.transform.position                   = m_StageGrid.getGridCharaStandPos(gridIndex);
            // 向きを設定
            player.transform.rotation = rot[(int)player.param.initDir];
            // 対応するグリッドに立っているプレイヤーのインデックスを設定
            m_StageGrid.getGridInfo(gridIndex).charaIndex = player.param.characterIndex;
        }

        // 各エネミーキャラクターの位置を設定
        for (int i = 0; i < m_Enemies.Count; ++i)
        {
            Enemy enemy = m_Enemies[i];
            // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
            int gridIndex = enemy.param.initGridIndex;
            // エネミーの画面上の位置を設定
            enemy.transform.position = m_StageGrid.getGridCharaStandPos(gridIndex);
            // 向きを設定
            enemy.transform.rotation = rot[(int)enemy.param.initDir];
            // 対応するグリッドに立っているプレイヤーのインデックスを設定
            m_StageGrid.getGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    void Update()
    {
        // 現在のグリッド上に存在するキャラクター情報を更新
        SelectCharacterIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        // キャラクターのパラメータ表示の更新
        UpdateCharacterParameter();

        m_PhaseManager.Update();
    }

    void LateUpdate()
    {
        m_PhaseManager.LateUpdate();

        // ステージグリッド上のキャラ情報を更新
        StageGrid.instance.ClearGridsCharaIndex();
        foreach( Player player in m_Players )
        {
            m_StageGrid.getGridInfo(player.tmpParam.gridIndex).charaIndex = player.param.characterIndex;
        }
        foreach (Enemy enemy in m_Enemies)
        {
            m_StageGrid.getGridInfo(enemy.tmpParam.gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    // プレイヤー行動フェーズ
    void PlayerPhase()
    {
        m_Phase = BattlePhase.BATTLE_PLAYER_COMMAND;
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
            if (character != null && m_PrevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            m_PrevCharacter = character;
        }
        else
        {
            // パラメータ表示を更新
            var character = SearchCharacterFromCharaIndex(SelectCharacterIndex);
            BattleUI.TogglePlayerParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

            // 前フレームで選択したキャラと異なる場合はレイヤーを元に戻す
            if (m_PrevCharacter != null && m_PrevCharacter != character)
            {
                m_PrevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
            }
            // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
            if (character != null && m_PrevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            m_PrevCharacter = character;
        }
    }

    public IEnumerator Battle()
    {
        while (m_Phase != BattlePhase.BATTLE_END)
        {
            yield return null;
            Debug.Log(m_Phase);

            switch (m_Phase)
            {
                case BattlePhase.BATTLE_START:
                    m_Phase = BattlePhase.BATTLE_PLAYER_COMMAND;
                    break;
                case BattlePhase.BATTLE_PLAYER_COMMAND:
                    PlayerPhase();
                    break;
                case BattlePhase.BATTLE_PLAYER_EXECUTE:
                    m_Phase = BattlePhase.BATTLE_ENEMY_COMMAND;
                    break;
                case BattlePhase.BATTLE_ENEMY_COMMAND:
                    m_Phase = BattlePhase.BATTLE_ENEMY_EXECUTE;
                    break;
                case BattlePhase.BATTLE_ENEMY_EXECUTE:
                    m_Phase = BattlePhase.BATTLE_RESULT;
                    break;
                case BattlePhase.BATTLE_RESULT:
                    m_Phase = BattlePhase.BATTLE_END;
                    break;
                case BattlePhase.BATTLE_END:
                    break;
            }
        }
    }

    // プレイヤーをリストに登録
    public void AddPlayerToList( Player player )
    {
        m_Players.Add( player );
    }

    // エネミーをリストに登録
    public void AddEnemyToList( Enemy enemy )
    {
        m_Enemies.Add( enemy );
    }

    public void registStageGrid( StageGrid script )
    {
        m_StageGrid = script;
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

    public Player SearchPlayerFromCharaIndex( int characterIndex)
    {
        foreach (Player player in m_Players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }

        return null;
    }

    public Character SearchCharacterFromCharaIndex(int characterIndex)
    {
        foreach (Player player in m_Players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }
        foreach (Enemy enemy in m_Enemies)
        {
            if (enemy.param.characterIndex == characterIndex)
            {
                return enemy;
            }
        }

        return null;
    }

    public Character GetSelectCharacter()
    {
        return SearchCharacterFromCharaIndex(SelectCharacterIndex);
    }

    public bool isEnd()
    {
        return m_Phase == BattlePhase.BATTLE_END;
    }
}