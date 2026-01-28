using Frontier.Combat;
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

            /// <summary>
            /// 情報をリセットします
            /// </summary>
            public void Reset()
            {
                tmpParam.Reset();
                rotDir = Quaternion.identity;
            }
        }

        // private bool _isPrevMoving = false;
        private PrevMoveInfo _prevMoveInfo;
        public ref PrevMoveInfo PrevMoveInformaiton => ref _prevMoveInfo;

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            _prevMoveInfo.tmpParam  = RefBattleParams.TmpParam.Clone();
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
            RefBattleParams.TmpParam = _prevMoveInfo.tmpParam;
            BattleLogic.SetPositionOnStage( RefBattleParams.TmpParam.gridIndex, _prevMoveInfo.rotDir );
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
            for( int i = 0; i < (int)COMMAND_TAG.NUM; ++i )
            {
                if( i == (int)COMMAND_TAG.MOVE )
                {
                    if (!RefBattleParams.TmpParam.IsEndCommand(COMMAND_TAG.MOVE))
                    {
                        isPossible = false;
                        break;
                    }
                }
                else
                {
                    if (RefBattleParams.TmpParam.IsEndCommand((COMMAND_TAG)i))
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
        public override void Init()
        {
            base.Init();
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
    }
}