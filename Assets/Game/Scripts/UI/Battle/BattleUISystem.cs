using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Frontier.Combat.Skill;

namespace Frontier.UI
{
    public class BattleUISystem : MonoBehaviour
    {
        [Inject] private CombatSkillEventController _combatSkillCtrl = null;

        [Header( "表示キャンバス" )]
        [SerializeField] private Canvas _canvas;

        [Header( "Status" )]
        public BattleParameterUI ParameterView;   // キャラクターパラメータ表示

        [Header( "PlayerCommand" )]
        public PlayerCommandUI PlCommandWindow;   // プレイヤーの選択コマンドUI

        [Header("SelectableCharaParam")]
        public SelectableCharaParamUI SelectableCharaParam;   // 選択可能なキャラクターパラメータ表示

        [Header( "CommandNameUI" )]
        public CommandNameUI CommandNameView;   // コマンド名表示UI

        [Header( "ConfirmTurnEndUI" )]
        public ConfirmUI ConfirmTurnEnd;          // ターン終了確認UI

        [Header( "DamageUI" )]
        public DamageUI DamageValue;              // ダメージ表記（テンプレート兼第1インスタンス）

        [Header( "PhaseUI" )]
        public PhaseUI Phase;                     // フェーズ表記UI

        [Header( "StageClearUI" )]
        public StageClearUI StageClear;           // ステージクリアUI

        [Header( "GameOver" )]
        public GameOverUI GameOver;               // ゲームオーバー画面

        private RectTransform _rectTransform;     // BattleUIのRectTransform
        private Camera _uiCamera;                 // UI表示用のカメラ

        // キャラクターInstanceID → DamageUI のマッピング（キャラクター毎に個別管理）
        private Dictionary<int, DamageUI> _damageUIByCharaId = new Dictionary<int, DamageUI>();

        public void Setup()
        {
            LazyInject.GetOrCreate( ref _rectTransform, () => GetComponent<RectTransform>() );
            LazyInject.GetOrCreate( ref _uiCamera, () => GameObject.Find( "UI_Camera" ).GetComponent<Camera>() );

            ParameterView?.Setup();
            PlCommandWindow?.Setup();
            ConfirmTurnEnd?.Setup();
            DamageValue?.Setup();
            Phase?.Setup();
            StageClear?.Setup();
            GameOver?.Setup();
            CommandNameView?.Setup();

            DamageValue.Init( _rectTransform, _uiCamera );
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

        public void SetActiveActionResultExpect( bool isActive )
        {
            ParameterView.PlayerParameter.TMPDiffHPValue.gameObject.SetActive( isActive );
            ParameterView.EnemyParameter.TMPDiffHPValue.gameObject.SetActive( isActive );
        }

        /// <summary>
        /// 全キャラクターのダメージUIをまとめて表示/非表示にします。
        /// isActive=true の場合は何もしません（キャラクター毎に ShowDamageOnCharacter を使用してください）。
        /// </summary>
        public void ToggleDamageUI( bool isActive )
        {
            if( !isActive )
            {
                foreach( var ui in _damageUIByCharaId.Values )
                {
                    ui.Hide();
                }
            }
        }

        public ConfirmUI GetConfirmTurnEndUI()
        {
            return ConfirmTurnEnd;
        }

        public SkillBoxUI GetPlayerParamSkillBox( int index )
        {
            return ParameterView.PlayerParameter.SkillBoxes[index];
        }

        public SkillBoxUI GetEnemyParamSkillBox( int index )
        {
            return ParameterView.EnemyParameter.SkillBoxes[index];
        }

        /// <summary>
        /// 指定キャラクターのダメージUIを表示します。
        /// duration が 0 以上の場合、指定秒数後に自動で非表示にします。
        /// duration が負の値の場合は自動非表示を行わず、HideDamageOnCharacter() による明示的な非表示が必要です。
        /// </summary>
        /// <param name="chara">ダメージ表示対象のキャラクター</param>
        /// <param name="duration">自動非表示までの秒数。負の値で無効（デフォルト: -1）。</param>
        public void ShowDamageOnCharacter( Character chara, float duration = -1f )
        {
            var ui = GetOrCreateDamageUI( chara );
            SetDamageUIContent( ui, chara, chara.BattleParams.TmpParam.ExpectedHpChange );
            ui.ShowWith( duration );
        }

        /// <summary>
        /// 指定キャラクターのダメージUIを明示的に非表示にします
        /// </summary>
        public void HideDamageOnCharacter( Character chara )
        {
            if( _damageUIByCharaId.TryGetValue( chara.GetInstanceID(), out DamageUI ui ) )
            {
                ui.Hide();
            }
        }

        public void SetTurnType( TurnType turntype )
        {
            Phase.SetTurnType( turntype );
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

        public void StartStageClearAnim()
        {
            StageClear.StartAnim();
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
        /// 生成したすべての DamageUI インスタンスを破棄し、管理辞書をクリアします。
        /// バトル終了などのタイミングで明示的に破棄したい場合に呼び出してください。
        /// </summary>
        public void CleanupDamageUIs()
        {
            foreach( var ui in _damageUIByCharaId.Values )
            {
                if( ui != null ) { Destroy( ui.gameObject ); }
            }
            _damageUIByCharaId.Clear();
        }

        /// <summary>
        /// キャラクターに対応する DamageUI インスタンスを返します。
        /// 存在しない場合は DamageValue をテンプレートとして新規生成します。
        /// </summary>
        private DamageUI GetOrCreateDamageUI( Character chara )
        {
            int id = chara.GetInstanceID();
            if( !_damageUIByCharaId.TryGetValue( id, out DamageUI ui ) )
            {
                ui = Instantiate( DamageValue, DamageValue.transform.parent );
                ui.Init( _rectTransform, _uiCamera );
                _damageUIByCharaId[id] = ui;
            }
            return ui;
        }

        /// <summary>
        /// 指定の DamageUI にキャラクターのトランスフォームとダメージ値を設定します
        /// </summary>
        private void SetDamageUIContent( DamageUI ui, Character character, int damageValue )
        {
            ui.CharacterTransform = character.transform;

            ParrySkillHandler parrySkillHdlr = _combatSkillCtrl.CurrentSkillHandler as ParrySkillHandler;
            if( parrySkillHdlr != null &&
                ( parrySkillHdlr.IsMatchResult( JudgeResult.SUCCESS ) ||
                  parrySkillHdlr.IsMatchResult( JudgeResult.JUST ) ) )
            {
                ui.damageText.color = Color.yellow;
                ui.damageText.text = "DEFLECT";
            }
            else
            {
                int absDamage = Mathf.Abs( damageValue );
                ui.damageText.text = absDamage.ToString();
                if( damageValue < 0 )
                {
                    ui.damageText.color = Color.red;
                }
                else if( 0 < damageValue )
                {
                    ui.damageText.color = Color.green;
                }
                else
                {
                    ui.damageText.color = Color.white;
                }
            }
        }
    }
}
