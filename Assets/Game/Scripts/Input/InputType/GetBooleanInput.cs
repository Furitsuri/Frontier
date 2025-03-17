public class GetBooleanInput : IGetInput<bool>, IGetInputBase
{
    public delegate bool GetBooleanInputCallback();

    private GetBooleanInputCallback _callback;

    public GetBooleanInput(GetBooleanInputCallback callback)
    {
        _callback = callback;
    }

    public bool GetInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetInput();
    }
}