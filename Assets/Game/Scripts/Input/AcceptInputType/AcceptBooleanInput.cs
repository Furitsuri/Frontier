using System.Diagnostics;
using Unity.Burst.Intrinsics;

public class AcceptBooleanInput : IAcceptInputBase, IAcceptGenericInput<bool>
{
    public delegate bool AcceptBooleanInputCallback( bool input );

    private AcceptBooleanInputCallback _callback;

    public AcceptBooleanInput(AcceptBooleanInputCallback callback)
    {
        _callback = callback;
    }

    public bool AcceptGenericInput( bool input )
    {
        return _callback( input );
    }

    bool IAcceptInputBase.Accept( object obj )
    {
        bool? value = obj as bool?;

        if ( value == null )
        {
            Debug.Assert(false, "Argument is not of type bool.");

            return false;
        }

        return AcceptGenericInput( value.Value );
    }
}