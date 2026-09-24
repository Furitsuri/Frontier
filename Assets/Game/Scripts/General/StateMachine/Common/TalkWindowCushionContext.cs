using Frontier.UI;

namespace Frontier.StateMachine
{
    /// <summary>
    /// TalkWindowCushionState に渡すコンテキストです。表示する話者名・メッセージのLocKeyと、表示位置を保持します。
    /// </summary>
    public class TalkWindowCushionContext
    {
        public LocKey SpeakerKey { get; }
        public LocKey MessageKey { get; }
        public TalkWindowPosition Position { get; }

        /// <param name="position">表示位置(既定は画面右上)</param>
        public TalkWindowCushionContext( LocKey speakerKey, LocKey messageKey, TalkWindowPosition position = TalkWindowPosition.TopRight )
        {
            SpeakerKey = speakerKey;
            MessageKey = messageKey;
            Position   = position;
        }
    }
}
