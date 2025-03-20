public class GetDirectionalInput : IGetInputBase, IGetGenericInput<Constants.Direction>
{
    public delegate Constants.Direction GetDirectionalInputCallback();

    private GetDirectionalInputCallback _callback;

    public GetDirectionalInput(GetDirectionalInputCallback callback)
    {
        _callback = callback;
    }

    public Constants.Direction GetGenericInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetGenericInput();
    }
}