using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Frontier
{
    /// <summary>
    /// Timeline(PlayableDirector)で構成されたエフェクトを再生し、Timeline上の EffectEventMarker をイベントとして通知するコンポーネントです。
    /// エフェクトの見た目(パーティクルはControl Trackで再生)とイベントのタイミングは、どちらもTimelineウィンドウ上で
    /// プレビューしながら編集できるため、スクリプターでなくても調整できます。
    /// MEMO : マーカーはTimeline上部の Markers 行に置いてください。通知はPlayableDirectorのGameObject(=このコンポーネント)に届きます
    /// </summary>
    [RequireComponent( typeof( PlayableDirector ) )]
    public class TimelineEffectPlayer : MonoBehaviour, INotificationReceiver
    {
        private PlayableDirector _director                      = null;
        private readonly HashSet<EffectEventMarker> _firedMarkers = new HashSet<EffectEventMarker>();
        private bool _isPlaying                                 = false;

        /// <summary>
        /// イベントの発火時に呼ばれます
        /// </summary>
        public event Action<EffectEventID> OnEffectEvent;

        public bool IsFinished => !_isPlaying;

        void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.stopped += OnDirectorStopped;
        }

        void OnDestroy()
        {
            if( _director != null ) { _director.stopped -= OnDirectorStopped; }
        }

        /// <summary>
        /// エフェクトの再生を開始します
        /// </summary>
        public void Play()
        {
            _firedMarkers.Clear();
            _isPlaying      = true;
            _director.time  = 0;
            _director.Play();
        }

        public void OnNotify( Playable origin, INotification notification, object context )
        {
            if( !_isPlaying ) { return; }
            if( notification is EffectEventMarker marker )
            {
                Fire( marker );
            }
        }

        private void OnDirectorStopped( PlayableDirector director )
        {
            if( !_isPlaying ) { return; }

            // マーカーがTimelineの終端に置かれていた等で通知されなかった場合も、効果の適用漏れが起きないよう全て発火させてから終える
            foreach( var marker in CollectMarkers().OrderBy( m => m.time ) )
            {
                Fire( marker );
            }

            _isPlaying = false;
        }

        private void Fire( EffectEventMarker marker )
        {
            if( !_firedMarkers.Add( marker ) ) { return; }

            OnEffectEvent?.Invoke( marker.EventId );
        }

        private IEnumerable<EffectEventMarker> CollectMarkers()
        {
            if( !( _director.playableAsset is TimelineAsset timeline ) ) { yield break; }

            if( timeline.markerTrack != null )
            {
                foreach( var marker in timeline.markerTrack.GetMarkers().OfType<EffectEventMarker>() ) { yield return marker; }
            }

            foreach( var track in timeline.GetOutputTracks() )
            {
                foreach( var marker in track.GetMarkers().OfType<EffectEventMarker>() ) { yield return marker; }
            }
        }
    }
}
