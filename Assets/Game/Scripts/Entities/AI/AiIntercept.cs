using Frontier.Combat;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// 迎撃型のAI。その場で待ち構え、自身の行動範囲(攻撃範囲)に対象が入ってきたら行動を開始します。
    /// 行動を開始する契機は、所属する集団(単独型の場合は自身のみの集団)単位で InterceptGroupRegistry が判定します。
    /// 行動開始後はAGGRESSIVEと同じ動作をします。
    /// TODO : 現在は暫定的にAiAggressiveを継承することで行動開始後の動作を共有している。
    ///        AGGRESSIVEと共通の行動決定処理を用意した際は、継承をやめてそれを呼び出す形にする
    /// </summary>
    public class AiIntercept : AiAggressive
    {
        private InterceptType _interceptType    = InterceptType.SOLO;   // 待ち構え方のタイプ
        private int _interceptGroupId           = 0;                    // 待ち構え方が集団型である場合の集団ID
        private bool _isActivated               = false;                // 行動を開始しているか(一度開始した後は待ち構えの状態には戻らない)

        public InterceptType InterceptType => _interceptType;
        public int InterceptGroupId => _interceptGroupId;
        public bool IsActivated => _isActivated;

        /// <summary>
        /// 思考に関する設定から、待ち構え方のタイプと集団IDを取り込みます
        /// </summary>
        public override void ApplyThinkingSetting( in ThinkingSetting setting )
        {
            _interceptType      = setting.InterceptType;
            _interceptGroupId   = setting.InterceptGroupId;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init( Character owner )
        {
            base.Init( owner );

            _isActivated = false;

            _btlRtnCtrl.BtlCharaCdr.InterceptGroupReg.Register( this, owner.GetStatusRef.characterTag, _interceptType, _interceptGroupId );
        }

        /// <summary>
        /// 行動を開始させます
        /// </summary>
        public void Activate()
        {
            _isActivated = true;
        }

        /// <summary>
        /// 自身の行動範囲(現在の位置から移動した上で攻撃が届く範囲)に、攻撃対象が存在するかを取得します
        /// </summary>
        /// <returns>攻撃対象が存在するか</returns>
        public bool IsOpponentInActionRange()
        {
            return _owner.BattleLogic.ActionRangeCtrl.HasAttackTargetInDangerRange();
        }

        /// <summary>
        /// 目的地と攻撃対象を決定します。
        /// 行動を開始していない間は移動も攻撃も行わず、行動開始後はAGGRESSIVEと同じ決定を行います
        /// </summary>
        /// <returns>有効となる目的地及び攻撃対象がそれぞれ設定されたか否か</returns>
        public override (bool, bool) DetermineDestinationAndTarget( BattleParameters battleParams, in Status status, in int[] ownerTileCosts, in CharacterKey ownerKey )
        {
            if( !_isActivated )
            {
                _isDetermined = true;

                return (false, false);
            }

            return base.DetermineDestinationAndTarget( battleParams, in status, in ownerTileCosts, in ownerKey );
        }
    }
}
