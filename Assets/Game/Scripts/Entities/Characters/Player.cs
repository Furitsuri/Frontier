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
        private Vector3 _movementDestination = Vector3.zero;
        private PrevMoveInfo _prevMoveInfo;
        public ref PrevMoveInfo PrevMoveInformaiton => ref _prevMoveInfo;

        /// <summary>
        /// プレイヤーキャラクターを作成したパスに沿って移動させます
        /// </summary>
        /// <param name="moveSpeedRate">移動速度レート</param>
        /// <returns>移動が終了したか</returns>
        public bool UpdateMovePath( float moveSpeedRate = 1.0f )
        {
            // 移動ルートの最終インデックスに到達している場合は、目標タイルに到達しているため終了
            if ( _baseAi.MovePathHandler.IsEndPathTrace() ) { return true; }

            bool toggleAnimation    = false;
            var focusedTileInfo     = _baseAi.MovePathHandler.GetFocusedTileInformation();
            var focusedTilePos      = focusedTileInfo.charaStandPos;

            Vector3 dir         = (focusedTilePos - transform.position).normalized;
            Vector3 afterPos    = transform.position + dir * Constants.CHARACTER_MOVE_SPEED * moveSpeedRate * DeltaTimeProvider.DeltaTime;
            Vector3 afterDir    = (focusedTilePos - afterPos);
            afterDir.y          = 0f;
            afterDir            = afterDir.normalized;

            Vector3 diffXZ = focusedTilePos - afterPos;
            diffXZ.y = 0f;

            // 現在の目標タイルに到達している場合はインデックス値をインクリメントすることで目標タイルを更新する
            if ( Vector3.Dot( dir, afterDir ) <= 0 )
            {
                // 位置とタイル位置情報を更新
                transform.position          = focusedTilePos;
                _params.TmpParam.gridIndex  = _baseAi.MovePathHandler.GetFocusedWaypointIndex();

                if ( _isPrevMoving ) { toggleAnimation = true; }

                _baseAi.MovePathHandler.IncrementFocusedWaypointIndex();  // 目標インデックス値をインクリメント

                // 最終インデックスに到達している場合は移動アニメーションを停止して終了
                if ( _baseAi.MovePathHandler.IsEndPathTrace() )
                {
                    _isPrevMoving = false;
                }
            }
            else
            {
                Vector3 diffPositionXZ = focusedTilePos - transform.position;
                diffPositionXZ.y = 0f;
                if ( diffPositionXZ.sqrMagnitude < TILE_SIZE * TILE_SIZE )
                {
                    _underfootTileInfo = focusedTileInfo;
                }

                transform.position = afterPos;
                transform.rotation = Quaternion.LookRotation( dir );

                if ( !_isPrevMoving ) toggleAnimation = true;
                _isPrevMoving = true;
            }

            if ( toggleAnimation ) { AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.MOVE, _isPrevMoving ); }

            return !_isPrevMoving;
        }

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            _prevMoveInfo.tmpParam  = _params.TmpParam.Clone();
            _prevMoveInfo.rotDir    = transform.rotation;
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
        /// 移動入力受付の可否判定を行います
        /// TODO : 必要がなくなった可能性があるため、不必要と確信出来れば削除
        /// </summary>
        /// <returns>移動入力の受付可否</returns>
        public bool IsAcceptableMovementOperation(float gridSize)
        {
            if (_isPrevMoving)
            {
                var diff = _movementDestination - transform.position;
                diff.y = 0;
                if (diff.sqrMagnitude <= Mathf.Pow(gridSize * Constants.ACCEPTABLE_INPUT_TILE_SIZE_RATIO, 2f)) return true;

                return false;
            }

            return true;
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
    }
}