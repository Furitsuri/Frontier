using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    /// <summary>
    /// 戦闘キャラクターの配置や消去、取得などを行います
    /// </summary>
    public class BattleCharacterCoordinator
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;
        [Inject] StageController _stgCtrl     = null;

        private List<Player> _candidatePlayers          = new List<Player>(Constants.CHARACTER_MAX_NUM);   // ステージ配置候補プレイヤーリスト
        private List<Player> _players                   = new List<Player>(Constants.CHARACTER_MAX_NUM);
        private List<Enemy> _enemies                    = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
        private List<Other> _others                     = new List<Other>(Constants.CHARACTER_MAX_NUM);
        private List<Character> _allCharacters          = new List<Character>();
        private Dictionary<CHARACTER_TAG, IEnumerable<Character>> _characterGroups;
        private CharacterDictionary _characterDict      = null;
        private CharacterKey _diedCharacterKey;
        private CharacterKey _battleBossCharacterKey;
        private CharacterKey _escortTargetCharacterKey;
        

        public delegate void StageAnim();

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            _diedCharacterKey           = new CharacterKey(CHARACTER_TAG.NONE, -1);
            _battleBossCharacterKey     = new CharacterKey(CHARACTER_TAG.NONE, -1);
            _escortTargetCharacterKey   = new CharacterKey(CHARACTER_TAG.NONE, -1);

            _characterGroups = new Dictionary<CHARACTER_TAG, IEnumerable<Character>>
            {
                { CHARACTER_TAG.PLAYER, _players },
                { CHARACTER_TAG.ENEMY,  _enemies },
                { CHARACTER_TAG.OTHER,  _others }
            };

            if( _characterDict == null )
            {
                _characterDict = _hierarchyBld.InstantiateWithDiContainer<CharacterDictionary>( false );
                NullCheck.AssertNotNull( _characterDict, nameof( _characterDict ) );
            }
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
            Quaternion[] rot = new Quaternion[(int)Direction.NUM_MAX];
            for (int i = 0; i < (int)Direction.NUM_MAX; ++i)
            {
                rot[i] = Quaternion.AngleAxis(90 * i, Vector3.up);
            }

            foreach( var charaList in charaLists )
            {
                foreach ( var chara in charaList )
                {
                    int gridIndex = chara.Params.CharacterParam.initGridIndex;                                  // ステージ開始時のプレイヤー立ち位置(インデックス)をキャッシュ
                    chara.Params.TmpParam.SetCurrentGridIndex( gridIndex );                                     // ステージ上のグリッド位置の設定
                    chara.GetTransformHandler.SetPosition( _stgCtrl.GetTileStaticData( gridIndex ).CharaStandPos );   // プレイヤーの画面上の位置を設定
                    chara.GetTransformHandler.SetRotation( rot[(int)chara.Params.CharacterParam.initDir] );     // 向きを設定
                    _stgCtrl.GetTileDynamicData( gridIndex ).SetExistCharacter( chara );                               // 対応するグリッドに立っているキャラクターを登録
                }
            }
        }

        /// <summary>
        /// キャラクターをリストとハッシュに登録します
        /// </summary>
        /// <param name="chara">登録対象のキャラクター</param>
        public void loadCharacterToList( Character chara )
        {
            Action<Character>[] addActionsByType = new Action<Character>[]
            {
                // ステージ配置候補のPLAYER
                c => _candidatePlayers.Add(c as Player),
                // ENEMY
                c =>
                {
                    var param = chara.Params.CharacterParam;
                    CharacterKey charaKey = new CharacterKey( param.characterTag, param.characterIndex );
                    _enemies.Add(c as Enemy);
                    _allCharacters.Add( chara );
                    _characterDict.Add( in charaKey, chara );
                },
                // OTHER
                c =>
                {
                    var param = chara.Params.CharacterParam;
                    CharacterKey charaKey = new CharacterKey( param.characterTag, param.characterIndex );
                    _others.Add(c as Other);
                    _allCharacters.Add( chara );
                    _characterDict.Add( in charaKey, chara );
                }
            };

            Debug.Assert( addActionsByType.Length == ( int ) CHARACTER_TAG.NUM, "配列数とキャラクターのタグ数が合致していません。" );

            addActionsByType[( int ) chara.Params.CharacterParam.characterTag]( chara );
            
        }

        public void AddPlayerToList( Character pl )
        {
            var param = pl.Params.CharacterParam;
            CharacterKey charaKey = new CharacterKey( param.characterTag, param.characterIndex );
            _players.Add( pl as Player );
            _characterDict.Add( in charaKey, pl );
            _allCharacters.Add( pl );
        }

        /// <summary>
        /// 該当キャラクターが死亡した際などにリストから対象を削除します
        /// </summary>
        /// <param name="charaKey">削除対象のキャラクター</param>
        public void RemoveCharacterFromList( CharacterKey charaKey )
        {
            var chara = _characterDict.Get( charaKey );
            NullCheck.AssertNotNull( chara, nameof( chara ) );

            Action<Character>[] removeActionsByType = new Action<Character>[( int ) CHARACTER_TAG.NUM]
            {
                c => _players.Remove(c as Player), // PLAYER
                c => _enemies.Remove(c as Enemy),  // ENEMY
                c => _others.Remove(c as Other)    // OTHER
            };

            removeActionsByType[( int ) chara.Params.CharacterParam.characterTag]( chara );
            _allCharacters.Remove( chara );
            _characterDict.Remove( in charaKey );
        }

        /// <summary>
        /// すべてのキャラクターのタイルメッシュをクリアします
        /// </summary>
        public void ClearAllTileMeshes()
        {
            foreach( var character in _allCharacters )
            {
                character.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshes();
            }
        }

        /// <summary>
        /// 配置候補プレイヤーをリストから順番に取得します
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Player> GetCandidatePlayerEnumerable()
        {
            foreach( var player in _candidatePlayers )
            {
                yield return player;
            }
        }

        /// <summary>
        /// キャラクターをリストから順番に取得します
        /// </summary>
        /// <returns>キャラクター</returns>
        public IEnumerable<Character> GetCharacterEnumerable( params CHARACTER_TAG[] tags )
        {
            foreach ( var character in _allCharacters )
            {
                if ( tags.Contains( character.Params.CharacterParam.characterTag ) )
                {
                    yield return character;
                }
            }
        }

        public bool IsContains( in CharacterKey charaKey )
        {
            return _characterDict.IsContains( charaKey );
        }

        /// <summary>
        /// 全ての行動可能キャラクターの行動が終了したかを判定します
        /// </summary>
        /// <returns>全ての行動可能キャラクターの行動が終了したか</returns>
        public bool IsEndAllArmyWaitCommand(CHARACTER_TAG tag)
        {
            if (_characterGroups.TryGetValue(tag, out var group))
            {
                foreach (var c in group)
                {
                    if (!c.Params.TmpParam.IsEndAction())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// ステージのボスキャラクターが倒されているかを取得します
        /// </summary>
        /// <param name="diedCharacterKey">倒されたキャラクターのハッシュキー</param>
        /// <returns>ボスキャラクターが設定されている上で倒されているか</returns>
        public bool IsDiedBossCharacter( in CharacterKey diedCharacterKey )
        {
            // ステージにボスが設定されているかのチェック
            if( _battleBossCharacterKey.CharacterTag != CHARACTER_TAG.NONE )
            {
                return ( diedCharacterKey == _battleBossCharacterKey );
            }

            return false;
        }

        /// <summary>
        /// ステージの庇護対象キャラクターが倒されているかを取得します
        /// </summary>
        /// <param name="diedCharacterKey">倒されたキャラクターのハッシュキー</param>
        /// <returns>庇護対象のキャラクターが設定されている上で倒されているか</returns>
        public bool IsDiedEscortCharacter( in CharacterKey diedCharacterKey )
        {
            // ステージ上に庇護対象のキャラクターが設定されているかのチェック
            if( _escortTargetCharacterKey.CharacterTag != CHARACTER_TAG.NONE )
            {
                return ( diedCharacterKey == _escortTargetCharacterKey );
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
            if (_diedCharacterKey.CharacterTag == CHARACTER_TAG.NONE) return false;
            
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

            if ( CheckCharacterAnnihilated(_diedCharacterKey.CharacterTag ) )
            {
                if (_diedCharacterKey.CharacterTag == CHARACTER_TAG.ENEMY)
                {
                    clearAnim();
                }
                else if (_diedCharacterKey.CharacterTag == CHARACTER_TAG.PLAYER)
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
        /// <param name="tag">軍勢のタグ</param>
        /// <returns>対象軍勢が全滅しているか</returns>
        public bool CheckCharacterAnnihilated(CHARACTER_TAG tag)
        {
            bool isAnnihilated = true;

            if (_characterGroups.TryGetValue(tag, out var group))
            {
                foreach (var c in group)
                {
                    if (!c.Params.CharacterParam.IsDead())
                    {
                        isAnnihilated = false;
                        break;
                    }
                }
            }
            return isAnnihilated;
        }

        /// <summary>
        /// ハッシュテーブルから指定のタグとインデックスをキーとするキャラクターを取得します
        /// </summary>
        /// <param name="key">ハッシュキー</param>
        /// <returns>指定のキーに対応するキャラクター</returns>
        public Character GetCharacterFromDictionary( in CharacterKey key )
        {
            if( IsContains( key ) )
            {
                return _characterDict.Get( key );
            }
            else { return null; }
        }

        /// <summary>
        /// 現在選択しているグリッド上のキャラクターを取得します
        /// </summary>
        /// <returns>選択しているグリッド上のキャラクター</returns>
        public Character GetSelectCharacter()
        {
            TileDynamicData tileData = _stgCtrl.TileDataHdlr().GetCurrentTileDatas().Item2;

            return GetCharacterFromDictionary( tileData.CharaKey );
        }

        /// <summary>
        /// 対象のキャラクターにとって、最も近い視線上のキャラクターを取得します
        /// </summary>
        /// <param name="baseChara">対象キャラクター</param>
        /// <returns>最も近い視線上のキャラクター</returns>
        public List<Character> GetLineOfSightCharacter( Character baseChara, CHARACTER_TAG tag )
        {
            List<Character> list = new List<Character>();

            Vector3 basePos     = _stgCtrl.GetTileStaticData( baseChara.Params.TmpParam.gridIndex ).CharaStandPos;
            Vector3 baseForward = baseChara.transform.forward;
            baseForward.y = 0f;

            if (_characterGroups.TryGetValue(tag, out var group))
            {
                foreach (var c in group)
                {
                    Vector3 targetPos = _stgCtrl.GetTileStaticData( c.Params.TmpParam.gridIndex ).CharaStandPos;

                    var direction   = targetPos - basePos;
                    direction.y     = 0f;
                    direction       = direction.normalized;

                    // 内積で向きが一致しているかを確認
                    float dot = Vector3.Dot(baseForward, direction);
                    if( Constants.DOT_THRESHOLD < dot )
                    {
                        list.Add(c);
                    }
                }
            }

            return list;
        }

        public Character GetNearestLineOfSightCharacter( Character baseChara, CHARACTER_TAG tag )
        {
            Character retChara = null;

            List<Character> charaList = GetLineOfSightCharacter(baseChara, tag);
            if( charaList.Count <= 0 ) { return null; }

            int totalRange = int.MaxValue;

            foreach( var chara in charaList )
            {
                int range = _stgCtrl.CalcurateTotalRange(baseChara.Params.TmpParam.gridIndex, chara.Params.TmpParam.gridIndex);
                if( range < totalRange )
                {
                    totalRange  = range;
                    retChara    = chara;
                }
            }

            return retChara;
        }

        /// <summary>
        /// 指定されたキャラクタータグの総ユニット数を取得します
        /// </summary>
        /// <param name="tag">指定するキャラクターのタグ</param>
        /// <returns>指定タグの総ユニット数</returns>
        public int GetCharacterCount( CHARACTER_TAG tag )
        {
            switch( tag )
            {
                case CHARACTER_TAG.PLAYER: return _players.Count;
                case CHARACTER_TAG.ENEMY: return _enemies.Count;
                case CHARACTER_TAG.OTHER: return _others.Count;
                default: return -1;
            }
        }

        /// <summary>
        /// 直近の戦闘で死亡したキャラクターのキャラクタータグを設定します
        /// </summary>
        /// <param name="tag">死亡したキャラクターのキャラクタータグ</param>
        public void SetDiedCharacterKey( in CharacterKey key ) { _diedCharacterKey = key; }

        /// <summary>
        /// 対象とする軍勢の全てのキャラクターを待機済みに変更します
        /// 主にターンを終了させる際に使用します
        /// </summary>
        /// /// <param name="tag">指定する軍勢のタグ</param>
        public void ApplyAllArmyEndAction(CHARACTER_TAG tag)
        {
            if (_characterGroups.TryGetValue(tag, out var group))
            {
                foreach (var c in group)
                {
                    c.Params.TmpParam.EndAction();
                }
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

            int targetDef   = (int)Mathf.Floor((target.Params.CharacterParam.Def + target.Params.ModifiedParam.Def) * target.Params.SkillModifiedParam.DefMagnification);
            int attackerAtk = (int)Mathf.Floor((attacker.Params.CharacterParam.Atk + attacker.Params.ModifiedParam.Atk) * attacker.Params.SkillModifiedParam.AtkMagnification);
            int changeHP    = (targetDef - attackerAtk);

            target.Params.TmpParam.SetExpectedHpChange( Mathf.Min(changeHP, 0), Mathf.Min(changeHP * attacker.Params.SkillModifiedParam.AtkNum, 0) );
        }

        /// <summary>
        /// 全てのキャラクターの一時パラメータをリセットします
        /// </summary>
        public void ResetTmpParamAllCharacter()
        {
            for (int i = 0; i < (int)CHARACTER_TAG.NUM; ++i)
            {
                if (_characterGroups.TryGetValue((CHARACTER_TAG)i, out var group))
                {
                    foreach (var c in group)
                    {
                        c.BePossibleAction();
                    }
                }
            }
        }

        /// <summary>
        /// 死亡キャラクターのキャラクタータグをリセットします
        /// </summary>
        public void ResetDiedCharacter()
        {
            _diedCharacterKey.CharacterTag      = CHARACTER_TAG.NONE;
            _diedCharacterKey.CharacterIndex    = -1;
        }

        /// <summary>
        /// 指定のキャラクター群のアクションゲージを回復させます
        /// </summary>
        /// <param name="tag">キャラクター群のタグ</param>
        public void RecoveryActionGaugeForGroup(CHARACTER_TAG tag)
        {
            if (_characterGroups.TryGetValue(tag, out var group))
            {
                foreach (var c in group)
                {
                    c.Params.CharacterParam.RecoveryActionGauge();
                }
            }
        }
    }
}