using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class BattleRoutinePresenter : PhasePresenterBase, IConfirmPresenter
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;
        [Inject] StageController _stageCtrl         = null;

        private const float USE_SKILL_OPTION_MENU_MARGIN_X = 16.0f;  // スキル使用オプション選択時、縦一列に並んだSkillBoxUIとコマンドメニューの間隔

        private bool _isTileHeightDisplayEnabled = false;       // タイルの高さ表示を行うフェーズであるか
        private float _displayedTileHeight       = float.NaN;   // 表示中の高さ(同値の場合は文字列の再生成を省く)

        private CharacterParameterPresenter[] _parameterPresenters;
        private SelectableCharaParamPresenter _selectableCharaParamPresenter;
        private Action<bool>[] SetActiveActionDirectionCallbacks;
        static private int[] _layerMaskIndecies;
        private SkillBoxLayoutAnimator _skillBoxLayoutAnimator = new SkillBoxLayoutAnimator();
        public CharacterParameterPresenter CharaParamView( ParameterWindowType winType ) => _parameterPresenters[( int ) winType];

        public void Setup()
        {
            _parameterPresenters = new CharacterParameterPresenter[( int ) ParameterWindowType.NUM]
            {
                _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>( new object[] { _uiSystem.BattleUi.ParameterView.PlayerParameter, true }, false ),
                _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>( new object[] { _uiSystem.BattleUi.ParameterView.EnemyParameter, true }, false ),
            };

            // グラデーション用マテリアルを設定済みのPlayerParameter/EnemyParameterにのみ、勢力に応じた背景色決定処理を差し込む
            foreach( var parameterPresenter in _parameterPresenters )
            {
                parameterPresenter.SetBackgroundColorResolver( c => GetParameterBackgroundColor( c.GetStatusRef.characterTag ) );
            }

            SetActiveActionDirectionCallbacks = new Action<bool>[( int ) ParameterWindowType.NUM]
            {
                SetActiveParamWinDirectionLeft2Right,
                SetActiveParamWinDirectionRight2Left
            };

            _layerMaskIndecies = new int[]
            {
                LAYER_MASK_INDEX_PLAYER,
                LAYER_MASK_INDEX_ENEMY
            };

            // LazyInject.GetOrCreate( ref _selectableCharaParamPresenter, () => _hierarchyBld.InstantiateWithDiContainer<SelectableCharaParamPresenter>( true ) );
        }

        public void Init()
        {
            foreach( var paramPresenter in _parameterPresenters )
            {
                paramPresenter.Init();
            }
        }

        public void Update()
        {
            foreach( var paramPresenter in _parameterPresenters )
            {
                paramPresenter.Update();
            }

            UpdateTileHeightDisplay();
        }

        /// <summary>
        /// グリッドカーソルが選択しているタイルの高さ表示を行うか否かを設定します。
        /// 有効にしている間、グリッドカーソルが表示されていれば高さを表示します。
        /// </summary>
        public void SetTileHeightDisplayEnabled( bool isEnabled )
        {
            _isTileHeightDisplayEnabled = isEnabled;

            UpdateTileHeightDisplay();
        }

        /// <summary>
        /// グリッドカーソルが選択しているタイルの高さ表示を更新します
        /// </summary>
        private void UpdateTileHeightDisplay()
        {
            var tileHeightUi = _uiSystem.BattleUi.TileHeight;
            bool isVisible   = _isTileHeightDisplayEnabled && _stageCtrl.IsActiveGridCursor();

            tileHeightUi.SetVisible( isVisible );
            if( !isVisible ) { return; }

            float height = _stageCtrl.GetTileStaticData( _stageCtrl.GetCurrentGridIndex() ).Height;
            if( height == _displayedTileHeight ) { return; }

            _displayedTileHeight = height;
            tileHeightUi.SetHeightText( "HEIGHT : " + height.ToString( "0.##", CultureInfo.InvariantCulture ) );
        }

        public void SetActiveBattleUI( bool isActive )
        {
            _uiSystem.BattleUi.gameObject.SetActive( isActive );
        }

        /// <summary>
        /// 戦闘中に獲得したアニマの表示を更新します
        /// </summary>
        public void SetBattleAnimaDisplay( int anima )
        {
            _uiSystem.BattleUi.SetBattleAnima( anima );
        }

        /// <summary>
        /// 戦闘中アニマの加算(または将来の消費)値を、獲得アニマUIの右上にポップアップ表示します
        /// </summary>
        public void ShowBattleAnimaAddedValue( int amount )
        {
            _uiSystem.BattleUi.ShowBattleAnimaAddedValue( amount );
        }

        /// <summary>
        /// 敵撃破位置からアニマ獲得エフェクトを再生します。演出がUIへ到達した時点でonArrivedが呼ばれます。
        /// </summary>
        public void PlayAnimaRewardEffect( Vector3 worldPosition, Action onArrived )
        {
            _uiSystem.BattleUi.PlayAnimaRewardEffect( worldPosition, onArrived );
        }

        public void SetActiveConfirmUI( bool isActive, Frontier.StateMachine.ConfirmUIType uiType )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.gameObject.SetActive( isActive );
        }

        public void SetActiveParamWinDirectionLeft2Right( bool isActive )
        {
            _uiSystem.BattleUi.SetActiveLeft2RightDirection( isActive );
            _uiSystem.BattleUi.SetActiveRightParameterWindow( isActive );
        }

        public void SetActiveParamWinDirectionRight2Left( bool isActive )
        {
            _uiSystem.BattleUi.SetActiveRight2LeftDirection( isActive );
            _uiSystem.BattleUi.SetActiveLeftParameterWindow( isActive );
        }

        public void SetActiveActionResultExpect( bool isActive, ParameterWindowType fromWinType )
        {
            SetActiveActionDirectionCallbacks[( int ) fromWinType]( isActive ); // アクション対象指定UIの表示・非表示
            _uiSystem.BattleUi.SetActiveActionResultExpect( isActive );         // ダメージ予測表示UIの表示・非表示
        }

        /// <summary>
        /// 指定キャラクターのHPゲージに、予測ダメージ分の点滅表示を設定します。
        /// </summary>
        public void SetPredictedDamageOnCharacter( Character chara, int amount )
        {
            _uiSystem.BattleUi.SetPredictedDamageOnCharacter( chara, amount );
        }

        /// <summary>
        /// 表示中の全キャラクターのHPゲージから、予測ダメージ点滅表示を解除します。
        /// </summary>
        public void ClearAllPredictedDamage()
        {
            _uiSystem.BattleUi.ClearAllPredictedDamage();
        }

        /// <summary>
        /// 全キャラクター頭上のHPゲージの表示・非表示を切り替えます。
        /// </summary>
        public void SetHpGaugesActive( bool isActive )
        {
            _uiSystem.BattleUi.SetHpGaugesActive( isActive );
        }

        /// <summary>
        /// 暫定移動中のキャラクター頭上に表示するアイコンを、指定キャラクターに限り一時的に非表示にします。
        /// 他のキャラクターのアイコンは表示されたままとなります。nullを渡すと、非表示の指定を解除します。
        /// </summary>
        public void SuppressProvisionalMoveIcon( Character character )
        {
            _uiSystem.BattleUi.SuppressProvisionalMoveIcon( character );
        }

        public void ApplyColor2Options( int selectIndex )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.ApplyTextColor( selectIndex );
        }

        /// <summary>
        /// ゲームオーバー時のUIとアニメーションを表示します
        /// </summary>
        public void StartGameOverAnim()
        {
            _uiSystem.BattleUi.ToggleGameOverUI( true );
            _uiSystem.BattleUi.StartGameOverAnim();
        }

        public bool IsActiveGameOverAnimation()
        {
            return _uiSystem.BattleUi.GameOver.isActiveAndEnabled;
        }

        public void ShowCommandNameUI( SkillID skillId, float duration = 2.0f )
        {
            _uiSystem.BattleUi.CommandNameView.Show( skillId, duration );
        }

        /// <summary>
        /// PlSelectSkillState中のスキル詳細情報パネルの表示・非表示を切り替えます
        /// </summary>
        public void SetActiveSkillDetail( bool isActive )
        {
            _uiSystem.BattleUi.SetSkillDetailActive( isActive );
        }

        /// <summary>
        /// 指定側パラメータパネルの全SkillBoxUIの明滅を停止します。
        /// アクションゲージ消費時(BattleLogicBase.ConsumeActionGauge)に呼ばれます。
        /// </summary>
        public void StopAllSkillBoxFlick( ParameterWindowType winType )
        {
            foreach( var skillBox in _parameterPresenters[( int ) winType].GetSkillBoxes() )
            {
                skillBox.StopFlick();
            }
        }

        /// <summary>
        /// SkillBoxUIの並び替え・元位置への復帰アニメーションが再生中かどうかを取得します
        /// </summary>
        public bool IsSkillBoxLayoutAnimating => _skillBoxLayoutAnimator.IsAnimating;

        /// <summary>
        /// PlSelectSkillState遷移時、SkillBoxUIを選択用の縦一列レイアウトへアニメーション移動させます
        /// </summary>
        public void AnimateSkillBoxesForSelection( ParameterWindowType winType )
        {
            _skillBoxLayoutAnimator.AnimateToSelectionLayout( _parameterPresenters[( int ) winType].GetSkillBoxes() );
        }

        /// <summary>
        /// 指定インデックスのSkillBoxUIのみをカーソルハイライト状態にします(-1で全解除)
        /// </summary>
        public void SetSkillBoxCursorIndex( ParameterWindowType winType, int index )
        {
            _parameterPresenters[( int ) winType].SetSkillBoxCursorIndex( index );
        }

        /// <summary>
        /// AnimateSkillBoxesForSelectionで移動させたSkillBoxUIを元の位置へアニメーション移動させます
        /// </summary>
        public void RevertSkillBoxesFromSelection( ParameterWindowType winType )
        {
            _skillBoxLayoutAnimator.RevertToOriginalLayout( _parameterPresenters[( int ) winType].GetSkillBoxes() );
        }

        /// <summary>
        /// プレイヤーのコマンドメニューを表示します
        /// </summary>
        public void InitPLCommandView( PlSelectCommandState script, List<COMMAND_TAG> executableCommands )
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistCommandScript( script );
            _uiSystem.BattleUi.PlCommandWindow.SetExecutableCommandList( executableCommands );
            _uiSystem.BattleUi.PlCommandWindow.SetHorizontalOffset( 0f );
            _uiSystem.BattleUi.SetPlayerCommandActive( true );
        }

        public void ExitPLCommandView()
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistCommandScript( null );
            _uiSystem.BattleUi.SetPlayerCommandActive( false );
        }

        public void InitUseSkillOptionView( PlSkillUseOptionState script, List<USE_SKILL_OPTION_TAG> options )
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistCommandScript( script );
            _uiSystem.BattleUi.PlCommandWindow.SetUseSkillOptionList( options );
            // この時点ではSkillBoxUIがステータスウィンドウの下に縦一列で並んだままであるため、重ならないようその右隣へずらす
            _uiSystem.BattleUi.PlCommandWindow.SetHorizontalOffset( GetSkillBoxColumnWidth( ParameterWindowType.Left ) + USE_SKILL_OPTION_MENU_MARGIN_X );
            _uiSystem.BattleUi.SetPlayerCommandActive( true );
        }

        public void ExitUseSkillOptionView()
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistCommandScript( null );
            _uiSystem.BattleUi.SetPlayerCommandActive( false );
        }

        public void InitReservedActionOptionView( PlSelectReservedActionState script, List<RESERVED_ACTION_OPTION_TAG> options )
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistCommandScript( script );
            _uiSystem.BattleUi.PlCommandWindow.SetReservedActionOptionList( options );
            _uiSystem.BattleUi.PlCommandWindow.SetHorizontalOffset( 0f );
            _uiSystem.BattleUi.SetPlayerCommandActive( true );
        }

        public void ExitReservedActionOptionView()
        {
            _uiSystem.BattleUi.PlCommandWindow.RegistCommandScript( null );
            _uiSystem.BattleUi.SetPlayerCommandActive( false );
        }

        public void InitTileMenuOptionView( PlSelectMenuState script, List<TILE_MENU_OPTION_TAG> options )
        {
            _uiSystem.BattleUi.TileMenuWindow.RegistCommandScript( script );
            _uiSystem.BattleUi.TileMenuWindow.SetTileMenuOptionList( options );
            _uiSystem.BattleUi.SetTileMenuActive( true );
        }

        public void ExitTileMenuOptionView()
        {
            _uiSystem.BattleUi.TileMenuWindow.RegistCommandScript( null );
            _uiSystem.BattleUi.SetTileMenuActive( false );
        }

        /// <summary>
        /// 縦一列に並べたSkillBoxUIの幅を取得します
        /// </summary>
        private float GetSkillBoxColumnWidth( ParameterWindowType winType )
        {
            var skillBoxes = _parameterPresenters[( int ) winType].GetSkillBoxes();
            if( skillBoxes == null || skillBoxes.Length == 0 ) { return 0f; }

            return ( ( RectTransform ) skillBoxes[0].transform ).rect.width;
        }

        /// <summary>
        /// 画面中央上部に操作状態の案内文言を表示します
        /// </summary>
        public void ShowGuideMessage( LocKey messageKey )
        {
            _uiSystem.BattleUi.GuideMessage.Show( messageKey );
        }

        /// <summary>
        /// 画面中央上部の操作状態の案内文言を非表示にします
        /// </summary>
        public void HideGuideMessage()
        {
            _uiSystem.BattleUi.GuideMessage.Hide();
        }

        public void SetConfirmMessage( string message )
        {
            _uiSystem.BattleUi.ConfirmTurnEnd.SetMessageText( message );
        }

        /// <summary>
        /// バトル終了時に戦闘関連UIのインスタンスをまとめて破棄・終了させます
        /// </summary>
        public void Exit()
        {
            CleanupDamageUIs();
        }

        private void CleanupDamageUIs()
        {
            _uiSystem.BattleUi.CleanupDamageUIs();
        }

        static public int GetLayerMaskIndexFromWinType( ParameterWindowType winType )
        {
            return _layerMaskIndecies[( int ) winType];
        }

        /// <summary>
        /// キャラクターパラメータUIの背景色を、キャラクターの勢力に応じて返します。
        /// アルファ値は従来ヒエラルキー側で設定していた値(221/255)を踏襲します。
        /// </summary>
        static public Color GetParameterBackgroundColor( CHARACTER_TAG characterTag )
        {
            const float alpha = 221f / 255f;

            switch( characterTag )
            {
                case CHARACTER_TAG.PLAYER:
                    return new Color( 0.20f, 0.40f, 0.85f, alpha );
                case CHARACTER_TAG.ENEMY:
                    return new Color( 0.80f, 0.20f, 0.20f, alpha );
                default:
                    // OTHER(第三勢力)、および想定外のタグはデフォルトとして緑を用いる
                    return new Color( 0.20f, 0.70f, 0.30f, alpha );
            }
        }
    }
}