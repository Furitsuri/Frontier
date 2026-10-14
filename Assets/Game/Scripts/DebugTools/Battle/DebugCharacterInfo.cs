using Frontier.Entities;
using Frontier.Entities.Ai;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Battle
{
    /// <summary>
    /// デバッグ表示用に、キャラクターの情報を文字列化します。
    /// デバッグメニューの項目と頭上のラベルとで、同じ情報を同じ表記で表示するために共通化しています
    /// </summary>
    public static class DebugCharacterInfo
    {
        public const string NONE_TEXT = "-";    // 対象に該当する情報が存在しない場合の表記

        /// <summary>
        /// キャラクターを識別するための名前を取得します("ENEMY#1" 等)
        /// </summary>
        public static string GetName( Character chara )
        {
            if( chara == null ) { return NONE_TEXT; }

            var status = chara.GetStatusRef;
            return $"{status.characterTag}#{status.characterIndex}";
        }

        /// <summary>
        /// 思考タイプを取得します。思考タイプを持たないキャラクター(プレイヤー)の場合は NONE_TEXT を返します
        /// </summary>
        public static string GetThinkingType( Character chara )
        {
            return ( chara is Npc npc ) ? npc.ThinkingSetting.Type.ToString() : NONE_TEXT;
        }

        /// <summary>
        /// INTERCEPT型のAIを取得します。INTERCEPT型でない場合はnullを返します
        /// </summary>
        public static AiIntercept GetInterceptAi( Character chara )
        {
            return chara?.BattleLogic?.GetAi() as AiIntercept;
        }

        public static string GetInterceptType( Character chara )
        {
            var ai = GetInterceptAi( chara );
            return ( ai != null ) ? ai.InterceptType.ToString() : NONE_TEXT;
        }

        /// <summary>
        /// 集団IDを取得します。集団型でない場合は NONE_TEXT を返します
        /// </summary>
        public static string GetInterceptGroupId( Character chara )
        {
            var ai = GetInterceptAi( chara );
            return ( ai != null && ai.InterceptType == InterceptType.GROUP ) ? ai.InterceptGroupId.ToString() : NONE_TEXT;
        }

        public static string GetInterceptState( Character chara )
        {
            var ai = GetInterceptAi( chara );
            if( ai == null ) { return NONE_TEXT; }

            return ai.IsActivated ? "ACTIVE" : "WAITING";
        }

        /// <summary>
        /// 頭上のラベルに表示する、思考に関する情報をまとめた文字列を取得します。
        /// INTERCEPT型の場合は、2行目に待ち構え方(集団型であれば集団ID)と起動状態を付け加えます
        /// </summary>
        public static string GetThinkingLabel( Character chara )
        {
            string label = GetThinkingType( chara );

            var ai = GetInterceptAi( chara );
            if( ai == null ) { return label; }

            string interceptType = ( ai.InterceptType == InterceptType.GROUP ) ? $"GROUP {ai.InterceptGroupId}" : ai.InterceptType.ToString();
            return $"{label}\n{interceptType} / {GetInterceptState( chara )}";
        }
    }
}

#endif // UNITY_EDITOR
