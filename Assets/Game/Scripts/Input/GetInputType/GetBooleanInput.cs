public class GetBooleanInput : IGetInputBase, IGetGenericInput<bool>
{
    public delegate bool GetBooleanInputCallback();

    private GetBooleanInputCallback _callback;
    private GameButton _button;

    public GetBooleanInput( GetBooleanInputCallback callback, GameButton button )
    {
        _callback   = callback;
        _button     = button;
    }

    public bool GetGenericInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetGenericInput();
    }

    public void Apply( InputContext context )
    {
        context.SetButton( _button, _callback() );
    }
}