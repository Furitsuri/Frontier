using Frontier.Combat;
using Frontier.FormTroop;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    public class Player : Character
    {
        /// <summary>
        /// プレイヤーキャラクターが移動を開始する前の情報です
        /// 移動後に状態を巻き戻す際に使用します
        /// </summary>
        public struct PrevMoveInfo
        {
            public TemporaryParameter tmpParam;
            public Quaternion rotDir;
            // 移動前の位置から移動後の位置までに実際に通った経路(出発タイルを含まない)。経路を保持しない移動の場合はnull
            public List<WaypointInformation> movedPath;

            /// <summary>
            /// 情報をリセットします
            /// </summary>
            public void Reset()
            {
                tmpParam.Reset();
                rotDir      = Quaternion.identity;
                movedPath   = null;
            }
        }

        private RecruitLogic _recruitLogic;

        public RecruitLogic RecruitLogic => _recruitLogic;
        public ref PrevMoveInfo PrevMoveInformaiton => ref ( ( PlayerBattleLogic )_battleLogic ).PrevMoveInformaiton;

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            ( ( PlayerBattleLogic ) _battleLogic ).HoldBeforeMoveInfo();
        }

        /// <summary>
        /// 移動で実際に通った経路を保持します(移動前の位置を示す目印の経路表示に使用します)。
        /// HoldBeforeMoveInfoの後に呼び出してください。
        /// </summary>
        public void HoldMovedPath( IReadOnlyList<WaypointInformation> path )
        {
            PrevMoveInformaiton.movedPath = ( null == path ) ? null : new List<WaypointInformation>( path );
        }

        public void PushCommandHistory( COMMAND_TAG commandTag )
        {
            ( ( PlayerBattleLogic ) _battleLogic ).PushCommandHistory( commandTag );
        }

        public COMMAND_TAG PopCommandHistory()
        {
            var prevExecCommandTag = ( ( PlayerBattleLogic ) _battleLogic ).PopCommandHistory();
            if( prevExecCommandTag == COMMAND_TAG.NONE ) { return COMMAND_TAG.NONE; }

            // コマンド履歴から直前のコマンドを取得して、コマンドの状態を巻き戻す(ただし攻撃、待機を行っている場合は不可)
            RevertToPreviousExecCommand( prevExecCommandTag );

            return prevExecCommandTag;
        }

        public void ClearCommandHistory()
        {
            ( ( PlayerBattleLogic ) _battleLogic ).ClearCommandHistory();
        }

        /// <summary>
        /// 指定コマンドを実行完了として確定します。以前の状態には戻せなくなるため、
        /// 攻撃・スキルなど実行し終えたコマンドに対して呼び出してください。
        /// </summary>
        public void FinalizeCommand( COMMAND_TAG commandTag )
        {
            ( ( PlayerBattleLogic ) _battleLogic ).FinalizeCommand( commandTag );
        }

        /// <summary>
        /// コマンドの可否や位置を以前の状態に巻き戻します
        /// </summary>
        public void RevertBeforeMoving()
        {
            ( ( PlayerBattleLogic ) _battleLogic ).RevertBeforeMoving();
        }

        public void OnRecruitEnter( int cost )
        {
            LazyInject.GetOrCreate( ref _recruitLogic, () => _hierarchyBld.InstantiateWithDiContainer<RecruitLogic>( false ) );
            _recruitLogic.Setup( this, cost );
        }

        public void OnRecruitExit()
        {
            _recruitLogic.Dispose();
            _recruitLogic = null;
        }

        /// <summary>
        /// 移動後などに直前のコマンド状態に戻れるかどうかを取得します
        /// </summary>
        /// <returns>直前のコマンドに戻れるか否か</returns>
        public bool IsEnableRevertState()
        {
            var playerBattleLogic = ( PlayerBattleLogic ) _battleLogic;

            // コマンド履歴が存在しない場合は巻き戻せない
            if( playerBattleLogic.GetCommandHistoryCount() <= 0 ) { return false; }
            // 攻撃コマンドが履歴に存在する場合は巻き戻せない
            if( playerBattleLogic.IsContainsCommandHistory( COMMAND_TAG.ATTACK ) ) { return false; }
            // スキルコマンドが履歴に存在する場合は巻き戻せない
            if( playerBattleLogic.IsContainsCommandHistory( COMMAND_TAG.SKILL ) ) { return false; }
            // 待機コマンドが履歴に存在する場合は巻き戻せない
            if( playerBattleLogic.IsContainsCommandHistory( COMMAND_TAG.WAIT ) ) { return false; }

            return true;
        }

        /// <summary>
        /// 暫定的に移動している(移動後、攻撃・スキル・待機の実行やターン終了による確定がされておらず、
        /// キャンセルで移動前に戻せる)状態かどうかを取得します
        /// </summary>
        public bool IsProvisionallyMoved()
        {
            if( null == _battleLogic ) { return false; }

            return IsEnableRevertState() && ( ( PlayerBattleLogic ) _battleLogic ).IsContainsCommandHistory( COMMAND_TAG.MOVE );
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init()
        {
            base.Init();
        }

        public override void Dispose()
        {
            _recruitLogic?.Dispose();
            _recruitLogic = null;

            base.Dispose();
        }

        public override void OnFieldEnter()
        {
            base.OnFieldEnter();
            LazyInject.GetOrCreate( ref _fieldLogic, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<PlayerFieldLogic>( gameObject, true, false, "FieldLogic" ) );
            _fieldLogic.Setup();
            _fieldLogic.Regist( this );
            _fieldLogic.Init();
        }

        public override void OnBattleEnter( BattleCameraController btlCamCtrl )
        {
            LazyInject.GetOrCreate( ref _battleLogic, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<PlayerBattleLogic>( gameObject, true, false, "BattleLogic" ) );
            _battleLogic.Setup();
            _battleLogic.Regist( this );
            _battleLogic.Init();

            base.OnBattleEnter( btlCamCtrl );   // 基底クラスのOnBattleEnterは最後に呼ぶ
        }

        public override void OnBattleExit()
        {
            _battleLogic.Dispose();
            _battleLogic = null;
            base.OnBattleExit();
        }

        private void RevertToPreviousExecCommand( COMMAND_TAG commandTag )
        {
            ( ( PlayerBattleLogic ) _battleLogic ).RevertToPreviousExecCommand( commandTag );
        }
    }
}