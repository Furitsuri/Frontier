using System.Diagnostics;

public class AcceptDirectionInput : IAcceptInputBase, IAcceptGenericInput<Direction>
{
    public delegate bool AcceptDirectionInputCallback(Direction input);

    private AcceptDirectionInputCallback _callback;

    public AcceptDirectionInput(AcceptDirectionInputCallback callback)
    {
        _callback = callback;
    }

    public bool AcceptGenericInput(Direction input)
    {
        return _callback(input);
    }

    bool IAcceptInputBase.Accept(object obj)
    {
        Direction? value = obj as Direction?;

        if ( value == null )
        {
            Debug.Assert( false,  "Argument is not of type Direction." );

            return false;
        }

        return AcceptGenericInput( value.Value );
    }
}