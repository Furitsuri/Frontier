namespace Frontier.Entities
{
    public class Npc : Character
    {
        public ThinkingType ThinkingType { get; set; } = ThinkingType.BASE;

        public override void Init()
        {
            base.Init();
        }
    }
}