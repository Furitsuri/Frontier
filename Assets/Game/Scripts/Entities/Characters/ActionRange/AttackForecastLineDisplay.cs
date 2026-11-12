using System.Collections.Generic;
using Frontier.Battle;
using Frontier.Registries;
using Frontier.Stage;
using Frontier.UI;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// グリッドカーソルが指しているタイルを攻撃出来る敵対勢力のキャラクターから、そのタイルへ向けて攻撃予告線を表示するための共有シングルトンです。
    /// 「そのタイルに立つと、線を伸ばしているキャラクターから攻撃される可能性がある」ことを示します。
    /// タイル選択中(PlSelectTileState)と移動の操作中(PlMoveState)はカーソル位置、グループ移動の操作中(PlGroupMoveState)は各キャラクターの移動先、
    /// 移動を伴うスキルの対象選択中(PlSkillActionToTargetState)は着地先のタイルを対象とし、それらが変わる度に呼び出すことで、常に現在の状況に対応した表示を維持します。
    /// 線を伸ばしているキャラクターが画面から見切れている場合は、そのキャラクターの顔アイコンが線上に表示されます(AttackForecastIconPresenter)。
    ///
    /// 各キャラクターが攻撃出来るタイルは、初回の表示時に求めた結果を使い回します(カーソル移動の度に全員分を求め直すと負荷が大きいため)。
    /// この結果は「移動操作中のキャラクターが盤面上にいない」状態で求めたものです。
    /// 実際には移動操作中のキャラクターは移動先に立つため、その移動先が壁となって攻撃出来なくなる場合があります。
    /// 移動先を塞いでも攻撃出来るタイルが増えることはないため、使い回している結果で攻撃出来るとされ、かつ移動先を通過し得るキャラクターに絞って、
    /// 移動先を塞いだ状態で求め直します(絞り込みにより、求め直す対象は通常カーソル付近の数人に限られます)。
    ///
    /// キャラクターの位置等、状況が変わり得る箇所(ステートの終了・中断時、移動の取り消し時など)ではClearを呼び出して破棄してください。
    /// </summary>
    public class AttackForecastLineDisplay
    {
        /// <summary>
        /// 敵対勢力のキャラクター1人分の、攻撃出来るタイルの計算結果
        /// </summary>
        private class DangerEntry
        {
            public Character Attacker;
            // 移動操作中のキャラクターが盤面上にいない状態で求めた、移動した上で攻撃出来るタイル
            public HashSet<int> AttackableTiles;
            // 上記の計算時に通過・到達出来るタイル(立ち止まれないタイルも含む)
            public HashSet<int> MoveableTiles;
            // 今回のRefreshにおいて、移動先を塞いだ状態で求め直した攻撃出来るタイル。nullの場合は求め直していない
            public HashSet<int> RefinedAttackableTiles;
            // 今回のRefreshにおいて、求め直す必要があるかの判定を済ませたか
            public bool IsRefineChecked;
        }

        [Inject] private BattleRoutineController _btlRtnCtrl    = null;
        [Inject] private StageController _stageCtrl             = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private AttackForecastIconPresenter _iconPresenter = null;

        // 敵対勢力の各キャラクターの計算結果。nullの場合は未計算
        private List<DangerEntry> _dangerEntries = null;
        // _dangerEntriesの計算時の配置の差し替え(移動操作中のキャラクターを盤面上から取り除く)
        private readonly TileOccupancyOverride _baseOverride    = new TileOccupancyOverride();
        // 求め直す際の配置の差し替え(移動操作中のキャラクターを盤面上から取り除いた上で、各移動先に立たせる)
        private readonly TileOccupancyOverride _refineOverride  = new TileOccupancyOverride();
        // 移動操作中のキャラクターが立つ予定のタイル(求め直す必要があるかの判定に用いる)
        private readonly HashSet<int> _placedTiles = new HashSet<int>();
        // 移動操作中のキャラクターが1人の場合に、複数人用の処理へ渡すための使い回しの配列
        private readonly CharacterKey[] _singleMovingKeyBuffer = new CharacterKey[1];
        // 移動操作中のキャラクターがいない場合に渡す空の配列
        private static readonly CharacterKey[] NoMovingKeys = new CharacterKey[0];
        // 生成済みの線(表示する本数が増えた場合のみ追加で生成し、以降は使い回す)
        private readonly List<AttackForecastLine> _lines = new List<AttackForecastLine>();
        // 対象のタイルが1つの場合に、複数タイル用の処理へ渡すための使い回しの配列
        private readonly int[] _singleTargetBuffer = new int[1];
        // 表示中の線と、その線を伸ばしている攻撃側のキャラクターの組(顔アイコンの表示対象として渡すための使い回しのリスト)
        private readonly List<(AttackForecastLine line, Character attacker)> _shownLines = new List<(AttackForecastLine line, Character attacker)>();

        [Inject]
        public AttackForecastLineDisplay() { }

        /// <summary>
        /// 指定のタイルを攻撃出来る敵対勢力のキャラクターから、そのタイルへ向けて線を表示します。
        /// 指定のタイルに敵対勢力のキャラクターがいる場合は表示しません。
        /// 誰がそのタイルに立つかが定まっていない場合(タイル選択中など)に用います。盤面上のキャラクターは現在の配置のまま計算します。
        /// </summary>
        /// <param name="targetTileIndex">対象のタイル(グリッドカーソルが指しているタイルなど)</param>
        public void Refresh( int targetTileIndex )
        {
            _singleTargetBuffer[0] = targetTileIndex;
            Refresh( _singleTargetBuffer, NoMovingKeys );
        }

        /// <summary>
        /// 指定のタイルを攻撃出来る敵対勢力のキャラクターから、そのタイルへ向けて線を表示します。
        /// 移動操作中のキャラクターは、移動前の位置から離れて指定のタイルに立つものとして計算します。
        /// </summary>
        /// <param name="targetTileIndex">対象のタイル(移動先の候補、スキルの着地先など)</param>
        /// <param name="movingCharaKey">移動操作中のキャラクター</param>
        public void Refresh( int targetTileIndex, CharacterKey movingCharaKey )
        {
            _singleTargetBuffer[0]    = targetTileIndex;
            _singleMovingKeyBuffer[0] = movingCharaKey;
            Refresh( _singleTargetBuffer, _singleMovingKeyBuffer );
        }

        /// <summary>
        /// 指定の各タイルについて、そのタイルを攻撃出来る敵対勢力のキャラクターから、そのタイルへ向けて線を表示します。
        /// 敵対勢力のキャラクターがいるタイルに対しては表示しません。
        /// 移動操作中のキャラクターは、移動前の位置から離れて、対応する対象のタイルに立つものとして計算します
        /// (移動前の位置に立ったままの扱いにすると、そこが壁となって実際には攻撃されるタイルが安全と表示され、
        /// 移動先を空きのままの扱いにすると、そこを通過して回り込む経路により、実際には攻撃されないタイルに線が表示されてしまうため)。
        /// </summary>
        /// <param name="targetTileIndices">対象のタイル(グループ移動での各キャラクターの移動先など)</param>
        /// <param name="movingCharaKeys">移動操作中のキャラクター。movingCharaKeys[i]がtargetTileIndices[i]に立つものとします。空の場合は現在の配置のまま計算します</param>
        public void Refresh( IReadOnlyList<int> targetTileIndices, IReadOnlyList<CharacterKey> movingCharaKeys )
        {
            Debug.Assert( 0 == movingCharaKeys.Count || targetTileIndices.Count == movingCharaKeys.Count, "移動操作中のキャラクターの数が、対象のタイルの数と一致していません。" );

            int lineCount = 0;
            _shownLines.Clear();

            PrepareRefineOverride( targetTileIndices, movingCharaKeys );

            foreach( int targetTileIndex in targetTileIndices )
            {
                if( IsOpponentOfPlayer( _stageCtrl.GetTileDynamicData( targetTileIndex ).CharaKey ) ) { continue; }

                // 未計算、または前回の計算時から移動操作中のキャラクターが変わった場合は計算し直す
                if( null == _dangerEntries || !_baseOverride.HasSameIgnored( movingCharaKeys ) )
                {
                    _baseOverride.Clear();
                    foreach( var key in movingCharaKeys ) { _baseOverride.AddIgnored( key ); }
                    _dangerEntries = ExtractDangerEntries( _baseOverride );
                }

                var targetPos = _stageCtrl.GetTileStaticData( targetTileIndex ).CursorStandPos;

                foreach( var entry in _dangerEntries )
                {
                    // 表示中に撃破等で戦闘から離脱したキャラクターは対象外とする
                    if( null == entry.Attacker || null == entry.Attacker.BattleLogic ) { continue; }
                    // 使い回している結果で攻撃出来ないタイルは、移動先を塞いでも攻撃出来ないため、求め直さずに対象外とする
                    if( !entry.AttackableTiles.Contains( targetTileIndex ) ) { continue; }
                    // 移動先を塞いだことで攻撃出来なくなる場合は対象外とする
                    if( !GetEffectiveAttackableTiles( entry ).Contains( targetTileIndex ) ) { continue; }

                    if( _lines.Count <= lineCount )
                    {
                        _lines.Add( _hierarchyBld.CreateComponentAndOrganize<AttackForecastLine>( _prefabReg.AttackForecastLinePrefab, false ) );
                    }
                    var line = _lines[lineCount++];
                    line.Show( entry.Attacker.transform.position, targetPos );
                    _shownLines.Add( (line, entry.Attacker) );
                }
            }

            for( int i = lineCount; i < _lines.Count; ++i )
            {
                _lines[i].Hide();
            }

            // 線を伸ばしているキャラクターが画面から見切れている場合に、線上へ顔アイコンを表示させる
            _iconPresenter.SetTargets( _shownLines );
        }

        /// <summary>
        /// 表示中の線を全て非表示にし、各キャラクターが攻撃出来るタイルの計算結果を破棄します
        /// </summary>
        public void Clear()
        {
            foreach( var line in _lines )
            {
                // シーンの破棄等で既に破棄されている場合は対象外とする
                if( null != line ) { line.Hide(); }
            }
            _shownLines.Clear();
            _iconPresenter.Clear();

            _dangerEntries = null;
            _baseOverride.Clear();
            _refineOverride.Clear();
            _placedTiles.Clear();
        }

        /// <summary>
        /// 今回のRefreshで用いる、移動先を塞いだ状態の配置の差し替えを準備し、前回のRefreshで求め直した結果を破棄します
        /// </summary>
        private void PrepareRefineOverride( IReadOnlyList<int> targetTileIndices, IReadOnlyList<CharacterKey> movingCharaKeys )
        {
            _refineOverride.Clear();
            _placedTiles.Clear();

            if( 0 < movingCharaKeys.Count )
            {
                foreach( var key in movingCharaKeys ) { _refineOverride.AddIgnored( key ); }

                for( int i = 0; i < targetTileIndices.Count; ++i )
                {
                    int tileIndex = targetTileIndices[i];
                    // 敵対勢力のキャラクターがいるタイルには立てない(線も表示しない)ため、塞ぐ対象から除く
                    if( IsOpponentOfPlayer( _stageCtrl.GetTileDynamicData( tileIndex ).CharaKey ) ) { continue; }

                    _refineOverride.AddPlaced( tileIndex, movingCharaKeys[i] );
                    _placedTiles.Add( tileIndex );
                }
            }

            if( null == _dangerEntries ) { return; }

            foreach( var entry in _dangerEntries )
            {
                entry.RefinedAttackableTiles = null;
                entry.IsRefineChecked        = false;
            }
        }

        /// <summary>
        /// 移動先を塞いだ状態を考慮した、攻撃出来るタイルを取得します。
        /// 移動先を通過・到達し得ないキャラクターは、塞いでも結果が変わらないため、使い回している結果をそのまま返します。
        /// 求め直した結果は、今回のRefresh内の他の対象のタイルの判定でも使い回します。
        /// </summary>
        private HashSet<int> GetEffectiveAttackableTiles( DangerEntry entry )
        {
            if( !entry.IsRefineChecked )
            {
                entry.IsRefineChecked = true;

                if( 0 < _placedTiles.Count && entry.MoveableTiles.Overlaps( _placedTiles ) )
                {
                    var (attackable, _) = entry.Attacker.BattleLogic.ActionRangeCtrl.ExtractDangerRangeTileIndices( _refineOverride );
                    entry.RefinedAttackableTiles = new HashSet<int>( attackable );
                }
            }

            return entry.RefinedAttackableTiles ?? entry.AttackableTiles;
        }

        /// <summary>
        /// 敵対勢力の各キャラクターについて、現在の位置から移動した上で攻撃出来るタイルと、その際に通過・到達出来るタイルを求めます
        /// </summary>
        /// <param name="occupancyOverride">配置の差し替え(移動操作中のキャラクターを盤面上から取り除く)</param>
        private List<DangerEntry> ExtractDangerEntries( TileOccupancyOverride occupancyOverride )
        {
            var entries = new List<DangerEntry>();

            foreach( Character character in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                if( null == character.BattleLogic ) { continue; }

                var (attackable, moveable) = character.BattleLogic.ActionRangeCtrl.ExtractDangerRangeTileIndices( occupancyOverride );
                entries.Add( new DangerEntry
                {
                    Attacker        = character,
                    AttackableTiles = new HashSet<int>( attackable ),
                    MoveableTiles   = new HashSet<int>( moveable ),
                } );
            }

            return entries;
        }

        /// <summary>
        /// プレイヤーにとっての敵対勢力のキャラクターか否かを取得します
        /// </summary>
        private static bool IsOpponentOfPlayer( in CharacterKey key )
        {
            return key.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) CHARACTER_TAG.PLAYER]( key.CharacterTag );
        }
    }
}
