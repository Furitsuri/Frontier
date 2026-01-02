using Froniter.StateMachine;
using Frontier.Entities;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Zenject.SpaceFighter;
using static Constants;
using static InputCode;

namespace Frontier.StateMachine
{
    /// <summary>
    /// キャラクターの選択・配置と、配置先のタイル選択の両方を担います
    /// </summary>
    public class DeploymentRootState : DeploymentPhaseStateBase
    {
        [Inject] private IStageDataProvider _stageDataProvider = null;

        private int _focusCharacterIndex                        = 0;     // フォーカス中のキャラクターインデックス
        private List<DeploymentCandidate> _deploymentCandidates = new List<DeploymentCandidate>();  // 配置可能なキャラクターリスト

        private enum TransitTag
        {
            CHARACTER_STATUS = 0,
            CONFIRM_COMPLETED,
        }

        private void InitDeploymentCandidates()
        {
            _deploymentCandidates.Clear();

            foreach( var player in _btlRtnCtrl.BtlCharaCdr.GetCandidatePlayerEnumerable() )
            {
                player.gameObject.SetActive( false );
                var reservePos = new Vector3( DEPLOYMENT_CHARACTER_SPACING_X * player.Params.CharacterParam.characterIndex, DEPLOYMENT_CHARACTER_OFFSET_Y, DEPLOYMENT_CHARACTER_OFFSET_Z );
                player.GetTransformHandler.SetPosition( reservePos );

                // UI表示用に各キャラクターのスナップショットを撮影
                var size = _presenter.GetDeploymentCharacterDisplaySize();
                Texture2D candidateSnapshot = _btlRtnCtrl.TakeCharacterSnapshot( size.Item1, size.Item2, player, false );

                // 配置候補キャラクターを生成・初期化してスナップショットと共にリストに追加
                DeploymentCandidate candidate = _hierarchyBld.InstantiateWithDiContainer<DeploymentCandidate>( false );
                candidate.Init( player, candidateSnapshot );
                _deploymentCandidates.Add( candidate );
            }
        }

        private bool UndoDeploymentCandidates()
        {
            // 既にキャラクターが配置されているタイルに配置する場合は、そのキャラクターを配置済みリストから削除
            var charaOnSelectTile = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null != charaOnSelectTile && charaOnSelectTile.CharaKey.CharacterTag == CHARACTER_TAG.PLAYER )
            {
                // 配置済みフラグをOFFに
                _deploymentCandidates.Find( c => c.Character.CharaKey == charaOnSelectTile.CharaKey ).IsDeployed = false;
                // キャラクター管理リストから削除
                _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList( charaOnSelectTile.CharaKey );
                // 見えない位置に退避
                charaOnSelectTile.Params.TmpParam.gridIndex = -1;
                var reservePos = new Vector3( DEPLOYMENT_CHARACTER_SPACING_X * charaOnSelectTile.Params.CharacterParam.characterIndex, DEPLOYMENT_CHARACTER_OFFSET_Y, DEPLOYMENT_CHARACTER_OFFSET_Z );
                charaOnSelectTile.GetTransformHandler.SetPosition( reservePos );

                return true;
            }

            return false;
        }

        private void OnCompleteSlideAnimation( DeploymentPhasePresenter.SlideDirection direction )
        {
            _presenter.ClearFocusCharacter();

            if ( direction == DeploymentPhasePresenter.SlideDirection.LEFT )
            {
                if( --_focusCharacterIndex < 0 )
                {
                    _focusCharacterIndex = _deploymentCandidates.Count - 1;
                }
            }
            else
            {
                if( ++_focusCharacterIndex >= _deploymentCandidates.Count )
                {
                    _focusCharacterIndex = 0;
                }
            }

            _presenter.SetFocusCharacters( _focusCharacterIndex );
            _presenter.RefreshFocusDeploymentCharacter();
            _presenter.ResetDeploymentCharacterDispPosition();
        }

        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        override public void Init()
        {
            base.Init();

            _focusCharacterIndex = 0;

            InitDeploymentCandidates();

            _stageCtrl.SetGridCursorControllerActive( true );   // グリッド選択を有効化
            
            _presenter.SetActiveCharacterSelectUis( true );                                 // キャラクター選択画面の表示を有効化
            _presenter.AssignDeploymentCandidates( _deploymentCandidates.AsReadOnly() );    // 配置可能キャラクターリストを読取専用参照としてPresenterに渡す
            _presenter.SetFocusCharacters( _focusCharacterIndex );                          // 最初のキャラクターにフォーカスを当てておく
            _presenter.RefreshGridCursorSelectCharacter();
            _presenter.RefreshFocusDeploymentCharacter();
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            return ( 0 <= TransitIndex );
        }

        override public void ExitState()
        {
            base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "MOVE", CanAcceptDefault,               new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      "PLACE CHARACTER", CanAcceptConfirm,    new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,       "UNDO PLACE", CanAcceptCancel,          new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode),
               (GuideIcon.INFO,         "STATUS", CanAcceptInfo,                new AcceptBooleanInput( AcceptInfo ), 0.0f, hashCode),
               (new GuideIcon[] { GuideIcon.SUB1, GuideIcon.SUB2 }, "CHANGE CHARACTER", new EnableCallback[] { CanAcceptSub1, CanAcceptSub2 }, new IAcceptInputBase[] { new AcceptBooleanInput( AcceptSub1 ), new AcceptBooleanInput( AcceptSub2 ) }, 0.0f, hashCode),
               (GuideIcon.OPT2,         "COMPLETE", CanAcceptOptional,          new AcceptBooleanInput( AcceptOptional ), 0.0f, hashCode)
            );
        }

        override protected bool CanAcceptConfirm()
        {
            // キャラクター選択UIのスライドアニメーションが再生中であれば入力不可
            // if( _presenter.IsSlideAnimationPlaying() ) { return false; }

            // 配置不可のタイルを選択している場合は入力不可
            if( !_stageDataProvider.CurrentData.GetTile( _stageCtrl.GetCurrentGridIndex() ).StaticData().IsDeployable )
            {
                return false;
            }

            // タイル上に同じキャラクターが配置されている場合は入力不可
            Character characterOnSelectTile = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null != characterOnSelectTile && characterOnSelectTile.CharaKey == _deploymentCandidates[_focusCharacterIndex].Character.CharaKey )
            {
                return false;
            }

            return true;
        }

        override protected bool CanAcceptCancel()
        {
            // タイル上にキャラクターが配置されていなければ入力不可
            Character characterOnSelectTile = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == characterOnSelectTile || characterOnSelectTile.CharaKey.CharacterTag != CHARACTER_TAG.PLAYER )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// グリッドカーソルがキャラクターを選択している場合はステータス情報表示を受け付けます
        /// </summary>
        /// <returns></returns>
        override protected bool CanAcceptInfo()
        {
            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            // キャラクター選択UIのスライドアニメーションが再生中であれば入力を受け付けない
            // if( _presenter.IsSlideAnimationPlaying() ) { return false; }

            return true;
        }

        override protected bool CanAcceptSub1()
        {
            // キャラクター選択UIのスライドアニメーションが再生中であれば入力を受け付けない
            // if( _presenter.IsSlideAnimationPlaying() ) { return false; }

            return true;
        }

        override protected bool CanAcceptSub2()
        {
            // キャラクター選択UIのスライドアニメーションが再生中であれば入力を受け付けない
            // if( _presenter.IsSlideAnimationPlaying() ) { return false; }

            return true;
        }

        /// <summary>
        /// 配置キャラクターが1体以上いる場合は配置完了入力を受け付けます
        /// </summary>
        /// <returns></returns>
        override protected bool CanAcceptOptional()
        {
            // キャラクター選択UIのスライドアニメーションが再生中であれば入力を受け付けない
            // if( _presenter.IsSlideAnimationPlaying() ) { return false; }
            // 遷移には配置キャラクターが1体以上存在している必要がある
            if( _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.PLAYER ) <= 0 ) { return false; }

            return true;
        }

        /// <summary>
        /// 方向入力を受け取り、選択グリッドを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection( Direction dir )
        {
            bool isOperated = _stageCtrl.OperateGridCursorController( dir );

            if( isOperated ) { _presenter.RefreshGridCursorSelectCharacter(); }

            return isOperated;
        }

        /// <summary>
        /// キャラクターを選択したタイルに配置します
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) { return false; }

            UndoDeploymentCandidates();

            var candidate = _deploymentCandidates[_focusCharacterIndex];
            candidate.IsDeployed = true;
            var focusCharacter = candidate.Character;
            focusCharacter.gameObject.SetActive( true );
            focusCharacter.GetTransformHandler.SetPosition( _stageCtrl.GetCurrentGridPosition() );
            focusCharacter.Params.TmpParam.SetCurrentGridIndex( _stageCtrl.GetCurrentGridIndex() );

            _presenter.RefreshGridCursorSelectCharacter();

            // リストに挿入されていない場合は挿入
            if( !_btlRtnCtrl.BtlCharaCdr.IsContains( focusCharacter.CharaKey ) )
            {
                _btlRtnCtrl.BtlCharaCdr.AddPlayerToList( focusCharacter );
            }

            return true;
        }

        /// <summary>
        /// タイル上に既に配置されているキャラクターを配置前の状態に戻します
        /// </summary>
        /// <param name="isCancel"></param>
        /// <returns></returns>
        override protected bool AcceptCancel( bool isCancel )
        {
            if( !isCancel ) { return false; }

            if( UndoDeploymentCandidates() )
            {
                _presenter.RefreshGridCursorSelectCharacter();
                return true;
            }

            return false;
        }

        /// <summary>
        /// キャラクターのステータス情報を表示します
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptInfo( bool isInput )
        {
            if( !isInput ) { return false; }

            Handler.ReceiveContext( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() );

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }

        /// <summary>
        /// キャラクター選択を前に進めます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptSub1( bool isInput )
        {
            if( !isInput ) { return false; }

            _presenter.SlideAnimationDeploymentCharacterDisplay( DeploymentPhasePresenter.SlideDirection.LEFT, OnCompleteSlideAnimation );

            return true;
        }

        /// <summary>
        /// キャラクター選択を後ろに進めます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptSub2( bool isInput )
        {
            if( !isInput ) { return false; }

            _presenter.SlideAnimationDeploymentCharacterDisplay( DeploymentPhasePresenter.SlideDirection.RIGHT, OnCompleteSlideAnimation );

            return true;
        }

        /// <summary>
        /// OPTION入力を受けた際に配置確認画面へ遷移させます
        /// </summary>
        /// <param name="isOptional"></param>
        /// <returns></returns>
        override protected bool AcceptOptional( bool isOptional )
        {
            if( !isOptional ) { return false; }

            TransitState( ( int ) TransitTag.CONFIRM_COMPLETED );

            return true;
        }
    }
}