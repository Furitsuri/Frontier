namespace Frontier
{
    /// <summary>
    /// エフェクト内の任意のタイミングで発火するイベントの種類です。
    /// キャラクターのモーションにおけるアニメーションイベントと同様、エフェクト側(Timeline上のEffectEventMarker)に「いつ」を、
    /// 再生する側(スキル等)に「何をするか」を持たせるために用います。
    /// </summary>
    public enum EffectEventID
    {
        APPLY_EFFECT,   // 効果の適用(回復量の反映・表示など)
    }
}
