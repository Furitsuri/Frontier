using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// レベルアップ画面における「仮の割り振り」状態を保持します。
    /// 実際のCharacter.Status/UserDomain.Animaはここでは一切書き換えず、
    /// 決定(OK)操作が行われた際にLevelUpHandlerが本データへ反映します。
    /// 能力値(MaxHP/Atk/Def等)の成長はステータス上昇画面(StatusUpContext)側の役割のため、
    /// このクラスはレベル・消費するアニマ・そのレベルアップで得られるStatusPointのみを扱う。
    /// </summary>
    public class LevelUpContext
    {
        public Character Character { get; }
        public int OriginalLevel { get; }
        public int OriginalAnima { get; }
        public int OriginalStatusPoint { get; }

        public int TentativeLevel { get; private set; }
        public int TentativeAnima { get; private set; }
        public int TentativeStatusPoint { get; private set; }

        // Increase()で実際に加算したStatusPoint量(Constants.STATUS_POINT_MAXでクランプ済み)の履歴。
        // Decrease()で正確に同じ量を取り消すために、レベル毎の付与量を再計算せずここから取り出す
        // (上限付近では「本来の付与量」と「実際に加算できた量」がずれるため)。
        private readonly List<int> _grantedStatusPointHistory = new List<int>();

        /// <param name="character">レベルアップ対象のキャラクター</param>
        /// <param name="currentAnima">部隊共有ポイントの現在値(UserDomain.Anima)</param>
        public LevelUpContext( Character character, int currentAnima )
        {
            Character = character;

            var status = character.GetStatusRef;
            OriginalLevel       = status.Level;
            OriginalAnima       = currentAnima;
            OriginalStatusPoint = status.StatusPoint;

            TentativeLevel       = OriginalLevel;
            TentativeAnima       = currentAnima;
            TentativeStatusPoint = status.StatusPoint;
        }

        /// <summary>
        /// 現在の仮レベルから次のレベルへ上げるために必要なアニマ数を返します。
        /// 最大レベルの場合は0を返します。
        /// </summary>
        public int GetNextLevelCost()
        {
            if ( LevelExpData.IsMaxLevel( TentativeLevel ) ) return 0;

            return LevelExpData.data[TentativeLevel + 1].RequiredTotalExp - LevelExpData.data[TentativeLevel].RequiredTotalExp;
        }

        public bool CanIncrease()
        {
            if ( LevelExpData.IsMaxLevel( TentativeLevel ) ) return false;

            return TentativeAnima >= GetNextLevelCost();
        }

        /// <summary>
        /// 現在のキャラクターの実レベル(OriginalLevel)より下げることはできません。
        /// </summary>
        public bool CanDecrease()
        {
            return OriginalLevel < TentativeLevel;
        }

        /// <summary>
        /// 仮レベルを1つ上げます(必要アニマが消費され、そのレベル到達で得られるStatusPointが加算されます)。
        /// 上げられなかった場合はfalseを返します。
        /// </summary>
        public bool Increase()
        {
            if ( !CanIncrease() ) return false;

            TentativeAnima -= GetNextLevelCost();
            TentativeLevel++;

            int granted = LevelUpStatusPointData.GetGrantedStatusPoint( TentativeLevel );
            int actualGranted = Mathf.Min( granted, Constants.STATUS_POINT_MAX - TentativeStatusPoint );
            TentativeStatusPoint += actualGranted;
            _grantedStatusPointHistory.Add( actualGranted );

            return true;
        }

        /// <summary>
        /// 仮レベルを1つ下げます(消費したアニマが払い戻され、そのレベル到達で得ていたStatusPointも取り消されます)。
        /// 下げられなかった場合はfalseを返します。
        /// </summary>
        public bool Decrease()
        {
            if ( !CanDecrease() ) return false;

            int lastIndex = _grantedStatusPointHistory.Count - 1;
            TentativeStatusPoint -= _grantedStatusPointHistory[lastIndex];
            _grantedStatusPointHistory.RemoveAt( lastIndex );

            int refund = LevelExpData.data[TentativeLevel].RequiredTotalExp - LevelExpData.data[TentativeLevel - 1].RequiredTotalExp;
            TentativeAnima += refund;
            TentativeLevel--;

            return true;
        }
    }
}
