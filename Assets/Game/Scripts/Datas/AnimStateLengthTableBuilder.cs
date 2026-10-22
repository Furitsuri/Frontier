#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// プロジェクト内の全ての Animator Controller から、各アニメーションステート(AnimDatas.AnimeConditionsTag)の再生時間と
    /// ステートのクリップに設定された全てのアニメーションイベントの時刻を収集し、
    /// AnimStateLengthTable へ保存します。
    /// Animator Controller・アニメーションクリップ・モデルが追加・変更された際に自動で再収集します。
    /// 手動で再収集する場合は、メニューの Frontier/Animation/Rebuild Anim State Length Table から実行してください。
    /// </summary>
    public class AnimStateLengthTableBuilder : AssetPostprocessor
    {
        private const string AssetPath = "Assets/Resources/" + AnimStateLengthTable.ResourcesPath + ".asset";

        // 収集対象のアセットの拡張子(これらが変更された場合に再収集する)
        private static readonly string[] WatchExtensions = { ".controller", ".overridecontroller", ".anim", ".fbx" };

        private static bool _isRebuildScheduled = false;

        [MenuItem( "Frontier/Animation/Rebuild Anim State Length Table" )]
        public static void Rebuild()
        {
            var table = LoadOrCreateTable();

            var newEntries = new List<AnimStateLengthTable.Entry>();
            foreach( var guid in AssetDatabase.FindAssets( "t:AnimatorController" ) )
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>( AssetDatabase.GUIDToAssetPath( guid ) );
                if( controller == null ) { continue; }

                newEntries.Add( CreateEntry( controller, controller, clip => clip ) );
            }
            foreach( var guid in AssetDatabase.FindAssets( "t:AnimatorOverrideController" ) )
            {
                var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>( AssetDatabase.GUIDToAssetPath( guid ) );
                if( overrideCtrl == null || !( overrideCtrl.runtimeAnimatorController is AnimatorController baseController ) ) { continue; }

                // 上書きされたクリップがあればそちらの長さを用いる
                newEntries.Add( CreateEntry( overrideCtrl, baseController, clip => overrideCtrl[clip] ?? clip ) );
            }

            // 内容に変化がなければ保存しない(不要なアセットの更新を避けるため)
            if( IsSameEntries( table.Entries, newEntries ) ) { return; }

            table.Entries.Clear();
            table.Entries.AddRange( newEntries );
            table.InvalidateLookup();

            EditorUtility.SetDirty( table );
            AssetDatabase.SaveAssetIfDirty( table );
        }

        private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
        {
            bool isChanged = importedAssets.Concat( deletedAssets ).Concat( movedAssets ).Any( IsWatchTarget );
            if( !isChanged || _isRebuildScheduled ) { return; }

            // インポート処理の最中にアセットを生成・保存しないよう、インポート完了後にまとめて1回だけ再収集する
            _isRebuildScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _isRebuildScheduled = false;
                Rebuild();
            };
        }

        private static bool IsWatchTarget( string path )
        {
            return WatchExtensions.Any( ext => path.EndsWith( ext, StringComparison.OrdinalIgnoreCase ) );
        }

        private static AnimStateLengthTable LoadOrCreateTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<AnimStateLengthTable>( AssetPath );
            if( table != null ) { return table; }

            var directory = System.IO.Path.GetDirectoryName( AssetPath ).Replace( '\\', '/' );
            if( !AssetDatabase.IsValidFolder( directory ) )
            {
                var parent = System.IO.Path.GetDirectoryName( directory ).Replace( '\\', '/' );
                AssetDatabase.CreateFolder( parent, System.IO.Path.GetFileName( directory ) );
            }

            table = ScriptableObject.CreateInstance<AnimStateLengthTable>();
            AssetDatabase.CreateAsset( table, AssetPath );
            return table;
        }

        /// <summary>
        /// Animator Controller のベースレイヤーから、AnimDatas.AnimeConditionsTag に対応する名前のステートを探し、再生時間とアニメーションイベントの時刻を求めます
        /// </summary>
        /// <param name="owner">テーブルに登録する Animator Controller</param>
        /// <param name="controller">ステートを探す Animator Controller(AnimatorOverrideControllerの場合はその基となるもの)</param>
        /// <param name="resolveClip">ステートに設定されたクリップから、実際に再生されるクリップを求める処理(AnimatorOverrideController用)</param>
        private static AnimStateLengthTable.Entry CreateEntry( RuntimeAnimatorController owner, AnimatorController controller, Func<AnimationClip, AnimationClip> resolveClip )
        {
            var lengths = Enumerable.Repeat( -1f, AnimDatas.ANIME_CONDITIONS_NAMES.Length ).ToArray();
            var entry   = new AnimStateLengthTable.Entry { Controller = owner, Lengths = lengths };
            if( controller.layers.Length <= 0 ) { return entry; }

            foreach( var state in EnumerateStates( controller.layers[0].stateMachine ) )
            {
                int index = Array.IndexOf( AnimDatas.ANIME_CONDITIONS_NAMES, state.name );
                if( index < 0 ) { continue; }

                // ブレンドツリー等、単一のクリップでないステートは対象外とする
                if( !( state.motion is AnimationClip clip ) ) { continue; }

                var resolvedClip = resolveClip( clip );
                float speed      = Mathf.Abs( state.speed );
                if( resolvedClip == null || speed <= 0f ) { continue; }

                lengths[index] = resolvedClip.length / speed;

                foreach( var animEvent in AnimationUtility.GetAnimationEvents( resolvedClip ) )
                {
                    entry.Events.Add( new AnimStateLengthTable.EventTime { StateIndex = index, FunctionName = animEvent.functionName, Time = animEvent.time / speed } );
                }
            }

            return entry;
        }

        private static IEnumerable<AnimatorState> EnumerateStates( AnimatorStateMachine stateMachine )
        {
            foreach( var childState in stateMachine.states )
            {
                yield return childState.state;
            }
            foreach( var childStateMachine in stateMachine.stateMachines )
            {
                foreach( var state in EnumerateStates( childStateMachine.stateMachine ) )
                {
                    yield return state;
                }
            }
        }

        private static bool IsSameEntries( List<AnimStateLengthTable.Entry> lhs, List<AnimStateLengthTable.Entry> rhs )
        {
            if( lhs.Count != rhs.Count ) { return false; }

            for( int i = 0; i < lhs.Count; ++i )
            {
                if( lhs[i].Controller != rhs[i].Controller ) { return false; }
                if( lhs[i].Lengths == null || !lhs[i].Lengths.SequenceEqual( rhs[i].Lengths ) ) { return false; }
                if( lhs[i].Events == null || !lhs[i].Events.Select( ToKey ).SequenceEqual( rhs[i].Events.Select( ToKey ) ) ) { return false; }
            }

            return true;
        }

        private static (int, string, float) ToKey( AnimStateLengthTable.EventTime eventTime )
        {
            return ( eventTime.StateIndex, eventTime.FunctionName, eventTime.Time );
        }
    }
}

#endif
