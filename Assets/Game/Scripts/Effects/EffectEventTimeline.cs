using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// エフェクト内の任意のタイミングで発火するイベントの種類です。
    /// キャラクターのモーションにおけるアニメーションイベントと同様、エフェクト側に「いつ」を、
    /// 再生する側(スキル等)に「何をするか」を持たせるために用います。
    /// </summary>
    public enum EffectEventID
    {
        APPLY_EFFECT,   // 効果の適用(回復量の反映・表示など)
    }

    /// <summary>
    /// エフェクトのプレハブに付け、再生開始からの経過時間に応じてイベントを発火させるコンポーネントです。
    /// イベントの時刻と種類はInspector上で編集できるため、エフェクトの見た目に合わせて
    /// 「どのタイミングで回復量を表示するか」等をデザイナー側で調整できます。
    /// 再生は Play() で開始し、子階層を含む全ての ParticleSystem も同時に再生します。
    /// </summary>
    public class EffectEventTimeline : MonoBehaviour
    {
        [Serializable]
        public struct EffectEventEntry
        {
            [Tooltip( "再生開始からの経過秒数" )]
            public float Time;
            public EffectEventID Id;
        }

        [SerializeField]
        [Tooltip( "エフェクト全体の長さ(秒)。この時間が経過すると IsFinished が true になります" )]
        private float _duration = 1.5f;

        [SerializeField]
        private List<EffectEventEntry> _events = new List<EffectEventEntry>();

        private ParticleSystem[] _particles = null;
        private float _elapsedTime          = 0f;
        private int _nextEventIndex         = 0;
        private bool _isPlaying             = false;

        /// <summary>
        /// イベントの発火時に呼ばれます
        /// </summary>
        public event Action<EffectEventID> OnEffectEvent;

        public bool IsFinished => !_isPlaying;

        /// <summary>
        /// エフェクトの再生を開始します
        /// </summary>
        public void Play()
        {
            // 時刻順に発火させるため、Inspector上の並び順に依らず並べ替えておく
            _events.Sort( ( a, b ) => a.Time.CompareTo( b.Time ) );

            _particles ??= GetComponentsInChildren<ParticleSystem>( true );
            foreach( var particle in _particles )
            {
                particle.Clear( false );
                particle.Play( false );
            }

            _elapsedTime    = 0f;
            _nextEventIndex = 0;
            _isPlaying      = true;
        }

        void Update()
        {
            if( !_isPlaying ) { return; }

            // パーティクルと同じ時間(Time.timeScaleの影響を受ける)で進めることで、早送り中も見た目とイベントのタイミングがずれないようにする
            _elapsedTime += Time.deltaTime;

            FireEventsUntil( _elapsedTime );

            if( _duration <= _elapsedTime )
            {
                // イベントの時刻がエフェクトの長さを超えて設定されていた場合も、効果の適用漏れが起きないよう全て発火させてから終える
                FireEventsUntil( float.MaxValue );
                _isPlaying = false;
            }
        }

        private void FireEventsUntil( float time )
        {
            while( _nextEventIndex < _events.Count && _events[_nextEventIndex].Time <= time )
            {
                var id = _events[_nextEventIndex].Id;
                ++_nextEventIndex;
                OnEffectEvent?.Invoke( id );
            }
        }
    }
}
