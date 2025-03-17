public class GetDirectionalInput : IGetInput<Constants.Direction>, IGetInputBase
{
    public delegate Constants.Direction GetDirectionalInputCallback();

    private GetDirectionalInputCallback _callback;

    public GetDirectionalInput(GetDirectionalInputCallback callback)
    {
        _callback = callback;
    }

    public Constants.Direction GetInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetInput();
    }
}