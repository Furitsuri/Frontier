using DG.Tweening;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// PlSelectSkillState遷移時、SkillBoxUIを縦一列の選択レイアウトへDOTweenでアニメーション移動させます。
    /// </summary>
    public class SkillBoxLayoutAnimator
    {
        private const float BASE_OFFSET_Y = 30.0f;   // 1つ目のSkillBoxを基準とした基点までのYオフセット
        private const float PADDING_Y     = 10.0f;   // SkillBox間のY方向のpadding
        private const float MOVE_DURATION = 0.1f;    // 移動アニメーションにかける時間(全SkillBox共通)

        private Vector2[] _originalPositions;   // AnimateToSelectionLayout時点でのSkillBox毎の元の位置

        /// <summary>
        /// SkillBoxを、1つ目のSkillBoxのY軸30程度下を基点とした縦一列に並び替えます
        /// </summary>
        /// <param name="skillBoxes">並び替え対象のSkillBox(インデックス順に上から並びます)</param>
        public void AnimateToSelectionLayout( SkillBoxUI[] skillBoxes )
        {
            if( skillBoxes == null || skillBoxes.Length == 0 ) { return; }

            _originalPositions = new Vector2[skillBoxes.Length];
            for( int i = 0; i < skillBoxes.Length; ++i )
            {
                _originalPositions[i] = ( ( RectTransform ) skillBoxes[i].transform ).anchoredPosition;
            }

            var firstRect   = ( RectTransform ) skillBoxes[0].transform;
            float baseX     = firstRect.anchoredPosition.x;
            float baseY     = firstRect.anchoredPosition.y - BASE_OFFSET_Y;
            float boxHeight = firstRect.rect.height;

            for( int i = 0; i < skillBoxes.Length; ++i )
            {
                var rect      = ( RectTransform ) skillBoxes[i].transform;
                var targetPos = new Vector2( baseX, baseY - i * ( boxHeight + PADDING_Y ) );

                rect.DOKill();
                rect.DOAnchorPos( targetPos, MOVE_DURATION );
            }
        }

        /// <summary>
        /// SkillBoxを、AnimateToSelectionLayoutで並び替える前の元の位置へ戻します
        /// </summary>
        /// <param name="skillBoxes">対象のSkillBox(AnimateToSelectionLayoutに渡したものと同じ配列)</param>
        public void RevertToOriginalLayout( SkillBoxUI[] skillBoxes )
        {
            if( skillBoxes == null || _originalPositions == null ) { return; }

            for( int i = 0; i < skillBoxes.Length && i < _originalPositions.Length; ++i )
            {
                var rect = ( RectTransform ) skillBoxes[i].transform;

                rect.DOKill();
                rect.DOAnchorPos( _originalPositions[i], MOVE_DURATION );
            }
        }
    }
}
