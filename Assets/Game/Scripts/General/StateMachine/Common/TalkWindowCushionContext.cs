namespace Frontier.StateMachine
{
    /// <summary>
    /// TalkWindowCushionState に渡すコンテキストです。表示する話者名・メッセージのLocKeyを保持します。
    /// </summary>
    public class TalkWindowCushionContext
    {
        public LocKey SpeakerKey { get; }
        public LocKey MessageKey { get; }

        public TalkWindowCushionContext( LocKey speakerKey, LocKey messageKey )
        {
            SpeakerKey = speakerKey;
            MessageKey = messageKey;
        }
    }
}
