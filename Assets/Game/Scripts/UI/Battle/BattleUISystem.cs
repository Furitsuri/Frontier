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

        [Header( "ConfirmTurnEndUI" )]
        public ConfirmUI ConfirmTurnEnd;          // ターン終了確認UI

        [Header( "DamageUI" )]
        public DamageUI DamageValue;              // ダメージ表記（テンプレート兼第1インスタンス）

        [Header( "CharacterHpGaugeUI" )]
        public CharacterHpGaugeUI HpGauge;        // キャラクター頭上の常時表示HPゲージ（テンプレート）

        [Header( "CooperativeVortexUI" )]
        public CooperativeVortexUI CooperativeVortex; // 連携演出用渦巻きエフェクト（テンプレート兼第1インスタンス）

        [Header( "PhaseUI" )]
        public PhaseUI Phase;                     // フェーズ表記UI

        [Header( "BattleAnimaUI" )]
        public BattleAnimaUI BattleAnima;         // 戦闘中に獲得したアニマの常時表示

        [Header( "AnimaRewardEffectUI" )]
        public AnimaRewardEffectUI AnimaRewardEffect; // アニマ獲得エフェクト（テンプレート兼第1インスタンス）

        [Header( "StageClearUI" )]
        public StageClearUI StageClear;           // ステージクリアUI

        [Header( "StageResultUI" )]
        public StageResultUI StageResult;         // ステージクリア時のリザルト画面(獲得アニマ等)

        [Header( "GameOver" )]
        public GameOverUI GameOver;               // ゲームオーバー画面

        private RectTransform _rectTransform;     // BattleUIのRectTransform
        private Camera _uiCamera;                 // UI表示用のカメラ

        // キャラクターInstanceID → DamageUI のマッピング（キャラクター毎に個別管理）
        private Dictionary<int, DamageUI> _damageUIByCharaId = new Dictionary<int, DamageUI>();

        // キャラクターInstanceID → CharacterHpGaugeUI のマッピング（キャラクター毎に個別管理）
        private Dictionary<int, CharacterHpGaugeUI> _hpGaugeUIByCharaId = new Dictionary<int, CharacterHpGaugeUI>();

        // キャラクターInstanceID → CooperativeVortexUI のマッピング（キャラクター毎に個別管理）
        private Dictionary<int, CooperativeVortexUI> _cooperativeVortexUIByCharaId = new Dictionary<int, CooperativeVortexUI>();

        // 生成済みのAnimaRewardEffectUIインスタンス一覧（同時に複数の撃破報酬が発生した場合に備え、
        // キャラクター単位ではなく非アクティブなものを使い回すプールとして管理する）
        private List<AnimaRewardEffectUI> _animaRewardEffects = new List<AnimaRewardEffectUI>();

        public void Setup()
        {
            LazyInject.GetOrCreate( ref _rectTransform, () => GetComponent<RectTransform>() );
            LazyInject.GetOrCreate( ref _uiCamera, () => GameObject.Find( "UI_Camera" ).GetComponent<Camera>() );

            ParameterView?.Setup();
            PlCommandWindow?.Setup();
            TileMenuWindow?.Setup();
            ConfirmTurnEnd?.Setup();
            DamageValue?.Setup();
            HpGauge?.Setup();
            CooperativeVortex?.Setup();
            Phase?.Setup();
            BattleAnima?.Setup();
            AnimaRewardEffect?.Setup();
            StageClear?.Setup();
            StageResult?.Setup();
            GameOver?.Setup();
            CommandNameView?.Setup();
            SkillDetail?.Setup();

            DamageValue.Init( _rectTransform, _uiCamera );
            HpGauge.Init( _rectTransform, _uiCamera );
            CooperativeVortex.Init( _rectTransform, _uiCamera );
            AnimaRewardEffect.Init( _rectTransform, _uiCamera );
            _animaRewardEffects.Add( AnimaRewardEffect );
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

        /// <summary>
        /// 指定キャラクターのHPゲージを表示します。キャラクターが戦闘に参加している間、常時表示され続けます。
        /// </summary>
        public void ShowHpGaugeOnCharacter( Character chara )
        {
            var ui = GetOrCreateHpGaugeUI( chara );
            ui.ShowFor( chara );
        }

        /// <summary>
        /// 指定キャラクターのHPゲージを破棄します。
        /// キャラクターが戦闘から離脱した場合(死亡・配置解除など)に呼び出してください。
        /// </summary>
        public void RemoveHpGaugeOnCharacter( Character chara )
        {
            int id = chara.GetInstanceID();
            if( _hpGaugeUIByCharaId.TryGetValue( id, out CharacterHpGaugeUI ui ) )
            {
                if( ui != null ) { Destroy( ui.gameObject ); }
                _hpGaugeUIByCharaId.Remove( id );
            }
        }

        /// <summary>
        /// 現在表示中の全キャラクターのHPゲージをまとめて表示/非表示にします。
        /// 攻撃シーケンスなど、専用カメラ演出中に一時的に隠す用途を想定しています。
        /// </summary>
        public void SetHpGaugesActive( bool isActive )
        {
            foreach( var ui in _hpGaugeUIByCharaId.Values )
            {
                if( ui != null ) { ui.gameObject.SetActive( isActive ); }
            }
        }

        /// <summary>
        /// 指定キャラクターのHPゲージに、予測ダメージ分の点滅表示を設定します。
        /// 攻撃対象選択中、対象キャラクターに対して呼び出してください。
        /// </summary>
        public void SetPredictedDamageOnCharacter( Character chara, int amount )
        {
            if( _hpGaugeUIByCharaId.TryGetValue( chara.GetInstanceID(), out CharacterHpGaugeUI ui ) && ui != null )
            {
                ui.SetPredictedDamage( amount );
            }
        }

        /// <summary>
        /// 表示中の全キャラクターのHPゲージから、予測ダメージ点滅表示を解除します。
        /// </summary>
        public void ClearAllPredictedDamage()
        {
            foreach( var ui in _hpGaugeUIByCharaId.Values )
            {
                if( ui != null ) { ui.ClearPredictedDamage(); }
            }
        }

        /// <summary>
        /// 生成したすべての CharacterHpGaugeUI インスタンスを破棄し、管理辞書をクリアします。
        /// </summary>
        public void CleanupHpGaugeUIs()
        {
            foreach( var ui in _hpGaugeUIByCharaId.Values )
            {
                if( ui != null ) { Destroy( ui.gameObject ); }
            }
            _hpGaugeUIByCharaId.Clear();
        }

        /// <summary>
        /// キャラクターに対応する CharacterHpGaugeUI インスタンスを返します。
        /// 存在しない場合は HpGauge をテンプレートとして新規生成します。
        /// </summary>
        private CharacterHpGaugeUI GetOrCreateHpGaugeUI( Character chara )
        {
            int id = chara.GetInstanceID();
            if( !_hpGaugeUIByCharaId.TryGetValue( id, out CharacterHpGaugeUI ui ) )
            {
                ui = Instantiate( HpGauge, HpGauge.transform.parent );
                ui.Init( _rectTransform, _uiCamera );
                _hpGaugeUIByCharaId[id] = ui;
            }
            return ui;
        }

        /// <summary>
        /// 連携演出用の渦巻きエフェクトを、指定キャラクターの画面上の位置に表示します。
        /// duration 秒かけて縮小しながら回転し、経過後に自動的に非表示になります。
        /// </summary>
        /// <param name="initialScale">縮小開始時の拡大率(等倍=1)</param>
        public void ShowCooperativeVortexOnCharacter( Character chara, float duration, float initialScale = 1f )
        {
            var ui = GetOrCreateCooperativeVortexUI( chara );
            ui.CharacterTransform = chara.transform;
            ui.Play( duration, initialScale );
        }

        /// <summary>
        /// 生成したすべての CooperativeVortexUI インスタンスを破棄し、管理辞書をクリアします。
        /// </summary>
        public void CleanupCooperativeVortexUIs()
        {
            foreach( var ui in _cooperativeVortexUIByCharaId.Values )
            {
                if( ui != null ) { Destroy( ui.gameObject ); }
            }
            _cooperativeVortexUIByCharaId.Clear();
        }

        /// <summary>
        /// キャラクターに対応する CooperativeVortexUI インスタンスを返します。
        /// 存在しない場合は CooperativeVortex をテンプレートとして新規生成します。
        /// </summary>
        private CooperativeVortexUI GetOrCreateCooperativeVortexUI( Character chara )
        {
            int id = chara.GetInstanceID();
            if( !_cooperativeVortexUIByCharaId.TryGetValue( id, out CooperativeVortexUI ui ) )
            {
                ui = Instantiate( CooperativeVortex, CooperativeVortex.transform.parent );
                ui.Init( _rectTransform, _uiCamera );
                _cooperativeVortexUIByCharaId[id] = ui;
            }
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

        /// <summary>
        /// 敵撃破位置からBattleAnimaUIへ向けてアニマ獲得エフェクトを再生します。
        /// 演出がUIへ到達した時点でonArrivedが呼ばれます(実際のアニマ加算は呼び出し側の責務)。
        /// </summary>
        public void PlayAnimaRewardEffect( Vector3 worldPosition, Action onArrived )
        {
            var animaRect = ( RectTransform ) BattleAnima.transform;
            Vector2 targetLocalPos = ( Vector2 ) animaRect.localPosition + animaRect.rect.center;

            GetOrCreateAnimaRewardEffect().Play( worldPosition, targetLocalPos, onArrived );
        }

        /// <summary>
        /// いずれかのアニマ獲得エフェクトが再生中(球がBattleAnimaUIへ向けて移動中)かどうかを取得します。
        /// ステージクリア演出は、全ての球がUIへ到達するまで開始を待つために使用します。
        /// </summary>
        public bool IsAnyAnimaRewardEffectPlaying()
        {
            foreach( var effect in _animaRewardEffects )
            {
                if( effect.IsPlaying ) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 非アクティブなAnimaRewardEffectUIインスタンスを1つ返します。無ければAnimaRewardEffectを
        /// テンプレートとして新規生成します。
        /// </summary>
        private AnimaRewardEffectUI GetOrCreateAnimaRewardEffect()
        {
            foreach( var effect in _animaRewardEffects )
            {
                if( !effect.gameObject.activeSelf ) { return effect; }
            }

            var newEffect = Instantiate( AnimaRewardEffect, AnimaRewardEffect.transform.parent );
            newEffect.Setup();
            newEffect.Init( _rectTransform, _uiCamera );
            _animaRewardEffects.Add( newEffect );
            return newEffect;
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
