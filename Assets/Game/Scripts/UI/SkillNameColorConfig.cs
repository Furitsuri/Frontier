using Frontier.Combat;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// スキル名の表示色の設定です(デザイナーがInspector上で調整するためのアセット)。
    /// スキル名を表示する全ての枠(SkillBoxUI)から、Resources.Load で読み込んで参照されます。
    /// 回復系のスキルは、使用出来る場面に関わらず回復の色で表示します。
    /// </summary>
    [CreateAssetMenu( fileName = "SkillNameColorConfig", menuName = "Frontier/UI/SkillNameColorConfig" )]
    public class SkillNameColorConfig : ScriptableObject
    {
        // Resources配下からの読み込みパス
        public const string RESOURCE_PATH = "Config/SkillNameColorConfig";

        [Header( "使用出来る場面ごとの色" )]
        [SerializeField] private Color _attackColor     = Color.red;                        // 攻撃時
        [SerializeField] private Color _defenceColor    = new Color( 0.1f, 0.6f, 1.0f );    // 防御時
        [SerializeField] private Color _passiveColor    = Color.yellow;                     // パッシブ

        [Header( "スキルの種類ごとの色(使用出来る場面よりも優先)" )]
        [SerializeField] private Color _healColor       = Color.green;                      // 回復系

        /// <summary>
        /// 指定のスキルのスキル名の色を取得します
        /// </summary>
        public Color GetColor( in SkillsData.Data skillData )
        {
            if( ActionType.HEAL == skillData.ActionType ) { return _healColor; }

            return GetColor( skillData.SituationType );
        }

        /// <summary>
        /// 使用出来る場面に対応するスキル名の色を取得します
        /// </summary>
        public Color GetColor( SituationType situationType )
        {
            switch( situationType )
            {
                case SituationType.DEFENCE: return _defenceColor;
                case SituationType.PASSIVE: return _passiveColor;
                default:                    return _attackColor;
            }
        }
    }
}
