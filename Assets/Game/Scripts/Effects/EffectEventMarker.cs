using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Frontier
{
    /// <summary>
    /// エフェクトのTimeline上に置き、そのタイミングでイベント(EffectEventID)を発火させるマーカーです。
    /// Timelineウィンドウ上部の Markers 行を右クリック →「Add Effect Event」で追加し、Inspector で種類を選んでください。
    /// 発火したイベントは、PlayableDirector と同じGameObjectにある TimelineEffectPlayer が受け取ります。
    /// </summary>
    [Serializable]
    [DisplayName( "Effect Event" )]
    public class EffectEventMarker : Marker, INotification, INotificationOptionProvider
    {
        [SerializeField]
        private EffectEventID _eventId = EffectEventID.APPLY_EFFECT;

        public EffectEventID EventId => _eventId;

        public PropertyName id => new PropertyName( nameof( EffectEventMarker ) );

        // Retroactive : 再生開始位置がマーカーより後ろだった場合も発火させる(効果の適用漏れを防ぐ)
        // TriggerOnce : ループ・巻き戻し等で同じマーカーを二重に発火させない
        NotificationFlags INotificationOptionProvider.flags => NotificationFlags.Retroactive | NotificationFlags.TriggerOnce;
    }
}
