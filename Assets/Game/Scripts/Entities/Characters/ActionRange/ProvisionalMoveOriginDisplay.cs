using System.Collections.Generic;
using Frontier.Battle;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// 暫定的に移動している(移動が確定していない)キャラクターの「移動前の位置」を、どのキャラクターの分まで表示するかを決める共有シングルトンです。
    /// 表示そのものは、各キャラクターが保持する移動操作(PlayerMoveOperation)が自身の分を行います。
    /// ・タイル選択中: カーソルを合わせたキャラクターの分のみを、経路の矢印付きで表示します(ShowHovered)。
    /// ・他のキャラクターの移動中や、移動を伴うスキルの対象選択中: どこが他のキャラクターの移動前のタイルなのかが分かるよう、
    ///   操作中のキャラクターを除く全員の分を表示します(ShowAllExcept)。複数人分の矢印が並ぶと煩わしいため、経路の矢印は表示しません。
    /// 表示内容は呼び出された時点の状況で決まるため、状況が変わり得る箇所(カーソル移動時、ステートの開始・終了時など)で呼び出してください。
    /// </summary>
    public class ProvisionalMoveOriginDisplay
    {
        [Inject] private BattleRoutineController _btlRtnCtrl = null;

        // 現在、移動前の位置を表示させているキャラクター
        private readonly List<Player> _shownPlayers = new List<Player>();
        private bool _isPathVisible = false;

        [Inject]
        public ProvisionalMoveOriginDisplay() { }

        /// <summary>
        /// カーソルが指しているキャラクターが暫定的に移動している場合のみ、その移動前の位置を経路の矢印付きで表示します。
        /// 対象なし、または暫定的に移動していないキャラクターの場合は表示を消去します。
        /// </summary>
        public void ShowHovered( Character hoveredCharacter )
        {
            var targets = new List<Player>();
            if( hoveredCharacter is Player player && IsDisplayable( player ) ) { targets.Add( player ); }

            Apply( targets, true );
        }

        /// <summary>
        /// 暫定的に移動している全キャラクターの移動前の位置を、経路の矢印なし(残像と外枠のみ)で表示します。
        /// </summary>
        /// <param name="actingPlayer">操作中のキャラクター(表示の対象から除外します。自身の移動前の位置は、自身の移動操作が表示するため)</param>
        public void ShowAllExcept( Player actingPlayer )
        {
            var targets = new List<Player>();
            foreach( Player player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ) )
            {
                if( player == actingPlayer || !IsDisplayable( player ) ) { continue; }

                targets.Add( player );
            }

            Apply( targets, false );
        }

        /// <summary>
        /// 表示中の移動前の位置を全て消去します
        /// </summary>
        public void Clear()
        {
            foreach( var player in _shownPlayers )
            {
                HideOf( player );
            }
            _shownPlayers.Clear();
        }

        /// <summary>
        /// 表示対象を指定の内容に合わせます(対象から外れたキャラクターの表示を消去し、新たに対象となったキャラクターの表示を開始します)
        /// </summary>
        private void Apply( List<Player> targets, bool isPathVisible )
        {
            // 経路の矢印の表示有無が変わる場合は、表示し直すため一度全て消去する
            if( _isPathVisible != isPathVisible ) { Clear(); }
            _isPathVisible = isPathVisible;

            for( int i = _shownPlayers.Count - 1; 0 <= i; --i )
            {
                // 対象から外れた場合に加え、表示中に暫定移動の状態でなくなった場合(移動の取り消し等)も消去する
                if( targets.Contains( _shownPlayers[i] ) ) { continue; }

                HideOf( _shownPlayers[i] );
                _shownPlayers.RemoveAt( i );
            }

            foreach( var player in targets )
            {
                if( _shownPlayers.Contains( player ) ) { continue; }

                player.MoveOperation.ShowProvisionalOrigin( isPathVisible );
                _shownPlayers.Add( player );
            }
        }

        /// <summary>
        /// 移動前の位置を表示出来るキャラクターか(戦闘に参加しており、暫定的に移動しているか)を取得します
        /// </summary>
        private static bool IsDisplayable( Player player )
        {
            return null != player && null != player.BattleLogic && player.IsProvisionallyMoved();
        }

        private static void HideOf( Player player )
        {
            // 撃破等で既に戦闘から離脱しているキャラクターは対象外とする
            if( null == player || null == player.BattleLogic ) { return; }

            player.MoveOperation.HideProvisionalOrigin();
        }
    }
}
