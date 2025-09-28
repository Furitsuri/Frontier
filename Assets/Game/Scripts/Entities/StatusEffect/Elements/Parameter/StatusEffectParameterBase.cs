namespace Frontier.Entities
{
    public class StatusEffectParameterBase : StatusEffectElementBase
    {
        public StatusEffectParameterBase()
        {
            _additionalBitFlagByCategory = StatusEffectParameterCategory.GetAdditionalBitByCategory();
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを過重力状態用に変更する
        /// </summary>
        override public void ApplyEffect()
        {
        }
    }
}