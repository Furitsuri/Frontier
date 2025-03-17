public class AcceptBooleanInput : IAcceptInput<bool>
{
    public delegate void AcceptBooleanInputCallback( bool IsInput );

    private AcceptBooleanInputCallback _callback;

    public AcceptBooleanInput(AcceptBooleanInputCallback callback)
    {
        _callback = callback;
    }

    public void Accept( bool IsInput )
    {
        _callback( IsInput );
    }
}