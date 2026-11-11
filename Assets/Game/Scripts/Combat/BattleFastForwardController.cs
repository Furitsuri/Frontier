using Frontier.Option;
using Frontier.Sequences;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// 戦闘中、ユーザーの操作を必要としない場面(攻撃・スキルの演出中、敵・第三勢力の行動中など)で
    /// CONFIRMを押している間だけゲーム全体を早送りします。
    /// 「操作を必要としない場面」は、以下の両方を満たすことで判定します。
    ///  ・攻撃・スキル等のシーケンスの再生中、または敵・第三勢力のフェーズ中である
    ///    (配置・プレイヤーフェーズの操作中は、カーソル位置次第でCONFIRMが無効になる場面があるため除外する)
    ///  ・CONFIRMを受け付ける入力コードが1つも有効でない
    ///    (パリィの入力受付や防御スキルの選択など、CONFIRMによる操作が必要になった時点で自動的に等速へ戻る)
    /// 早送りはUnity全体のTime.timeScaleを変更して行うため、deltaTimeを用いる処理・Animator・
    /// パーティクル等がまとめて加速します(入力判定はInputCode側で実時間を用いています)。
    /// </summary>
    public class BattleFastForwardController
    {
        [Inject] private InputFacade _inputFcd      = null;
        [Inject] private OptionHandler _optionHdlr  = null;
        [Inject] private SequenceFacade _sequenceFcd = null;
        [Inject] private BattleRoutineController _btlRtnCtrl = null;

        private float _activeElapsed = 0f;  // 早送りを継続している実時間(秒)
        private int _inputHashCode = 0;     // 入力コード登録用のハッシュ値

        /// <summary>早送り中か否か</summary>
        public bool IsActive { get; private set; } = false;

        /// <summary>
        /// 早送りの入力コードを登録します。
        /// この入力コードは入力ガイドへの表示のためのもので、受付処理自体は何も行いません。
        /// (早送りは押している間ずっと続く入力であり、離す・押すの単発入力を扱う受付処理の枠組みに合わないため、
        ///  押下状態はUpdate()で直接参照する)
        /// CONFIRMを受け付ける他の入力コードとは有効になる場面が重ならないため、同じアイコンでの登録を許容させます
        /// </summary>
        public void Init()
        {
            _inputHashCode = Hash.GetStableHash( GetType().Name );

            var code = new InputCode( GuideIcon.CONFIRM, "FAST\nFORWARD", CanShowFastForwardGuide, new AcceptContextInput( AcceptFastForward ), 0.0f, _inputHashCode );
            code.AllowsSharedIcon = true;

            _inputFcd.RegisterInputCodes( code );
        }

        /// <summary>
        /// 入力コードの登録を解除し、等速に戻します
        /// </summary>
        public void Dispose()
        {
            _inputFcd.UnregisterInputCodes( _inputHashCode );

            Stop();
        }

        /// <summary>
        /// 毎フレーム呼び出し、CONFIRMの押下状態と現在の場面から早送りの開始・停止を判定します。
        /// 戦闘ルーチンがシーケンス等で中断中であっても判定が必要なため、GameMainのUpdateから直接呼び出します
        /// </summary>
        public void Update()
        {
            if( !_inputFcd.IsConfirmHeld() || !IsFastForwardAvailable() )
            {
                Stop();
                return;
            }

            IsActive        = true;
            Time.timeScale  = _optionHdlr.BattleFastForwardRate;

            // 一定時間以上早送りを続けた場合、操作が必要な場面へ切り替わった後にCONFIRMを離した瞬間の入力が
            // 決定入力として扱われないよう、CONFIRMを一度離すまで無効化する
            _activeElapsed += Time.unscaledDeltaTime;
            if( BATTLE_FAST_FORWARD_SUPPRESS_CONFIRM_TIME <= _activeElapsed )
            {
                _inputFcd.SuppressConfirmUntilReleased();
            }
        }

        /// <summary>
        /// 早送りの入力ガイドを表示するかを返します(入力コードの有効判定)。
        /// 早送りが可能な場面のうち、CONFIRMを押していない(早送りしていない)間のみ表示し、
        /// 押している(早送り中の)間は非表示にします
        /// </summary>
        private bool CanShowFastForwardGuide()
        {
            return !_inputFcd.IsConfirmHeld() && IsFastForwardAvailable();
        }

        /// <summary>
        /// 早送りが可能な場面かを返します
        /// </summary>
        private bool IsFastForwardAvailable()
        {
            // 自身の入力コードもCONFIRMを持つため、判定対象から除外する(再帰呼び出しの防止)
            return IsWatchingScene() && !_inputFcd.IsAnyConfirmInputEnabled( _inputHashCode );
        }

        /// <summary>
        /// 入力ガイド表示用の入力コードの受付処理です。早送りはUpdate()で処理するため、何も行いません。
        /// (falseを返すことで、同じフレームの他の入力コードの受付を妨げないようにする)
        /// </summary>
        private bool AcceptFastForward( InputContext context )
        {
            return false;
        }

        /// <summary>
        /// ユーザーが操作せずに眺めている場面(シーケンスの再生中、または敵・第三勢力のフェーズ中)かを返します
        /// </summary>
        private bool IsWatchingScene()
        {
            if( !_sequenceFcd.IsEmptySequence() ) { return true; }

            var phase = _btlRtnCtrl.CurrentPhase;
            return phase == BattlePhaseType.Enemy || phase == BattlePhaseType.Other;
        }

        /// <summary>
        /// 早送りを停止し、等速に戻します
        /// </summary>
        public void Stop()
        {
            _activeElapsed = 0f;

            if( !IsActive ) { return; }

            IsActive        = false;
            Time.timeScale  = 1f;
        }
    }
}
