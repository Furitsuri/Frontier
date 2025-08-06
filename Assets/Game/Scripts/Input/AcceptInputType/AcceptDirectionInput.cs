using System.Diagnostics;

public class AcceptDirectionInput : IAcceptInputBase, IAcceptGenericInput<Constants.Direction>
{
    public delegate bool AcceptDirectionInputCallback(Constants.Direction input);

    private AcceptDirectionInputCallback _callback;

    public AcceptDirectionInput(AcceptDirectionInputCallback callback)
    {
        _callback = callback;
    }

    public bool AcceptGenericInput(Constants.Direction input)
    {
        return _callback(input);
    }

    bool IAcceptInputBase.Accept(object obj)
    {
        Constants.Direction? value = obj as Constants.Direction?;

        if ( value == null )
        {
            Debug.Assert( false,  "Argument is not of type Constants.Direction." );

            return false;
        }

        return AcceptGenericInput( value.Value );
    }
}