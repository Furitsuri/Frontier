using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using static InputCode;

namespace Frontier.StateMachine
{
    /// <summary>
    /// 配置フェーズ：タイル選択状態
    /// </summary>
    public class PlacementSelectTile : PhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_STATUS = 0,
            CONFIRM_COMPLETED,
        }

        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        override public void Init()
        {
            base.Init();

            _stageCtrl.SetGridCursorControllerActive( true );   // グリッド選択を有効化
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            return ( 0 <= TransitIndex );
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "MOVE", CanAcceptDefault, new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, "PLACE CHARACTER", CanAcceptDefault, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.INFO, "STATUS", CanAcceptInfo, new AcceptBooleanInput( AcceptInfo ), 0.0f, hashCode),
               (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE CHARACTER", new EnableCallback[] { CanAcceptDefault, CanAcceptDefault }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptSub1 ), new AcceptBooleanInput( AcceptSub1 ) }, 0.0f, hashCode),
               (GuideIcon.OPT2, "COMPLETE", CanAcceptOptional, new AcceptBooleanInput( AcceptOptional ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// グリッドカーソルがキャラクターを選択している場合はステータス情報表示を受け付けます
        /// </summary>
        /// <returns></returns>
        override protected bool CanAcceptInfo()
        {
            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            return true;
        }

        /// <summary>
        /// 配置キャラクターが1体以上いる場合は配置完了入力を受け付けます
        /// </summary>
        /// <returns></returns>
        override protected bool CanAcceptOptional()
        {
            return true;
        }

        /// <summary>
        /// 方向入力を受け取り、選択グリッドを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection( Direction dir )
        {
            return _stageCtrl.OperateGridCursorController( dir );
        }

        /// <summary>
        /// キャラクターを選択したタイルに配置します
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) return false;

            return true;
        }

        /// <summary>
        /// キャラクターのステータス情報を表示します
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptInfo( bool isInput )
        {
            if( !isInput ) return false;

            TransitIndex = ( int ) TransitTag.CHARACTER_STATUS;

            return true;
        }

        /// <summary>
        /// キャラクター選択を前に進めます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptSub1( bool isInput )
        {
            if( !isInput ) return false;

            return true;
        }

        /// <summary>
        /// キャラクター選択を後ろに進めます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptSub2( bool isInput )
        {
            if( !isInput ) return false;

            return true;
        }

        /// <summary>
        /// OPTION入力を受けた際に配置確認画面へ遷移させます
        /// </summary>
        /// <param name="isOptional"></param>
        /// <returns></returns>
        override protected bool AcceptOptional( bool isOptional )
        {
            if( !isOptional ) return false;

            TransitIndex = ( int ) TransitTag.CONFIRM_COMPLETED;

            return true;
        }
    }
}