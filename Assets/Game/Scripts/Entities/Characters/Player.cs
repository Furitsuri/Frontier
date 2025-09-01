using Frontier.Combat;
using Frontier.Stage;
using UnityEngine;
using Frontier.Combat.Skill;

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
        /// プレイヤーキャラクターの移動時の更新処理を行います
        /// </summary>
        /// <param name="gridIndex">キャラクターの現在地となるグリッドのインデックス値</param>
        /// <param name="gridInfo">指定グリッドの情報</param>
        public void UpdateMove(int gridIndex, in GridInfo gridInfo)
        {
            bool toggleAnimation = false;

            // 移動可のグリッドに対してのみ目的地を更新(自身を除くキャラクターが存在するグリッドには移動させない)
            if ( 0 <= gridInfo.estimatedMoveRange && ( !gridInfo.IsExistCharacter() || gridInfo.IsMatchExistCharacter(this) ) )
            {
                _movementDestination        = gridInfo.charaStandPos;
                _params.TmpParam.gridIndex  = gridIndex;
            }

            Vector3 dir         = (_movementDestination - transform.position).normalized;
            Vector3 afterPos    = transform.position + dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
            Vector3 afterDir    = (_movementDestination - afterPos);
            afterDir.y          = 0f;
            afterDir            = afterDir.normalized;
            if ( Vector3.Dot(dir, afterDir) <= 0 )
            {
                transform.position = _movementDestination;

                if (_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = false;
            }
            else
            {
                transform.position = afterPos;
                transform.rotation = Quaternion.LookRotation(dir);

                if (!_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = true;
            }

            if (toggleAnimation) AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, _isPrevMoving);
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