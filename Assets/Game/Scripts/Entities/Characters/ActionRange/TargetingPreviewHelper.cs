using System.Collections.Generic;

namespace Frontier.Entities
{
    /// <summary>
    /// 攻撃対象選択中、対象キャラクター全員の頭上HPゲージへ、予測ダメージ分の点滅表示を反映する共通処理です。
    /// SkillTargetSelector・PlAttackState等、対象リスト(attackTargetCharaKeys)が変化しうるタイミングで
    /// 呼び出してください。呼び出しの都度、全キャラクターの点滅表示を一旦クリアしてから現在の対象リストへ
    /// 改めて反映し直すため、対象の増減を個別に追跡する必要はありません。
    /// </summary>
    public static class TargetingPreviewHelper
    {
        public static void RefreshPredictedDamageGauges( TargetingRangeContext context, List<CharacterKey> attackTargetCharaKeys )
        {
            context.Presenter.ClearAllPredictedDamage();

            if( attackTargetCharaKeys == null ) { return; }

            foreach( var key in attackTargetCharaKeys )
            {
                var target = context.BtlRtnCtrl.BtlCharaCdr.GetCharacter( key );
                if( target == null ) { continue; }

                var ( _, total ) = context.BtlRtnCtrl.BtlCharaCdr.CalculateExpectedHpChange( context.Owner, target );
                context.Presenter.SetPredictedDamageOnCharacter( target, -total );
            }
        }
    }
}
