#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Frontier.Entities;
using System.IO;
using UnityEngine;

namespace Frontier.DebugTools
{
    /// <summary>
    /// StreamingAssets/Debug/DebugUserData.json を読み込んで UserDomain に適用するローダー。
    /// UNITY_EDITOR または DEVELOPMENT_BUILD のビルドでのみ動作します。
    /// </summary>
    public static class DebugUserDataLoader
    {
        private static bool _hasApplied = false;

        private static string FilePath =>
            Path.Combine( Application.streamingAssetsPath, "Debug", "DebugUserData.json" );

        /// <summary>
        /// JSONファイルが存在する場合に読み込み、UserDomain へ反映します。
        /// プレイセッション中に1度だけ実行されます（2回目以降は即リターン）。
        /// ファイルが存在しない場合は何もしません（通常の起動フローで代替されます）。
        /// </summary>
        public static void TryApply( UserDomain userDomain )
        {
            if ( _hasApplied ) return;
            _hasApplied = true;

            if ( !File.Exists( FilePath ) )
            {
                Debug.Log( $"[DebugUserDataLoader] ファイルが存在しないためスキップします: {FilePath}" );
                return;
            }

            string json = File.ReadAllText( FilePath );
            var data = JsonUtility.FromJson<DebugUserData>( json );
            if ( data == null )
            {
                Debug.LogWarning( "[DebugUserDataLoader] JSONのパースに失敗しました。ファイル内容を確認してください。" );
                return;
            }

            userDomain.Debug_SetMoney( data.money );
            userDomain.Debug_SetStageLevel( data.stageLevel );

            if ( data.overrideMembers )
            {
                userDomain.Debug_ClearMembers();

                foreach ( var entry in data.members )
                {
                    userDomain.RecruitMember( entry.ToStatus() );
                }

                Debug.Log( $"[DebugUserDataLoader] デバッグデータを適用しました。Money={data.money} StageLevel={data.stageLevel} Members={data.members.Count}体" );
            }
            else
            {
                Debug.Log( $"[DebugUserDataLoader] デバッグデータを適用しました。Money={data.money} StageLevel={data.stageLevel} (Members は上書きなし)" );
            }
        }
    }
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
