namespace Frontier.Entities
{
    public class StatusEffectMoveBase : StatusEffectElementBase
    {
        public StatusEffectMoveBase()
        {
            _additionalBitFlagByCategory = StatusEffectMoveCategory.GetAdditionalBitByCategory();
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを過重力状態用に変更する
        /// </summary>
        public override void ApplyEffect()
        {
        }
    }
}