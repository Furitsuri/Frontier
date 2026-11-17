#if UNITY_EDITOR

namespace Frontier.DebugTools
{
    /// <summary>
    /// デバッグ表示等のON/OFFをまとめて保持する設定です。
    /// 値の変更はデバッグメニューの項目(またはコード)から行い、表示側はこの値を参照するだけにします
    /// </summary>
    public class DebugSettings
    {
        public bool ShowThinkingTypeLabel = false;  // NPCの頭上に思考タイプ(INTERCEPT型の場合は待ち構え方・集団ID・起動状態も)を表示する
        public bool LogTroopActionTiming  = false;  // 軍全体の行動の調整(TroopActionCoordinator)の処理時間をConsoleへ出力する(計測用。デバッグメニューの「AI TIMING LOG」で切り替える)
        public bool PlayerOneHitKill      = false;  // プレイヤー側の攻撃が、攻撃力・防御力に関わらず必ず対象を倒す(回復は対象外)
        public bool PlayerNoDamage        = false;  // プレイヤー側のキャラクターが、プレイヤー以外から受けるダメージを全て無効化する(回復は対象外)
    }
}

#endif // UNITY_EDITOR
