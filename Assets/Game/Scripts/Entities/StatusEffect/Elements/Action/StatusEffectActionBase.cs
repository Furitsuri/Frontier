namespace Frontier.Entities
{
    public class StatusEffectActionBase : StatusEffectElementBase
    {
        public StatusEffectActionBase()
        {
            _additionalBitFlagByCategory = StatusEffectActionCategory.GetAdditionalBitByCategory();
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを過重力状態用に変更する
        /// </summary>
        override public void ApplyEffect()
        {
        }
    }
}