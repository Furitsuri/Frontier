public class GetDirectionalInput : IGetInputBase, IGetGenericInput<Direction>
{
    public delegate Direction GetDirectionalInputCallback();

    private GetDirectionalInputCallback _callback;

    public GetDirectionalInput(GetDirectionalInputCallback callback)
    {
        _callback = callback;
    }

    public Direction GetGenericInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetGenericInput();
    }
}