using Frontier.Entities;
using Frontier.Loaders;
using Frontier.Sequences;
using Frontier.UI;

namespace Frontier.Combat
{
    public class SkillActionBase : ISequence
    {
        // スキル用カメラデータ(Assets/Resources/CameraData/Skills/)が未整備の場合に使う既定値。
        // 現行の連携攻撃(BattleCameraController._coopCameraXZDistance等)と同じ値にしてあります
        private const float DEFAULT_CAMERA_XZ_DISTANCE = 5.0f;
        private const float DEFAULT_CAMERA_HEIGHT      = 4.0f;
        private const float DEFAULT_CAMERA_DURATION    = 0.5f;

        protected Character              _owner        = null;
        protected IUiSystem              _uiSystem     = null;
        protected BattleCameraController _btlCamCtrl   = null;
        protected BattleFileLoader       _btlFileLoader = null;

        /// <summary>
        /// このスキルのID。継承先のコンストラクタで設定してください(例: _skillID = SkillID.DASH_SLASH;)。
        /// SkillID.NONE のままであれば、カメラ演出の自動セットアップは行われません。
        /// </summary>
        protected SkillID _skillID = SkillID.NONE;

        /// <summary>
        /// このスキル実行中のカメラ演出。SetupCameraProcess() などで設定してください。
        /// null のままであれば、カメラ演出は行われず既定の挙動(FOLLOWINGモードなど)のままになります。
        /// </summary>
        protected ISkillCameraProcess _cameraProcess = null;

        public SkillActionBase( Character owner )
        {
            _owner = owner;
        }

        public SkillActionBase( Character owner, IUiSystem uiSystem, BattleCameraController btlCamCtrl, BattleFileLoader btlFileLoader )
        {
            _owner         = owner;
            _uiSystem      = uiSystem;
            _btlCamCtrl    = btlCamCtrl;
            _btlFileLoader = btlFileLoader;
        }

        public void Start()
        {
            StartAction();

            if( _cameraProcess != null ) { _btlCamCtrl?.StartSkillCameraProcess( _cameraProcess ); }
        }

        public void End()
        {
            EndAction();

            if( _cameraProcess != null ) { _btlCamCtrl?.EndSkillCameraProcess(); }
        }

        public bool Update()
        {
            UpdateAction();

            return IsFinished();
        }

        /// <summary>
        /// スキル名表示の直前に呼ばれます。デフォルトではゴーストを非表示にします。
        /// ゴーストの参照は保持されるため、サブクラスは StartAction() 内で引き続き参照できます。
        /// </summary>
        public virtual void OnBeforeNameDisplay()
        {
            _owner?.GhostObj?.gameObject.SetActive( false );
        }

        protected virtual void StartAction()
        {
        }

        protected virtual void EndAction()
        {
        }

        protected virtual void UpdateAction()
        {
        }

        protected virtual bool IsFinished()
        {
            return true;
        }

        /// <summary>
        /// 対象キャラクターにダメージを適用し、HPに応じて死亡/被弾アニメーションを再生します。
        /// 連携攻撃中でまだ後続のヒットが予定されている場合(RemainingCooperativeHits参照)は、
        /// HPが0以下でも死亡アニメーションを再生させず、連携最後のヒットまで被弾アニメーションを再生します。
        /// </summary>
        protected void ApplyDamageToTarget( Character target )
        {
            int hpChange = target.BattleParams.TmpParam.ExpectedHpChange;
            target.GetStatusRef.CurHP += hpChange;

            target.BattleParams.TmpParam.ConsumeCooperativeHit();
            bool isFinalHit = target.BattleParams.TmpParam.RemainingCooperativeHits <= 0;

            if( hpChange != 0 )
            {
                if( isFinalHit && target.GetStatusRef.CurHP <= 0 )
                {
                    target.GetStatusRef.CurHP = 0;
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DIE );
                }
                else
                {
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
                }
            }

            _uiSystem.BattleUi.ShowDamageOnCharacter( target, 1f );
        }

        /// <summary>
        /// skillID に対応するカメラデータを読み込み、target を注視するオフセット型カメラ演出(TargetOffsetCameraProcess)を
        /// _cameraProcess に設定します。データファイルが存在しない場合は既定値(現行の連携攻撃と同じ数値)を使用します。
        /// 継承先は読み込むスキルIDと注視対象キャラクターを指定するだけで済みます。
        /// </summary>
        protected void SetupCameraProcess( SkillID skillID, Character target )
        {
            if( target == null ) { return; }

            float xzDistance = DEFAULT_CAMERA_XZ_DISTANCE;
            float height     = DEFAULT_CAMERA_HEIGHT;
            float duration   = DEFAULT_CAMERA_DURATION;

            var camParams = _btlFileLoader?.LoadSkillCameraParams( skillID );
            if( camParams != null && camParams.Count > 0 && camParams[0].Length > 0 )
            {
                var data   = camParams[0][0];
                xzDistance = data.XZDistance;
                height     = data.Height;
                duration   = data.Duration;
            }

            _cameraProcess = new TargetOffsetCameraProcess( _owner, target, xzDistance, height, duration );
        }
    }
}