namespace Frontier.Sequences
{
    public abstract class SequenceCreator<TParam>
    {
        public abstract ISequence CreateSequence( TParam param );
    }
}