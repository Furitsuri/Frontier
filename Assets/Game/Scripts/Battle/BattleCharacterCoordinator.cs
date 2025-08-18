using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class BattleCharacterCoordinator
    {
        Stage.StageController _stgCtrl = null;

        private List<Player> _players               = new List<Player>(Constants.CHARACTER_MAX_NUM);
        private List<Enemy> _enemies                = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
        private List<Other> _others                 = new List<Other>(Constants.CHARACTER_MAX_NUM);
        private CharacterHashtable _characterHash   = new CharacterHashtable();
        private CharacterHashtable.Key _diedCharacterKey;
        private CharacterHashtable.Key _battleBossCharacterKey;
        private CharacterHashtable.Key _escortTargetCharacterKey;

        public delegate void StageAnim();

        [Inject]
        public void Construct( Stage.StageController stgCtrl )
        {
            _stgCtrl    = stgCtrl;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            _diedCharacterKey           = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
            _battleBossCharacterKey     = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
            _escortTargetCharacterKey   = new CharacterHashtable.Key(Character.CHARACTER_TAG.NONE, -1);
        }

        /// <summary>
        /// キャラクター達をステージの初期座標に配置します
        /// </summary>
        public void PlaceAllCharactersAtStartPosition()
        {
            List<Character>[] charaLists = new List<Character>[]
            {
                new List<Character>(_players),
                new List<Character>(_enemies),
                new List<Character>(_others)
            };

            // 向きの値を設定
            Quaternion[] rot = new Quaternion[(int)Constants.Direction.NUM_MAX];
            for (int i = 0; i < (int)Constants.Direction.NUM_MAX; ++i)
            {
                rot[i] = Quaternion.AngleAxis(90 * i, Vector3.up);
            }

            foreach( var charaList in charaLists )
            {
                foreach ( var chara in charaList )
                {
                    // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
                    int gridIndex = chara.characterParam.initGridIndex;
                    // プレイヤーの画面上の位置を設定
                    chara.transform.position = _stgCtrl.GetGridCharaStandPos(gridIndex);
                    // 向きを設定
                    chara.transform.rotation = rot[(int)chara.characterParam.initDir];
                    // 対応するグリッドに立っているプレイヤーのインデックスを設定
                    _stgCtrl.GetGridInfo(gridIndex).SetExistCharacter(chara);
                }
            }
        }

        /// <summary>
        /// キャラクターをリストとハッシュに登録します
        /// </summary>
        /// <param name="chara">登録対象のキャラクター</param>
        public void AddCharacterToList(Character chara)
        {
            var param = chara.characterParam;
            CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

            switch( chara )
            {
                case Other:
                    Other ot = chara as Other;
                    Debug.Assert( ot != null, "Failed to cast from Character to Other" );

                    _others.Add(ot);
                    break;
                case Player:
                    Player pl = chara as Player;
                    Debug.Assert( pl != null, "Failed to cast from Character to Player" );

                    _players.Add(pl);
                    break;
                case Enemy:
                    Enemy em = chara as Enemy;
                    Debug.Assert( em != null, "Failed to cast from Character to Enemy" );

                    _enemies.Add(em);
                    break;
            }
            
            _characterHash.Add(key, chara);
        }

        /// <summary>
        /// 該当キャラクターが死亡した際などにリストから対象を削除します
        /// </summary>
        /// <param name="chara">削除対象のプレイヤー</param>
        public void RemoveCharacterFromList(Character chara)
        {
            switch (chara)
            {
                case Other:
                    Other ot = chara as Other;
                    Debug.Assert(ot != null, "Failed to cast from Character to Other");

                    _others.Remove(ot);
                    break;
                case Player:
                    Player pl = chara as Player;
                    Debug.Assert(pl != null, "Failed to cast from Character to Player");

                    _players.Remove(pl);
                    break;
                case Enemy:
                    Enemy em = chara as Enemy;
                    Debug.Assert(em != null, "Failed to cast from Character to Enemy");

                    _enemies.Remove(em);
                    break;
            }

            _characterHash.Remove(chara);
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
        /// キャラクターをリストから順番に取得します
        /// </summary>
        /// <returns>キャラクター</returns>
        public IEnumerable<Character> GetCharacterEnumerable( Character.CHARACTER_TAG tag )
        {
            List<Character>[] charaList = new List<Character>[]
            {
                _players.ToList<Character>(),
                _enemies.ToList<Character>(),
                _others.ToList<Character>()
            };

            foreach (var chara in charaList[(int)tag])
            {
                yield return chara;
            }

            /*
            switch ( tag )
            {
                case Character.CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        yield return player;
                    }
                    break;
                case Character.CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        yield return enemy;
                    }
                    break;
                case Character.CHARACTER_TAG.OTHER:
                    foreach (Other other in _others)
                    {
                        yield return other;
                    }
                    break;
            }
            */
        }

        /// <summary>
        /// 全ての行動可能キャラクターの行動が終了したかを判定します
        /// </summary>
        /// <returns>全ての行動可能キャラクターの行動が終了したか</returns>
        public bool IsEndAllArmyWaitCommand(Character.CHARACTER_TAG tag)
        {
            List<Character>[] charaList = new List<Character>[]
            {
                _players.ToList<Character>(),
                _enemies.ToList<Character>(),
                _others.ToList<Character>()
            };

            foreach( var chara in charaList[(int)tag])
            {
                if (!chara.tmpParam.IsEndAction())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// ステージのボスキャラクターが倒されているかを取得します
        /// </summary>
        /// <param name="diedCharacterKey">倒されたキャラクターのハッシュキー</param>
        /// <returns>ボスキャラクターが設定されている上で倒されているか</returns>
        public bool IsDiedBossCharacter(CharacterHashtable.Key diedCharacterKey)
        {
            // ステージにボスが設定されているかのチェック
            if (_battleBossCharacterKey.characterTag != Character.CHARACTER_TAG.NONE)
            {
                return (diedCharacterKey == _battleBossCharacterKey);
            }

            return false;
        }

        /// <summary>
        /// ステージの庇護対象キャラクターが倒されているかを取得します
        /// </summary>
        /// <param name="diedCharacterKey">倒されたキャラクターのハッシュキー</param>
        /// <returns>庇護対象のキャラクターが設定されている上で倒されているか</returns>
        public bool IsDiedEscortCharacter(CharacterHashtable.Key diedCharacterKey)
        {
            // ステージ上に庇護対象のキャラクターが設定されているかのチェック
            if (_escortTargetCharacterKey.characterTag != Character.CHARACTER_TAG.NONE)
            {
                return (diedCharacterKey == _escortTargetCharacterKey);
            }

            return false;
        }

        /// <summary>
        /// 勝利、敗戦判定を行います
        /// </summary>
        /// <param name="clearAnim">ステージクリア時のアニメーション呼び出し関数</param>
        /// <param name="overAnim">ゲームオーバー時のアニメーション呼び出し関数</param>
        /// <returns>勝利、敗戦処理に遷移するか否か</returns>
        public bool CheckVictoryOrDefeat( StageAnim clearAnim, StageAnim overAnim )
        {
            if (_diedCharacterKey.characterTag == Character.CHARACTER_TAG.NONE) return false;
            
            // ステージにボスが設定されているかのチェック
            if (IsDiedBossCharacter(_diedCharacterKey))
            {
                clearAnim();
                return true;
            }

            if ( IsDiedEscortCharacter(_diedCharacterKey) )
            {
                overAnim();
                return true;
            }

            if ( CheckCharacterAnnihilated(_diedCharacterKey.characterTag) )
            {
                if (_diedCharacterKey.characterTag == Character.CHARACTER_TAG.ENEMY)
                {
                    clearAnim();
                }
                else if (_diedCharacterKey.characterTag == Character.CHARACTER_TAG.PLAYER)
                {
                    overAnim();
                }

                return true;
            }
            else
            {
                ResetDiedCharacter();
            }

            return false;
        }

        /// <summary>
        /// 対象軍勢が全滅しているかを確認します
        /// </summary>
        /// <param name="characterTag">軍勢のタグ</param>
        /// <returns>対象軍勢が全滅しているか</returns>
        public bool CheckCharacterAnnihilated(Character.CHARACTER_TAG characterTag)
        {
            bool isAnnihilated = true;

            switch (characterTag)
            {
                case Character.CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        if (!player.characterParam.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
                case Character.CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        if (!enemy.characterParam.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
                case Character.CHARACTER_TAG.OTHER:
                    foreach (Other other in _others)
                    {
                        if (!other.characterParam.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
            }

            return isAnnihilated;
        }

        /// <summary>
        /// 現在選択しているグリッド上のキャラクターを取得します
        /// </summary>
        /// <returns>選択しているグリッド上のキャラクター</returns>
        public Character GetSelectCharacter()
        {
            Stage.GridInfo info;
            _stgCtrl.FetchCurrentGridInfo(out info);

            return GetCharacterFromHashtable(info.charaTag, info.charaIndex);
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
                case Character.CHARACTER_TAG.PLAYER:    return _players.Count;
                case Character.CHARACTER_TAG.ENEMY:     return _enemies.Count;
                case Character.CHARACTER_TAG.OTHER:     return _others.Count;
                default: return -1;
            }
        }

        /// <summary>
        /// 直近の戦闘で死亡したキャラクターのキャラクタータグを設定します
        /// </summary>
        /// <param name="tag">死亡したキャラクターのキャラクタータグ</param>
        public void SetDiedCharacterKey(CharacterHashtable.Key key) { _diedCharacterKey = key; }

        /// <summary>
        /// 対象とする軍勢の全てのキャラクターを待機済みに変更します
        /// 主にターンを終了させる際に使用します
        /// </summary>
        /// /// <param name="tag">指定する軍勢のタグ</param>
        public void ApplyAllArmyEndAction(Character.CHARACTER_TAG tag)
        {
            switch( tag )
            {
                case Character.CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        player.tmpParam.EndAction();
                    }

                    break;

                case Character.CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        enemy.tmpParam.EndAction();
                    }

                    break;

                case Character.CHARACTER_TAG.OTHER:
                    foreach (Other other in _others)
                    {
                        other.tmpParam.EndAction();
                    }

                    break;
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

            int targetDef = (int)Mathf.Floor((target.characterParam.Def + target.modifiedParam.Def) * target.skillModifiedParam.DefMagnification);
            int attackerAtk = (int)Mathf.Floor((attacker.characterParam.Atk + attacker.modifiedParam.Atk) * attacker.skillModifiedParam.AtkMagnification);
            int changeHP = (targetDef - attackerAtk);

            target.tmpParam.SetExpectedHpChange( Mathf.Min(changeHP, 0), Mathf.Min(changeHP * attacker.skillModifiedParam.AtkNum, 0) );
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
            foreach (Other other in _others)
            {
                other.BePossibleAction();
            }
        }

        /// <summary>
        /// 死亡キャラクターのキャラクタータグをリセットします
        /// </summary>
        public void ResetDiedCharacter()
        {
            _diedCharacterKey.characterTag = Character.CHARACTER_TAG.NONE;
            _diedCharacterKey.characterIndex = -1;
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
                        player.characterParam.RecoveryActionGauge();
                    }
                    break;
                case Character.CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        enemy.characterParam.RecoveryActionGauge();
                    }
                    break;
                case Character.CHARACTER_TAG.OTHER:
                    foreach (Other other in _others)
                    {
                        other.characterParam.RecoveryActionGauge();
                    }
                    break;
            }
        }
    }
}