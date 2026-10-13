namespace Frontier.Entities
{
    public class Npc : Character
    {
        public ThinkingSetting ThinkingSetting { get; set; }    // AIの生成時に渡す、思考に関する設定

        public override void Init()
        {
            base.Init();
        }
    }
}