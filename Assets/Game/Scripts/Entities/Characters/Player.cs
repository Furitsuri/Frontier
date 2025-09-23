using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities.Ai;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

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

            /// <summary>
            /// 情報をリセットします
            /// </summary>
            public void Reset()
            {
                tmpParam.Reset();
                rotDir = Quaternion.identity;
            }
        }

        private bool _isPrevMoving = false;
        private PrevMoveInfo _prevMoveInfo;
        public ref PrevMoveInfo PrevMoveInformaiton => ref _prevMoveInfo;

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            _prevMoveInfo.tmpParam  = _params.TmpParam.Clone();
            _prevMoveInfo.rotDir    = _transformHdlr.GetRotation();
        }

        /// <summary>
        /// 移動前情報をリセットします
        /// </summary>
        public void ResetPrevMoveInfo()
        {
            _prevMoveInfo.Reset();
        }

        /// <summary>
        /// コマンドの可否や位置を以前の状態に巻き戻します
        /// </summary>
        public void RewindToPreviousState()
        {
            _params.TmpParam = _prevMoveInfo.tmpParam;
            SetPosition( _params.TmpParam.gridIndex, _prevMoveInfo.rotDir );
        }

        /// <summary>
        /// 移動後などに直前のコマンド状態に戻れるかどうかを取得します
        /// </summary>
        /// <returns>直前のコマンドに戻れるか否か</returns>
        public bool IsRewindStatePossible()
        {
            // 移動コマンドだけが終了している場合のみ直前の状態に戻れるように
            // MEMO : コマンドが今後増えても問題ないようにfor文で判定しています
            bool isPossible = true;
            for( int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i )
            {
                if( i == (int)Command.COMMAND_TAG.MOVE )
                {
                    if (!_params.TmpParam.IsEndCommand(Command.COMMAND_TAG.MOVE))
                    {
                        isPossible = false;
                        break;
                    }
                }
                else
                {
                    if (_params.TmpParam.IsEndCommand((Command.COMMAND_TAG)i))
                    {
                        isPossible = false;
                        break;
                    }
                }
            }

            return isPossible;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();
            _baseAi = _hierarchyBld.InstantiateWithDiContainer<AiBase>( false );
            NullCheck.AssertNotNull( _baseAi , nameof( _baseAi ) );
            _baseAi.Init( this );
            _baseAi.MovePathHandler.Init( this );
        }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        /// <returns>切替の有無</returns>
        override public bool ToggleUseSkillks(int index)
        {
            _params.TmpParam.isUseSkills[index] = !_params.TmpParam.isUseSkills[index];

            int skillID = (int)_params.CharacterParam.equipSkills[index];
            var skillData = SkillsData.data[skillID];

            if (_params.TmpParam.isUseSkills[index])
            {
                _params.CharacterParam.consumptionActionGauge += skillData.Cost;
                Params.SkillModifiedParam.AtkNum += skillData.AddAtkNum;
                Params.SkillModifiedParam.AtkMagnification += skillData.AddAtkMag;
                Params.SkillModifiedParam.DefMagnification += skillData.AddDefMag;
            }
            else
            {
                _params.CharacterParam.consumptionActionGauge -= skillData.Cost;
                Params.SkillModifiedParam.AtkNum -= skillData.AddAtkNum;
                Params.SkillModifiedParam.AtkMagnification -= skillData.AddAtkMag;
                Params.SkillModifiedParam.DefMagnification -= skillData.AddDefMag;
            }

            _btlRtnCtrl.BtlUi.GetPlayerParamSkillBox(index).SetFlickEnabled(_params.TmpParam.isUseSkills[index]);

            return true;
        }

        /// <summary>
        /// プレイヤーキャラクターを作成したパスに沿って移動させます
        /// </summary>
        /// <param name="moveSpeedRate">移動速度レート</param>
        /// <returns>移動が終了したか</returns>
        override public bool UpdateMovePath( float moveSpeedRate = 1.0f )
        {
            var pathHdlr = _baseAi.MovePathHandler;

            // 移動ルートの最終インデックスに到達している場合は、目標タイルに到達しているため終了
            if( pathHdlr.IsEndPathTrace() ) { return true; }

            bool toggleAnimation    = false;
            var focusedTileData     = pathHdlr.GetFocusedTileData();
            var focusedTileInfo     = pathHdlr.GetFocusedTileInformation();
            var focusedTilePos      = focusedTileInfo.charaStandPos;
            Vector3 prevDirXZ       = ( focusedTilePos - _transformHdlr.GetPreviousPosition() ).XZ().normalized;
            Vector3 focusDirXZ      = ( focusedTilePos - _transformHdlr.GetPosition() ).XZ().normalized;
            Action<float, float, Vector3, Vector3> jumpAction = ( float dprtHeight, float destHeight, Vector3 dprtPos, Vector3 destPos ) =>
            {
                // 高低差が一定以上ある場合はジャンプ動作を開始
                if( NEED_JUMP_HEIGHT_DIFFERENCE <= ( int ) Math.Abs( destHeight - dprtHeight ) )
                {
                    _transformHdlr.StartJump( in dprtPos, in destPos, moveSpeedRate );
                }
            };

            // 現在の目標タイルに到達している場合はインデックス値をインクリメントすることで目標タイルを更新する
            if( Vector3.Dot( prevDirXZ, focusDirXZ ) <= 0 )
            {
                _transformHdlr.SetPosition( focusedTilePos );   // 位置を目標タイルに合わせる
                _transformHdlr.ResetVelocityAcceleration();     // 速度、加速度をリセット
                _params.TmpParam.gridIndex  = pathHdlr.GetFocusedWaypointIndex();    // キャラクターが保持するタイルインデックスを更新
                pathHdlr.IncrementFocusedWaypointIndex();                            // 目標インデックス値をインクリメントして次の目標タイルに更新

                // 最終インデックスに到達している場合は移動アニメーションを停止して終了
                if( pathHdlr.IsEndPathTrace() )
                {
                    if( _isPrevMoving ) { toggleAnimation = true; }

                    _isPrevMoving = false;
                }
                // まだ移動が続く場合は次の目標タイルを目指して速度と向きを設定
                else
                {
                    var nextTileData    = pathHdlr.GetFocusedTileData();
                    var nextTileInfo    = pathHdlr.GetFocusedTileInformation();
                    var nextTilePos     = nextTileInfo.charaStandPos;
                    Vector3 nextDirXZ   = ( nextTilePos - _transformHdlr.GetPosition() ).XZ().normalized;

                    _transformHdlr.SetVelocityAcceleration( nextDirXZ * CHARACTER_MOVE_SPEED * moveSpeedRate, Vector3.zero );
                    _transformHdlr.SetRotation( Quaternion.LookRotation( nextDirXZ ) );

                    jumpAction( focusedTileData.Height, nextTileData.Height, focusedTilePos, nextTilePos );
                }
            }
            else
            {
                // 移動開始の場合は速度と向きを設定
                if( !_isPrevMoving )
                {
                    var currentTileData    = _stageCtrl.GetTileData( Params.TmpParam.gridIndex );
                    var currentTileInfo    = _stageCtrl.GetTileInfo( Params.TmpParam.gridIndex );

                    _transformHdlr.SetVelocityAcceleration( focusDirXZ * CHARACTER_MOVE_SPEED * moveSpeedRate, Vector3.zero );
                    _transformHdlr.SetRotation( Quaternion.LookRotation( focusDirXZ ) );
                    toggleAnimation = true;

                    jumpAction( currentTileData.Height, focusedTileData.Height, currentTileInfo.charaStandPos, focusedTilePos );
                }

                _isPrevMoving = true;
            }

            if( toggleAnimation ) { AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.MOVE, _isPrevMoving ); } // 移動アニメーションの切替

            return !_isPrevMoving;
        }
    }
}