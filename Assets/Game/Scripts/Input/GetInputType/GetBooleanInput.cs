public class GetBooleanInput : IGetInputBase, IGetGenericInput<bool>
{
    public delegate bool GetBooleanInputCallback();

    private GetBooleanInputCallback _callback;

    public GetBooleanInput(GetBooleanInputCallback callback)
    {
        _callback = callback;
    }

    public bool GetGenericInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetGenericInput();
    }
}