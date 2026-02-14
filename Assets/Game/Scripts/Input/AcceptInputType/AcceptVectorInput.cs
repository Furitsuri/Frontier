using UnityEngine;

public class AcceptVectorInput : IAcceptInputBase, IAcceptGenericInput<Vector2>
{
    public delegate bool AcceptVectorInputCallback( Vector2 input );

    private AcceptVectorInputCallback _callback;

    public AcceptVectorInput( AcceptVectorInputCallback callback )
    {
        _callback = callback;
    }

    public bool AcceptGenericInput( Vector2 input )
    {
        return _callback( input );
    }

    bool IAcceptInputBase.Accept( object obj )
    {
        Vector2? value = obj as Vector2?;

        if( value == null )
        {
            Debug.Assert( false, "Argument is not of type Vector2." );

            return false;
        }

        return AcceptGenericInput( value.Value );
    }
}