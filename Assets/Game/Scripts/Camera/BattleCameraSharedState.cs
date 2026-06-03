using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// BattleCameraController と各 IBattleCameraSequence ハンドラが共有するカメラ状態です。
    /// MonoBehaviour 側の SerializeField や入力ロジックには含まれない、
    /// 「ハンドラが読み書きし、FOLLOWING モードが参照する」座標・参照のみを保持します。
    /// </summary>
    public class BattleCameraSharedState
    {
        // 外部参照（Setup 時に一度だけ設定）
        public Camera             MainCamera;
        public IUiSystem          UiSystem;
        public CameraMosaicEffect MosaicEffect;

        // 共有座標（ハンドラと FOLLOWING モード双方が読み書き）
        public Vector3 PrevCameraPosition;   // 直前フレームのカメラ位置（Lerp 開始点）
        public Vector3 LookAtPosition;       // 現在の注視対象ワールド座標
        public Vector3 PrevLookAtPosition;   // 直前フレームの注視座標（BATTLE_FIELD 内 Lerp 用）
        public Vector3 FollowingPosition;    // FOLLOWING モードのカメラ目標座標
    }
}
