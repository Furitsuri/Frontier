using Frontier.Combat.Skill;

namespace Frontier.Entities
{
    public class Npc : Character
    {
        public override void Init()
        {
            base.Init();
        }

        /// <summary>
        /// 攻撃可能範囲の表示・非表示を切り替えます
        /// </summary>
        virtual public void ToggleDisplayDangerRange()
        {
        }

        /// <summary>
        /// 攻撃可能範囲の表示・非表示を設定します
        /// </summary>
        /// <param name="isShow"></param>
        virtual public void SetDisplayDangerRange( bool isShow )
        {
        }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        public override void SelectUseSkills( SituationType type )
        {

        }
    }
}