using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// Animator Controller ごとに、各アニメーションステート(AnimDatas.AnimeConditionsTag)の再生時間と、
    /// ステートのクリップに設定されたアニメーションイベントの時刻を保持するテーブルです。
    /// 実行時の API ではステートとクリップの対応を取得出来ないため、エディタ上で AnimStateLengthTableBuilder が自動で収集・保存します。
    /// (Animator Controller・アニメーションクリップの追加・変更時に自動で再収集されるため、手動での設定は不要です)
    /// Resources/AnimData/AnimStateLengthTable.asset から読み込みます。
    /// </summary>
    public class AnimStateLengthTable : ScriptableObject
    {
        /// <summary>
        /// ステートのクリップに設定されたアニメーションイベント1件分の情報です
        /// </summary>
        [Serializable]
        public class EventTime
        {
            public int StateIndex;          // AnimDatas.AnimeConditionsTag の値
            public string FunctionName;     // イベントの関数名
            public float Time;              // ステートの開始からイベントまでの時間(秒)。ステートの再生速度を反映した値
        }

        [Serializable]
        public class Entry
        {
            public RuntimeAnimatorController Controller;
            // AnimDatas.AnimeConditionsTag をインデックスとする再生時間(秒)。ステートが存在しない場合は負の値
            public float[] Lengths;
            // 各ステートのクリップに設定されたアニメーションイベントの一覧
            public List<EventTime> Events = new List<EventTime>();
        }

        public const string ResourcesPath = "AnimData/AnimStateLengthTable";

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private static AnimStateLengthTable _instance = null;
        private Dictionary<RuntimeAnimatorController, Entry> _lookup = null;

        public List<Entry> Entries => _entries;

        private static AnimStateLengthTable Instance
        {
            get
            {
                if( _instance == null )
                {
                    _instance = Resources.Load<AnimStateLengthTable>( ResourcesPath );
                    if( _instance == null )
                    {
                        Debug.LogWarning( $"[AnimStateLengthTable] テーブルが見つかりません: Resources/{ResourcesPath}.asset" );
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// 指定の Animator Controller における、指定ステートの再生時間を取得します
        /// </summary>
        /// <param name="controller">対象の Animator Controller</param>
        /// <param name="animTag">対象のステート</param>
        /// <param name="length">再生時間(秒)。ステートの再生速度を反映した値</param>
        /// <returns>取得出来た場合はtrue</returns>
        public static bool TryGetLength( RuntimeAnimatorController controller, AnimDatas.AnimeConditionsTag animTag, out float length )
        {
            length = 0f;

            if( !TryGetEntry( controller, out var entry ) ) { return false; }

            int index = ( int ) animTag;
            if( entry.Lengths == null || index < 0 || entry.Lengths.Length <= index || entry.Lengths[index] < 0f ) { return false; }

            length = entry.Lengths[index];
            return true;
        }

        /// <summary>
        /// 指定の Animator Controller における、指定ステートの開始から指定のアニメーションイベントまでの時間を取得します。
        /// 同じ関数名のイベントが複数設定されている場合は、最も早いものの時間を返します。
        /// </summary>
        /// <param name="controller">対象の Animator Controller</param>
        /// <param name="animTag">対象のステート</param>
        /// <param name="functionName">イベントの関数名。nameof( BattleAnimationEventReceiver.XXX ) の形で指定してください</param>
        /// <param name="time">イベントまでの時間(秒)。ステートの再生速度を反映した値</param>
        /// <returns>取得出来た場合はtrue(イベントが設定されていない場合はfalse)</returns>
        public static bool TryGetEventTime( RuntimeAnimatorController controller, AnimDatas.AnimeConditionsTag animTag, string functionName, out float time )
        {
            time = 0f;

            if( !TryGetEntry( controller, out var entry ) || entry.Events == null ) { return false; }

            bool isFound = false;
            foreach( var eventTime in entry.Events )
            {
                if( eventTime.StateIndex != ( int ) animTag || eventTime.FunctionName != functionName ) { continue; }
                if( isFound && time <= eventTime.Time ) { continue; }

                time    = eventTime.Time;
                isFound = true;
            }

            return isFound;
        }

        /// <summary>
        /// エディタ上でテーブルが再収集された際に呼び出し、検索用のキャッシュを破棄します
        /// </summary>
        public void InvalidateLookup()
        {
            _lookup = null;
        }

        private static bool TryGetEntry( RuntimeAnimatorController controller, out Entry entry )
        {
            entry = null;

            var table = Instance;
            if( table == null || controller == null ) { return false; }

            table.BuildLookupIfNeeded();
            return table._lookup.TryGetValue( controller, out entry );
        }

        private void BuildLookupIfNeeded()
        {
            if( _lookup != null ) { return; }

            _lookup = new Dictionary<RuntimeAnimatorController, Entry>();
            foreach( var entry in _entries )
            {
                if( entry.Controller == null ) { continue; }
                _lookup[entry.Controller] = entry;
            }
        }
    }
}
