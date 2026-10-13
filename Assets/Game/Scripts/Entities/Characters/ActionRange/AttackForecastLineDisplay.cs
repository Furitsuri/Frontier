using System.Collections.Generic;
using Frontier.Battle;
using Frontier.Registries;
using Frontier.Stage;
using Frontier.UI;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// グリッドカーソルが指しているタイルを攻撃出来る敵対勢力のキャラクターから、そのタイルへ向けて攻撃予告線を表示するための共有シングルトンです。
    /// 「そのタイルに立つと、線を伸ばしているキャラクターから攻撃される可能性がある」ことを示します。
    /// タイル選択中(PlSelectTileState)と移動の操作中(PlMoveState)はカーソル位置、グループ移動の操作中(PlGroupMoveState)は各キャラクターの移動先、
    /// 移動を伴うスキルの対象選択中(PlSkillActionToTargetState)は着地先のタイルを対象とし、それらが変わる度に呼び出すことで、常に現在の状況に対応した表示を維持します。
    /// 線を伸ばしているキャラクターが画面から見切れている場合は、そのキャラクターの顔アイコンが線上に表示されます(AttackForecastIconPresenter)。
    /// 各キャラクターが攻撃出来るタイルは、初回の表示時に求めた結果を使い回します(カーソル移動の度に全員分を求め直すと負荷が大きいため)。
    /// キャラクターの位置等、状況が変わり得る箇所(ステートの終了・中断時、移動の取り消し時など)ではClearを呼び出して破棄してください。
    /// </summary>
    public class AttackForecastLineDisplay
    {
        [Inject] private BattleRoutineController _btlRtnCtrl    = null;
        [Inject] private StageController _stageCtrl             = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private AttackForecastIconPresenter _iconPresenter = null;

        // 敵対勢力の各キャラクターが、移動した上で攻撃出来るタイル。nullの場合は未計算
        private List<(Character attacker, HashSet<int> tileIndices)> _dangerTiles = null;
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
        /// </summary>
        /// <param name="targetTileIndex">対象のタイル(グリッドカーソルが指しているタイル、スキルの着地先など)</param>
        public void Refresh( int targetTileIndex )
        {
            _singleTargetBuffer[0] = targetTileIndex;
            Refresh( _singleTargetBuffer );
        }

        /// <summary>
        /// 指定の各タイルについて、そのタイルを攻撃出来る敵対勢力のキャラクターから、そのタイルへ向けて線を表示します。
        /// 敵対勢力のキャラクターがいるタイルに対しては表示しません。
        /// </summary>
        /// <param name="targetTileIndices">対象のタイル(グループ移動での各キャラクターの移動先など)</param>
        public void Refresh( IReadOnlyList<int> targetTileIndices )
        {
            int lineCount = 0;
            _shownLines.Clear();

            foreach( int targetTileIndex in targetTileIndices )
            {
                if( IsOpponentOfPlayer( _stageCtrl.GetTileDynamicData( targetTileIndex ).CharaKey ) ) { continue; }

                if( null == _dangerTiles ) { _dangerTiles = ExtractDangerTiles(); }

                var targetPos = _stageCtrl.GetTileStaticData( targetTileIndex ).CursorStandPos;

                foreach( var (attacker, tileIndices) in _dangerTiles )
                {
                    // 表示中に撃破等で戦闘から離脱したキャラクターは対象外とする
                    if( null == attacker || null == attacker.BattleLogic ) { continue; }
                    if( !tileIndices.Contains( targetTileIndex ) ) { continue; }

                    if( _lines.Count <= lineCount )
                    {
                        _lines.Add( _hierarchyBld.CreateComponentAndOrganize<AttackForecastLine>( _prefabReg.AttackForecastLinePrefab, false ) );
                    }
                    var line = _lines[lineCount++];
                    line.Show( attacker.transform.position, targetPos );
                    _shownLines.Add( (line, attacker) );
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

            _dangerTiles = null;
        }

        /// <summary>
        /// 敵対勢力の各キャラクターについて、現在の位置から移動した上で攻撃出来るタイルを求めます
        /// </summary>
        private List<(Character, HashSet<int>)> ExtractDangerTiles()
        {
            var dangerTiles = new List<(Character, HashSet<int>)>();

            foreach( Character character in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                if( null == character.BattleLogic ) { continue; }

                dangerTiles.Add( (character, new HashSet<int>( character.BattleLogic.ActionRangeCtrl.ExtractDangerTileIndices() )) );
            }

            return dangerTiles;
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
