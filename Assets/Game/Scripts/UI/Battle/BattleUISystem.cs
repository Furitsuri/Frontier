using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Frontier.Combat.Skill;

namespace Frontier.UI
{
    public class BattleUISystem : MonoBehaviour, ICharacterUiFeedback
    {
        [Inject] private CombatSkillEventController _combatSkillCtrl = null;

        [Header( "表示キャンバス" )]
        [SerializeField] private Canvas _canvas;

        [Header( "Status" )]
        public BattleParameterUI ParameterView;   // キャラクターパラメータ表示

        [Header( "PlayerCommand" )]
        public PlayerCommandUI PlCommandWindow;   // プレイヤーの選択コマンドUI

        [Header( "TileMenu" )]
        public PlayerCommandUI TileMenuWindow;    // PlSelectTileState用のタイルメニュー(Option/Turn End)専用UI

        [Header("SelectableCharaParam")]
        public SelectableCharaParamUI SelectableCharaParam;   // 選択可能なキャラクターパラメータ表示

        [Header( "CommandNameUI" )]
        public CommandNameUI CommandNameView;   // コマンド名表示UI

        [Header( "SkillDetailUI" )]
        public SkillDetailUI SkillDetail;       // PlSelectSkillState中のスキル詳細情報パネル

        [Header( "GuideMessageUI" )]
        public BattleGuideMessageUI GuideMessage; // 画面中央上部の操作状態の案内文言

        [Header( "TileHeightUI" )]
        public TileHeightUI TileHeight;           // グリッドカーソルが選択しているタイルの高さ表示

        [Header( "ConfirmTurnEndUI" )]
        public ConfirmUI ConfirmTurnEnd;          // ターン終了確認UI

        // MEMO : 実行時に複製して用いるUIは、機能ごとのGroupViewが自身の子として生成・管理します。描画の前後関係を生成順に左右されないよう、
        //        GroupViewはBattleUI直下の以下のレイヤーに配置しておくこと(同一Canvas内では、ヒエラルキーで後ろにあるものほど手前に描画される)
        //          CharacterFollowLayer : BattleUIの先頭。HPゲージ等、キャラクターに追従する表示(パラメータ・コマンド等のHUDより奥)
        //          EffectLayer          : HUDの後ろ、確認ダイアログ・リザルト等の前。ダメージ表記等の演出(HUDより手前)
        [Header( "DamageValueGroupView" )]
        public DamageValueGroupView DamageValues; // ダメージ表記の生成・管理（EffectLayerに配置）

        [Header( "CharacterHpGaugeGroupView" )]
        public CharacterHpGaugeGroupView HpGauges; // キャラクター頭上の常時表示HPゲージの生成・管理（CharacterFollowLayerに配置）

        [Header( "CooperativeVortexGroupView" )]
        public CooperativeVortexGroupView CooperativeVortices; // 連携演出用渦巻きエフェクトの生成・管理（EffectLayerに配置）

        [Header( "PhaseUI" )]
        public PhaseUI Phase;                     // フェーズ表記UI

        [Header( "BattleAnimaUI" )]
        public BattleAnimaUI BattleAnima;         // 戦闘中に獲得したアニマの常時表示

        [Header( "AnimaRewardEffectGroupView" )]
        public AnimaRewardEffectGroupView AnimaRewardEffects; // アニマ獲得エフェクトの生成・管理（EffectLayerに配置）

        [Header( "StageClearUI" )]
        public StageClearUI StageClear;           // ステージクリアUI

        [Header( "StageResultUI" )]
        public StageResultUI StageResult;         // ステージクリア時のリザルト画面(獲得アニマ等)

        [Header( "GameOver" )]
        public GameOverUI GameOver;               // ゲームオーバー画面

        private RectTransform _rectTransform;     // BattleUIのRectTransform
        private Camera _uiCamera;                 // UI表示用のカメラ

        public void Setup()
        {
            LazyInject.GetOrCreate( ref _rectTransform, () => GetComponent<RectTransform>() );
            LazyInject.GetOrCreate( ref _uiCamera, () => GameObject.Find( "UI_Camera" ).GetComponent<Camera>() );

            ParameterView?.Setup();
            PlCommandWindow?.Setup();
            TileMenuWindow?.Setup();
            ConfirmTurnEnd?.Setup();
            Phase?.Setup();
            BattleAnima?.Setup();
            StageClear?.Setup();
            StageResult?.Setup();
            GameOver?.Setup();
            CommandNameView?.Setup();
            SkillDetail?.Setup();
            GuideMessage?.Setup();
            TileHeight?.Setup();

            HpGauges.Setup( _rectTransform, _uiCamera );
            DamageValues.Setup( _rectTransform, _uiCamera );
            CooperativeVortices.Setup( _rectTransform, _uiCamera );
            AnimaRewardEffects.Setup( _rectTransform, _uiCamera );
        }

        public void SetActiveLeftParameterWindow( bool isActive )
        {
            ParameterView.PlayerParameter.gameObject.SetActive( isActive );
        }

        public void SetActiveRightParameterWindow( bool isActive )
        {
            ParameterView.EnemyParameter.gameObject.SetActive( isActive );
        }

        public void SetActiveLeft2RightDirection( bool isActive )
        {
            ParameterView.AttackDirection.attackCursorP2E.gameObject.SetActive( isActive );
        }

        public void SetActiveRight2LeftDirection( bool isActive )
        {
            ParameterView.AttackDirection.attackCursorE2P.gameObject.SetActive( isActive );
        }

        public void SetPlayerCommandActive( bool isActive )
        {
            PlCommandWindow.gameObject.SetActive( isActive );
        }

        public void SetTileMenuActive( bool isActive )
        {
            TileMenuWindow.gameObject.SetActive( isActive );
        }

        public void SetSkillDetailActive( bool isActive )
        {
            SkillDetail.gameObject.SetActive( isActive );
        }

        public void SetActiveActionResultExpect( bool isActive )
        {
            ParameterView.PlayerParameter.TMPDiffHPValue.gameObject.SetActive( isActive );
            ParameterView.EnemyParameter.TMPDiffHPValue.gameObject.SetActive( isActive );
        }

        public ConfirmUI GetConfirmTurnEndUI()
        {
            return ConfirmTurnEnd;
        }

        /// <summary>
        /// 指定キャラクターのダメージUIを表示します(ICharacterUiFeedback)。
        /// duration が 0 以上の場合、指定秒数後に自動で非表示にします。
        /// duration が負の値の場合は自動非表示を行わず、DamageValues.Hide() による明示的な非表示が必要です。
        /// </summary>
        /// <param name="chara">ダメージ表示対象のキャラクター</param>
        /// <param name="duration">自動非表示までの秒数。負の値で無効（デフォルト: -1）。</param>
        public void ShowDamageOnCharacter( Character chara, float duration = -1f )
        {
            ( string text, Color color ) = ResolveDamageContent( chara.BattleParams.TmpParam.ExpectedHpChange );
            DamageValues.Show( chara, text, color, duration );
        }

        /// <summary>
        /// 指定キャラクターのHPゲージを表示します(ICharacterUiFeedback)。
        /// </summary>
        public void ShowHpGaugeOnCharacter( Character chara )
        {
            HpGauges.Show( chara );
        }

        /// <summary>
        /// 指定キャラクターのHPゲージを破棄します(ICharacterUiFeedback)。
        /// </summary>
        public void RemoveHpGaugeOnCharacter( Character chara )
        {
            HpGauges.Remove( chara );
        }

        /// <summary>
        /// 攻撃予告線を伸ばしているキャラクターが画面から見切れている場合に表示する、顔アイコンを生成します。
        /// キャラクター頭上のHPゲージと同じ階層(CharacterFollowLayer。各種ウィンドウより奥)の、HPゲージより手前に非表示の状態で生成されます。
        /// </summary>
        /// <param name="prefab">顔アイコンのプレハブ(PrefabRegistry.AttackForecastIconPrefab)</param>
        public AttackForecastIconUI CreateAttackForecastIcon( AttackForecastIconUI prefab )
        {
            var ui = Instantiate( prefab, HpGauges.transform.parent );
            ui.Init( _rectTransform, _uiCamera );
            ui.Hide();

            return ui;
        }

        public void SetTurnType( TurnType turntype )
        {
            Phase.SetTurnType( turntype );
        }

        /// <summary>
        /// 戦闘中に獲得したアニマの表示を更新します
        /// </summary>
        public void SetBattleAnima( int anima )
        {
            BattleAnima.SetAnima( anima );
        }

        /// <summary>
        /// 戦闘中アニマの加算(または将来の消費)値を、獲得アニマUIの右上にポップアップ表示します
        /// </summary>
        public void ShowBattleAnimaAddedValue( int amount )
        {
            BattleAnima.ShowAddedValuePopup( amount );
        }

        /// <summary>
        /// アニマ加算ポップアップがいずれか表示中かどうかを取得します。
        /// ステージクリア演出は、この表示が終わるまで開始を待つために使用します。
        /// </summary>
        public bool IsShowingBattleAnimaAddedValue()
        {
            return BattleAnima.IsShowingAddedValuePopup;
        }

        public void StartAnimPhaseUI()
        {
            Phase.StartAnim();
        }

        public bool IsPlayingPhaseUI()
        {
            return Phase.IsPlayingAnim();
        }

        public void ToggleConfirmTurnEnd( bool isActive )
        {
            ConfirmTurnEnd.gameObject.SetActive( isActive );
        }

        public void ApplyTextColor2ConfirmTurnEndUI( int selectIndex )
        {
            ConfirmTurnEnd.ApplyTextColor( selectIndex );
        }

        public void ToggleStageClearUI( bool isActive )
        {
            StageClear.gameObject.SetActive( isActive );
        }

        /// <summary>
        /// ステージクリア時のリザルト画面(獲得アニマ・経過ターン数)を表示します。
        /// </summary>
        public void ShowStageResult( int anima, int turnCount )
        {
            StageResult.SetAnima( anima );
            StageResult.SetTurnCount( turnCount );
            StageResult.Show();
        }

        /// <summary>
        /// リザルト画面を閉じます
        /// </summary>
        public void HideStageResult()
        {
            StageResult.Hide();
        }

        /// <summary>
        /// ステージクリア演出を開始し、あわせて戦闘中アニマ表示・味方/敵のバトルパラメータUIを
        /// 非表示にします(演出中に余計なHUDが写り込まないようにするため)。
        /// </summary>
        public void StartStageClearAnim()
        {
            StageClear.StartAnim();
            BattleAnima.Hide();
            SetActiveLeftParameterWindow( false );
            SetActiveRightParameterWindow( false );
        }

        public void ToggleGameOverUI( bool isActive )
        {
            GameOver.gameObject.SetActive( isActive );
        }

        public void StartGameOverAnim()
        {
            GameOver.StartAnim();
        }

        /// <summary>
        /// ダメージ表記に表示する文言と色を決定します
        /// </summary>
        /// <param name="damageValue">HPの変化量(負の値でダメージ、正の値で回復)</param>
        private ( string text, Color color ) ResolveDamageContent( int damageValue )
        {

            ParrySkillHandler parrySkillHdlr = _combatSkillCtrl.CurrentSkillHandler as ParrySkillHandler;
            if( parrySkillHdlr != null &&
                ( parrySkillHdlr.IsMatchResult( JudgeResult.SUCCESS ) ||
                  parrySkillHdlr.IsMatchResult( JudgeResult.JUST ) ) )
            {
                return ( "DEFLECT", Color.yellow );
            }

            string text = Mathf.Abs( damageValue ).ToString();
            if( damageValue < 0 ) { return ( text, Color.red ); }
            if( 0 < damageValue ) { return ( text, Color.green ); }
            return ( text, Color.white );
        }
    }
}
