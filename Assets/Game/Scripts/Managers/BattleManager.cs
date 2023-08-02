using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Character;
using static StageGrid;

public class BattleManager : Singleton<BattleManager>
{
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
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
    private PhaseManagerBase[] _phaseManagers   = new PhaseManagerBase[((int)TurnType.NUM)];
    private List<Player> _players               = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> _enemies                = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private CharacterHashtable _characterHash   = new CharacterHashtable();
    private CharacterHashtable.Key _diedCharacterKey;
    private CharacterHashtable.Key _battleBossCharacterKey;
    private CharacterHashtable.Key _escortTargetCharacterKey;
    private Character _prevCharacter = null;
    private StageGrid.GridInfo _prevInfo;
    private bool _transitNextPhase = false;
    private int _phaseManagerIndex = 0;
    private int _currentStageIndex = 0;
    // 現在選択中のキャラクターインデックス
    public CharacterHashtable.Key SelectCharacterInfo { get; private set; } = new CharacterHashtable.Key(CHARACTER_TAG.CHARACTER_NONE, -1);
    // 攻撃フェーズ中において、攻撃を開始するキャラクター
    public Character AttackerCharacter { get; private set; } = null;

    override protected void Init()
    {
        if (StageGrid.Instance == null)
        {
            Instantiate(stageGridObject);
        }

        // FileReaderManagerからjsonファイルを読込み、各プレイヤー、敵に設定する
        FileReadManager.Instance.PlayerLoad(_currentStageIndex);
        FileReadManager.Instance.EnemyLord(_currentStageIndex);

        _phaseManagers[(int)TurnType.PLAYER_TURN]   = new PlayerPhaseManager();
        _phaseManagers[(int)TurnType.ENEMY_TURN]    = new EnemyPhaseManager();
        _currentPhaseManager                        = _phaseManagers[(int)TurnType.PLAYER_TURN];

        _phase = BattlePhase.BATTLE_START;
        _diedCharacterKey = new CharacterHashtable.Key(Character.CHARACTER_TAG.CHARACTER_NONE, -1);

        // TODO : ステージのファイルから読み込んで設定するように
        _battleBossCharacterKey = new CharacterHashtable.Key(Character.CHARACTER_TAG.CHARACTER_NONE, -1);
        _escortTargetCharacterKey = new CharacterHashtable.Key(Character.CHARACTER_TAG.CHARACTER_NONE, -1);

        // スキルデータの読込
        FileReadManager.Instance.SkillDataLord();
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
            int gridIndex               = player.param.initGridIndex;
            // プレイヤーの画面上の位置を設定
            player.transform.position   = _stageGrid.GetGridCharaStandPos(gridIndex);
            // 向きを設定
            player.transform.rotation   = rot[(int)player.param.initDir];
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
            enemy.transform.position = _stageGrid.GetGridCharaStandPos(gridIndex);
            // 向きを設定
            enemy.transform.rotation = rot[(int)enemy.param.initDir];
            // 対応するグリッドに立っているプレイヤーのインデックスを設定
            _stageGrid.GetGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }

        // グリッド情報を更新
        _stageGrid.UpdateGridInfo();
    }

    override protected void OnUpdate()
    {
        // TODO : 仮。あとでリファクタ
        if( GameManager.instance.IsInvoking() )
        {
            return;
        }

        // 現在のグリッド上に存在するキャラクター情報を更新
        StageGrid.GridInfo info;
        _stageGrid.FetchCurrentGridInfo(out info);
        if(_prevInfo.charaStandPos != info.charaStandPos)
        {
            BattleCameraController.Instance.SetLookAtBasedOnSelectCursor(info.charaStandPos);
            _prevInfo = info;
        } 

        SelectCharacterInfo = new CharacterHashtable.Key(info.characterTag, info.charaIndex);
        // キャラクターのパラメータ表示の更新
        UpdateCharacterParameter();

        if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

        if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;
        
        // フェーズマネージャを更新
        _transitNextPhase = _currentPhaseManager.Update();
    }

    override protected void OnLateUpdate()
    {
        if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

        if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;

        // 勝利、全滅チェックを行う
        if( CheckVictoryOrDefeat(_diedCharacterKey) ) {  return; }

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
    /// 選択グリッド上におけるキャラクターのパラメータ情報を更新します
    /// </summary>
    void UpdateCharacterParameter()
    {
        var BattleUI                = BattleUISystem.Instance;
        Character selectCharacter   = GetCharacterFromHashtable(SelectCharacterInfo);
        bool isAttaking             = IsAttackPhaseState();

        BattleUI.PlayerParameter.SetAttacking(false);
        BattleUI.EnemyParameter.SetAttacking(false);

        // 攻撃対象選択時
        if (isAttaking)
        {
            Debug.Assert(AttackerCharacter != null);

            // 画面構成は以下の通り
            //   左        右
            // PLAYER 対 ENEMY
            // OTHER  対 ENEMY
            // PLAYER 対 OTHER
            if ( AttackerCharacter.param.characterTag != Character.CHARACTER_TAG.CHARACTER_ENEMY )
            {
                BattleUI.PlayerParameter.SetCharacter(AttackerCharacter);
                BattleUI.PlayerParameter.SetAttacking(true);
                BattleUI.EnemyParameter.SetCharacter(selectCharacter);
            }
            else
            {
                BattleUI.PlayerParameter.SetCharacter(selectCharacter);
                BattleUI.EnemyParameter.SetCharacter(AttackerCharacter);
                BattleUI.EnemyParameter.SetAttacking(true);
            }
        }
        else
        {
            // ※1フレーム中にgameObjectのアクティブ切り替えを複数回行うと正しく反映されないため、無駄があって気持ち悪いが以下の判定文を用いる
            BattleUI.TogglePlayerParameter(selectCharacter != null && selectCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(selectCharacter != null && selectCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

            // パラメータ表示を更新
            if (selectCharacter != null)
            {
                if ( selectCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER )
                {
                    BattleUI.PlayerParameter.SetCharacter(selectCharacter);
                }
                else
                {
                    BattleUI.EnemyParameter.SetCharacter(selectCharacter);
                }
            }

            // 前フレームで選択したキャラと異なる場合はレイヤーを元に戻す
            if (_prevCharacter != null && _prevCharacter != selectCharacter)
            {
                _prevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
            }
        }

        // 選択しているキャラクターのレイヤーをパラメータUI表示のために一時的に変更
        if (selectCharacter != null && _prevCharacter != selectCharacter)
        {
            selectCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
        }

        _prevCharacter = selectCharacter;
    }

    /// <summary>
    /// 対象のキャラクター群が全滅しているかを確認します
    /// </summary>
    bool CheckCharacterAnnihilated(Character.CHARACTER_TAG characterTag)
    {
        bool isAnnihilated = true;

        switch (characterTag)
        {
            case CHARACTER_TAG.CHARACTER_PLAYER:
                foreach (Player player in _players)
                {
                    if (!player.IsDead()) { isAnnihilated = false; break; }
                }
                break;
            case CHARACTER_TAG.CHARACTER_ENEMY:
                foreach (Enemy enemy in _enemies)
                {
                    if (!enemy.IsDead()) { isAnnihilated = false; break; }
                }
                break;
            case CHARACTER_TAG.CHARACTER_OTHER:
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
        if (diedCharacterKey.characterTag != CHARACTER_TAG.CHARACTER_NONE)
        {
            // ステージにボスが設定されているかのチェック
            if(_battleBossCharacterKey.characterTag != CHARACTER_TAG.CHARACTER_NONE)
            {
                if( diedCharacterKey == _battleBossCharacterKey )
                {
                    // ステージクリアに遷移
                    StartStageClearAnim();

                    return true;
                }
            }

            if(_escortTargetCharacterKey.characterTag != CHARACTER_TAG.CHARACTER_NONE)
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
                if (diedCharacterKey.characterTag == CHARACTER_TAG.CHARACTER_ENEMY)
                {
                    // ステージクリアに遷移
                    StartStageClearAnim();
                }
                else if (diedCharacterKey.characterTag == CHARACTER_TAG.CHARACTER_PLAYER)
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
    public void AddPlayerToList( Player player )
    {
        var param = player.param;
        CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

        _players.Add( player );
        _characterHash.Add(key, player);
    }

    /// <summary>
    /// 敵をリストとハッシュに登録します
    /// </summary>
    /// <param name="enemy">登録する敵</param>
    public void AddEnemyToList( Enemy enemy )
    {
        var param = enemy.param;
        CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

        _enemies.Add( enemy );
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
    public void RemoveEnemyFromList( Enemy enemy )
    {
        _enemies.Remove(enemy);
        _characterHash.Remove(enemy);
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

        BattleUISystem.Instance.TogglePlayerParameter(true);
        BattleUISystem.Instance.ToggleEnemyParameter(true);
    }

    /// <summary>
    /// 直近の戦闘で死亡したキャラクターのキャラクタータグを設定します
    /// </summary>
    /// <param name="tag">死亡したキャラクターのキャラクタータグ</param>
    public void SetDiedCharacterKey( CharacterHashtable.Key key ) { _diedCharacterKey = key; }

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
        SelectCharacterInfo = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
        AttackerCharacter = null;
    }

    /// <summary>
    /// 死亡キャラクターのキャラクタータグをリセットします
    /// </summary>
    public void ResetDiedCharacter()
    { 
        _diedCharacterKey.characterTag      = CHARACTER_TAG.CHARACTER_NONE;
        _diedCharacterKey.characterIndex    = -1;
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
        CharacterHashtable.Key hashKey = new CharacterHashtable.Key( tag, index );

        return _characterHash.Get(hashKey) as Character;
    }

    /// <summary>
    /// ハッシュテーブルから指定のタグとインデックスをキーとするキャラクターを取得します
    /// </summary>
    /// <param name="key">ハッシュキー</param>
    /// <returns>指定のキーに対応するキャラクター</returns>
    public Character GetCharacterFromHashtable(CharacterHashtable.Key key)
    {
        if (key.characterTag == CHARACTER_TAG.CHARACTER_NONE || key.characterIndex < 0) return null;

        return _characterHash.Get( key ) as Character;
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
    /// 現在選択しているグリッド上のキャラクターを取得します
    /// </summary>
    /// <returns>選択しているグリッド上のキャラクター</returns>
    public Character GetSelectCharacter()
    {
        StageGrid.GridInfo info;
        StageGrid.Instance.FetchCurrentGridInfo(out info);

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

        int targetDef   = (int)((target.param.Def + target.modifiedParam.Def) * target.skillModifiedParam.DefMagnification);
        int attackerAtk = (int)((attacker.param.Atk + attacker.modifiedParam.Atk) * attacker.skillModifiedParam.AtkMagnification);
        int changeHP    = (targetDef - attackerAtk);

        target.tmpParam.expectedChangeHP        = Mathf.Min(changeHP, 0);
        target.tmpParam.totalExpectedChangeHP   = Mathf.Min(changeHP * attacker.skillModifiedParam.AtkNum, 0);
    }

    /// <summary>
    /// 全てのキャラクターの一時パラメータをリセットします
    /// </summary>
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
    /// 指定のキャラクター群のアクションゲージを回復させます
    /// </summary>
    /// <param name="tag">キャラクター群のタグ</param>
    public void RecoveryActionGaugeForGroup( CHARACTER_TAG tag )
    {
        switch( tag )
        {
            case CHARACTER_TAG.CHARACTER_PLAYER:
                foreach (Player player in _players)
                {
                    player.RecoveryActionGauge();
                }
                break;
            case CHARACTER_TAG.CHARACTER_ENEMY:
                foreach (Enemy enemy in _enemies)
                {
                    enemy.RecoveryActionGauge();
                }
                break;
            case CHARACTER_TAG.CHARACTER_OTHER:
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