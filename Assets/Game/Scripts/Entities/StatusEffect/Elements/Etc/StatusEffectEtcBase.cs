namespace Frontier.Entities
{
    public class StatusEffectEtcBase : StatusEffectElementBase
    {
        public StatusEffectEtcBase()
        {
            _additionalBitFlagByCategory = StatusEffectEtcCategory.GetAdditionalBitByCategory();
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを過重力状態用に変更する
        /// </summary>
        public override void ApplyEffect()
        {
        }
    }
}