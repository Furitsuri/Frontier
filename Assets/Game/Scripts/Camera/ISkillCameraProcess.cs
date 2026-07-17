using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// スキル実行中のカメラ演出を担うインターフェースです。
    /// SkillActionBase がフィールドとして保持し、スキル自身のライフサイクル(Start/Update/End)に連動して
    /// BattleCameraController 経由で呼び出されます。
    ///
    /// Begin で渡される fromPosition/fromRotation は、直前の演出(前のスキルや連携演出、あるいは
    /// 通常の FOLLOWING モード)が実際にカメラを置いていた姿勢です。ここを起点にLerpするなどして
    /// 演出を繋げることで、スキルをまたいでも連続的なカメラ移動に見せることができます。
    /// </summary>
    public interface ISkillCameraProcess : IBattleCameraSequence
    {
        /// <summary>
        /// カメラ演出を開始します。
        /// </summary>
        /// <param name="sharedState">BattleCameraController と共有するカメラ状態</param>
        /// <param name="fromPosition">開始時点の実際のカメラ位置(直前の演出からの引き継ぎ点)</param>
        /// <param name="fromRotation">開始時点の実際のカメラ回転(直前の演出からの引き継ぎ点)</param>
        void Begin( BattleCameraSharedState sharedState, Vector3 fromPosition, Quaternion fromRotation );

        /// <summary>
        /// 次のフェーズへ遷移します。呼び出しタイミングはスキル側の判断に委ねられます
        /// (例: 被弾直前に寄りのアングルへ切り替える、など)。呼ばれた瞬間の実際のカメラ姿勢を起点に
        /// 連続的に遷移するため、いつ呼んでも不自然な繋がりにはなりません。
        /// 複数フェーズを持たない実装では何もしなくて構いません。最終フェーズで呼ばれた場合も何もしません。
        /// </summary>
        void TransitNextPhase();

        /// <summary>
        /// カメラ演出を終了します。次の演出(次のスキルや連携終了後のFOLLOWING復帰など)への
        /// 引き継ぎは呼び出し側(BattleCameraController)が行うため、ここでは自身の後始末のみ行ってください。
        /// </summary>
        void End();
    }
}
