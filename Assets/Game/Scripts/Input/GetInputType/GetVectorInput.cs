using UnityEngine;

public class GetVectorInput : IGetInputBase, IGetGenericInput<Vector2>
{
    public delegate Vector2 GetVectorInputCallback();

    private GetVectorInputCallback _callback;

    public GetVectorInput( GetVectorInputCallback callback )
    {
        _callback = callback;
    }

    public Vector2 GetGenericInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetGenericInput();
    }
}