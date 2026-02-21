using UnityEngine;

public class AcceptContextInput : IAcceptInputBase, IAcceptGenericInput<InputContext>
{
    public delegate bool AcceptContextInputCallback( InputContext input );

    private AcceptContextInputCallback _callback;

    public AcceptContextInput( AcceptContextInputCallback callback )
    {
        _callback = callback;
    }

    public bool AcceptGenericInput( InputContext input )
    {
        return _callback( input );
    }

    bool IAcceptInputBase.Accept( InputContext context )
    {
        return AcceptGenericInput( context );
    }
}