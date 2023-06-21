using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Character;

public class BattleManager : Singleton<BattleManager>
{
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
        BATTLE_ENEMY_COMMAND,
        BATTLE_ENEMY_EXECUTE,
        BATTLE_RESULT,
        BATTLE_END,
    }

    public enum TurnType
    {
        PLAYER_TURN = 0,
        ENEMY_TURN,

        NUM
    }

    public GameObject stageGridObject;
    private BattlePhase _phase;
    private PhaseManagerBase _currentPhaseManager;
    private StageGrid _stageGrid;
    private PhaseManagerBase[] _phaseManagers = new PhaseManagerBase[((int)TurnType.NUM)];
    private List<Player> _players = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> _enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private CharacterHashtable characterHash = new CharacterHashtable();
    private Character _prevCharacter = null;
    private bool _transitNextPhase = false;
    private int _phaseManagerIndex = 0;
    // 現在選択中のキャラクターインデックス
    public (CHARACTER_TAG tag, int charaIndex) SelectCharacterTupleInfo { get; private set; } = (CHARACTER_TAG.CHARACTER_NONE, -1);
    // 攻撃フェーズ中において、攻撃を開始するキャラクター
    public Character AttackerCharacter { get; private set; } = null;

    override protected void Init()
    {
        if (StageGrid.Instance == null)
        {
            Instantiate(stageGridObject);
        }

        _phaseManagers[(int)TurnType.PLAYER_TURN]   = new PlayerPhaseManager();
        _phaseManagers[(int)TurnType.ENEMY_TURN]    = new EnemyPhaseManager();
        _currentPhaseManager                        = _phaseManagers[(int)TurnType.PLAYER_TURN];
    }

    override protected void OnStart()
    {
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
            int gridIndex                               = player.param.initGridIndex;
            // プレイヤーの画面上の位置を設定
            player.transform.position                   = _stageGrid.getGridCharaStandPos(gridIndex);
            // 向きを設定
            player.transform.rotation = rot[(int)player.param.initDir];
            // 対応するグリッドに立っているプレイヤーのインデックスを設定
            _stageGrid.GetGridInfo(gridIndex).charaIndex = player.param.characterIndex;
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
            _stageGrid.GetGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    override protected void OnUpdate()
    {
        // TODO : 仮。あとでリファクタ
        if( GameManager.instance.IsInvoking() )
        {
            return;
        }

        var stageGrid = StageGrid.Instance;

        // ステージグリッド上のキャラ情報を更新
        stageGrid.UpdateGridInfo();
        // 現在のグリッド上に存在するキャラクター情報を更新
        var gridInfo = stageGrid.GetCurrentGridInfo();
        SelectCharacterTupleInfo = ( gridInfo.characterTag, gridInfo.charaIndex );
        // キャラクターのパラメータ表示の更新
        UpdateCharacterParameter();
        // フェーズマネージャを更新
        _transitNextPhase = _currentPhaseManager.Update();
    }

    override protected void OnLateUpdate()
    {
        if (!_transitNextPhase)
        {
            _currentPhaseManager.LateUpdate();
        }
        else
        {
            // 次のマネージャに切り替える
            _phaseManagerIndex = (_phaseManagerIndex + 1) % (int)TurnType.NUM;
            _currentPhaseManager = _phaseManagers[_phaseManagerIndex];
            _currentPhaseManager.Init();
            // 一時パラメータをリセット
            ResetTmpParamAllCharacter();
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
            var character = GetCharacterFromHashtable(SelectCharacterTupleInfo);
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
            Character character = GetCharacterFromHashtable(SelectCharacterTupleInfo);
            BattleUI.TogglePlayerParameter(character != null && character.param.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(character != null && character.param.characterTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

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

    /// <summary>
    /// プレイヤーをリストとハッシュに登録します
    /// </summary>
    /// <param name="player">登録するプレイヤー</param>
    public void AddPlayerToList( Player player )
    {
        var param = player.param;

        _players.Add( player );
        characterHash.Add((param.characterTag, param.characterIndex), player);
    }

    /// <summary>
    /// 敵をリストとハッシュに登録します
    /// </summary>
    /// <param name="enemy">登録する敵</param>
    public void AddEnemyToList( Enemy enemy )
    {
        var param = enemy.param;

        _enemies.Add( enemy );
        characterHash.Add((param.characterTag, param.characterIndex), enemy);
    }

    /// <summary>
    /// ステージグリッドスクリプトを登録します
    /// </summary>
    /// <param name="script">登録するスクリプト</param>
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
        if( AttackerCharacter == null )
        {
            return;
        }

        var param = AttackerCharacter.param;

        // 選択しているタグ、及びインデックスを攻撃対象キャラから元に戻す
        SelectCharacterTupleInfo = (param.characterTag, param.characterIndex);

        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
        AttackerCharacter = null;
    }


    /// <summary>
    /// 攻撃フェーズ状態かどうかを確認します
    /// </summary>
    /// <returns>true : 攻撃フェーズ状態である false : 攻撃フェーズ状態ではない </returns>
    public bool IsAttackPhaseState()
    {
        // 攻撃フェーズ状態の際にはAttackerCharacterが必ずnullではない
        return AttackerCharacter != null;
    }

    /// <summary>
    /// ハッシュテーブルから指定のタグとインデックスをキーとするキャラクターを取得します
    /// </summary>
    /// <param name="tag">キャラクタータグ</param>
    /// <param name="index">キャラクターインデックス</param>
    /// <returns>指定のキーに対応するキャラクター</returns>
    public Character GetCharacterFromHashtable( CHARACTER_TAG tag, int index )
    {
        if (tag == CHARACTER_TAG.CHARACTER_NONE || index < 0) return null;

        return characterHash.Get((tag, index)) as Character;
    }

    /// <summary>
    /// ハッシュテーブルから指定のタグとインデックスをキーとするキャラクターを取得します
    /// </summary>
    /// <param name="tuple">指定するタグとインデックスを持たせたタプル</param>
    /// <returns>指定のキーに対応するキャラクター</returns>
    public Character GetCharacterFromHashtable( (CHARACTER_TAG tag, int index) tuple )
    {
        if (tuple.tag == CHARACTER_TAG.CHARACTER_NONE || tuple.index < 0) return null;

        return characterHash.Get( tuple ) as Character;
    }

    /// <summary>
    /// 全ての行動可能キャラクターの行動が終了したかを判定します
    /// </summary>
    /// <returns>全ての行動可能キャラクターの行動が終了したか</returns>
    public bool IsEndAllCharacterWaitCommand()
    {
        foreach (Player player in _players)
        {
            if ( !player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT] )
            {
                return false;
            }
        }

        return true;
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
        return SearchCharacterFromCharaIndex(StageGrid.Instance.GetCurrentGridInfo().charaIndex);
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
        foreach( Enemy enemy in _enemies)
        {
            yield return enemy;
        }

        yield break;
    }

    /// <summary>
    /// 全てのプレイヤーキャラクターを待機済みに変更します
    /// 主にターンを終了させる際に使用します
    /// </summary>
    public void ApplyAllPlayerWaitEnd()
    {
        foreach( Player player in _players )
        {
            player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT] = true;
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

        target.tmpParam.expectedChangeHP = Mathf.Min(target.param.Def - attacker.param.Atk, 0);
    }

    public void ResetTmpParamAllCharacter()
    {
        foreach (Player player in _players)
        {
            player.tmpParam.Reset();
        }
        foreach (Enemy enemy in _enemies)
        {
            enemy.tmpParam.Reset();
        }
    }

    /// <summary>
    /// ダメージ予測をリセットします
    /// </summary>
    /// <param name="attacker">攻撃キャラクター</param>
    /// <param name="target">標的キャラクター</param>
    public void ResetDamageExpect(Character attacker, Character target)
    {
        if( target == null )
        {
            return;
        }

        target.tmpParam.expectedChangeHP = 0;
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